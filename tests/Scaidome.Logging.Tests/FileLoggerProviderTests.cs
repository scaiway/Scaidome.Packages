using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Scaidome.Logging.File;
using Scaidome.Logging.Tests.Infrastructure;

namespace Scaidome.Logging.Tests;

public class FileLoggerProviderTests : IDisposable
{
    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void An_entry_carries_time_with_offset_level_category_event_id_and_message()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path))
        {
            provider.CreateLogger("Scai.Config").LogInformation(new EventId(12), "Started {Port}", 5000);
            provider.CreateLogger("Scai.Other").LogWarning("No event id");
        }

        var lines = System.IO.File.ReadAllLines(path);
        lines.Should().HaveCount(2);
        lines[0].Should().MatchRegex(
            @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2} \[Information\] Scai\.Config\[12\]: Started 5000$");
        lines[1].Should().MatchRegex(@"\[Warning\] Scai\.Other: No event id$");
    }

    [Fact]
    public void Every_text_uses_the_invariant_culture()
    {
        var path = _folder.Combine("app.log");
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            using var provider = Create(path);
            provider.CreateLogger("C").LogInformation("Value {Value}", 1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        var line = System.IO.File.ReadAllText(path);
        line.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2} ");
        line.Should().Contain("Value 1.5");
    }

    [Fact]
    public void An_exception_follows_on_the_next_lines_with_its_inner_exception()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path))
        {
            Exception failure;
            try
            {
                throw new InvalidOperationException("outer", new ArgumentException("inner cause"));
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            provider.CreateLogger("C").LogError(failure, "Failed");
        }

        var text = System.IO.File.ReadAllText(path);
        text.Should().Contain("C: Failed" + Environment.NewLine + "System.InvalidOperationException: outer");
        text.Should().Contain("inner cause");
        text.Should().Contain("at Scaidome.Logging.Tests.FileLoggerProviderTests");
    }

    [Fact]
    public void Entries_below_the_level_are_not_written()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path))
        {
            var logger = provider.CreateLogger("C");
            logger.LogDebug("hidden");
            logger.LogTrace("hidden");
            logger.LogInformation("shown");
            logger.Log(LogLevel.None, "never");
            logger.IsEnabled(LogLevel.None).Should().BeFalse();
        }

        System.IO.File.ReadAllText(path).Should().Contain("shown").And.NotContain("hidden").And.NotContain("never");
    }

    [Fact]
    public void The_longest_matching_category_decides_without_regard_to_case()
    {
        using var provider = Create(
            _folder.Combine("app.log"),
            levels: new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
            {
                ["Default"] = LogLevel.Warning,
                ["Microsoft"] = LogLevel.Error,
                ["microsoft.aspnetcore.hosting"] = LogLevel.Debug,
            });

        provider.GetLevel("Microsoft.AspNetCore.Hosting.Diagnostics").Should().Be(LogLevel.Debug);
        provider.GetLevel("Microsoft.AspNetCore.Routing").Should().Be(LogLevel.Error);
        provider.GetLevel("Scai.Config").Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Settings_without_a_default_take_information()
    {
        using var provider = Create(
            _folder.Combine("app.log"),
            levels: new Dictionary<string, LogLevel> { ["Noisy"] = LogLevel.Error });

        provider.GetLevel("Scai").Should().Be(LogLevel.Information);
        provider.GetLevel("Noisy.Thing").Should().Be(LogLevel.Error);
    }

    [Fact]
    public void A_logger_decides_its_level_when_created()
    {
        var path = _folder.Combine("app.log");
        var levels = new Dictionary<string, LogLevel> { ["Default"] = LogLevel.Information };
        using (var provider = Create(path, levels: levels))
        {
            var logger = provider.CreateLogger("C");
            levels["Default"] = LogLevel.Error;
            logger.LogInformation("still written");
            provider.CreateLogger("D").LogInformation("not written");
        }

        System.IO.File.ReadAllText(path).Should().Contain("still written").And.NotContain("not written");
    }

    [Fact]
    public void Every_logger_writes_to_the_same_file_in_order()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path))
        {
            for (var i = 0; i < 200; i++)
            {
                provider.CreateLogger($"Category{i % 3}").LogInformation("Entry {Index}", i);
            }
        }

        var indexes = System.IO.File.ReadAllLines(path)
            .Select(line => int.Parse(Regex.Match(line, @"Entry (\d+)$").Groups[1].Value, CultureInfo.InvariantCulture));
        indexes.Should().Equal(Enumerable.Range(0, 200));
    }

    [Fact]
    public void A_missing_folder_is_created()
    {
        var path = _folder.Combine("deep", "er", "app.log");
        using (var provider = Create(path))
        {
            provider.CreateLogger("C").LogInformation("hello");
        }

        System.IO.File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task The_file_is_not_held_between_entries()
    {
        var path = _folder.Combine("app.log");
        using var provider = Create(path);
        provider.CreateLogger("C").LogInformation("first");
        await WaitForAsync(() => System.IO.File.Exists(path) && System.IO.File.ReadAllText(path).Contains("first"));

        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
        }

        System.IO.File.Delete(path);
        provider.CreateLogger("C").LogInformation("second");
        await WaitForAsync(() => System.IO.File.Exists(path));
        await WaitForAsync(() => System.IO.File.ReadAllText(path).Contains("second"));
    }

    [Fact]
    public void A_failure_to_write_is_dropped()
    {
        var path = _folder.Combine("app.log");
        Directory.CreateDirectory(path);
        using var provider = Create(path);

        FluentActions.Invoking(() => provider.CreateLogger("C").LogInformation("lost")).Should().NotThrow();
        FluentActions.Invoking(provider.Dispose).Should().NotThrow();
    }

    [Fact]
    public void A_full_file_rolls_before_the_next_entry()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path, sizeLimit: 1, filesKept: 3))
        {
            var logger = provider.CreateLogger("C");
            logger.LogInformation("one");
            logger.LogInformation("two");
            logger.LogInformation("three");
            logger.LogInformation("four");
        }

        System.IO.File.ReadAllText(path).Should().Contain("four").And.NotContain("three");
        System.IO.File.ReadAllText(path + ".1").Should().Contain("three");
        System.IO.File.ReadAllText(path + ".2").Should().Contain("two");
        System.IO.File.Exists(path + ".3").Should().BeFalse();
        Directory.GetFiles(_folder.Path).Should().HaveCount(3);
    }

    [Fact]
    public void A_file_below_the_limit_does_not_roll()
    {
        var path = _folder.Combine("app.log");
        using (var provider = Create(path, sizeLimit: 1_000_000, filesKept: 3))
        {
            provider.CreateLogger("C").LogInformation("one");
            provider.CreateLogger("C").LogInformation("two");
        }

        System.IO.File.Exists(path + ".1").Should().BeFalse();
        System.IO.File.ReadAllLines(path).Should().HaveCount(2);
    }

    [Fact]
    public void Disposal_writes_what_is_queued_and_is_idempotent()
    {
        var path = _folder.Combine("app.log");
        var provider = Create(path);
        var logger = provider.CreateLogger("C");
        for (var i = 0; i < 500; i++)
        {
            logger.LogInformation("Entry {Index}", i);
        }

        provider.Dispose();
        provider.Dispose();

        System.IO.File.ReadAllLines(path).Should().HaveCount(500);
    }

    [Fact]
    public void An_entry_logged_after_disposal_is_dropped()
    {
        var path = _folder.Combine("app.log");
        var provider = Create(path);
        var logger = provider.CreateLogger("C");
        logger.LogInformation("before");
        provider.Dispose();

        FluentActions.Invoking(() => logger.LogInformation("after")).Should().NotThrow();
        System.IO.File.ReadAllText(path).Should().Contain("before").And.NotContain("after");
    }

    [Fact]
    public void The_defaults_are_as_specified()
    {
        var options = new FileLoggerOptions();

        options.Path.Should().Be("/logs/app.log");
        options.FileSizeLimitBytes.Should().Be(10485760);
        options.MaxRollingFiles.Should().Be(3);
        options.LogLevel.Should().Equal(new Dictionary<string, LogLevel> { ["Default"] = LogLevel.Information });
    }

    /// <summary>
    /// Known defect: beginning a scope returns nothing rather than a disposable scope, so "using (logger.BeginScope(...))"
    /// works only because a using statement tolerates null. The fix is to return a no-op scope; this test then fails and is
    /// changed to assert a non-null result.
    /// </summary>
    [Fact]
    public void Known_defect_beginning_a_scope_returns_nothing()
    {
        using var provider = Create(_folder.Combine("app.log"));

        provider.CreateLogger("C").BeginScope("scope").Should().BeNull();
    }

    private static FileLoggerProvider Create(
        string path,
        long sizeLimit = FileLoggerOptions.DefaultFileSizeLimitBytes,
        int filesKept = FileLoggerOptions.DefaultMaxRollingFiles,
        Dictionary<string, LogLevel>? levels = null)
    {
        var options = new FileLoggerOptions { Path = path, FileSizeLimitBytes = sizeLimit, MaxRollingFiles = filesKept };
        if (levels is not null)
        {
            options.LogLevel = levels;
        }

        return new FileLoggerProvider(options);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            try
            {
                if (condition())
                {
                    return;
                }
            }
            catch (IOException)
            {
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition was not met in time.");
            }

            await Task.Delay(10);
        }
    }
}
