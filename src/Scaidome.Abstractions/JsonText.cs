using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Scaidome;

/// <summary>Checks and compacts JSON text for the JSON kind.</summary>
internal static class JsonText
{
    /// <summary>True when the text holds exactly one well-formed JSON value, of any root. Blank text is not JSON.</summary>
    public static bool IsWellFormed(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // The check runs on every JSON value that cannot vouch for its own provenance, so the UTF-8 bytes go in a pooled buffer
        // and no document is built.
        var buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
        try
        {
            var written = Encoding.UTF8.GetBytes(text, buffer);

            // The reader refuses a second root value, comments and trailing commas by default.
            var reader = new Utf8JsonReader(buffer.AsSpan(0, written));
            while (reader.Read())
            {
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>The same JSON value without insignificant whitespace.</summary>
    public static string Compact(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Compact(document.RootElement);
    }

    public static string Compact(JsonElement element)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            element.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
