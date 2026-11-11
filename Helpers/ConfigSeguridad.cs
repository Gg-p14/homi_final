namespace Homi.Helpers;

/// Parámetros del límite de intentos de inicio de sesión (sección "Seguridad" de appsettings.json).
public class ConfigSeguridad
{
    /// Fallos seguidos de una cuenta antes de bloquearla.
    public int MaximoIntentosCuenta { get; set; } = 5;

    /// Minutos que dura el bloqueo de la cuenta.
    public int MinutosBloqueoCuenta { get; set; } = 15;

    /// Minutos sin fallos después de los cuales el conteo de la cuenta vuelve a cero.
    public int MinutosVentanaCuenta { get; set; } = 15;

    /// Intentos fallidos permitidos desde una misma conexión dentro de la ventana.
    public int MaximoIntentosIp { get; set; } = 30;

    /// Duración de la ventana de la conexión, en minutos. Es fija: empieza con el primer fallo.
    public int MinutosVentanaIp { get; set; } = 15;

    /// Código fijo para pruebas locales (por ejemplo 123456). Déjalo vacío al publicar: entonces el código es aleatorio.
    public string? CodigoVerificacionFijo { get; set; }

    /// Usar X-Forwarded-For como IP real. Activar SOLO si la app está detrás de un proxy que lo envía.
    public bool ConfiarEnEncabezadoProxy { get; set; } = false;
}
