namespace Homi.Helpers;

/// Registro de errores y eventos en archivos diarios (carpeta logs junto a la aplicación).
public static class Registro
{
    private static readonly object Candado = new();

    public static void Info(string mensaje) => Escribir("INFO", mensaje, null);

    public static void Error(string mensaje, Exception? ex = null) => Escribir("ERROR", mensaje, ex);

    private static void Escribir(string nivel, string mensaje, Exception? ex)
    {
        try
        {
            lock (Candado)
            {
                var carpeta = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(carpeta);
                var ahora = Reloj.Ahora;
                var texto = $"{ahora:yyyy-MM-dd HH:mm:ss} [{nivel}] {mensaje}" + (ex is null ? "" : Environment.NewLine + ex);
                File.AppendAllText(Path.Combine(carpeta, $"homi-{ahora:yyyy-MM-dd}.log"), texto + Environment.NewLine);
            }
        }
        catch
        {
            // Si no se puede escribir el registro, la aplicación sigue funcionando.
        }
    }
}
