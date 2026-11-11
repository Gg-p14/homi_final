using Homi.Data;
using Homi.Helpers;

namespace Homi.Services;

/// Tareas automáticas: expira reservas sin pagar cada minuto y limpia la bitácora una vez al día.
public class MantenimientoService : BackgroundService
{
    private readonly IServiceScopeFactory _ambitos;
    private readonly IConfiguration _configuracion;
    private DateTime _ultimaLimpiezaBitacora = DateTime.MinValue;

    public MantenimientoService(IServiceScopeFactory ambitos, IConfiguration configuracion)
    {
        _ambitos = ambitos;
        _configuracion = configuracion;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        while (!cancelacion.IsCancellationRequested)
        {
            try
            {
                using var ambito = _ambitos.CreateScope();
                var db = ambito.ServiceProvider.GetRequiredService<Db>();

                await db.EjecutarAsync("sp_ExpirarReservasPendientes");

                if (Reloj.Ahora - _ultimaLimpiezaBitacora > TimeSpan.FromHours(24))
                {
                    var dias = _configuracion.GetValue<int>("Bitacora:DiasRetencion", 365);
                    await db.EjecutarAsync("sp_LimpiarBitacora", Db.P("@DiasRetencion", dias));
                    _ultimaLimpiezaBitacora = Reloj.Ahora;
                }
            }
            catch (Exception ex)
            {
                Registro.Error("Mantenimiento automático", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancelacion);
            }
            catch (TaskCanceledException)
            {
                // La aplicación se está deteniendo.
            }
        }
    }
}
