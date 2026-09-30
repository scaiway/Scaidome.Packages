using System.Threading.Channels;

namespace Scaidome.Channels.Tests;

public class ActionQueueTests : IDisposable
{
    private readonly MetricsProbe _probe = new();
    private readonly RecordingMeterFactory _factory;

    public ActionQueueTests()
    {
        _factory = new RecordingMeterFactory(_probe.Track);
    }

    public void Dispose()
    {
        _probe.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public void The_queue_creates_its_own_meter_with_the_fixed_name_and_version()
    {
        using var first = CreateQueue("a");
        using var second = CreateQueue("a");

        _factory.Requests.Should().Equal(("Scaidome.Channels", "1.0"), ("Scaidome.Channels", "1.0"));
    }

    [Fact]
    public void A_factory_that_cannot_create_the_meter_fails_the_queue()
    {
        FluentActions.Invoking(() => new ActionQueue<int>("q", new UnboundedChannelOptions(), new FailingMeterFactory()))
            .Should().Throw<InvalidOperationException>().WithMessage("no meters today");
    }

    [Fact]
    public async Task Written_read_and_depth_are_published_with_the_channel_tag()
    {
        using var queue = CreateQueue("orders");
        _probe.Start();

        queue.Writer.TryWrite(1).Should().BeTrue();
        await queue.Writer.WriteAsync(2);
        await queue.Writer.WriteAsync(3);
        queue.Reader.TryRead(out _).Should().BeTrue();

        var metrics = _probe.Collect();
        metrics[("orders", "scai.channel.written")].Should().Be(3);
        metrics[("orders", "scai.channel.read")].Should().Be(1);
        metrics[("orders", "scai.channel.depth")].Should().Be(2);
        _probe.Tags.Should().OnlyContain(tags => tags.Length == 1 && tags[0].Key == "channel");
    }

    [Fact]
    public async Task Every_way_of_reading_counts_each_item_once()
    {
        using var queue = CreateQueue("reads");
        _probe.Start();
        for (var i = 0; i < 6; i++)
        {
            queue.Writer.TryWrite(i);
        }

        queue.Reader.TryRead(out _);
        (await queue.Reader.ReadAsync()).Should().Be(1);
        queue.Writer.Complete();
        var rest = new List<int>();
        await foreach (var item in queue.Reader.ReadAllAsync())
        {
            rest.Add(item);
        }

        rest.Should().Equal(2, 3, 4, 5);
        var metrics = _probe.Collect();
        metrics[("reads", "scai.channel.read")].Should().Be(6);
        metrics[("reads", "scai.channel.depth")].Should().Be(0);
    }

    [Fact]
    public async Task A_read_that_waits_counts_once_when_the_item_arrives()
    {
        using var queue = CreateQueue("waiting");
        _probe.Start();
        var pending = queue.Reader.ReadAsync().AsTask();
        pending.IsCompleted.Should().BeFalse();

        queue.Writer.TryWrite(42);

        (await pending).Should().Be(42);
        _probe.Collect()[("waiting", "scai.channel.read")].Should().Be(1);
    }

    [Fact]
    public async Task Refused_cancelled_and_empty_operations_count_nothing()
    {
        using var queue = CreateQueue("refused");
        _probe.Start();
        queue.Reader.TryRead(out _).Should().BeFalse();
        using (var cancelled = new CancellationTokenSource())
        {
            await cancelled.CancelAsync();
            await FluentActions.Awaiting(() => queue.Writer.WriteAsync(1, cancelled.Token).AsTask())
                .Should().ThrowAsync<OperationCanceledException>();
        }

        queue.Writer.Complete();
        queue.Writer.TryWrite(2).Should().BeFalse();
        await FluentActions.Awaiting(() => queue.Writer.WriteAsync(3).AsTask()).Should().ThrowAsync<ChannelClosedException>();

        var metrics = _probe.Collect();
        metrics[("refused", "scai.channel.written")].Should().Be(0);
        metrics[("refused", "scai.channel.read")].Should().Be(0);
    }

    [Fact]
    public async Task Completion_and_waiting_pass_through()
    {
        using var queue = CreateQueue("completion");
        queue.Writer.TryWrite(1);
        (await queue.Writer.WaitToWriteAsync()).Should().BeTrue();
        (await queue.Reader.WaitToReadAsync()).Should().BeTrue();

        var failure = new InvalidOperationException("stopped");
        queue.Writer.TryComplete(failure).Should().BeTrue();
        queue.Reader.TryRead(out _).Should().BeTrue();

        await FluentActions.Awaiting(() => queue.Reader.Completion).Should().ThrowAsync<InvalidOperationException>();
        (await queue.Writer.WaitToWriteAsync().AsTask().ContinueWith(t => t.IsFaulted || !t.Result)).Should().BeTrue();
    }

    [Fact]
    public void The_reader_cannot_peek()
    {
        using var queue = CreateQueue("peek");
        queue.Writer.TryWrite(1);

        queue.Reader.CanPeek.Should().BeFalse();
        queue.Reader.TryPeek(out _).Should().BeFalse();
        queue.Reader.TryRead(out var item).Should().BeTrue();
        item.Should().Be(1);
    }

    [Fact]
    public void The_item_count_is_the_channel_own()
    {
        using var counting = CreateQueue("count");
        counting.Writer.TryWrite(1);
        counting.Writer.TryWrite(2);
        counting.Reader.CanCount.Should().BeTrue();
        counting.Reader.Count.Should().Be(2);

        using var singleReader = new ActionQueue<int>("single", new UnboundedChannelOptions { SingleReader = true });
        singleReader.Reader.CanCount.Should().BeFalse();
    }

    [Fact]
    public async Task Counts_are_exact_under_concurrent_producers_and_readers()
    {
        using var queue = CreateQueue("concurrent");
        _probe.Start();
        const int producers = 8;
        const int perProducer = 5_000;

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            var taken = 0;
            await foreach (var unused in queue.Reader.ReadAllAsync())
            {
                taken++;
            }

            return taken;
        })).ToArray();
        await Task.WhenAll(Enumerable.Range(0, producers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perProducer; i++)
            {
                queue.Writer.TryWrite(i);
            }
        })));
        queue.Writer.Complete();
        var taken = (await Task.WhenAll(readers)).Sum();

        taken.Should().Be(producers * perProducer);
        var metrics = _probe.Collect();
        metrics[("concurrent", "scai.channel.written")].Should().Be(producers * perProducer);
        metrics[("concurrent", "scai.channel.read")].Should().Be(producers * perProducer);
        metrics[("concurrent", "scai.channel.depth")].Should().Be(0);
    }

    [Fact]
    public void Queues_with_the_same_name_publish_separately_under_the_same_tag()
    {
        using var first = CreateQueue("same");
        using var second = CreateQueue("same");
        _probe.Start();
        first.Writer.TryWrite(1);
        second.Writer.TryWrite(1);
        second.Writer.TryWrite(2);

        _probe.Collect();

        _probe.Tags.Should().OnlyContain(tags => (string?)tags[0].Value == "same");
    }

    [Fact]
    public void Items_left_in_a_completed_queue_stay_in_depth()
    {
        using var queue = CreateQueue("left");
        _probe.Start();
        queue.Writer.TryWrite(1);
        queue.Writer.TryWrite(2);
        queue.Writer.Complete();

        _probe.Collect()[("left", "scai.channel.depth")].Should().Be(2);
    }

    [Fact]
    public async Task Disposal_unpublishes_but_the_queue_keeps_working()
    {
        var queue = CreateQueue("disposed");
        _probe.Start();
        queue.Writer.TryWrite(1);

        queue.Dispose();
        queue.Dispose();

        _factory.Meters.Single().Disposals.Should().Be(2);
        _probe.Collect().Should().NotContainKey(("disposed", "scai.channel.written"));
        queue.Reader.Completion.IsCompleted.Should().BeFalse();
        queue.Writer.TryWrite(2).Should().BeTrue();
        (await queue.Reader.ReadAsync()).Should().Be(1);
        (await queue.Reader.ReadAsync()).Should().Be(2);
    }

    [Fact]
    public void Without_a_factory_the_queue_counts_and_publishes_nothing()
    {
        using var queue = new ActionQueue<string>("quiet", new UnboundedChannelOptions());
        _probe.Start();

        queue.Writer.TryWrite("a").Should().BeTrue();
        queue.Reader.TryRead(out var item).Should().BeTrue();
        item.Should().Be("a");

        _probe.Collect().Should().BeEmpty();
        queue.Dispose();
    }

    [Fact]
    public void Measurements_are_evaluated_only_when_a_collector_asks()
    {
        using var queue = CreateQueue("lazy");
        for (var i = 0; i < 100; i++)
        {
            queue.Writer.TryWrite(i);
            queue.Reader.TryRead(out _);
        }

        _factory.Meters.Single().Evaluations.Should().Be(0);
        _probe.Start();
        _probe.Collect();
        _factory.Meters.Single().Evaluations.Should().Be(3);
    }

    private ActionQueue<int> CreateQueue(string name) => new(name, new UnboundedChannelOptions(), _factory);
}
