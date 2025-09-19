namespace Gestus.Services;

public interface ITimezoneService
{
    DateTime GetCurrentUtc();
    DateTime CorrectDate(DateTime dateTime);
    string GetSystemTimezone();
}

public class TimezoneService : ITimezoneService
{
    private readonly ILogger<TimezoneService> _logger;

    public TimezoneService(ILogger<TimezoneService> logger)
    {
        _logger = logger;
    }

    public DateTime GetCurrentUtc()
    {
        // ✅ USAR SISTEMA REAL
        var systemUtc = DateTime.UtcNow;
        var systemLocal = DateTime.Now;
        var timezone = TimeZoneInfo.Local;
        
        _logger.LogInformation("🕐 Sistema atual - UTC: {SystemUtc}, Local: {SystemLocal}, Timezone: {Timezone}", 
            systemUtc, systemLocal, timezone.Id);

        return systemUtc; // ✅ Usar horário real do sistema
    }

    public DateTime CorrectDate(DateTime dateTime)
    {
        // ✅ NÃO CORRIGIR NADA - usar data real
        return dateTime;
    }

    public string GetSystemTimezone()
    {
        var timezone = TimeZoneInfo.Local;
        var offset = timezone.GetUtcOffset(DateTime.Now);
        
        _logger.LogInformation("🌍 Timezone: {Id} ({DisplayName}), Offset: {Offset}", 
            timezone.Id, timezone.DisplayName, offset);
        
        return timezone.Id;
    }
}