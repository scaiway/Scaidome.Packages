using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scaidome;

/// <summary>
/// The JSON form of a value. It drops kinds: read back, a number that fits a 32-bit integer becomes an integer, any other number
/// a number, a string text, and an object or array JSON. Every refusal is reported as malformed JSON.
/// </summary>
public sealed class VariantJsonConverter : JsonConverter<Variant>
{
    public override bool HandleNull => true;

    public override Variant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return Variant.Null;
                case JsonTokenType.True:
                    return Variant.FromBool(true);
                case JsonTokenType.False:
                    return Variant.FromBool(false);
                case JsonTokenType.Number:
                    // TryGetInt32 refuses a fraction or an exponent in the text, so 7.0 stays a number.
                    if (reader.TryGetInt32(out var i))
                    {
                        return Variant.FromInt(i);
                    }

                    if (!reader.TryGetDouble(out var d) || !double.IsFinite(d))
                    {
                        throw new JsonException("A number too large to be finite is not a value.");
                    }

                    return Variant.FromDouble(d);
                case JsonTokenType.String:
                    return Variant.FromString(reader.GetString() ?? string.Empty);
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    using (var document = JsonDocument.ParseValue(ref reader))
                    {
                        // The text comes from the parser within this operation, so it is not checked again.
                        return Variant.FromTrustedJson(JsonText.Compact(document.RootElement));
                    }

                default:
                    throw new JsonException($"A JSON {reader.TokenType} token is not a value.");
            }
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            // A value that breaks its kind's rules is malformed input, never a fault of the reader.
            throw new JsonException("The JSON is not a valid value.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, Variant value, JsonSerializerOptions options)
    {
        switch (value.Kind)
        {
            case VariantKind.Null:
                writer.WriteNullValue();
                break;
            case VariantKind.Bool:
                writer.WriteBooleanValue(value.AsBool());
                break;
            case VariantKind.Int:
                writer.WriteNumberValue(value.AsInt());
                break;
            case VariantKind.Double:
            case VariantKind.Epoch:
                writer.WriteNumberValue(value.AsDouble());
                break;
            case VariantKind.DateTime:
                writer.WriteStringValue(value.AsDateTime().ToString("O", CultureInfo.InvariantCulture));
                break;
            case VariantKind.Json:
                writer.WriteRawValue(JsonText.Compact(value.AsText()), skipInputValidation: true);
                break;
            default:
                writer.WriteStringValue(value.AsText());
                break;
        }
    }
}
