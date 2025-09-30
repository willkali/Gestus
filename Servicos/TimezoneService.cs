using System.Globalization;

namespace Gestus.Services;

public interface ITimezoneService
{
    DateTime GetCurrentUtc();
    DateTime GetCurrentLocal();
    DateTime ToLocal(DateTime utcDateTime);
    DateTime ToUtc(DateTime localDateTime);
    string GetSystemTimezone();
    string GetTimezoneDisplay();
    TimeSpan GetUtcOffset();
    DateTimeOffset GetCurrentDateTimeOffset();
    string FormatDateTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss");
    string FormatDateTimeWithTimezone(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss zzz");
    string ToIso8601String(DateTime dateTime);
    DateTime FromIso8601String(string iso8601String);
    object GetTimezoneDebugInfo();
}

public class TimezoneService : ITimezoneService
{
    private readonly ILogger<TimezoneService> _logger;
    private readonly TimeZoneInfo _systemTimeZone;

    public TimezoneService(ILogger<TimezoneService> logger)
    {
        _logger = logger;
        _systemTimeZone = TimeZoneInfo.Local;
        
        LogTimezoneInfo();
    }

    private void LogTimezoneInfo()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var offset = _systemTimeZone.GetUtcOffset(now);
        
        _logger.LogInformation("🌍 Timezone Service inicializado:");
        _logger.LogInformation("   - Timezone: {Id} ({DisplayName})", _systemTimeZone.Id, _systemTimeZone.DisplayName);
        _logger.LogInformation("   - Offset atual: {Offset}", offset);
        _logger.LogInformation("   - Horário local: {LocalTime}", now.ToString("dd/MM/yyyy HH:mm:ss"));
        _logger.LogInformation("   - Horário UTC: {UtcTime}", utcNow.ToString("dd/MM/yyyy HH:mm:ss"));
        _logger.LogInformation("   - Suporte a horário de verão: {SupportsDaylightSavingTime}", _systemTimeZone.SupportsDaylightSavingTime);
        
        if (_systemTimeZone.SupportsDaylightSavingTime)
        {
            _logger.LogInformation("   - Em horário de verão: {IsDaylightSavingTime}", _systemTimeZone.IsDaylightSavingTime(now));
        }
    }

    public DateTime GetCurrentUtc()
    {
        var utcNow = DateTime.UtcNow;
        
        _logger.LogDebug("🕐 Horário UTC atual: {UtcTime}", utcNow.ToString("dd/MM/yyyy HH:mm:ss"));
        
        return utcNow;
    }

    public DateTime GetCurrentLocal()
    {
        var localNow = DateTime.Now;
        
        _logger.LogDebug("🕐 Horário local atual: {LocalTime}", localNow.ToString("dd/MM/yyyy HH:mm:ss"));
        
        return localNow;
    }

    public DateTime ToLocal(DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            _logger.LogWarning("⚠️ DateTime sem especificação de tipo (Kind), assumindo UTC");
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        else if (utcDateTime.Kind == DateTimeKind.Local)
        {
            _logger.LogWarning("⚠️ DateTime já está em horário local, retornando sem conversão");
            return utcDateTime;
        }

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _systemTimeZone);
        
        _logger.LogDebug("🔄 Convertido UTC {UtcTime} para Local {LocalTime}", 
            utcDateTime.ToString("dd/MM/yyyy HH:mm:ss"), 
            localTime.ToString("dd/MM/yyyy HH:mm:ss"));
        
        return localTime;
    }

    public DateTime ToUtc(DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            _logger.LogWarning("⚠️ DateTime já está em UTC, retornando sem conversão");
            return localDateTime;
        }

        if (localDateTime.Kind == DateTimeKind.Unspecified)
        {
            _logger.LogWarning("⚠️ DateTime sem especificação de tipo (Kind), assumindo Local");
            localDateTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Local);
        }

        var utcTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, _systemTimeZone);
        
        _logger.LogDebug("🔄 Convertido Local {LocalTime} para UTC {UtcTime}", 
            localDateTime.ToString("dd/MM/yyyy HH:mm:ss"), 
            utcTime.ToString("dd/MM/yyyy HH:mm:ss"));
        
        return utcTime;
    }

    public string GetSystemTimezone()
    {
        return _systemTimeZone.Id;
    }

    public string GetTimezoneDisplay()
    {
        var offset = GetUtcOffset();
        var offsetString = offset.TotalHours >= 0 ? $"+{offset:hh\\:mm}" : $"{offset:hh\\:mm}";
        
        return $"{_systemTimeZone.DisplayName} (UTC{offsetString})";
    }

    public TimeSpan GetUtcOffset()
    {
        return _systemTimeZone.GetUtcOffset(DateTime.Now);
    }

    public DateTimeOffset GetCurrentDateTimeOffset()
    {
        var now = DateTime.Now;
        var offset = _systemTimeZone.GetUtcOffset(now);
        var dateTimeOffset = new DateTimeOffset(now, offset);
        
        _logger.LogDebug("🕐 DateTimeOffset atual: {DateTimeOffset}", dateTimeOffset.ToString("dd/MM/yyyy HH:mm:ss zzz"));
        
        return dateTimeOffset;
    }

    public string FormatDateTime(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss")
    {
        try
        {
            return dateTime.ToString(format, CultureInfo.GetCultureInfo("pt-BR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao formatar data/hora: {DateTime} com formato: {Format}", dateTime, format);
            return dateTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }

    public string FormatDateTimeWithTimezone(DateTime dateTime, string format = "dd/MM/yyyy HH:mm:ss zzz")
    {
        try
        {
            // Converter para DateTimeOffset para incluir timezone
            DateTimeOffset dateTimeOffset;
            
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                // Se é UTC, converter para local timezone
                var localTime = ToLocal(dateTime);
                var offset = GetUtcOffset();
                dateTimeOffset = new DateTimeOffset(localTime, offset);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                // Se é local, usar offset atual
                var offset = GetUtcOffset();
                dateTimeOffset = new DateTimeOffset(dateTime, offset);
            }
            else
            {
                // Se é Unspecified, assumir local
                var offset = GetUtcOffset();
                dateTimeOffset = new DateTimeOffset(dateTime, offset);
            }
            
            return dateTimeOffset.ToString(format, CultureInfo.GetCultureInfo("pt-BR"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao formatar data/hora com timezone: {DateTime} com formato: {Format}", dateTime, format);
            return FormatDateTime(dateTime, "dd/MM/yyyy HH:mm:ss");
        }
    }

    /// <summary>
    /// Converte uma data/hora para o formato ISO 8601 com timezone
    /// </summary>
    public string ToIso8601String(DateTime dateTime)
    {
        DateTimeOffset dateTimeOffset;
        
        if (dateTime.Kind == DateTimeKind.Utc)
        {
            dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
        }
        else if (dateTime.Kind == DateTimeKind.Local)
        {
            var offset = GetUtcOffset();
            dateTimeOffset = new DateTimeOffset(dateTime, offset);
        }
        else
        {
            // Assumir local se não especificado
            var offset = GetUtcOffset();
            dateTimeOffset = new DateTimeOffset(dateTime, offset);
        }
        
        return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    /// <summary>
    /// Converte string ISO 8601 para DateTime local
    /// </summary>
    public DateTime FromIso8601String(string iso8601String)
    {
        try
        {
            if (DateTimeOffset.TryParse(iso8601String, out var dateTimeOffset))
            {
                // Converter para horário local do sistema
                return ToLocal(dateTimeOffset.UtcDateTime);
            }
            
            // Se não conseguir fazer parse como DateTimeOffset, tentar como DateTime
            if (DateTime.TryParse(iso8601String, out var dateTime))
            {
                return dateTime.Kind == DateTimeKind.Utc ? ToLocal(dateTime) : dateTime;
            }
            
            throw new ArgumentException($"Formato de data inválido: {iso8601String}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao converter string ISO8601 para DateTime: {Iso8601String}", iso8601String);
            throw;
        }
    }

    /// <summary>
    /// Obter informações detalhadas sobre o timezone para debug
    /// </summary>
    public object GetTimezoneDebugInfo()
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var offset = GetUtcOffset();
        
        return new
        {
            SystemTimezoneId = _systemTimeZone.Id,
            SystemTimezoneDisplay = _systemTimeZone.DisplayName,
            CurrentLocalTime = FormatDateTime(now),
            CurrentUtcTime = FormatDateTime(utcNow),
            UtcOffset = offset,
            UtcOffsetString = offset.TotalHours >= 0 ? $"+{offset:hh\\:mm}" : $"{offset:hh\\:mm}",
            SupportsDaylightSaving = _systemTimeZone.SupportsDaylightSavingTime,
            IsDaylightSavingTime = _systemTimeZone.IsDaylightSavingTime(now),
            CurrentDateTimeOffset = GetCurrentDateTimeOffset().ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            Culture = CultureInfo.CurrentCulture.Name
        };
    }
}