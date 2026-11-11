using System.Collections.Concurrent;
using Homi.Helpers;

namespace Homi.Services;

/// Límite de intentos por conexión. Ventana fija: se cuenta desde el primer fallo y, al vencer, se reinicia.
/// Vive en memoria: se reinicia si la aplicación se reinicia (los bloqueos de cuenta sí quedan en la base).
public class LimitadorIntentos
{
    private readonly ConfigSeguridad _config;
    private readonly Func<DateTime> _reloj;
    private readonly ConcurrentDictionary<string, Ventana> _ventanas = new();

    private sealed record Ventana(DateTime Inicio, int Fallos);

    public LimitadorIntentos(ConfigSeguridad config, Func<DateTime>? reloj = null)
    {
        _config = config;
        _reloj = reloj ?? (() => Reloj.Ahora);
    }

    /// Indica si la conexión llegó al límite. Si sí, devuelve cuántos minutos faltan para volver a intentar.
    public bool EstaBloqueado(string ip, out int minutosEspera)
    {
        minutosEspera = 0;
        if (!_ventanas.TryGetValue(ip, out var ventana)) return false;

        var ahora = _reloj();
        var fin = ventana.Inicio.AddMinutes(_config.MinutosVentanaIp);
        if (ahora >= fin)
        {
            _ventanas.TryRemove(ip, out _);
            return false;
        }

        if (ventana.Fallos < _config.MaximoIntentosIp) return false;

        minutosEspera = Math.Max(1, (int)Math.Ceiling((fin - ahora).TotalMinutes));
        return true;
    }

    /// Suma un fallo. Un fallo nunca alarga la ventana: solo la de la primera vez.
    public void RegistrarFallo(string ip)
    {
        var ahora = _reloj();
        _ventanas.AddOrUpdate(
            ip,
            _ => new Ventana(ahora, 1),
            (_, actual) => ahora >= actual.Inicio.AddMinutes(_config.MinutosVentanaIp)
                ? new Ventana(ahora, 1)
                : actual with { Fallos = actual.Fallos + 1 });

        if (_ventanas.Count > 10_000) Limpiar(ahora);
    }

    private void Limpiar(DateTime ahora)
    {
        foreach (var (ip, ventana) in _ventanas)
            if (ahora >= ventana.Inicio.AddMinutes(_config.MinutosVentanaIp))
                _ventanas.TryRemove(ip, out _);
    }
}
