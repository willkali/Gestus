using System.Text.Json;
using System.Text.Json.Serialization;
using Gestus.Services;

namespace Gestus.Converters;

public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime;
        }
        throw new JsonException($"Unable to convert \"{value}\" to DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // ✅ SEMPRE ESCREVER EM HORÁRIO LOCAL (sem 'Z')
        if (value.Kind == DateTimeKind.Utc)
        {
            // Converter UTC para local antes de serializar
            var local = TimeZoneInfo.ConvertTimeFromUtc(value, TimeZoneInfo.Local);
            writer.WriteStringValue(local.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
        }
        else
        {
            // Já é local, serializar diretamente
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
        }
    }
}

public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return null;
            
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime;
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // ✅ SEMPRE ESCREVER EM HORÁRIO LOCAL
            if (value.Value.Kind == DateTimeKind.Utc)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(value.Value, TimeZoneInfo.Local);
                writer.WriteStringValue(local.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
            }
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}