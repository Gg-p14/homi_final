namespace Homi.Helpers;

/// Reloj único de la aplicación: hora de Ciudad de México, igual que la base de datos (fn_Ahora).
public static class Reloj
{
    private static readonly TimeZoneInfo Zona = BuscarZona();

    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    private static TimeZoneInfo BuscarZona()
    {
        foreach (var id in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
