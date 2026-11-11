using System.Data;
using Homi.Data;
using Homi.Models;
using Homi.ViewModels;

namespace Homi.Services;

/// Publicar, editar, fotos, activar, eliminar y panel del anfitrión.
public class HostService
{
    public const int MaximoFotos = 10;

    private readonly Db _db;

    public HostService(Db db) => _db = db;

    public async Task<ResumenAnfitrion> ResumenAsync(int anfitrionId) =>
        (await _db.ListarAsync<ResumenAnfitrion>("sp_ObtenerResumenAnfitrion", Db.P("@AnfitrionId", anfitrionId))).FirstOrDefault()
        ?? new ResumenAnfitrion();

    public Task<List<HostAlojamiento>> AlojamientosAsync(int anfitrionId) =>
        _db.ListarAsync<HostAlojamiento>("sp_ListarAlojamientosPorAnfitrion", Db.P("@AnfitrionId", anfitrionId));

    public Task<List<HostReserva>> ReservasAsync(int anfitrionId) =>
        _db.ListarAsync<HostReserva>("sp_ListarReservasDeAnfitrion", Db.P("@AnfitrionId", anfitrionId));

    /// Pagos del anfitrión: monto cobrado, comisión de Homi y monto que le corresponde.
    public Task<List<PagoAnfitrionItem>> PagosAsync(int anfitrionId) =>
        _db.ListarAsync<PagoAnfitrionItem>("sp_ListarPagosDeAnfitrion", Db.P("@AnfitrionId", anfitrionId));

    /// Publica como borrador (Activo = 0). Después se suben fotos y se activa.
    public async Task<int> RegistrarAsync(int anfitrionId, PropertyFormViewModel f)
    {
        var id = Db.Salida("@AlojamientoId", SqlDbType.Int);
        await _db.EjecutarAsync("sp_RegistrarAlojamiento",
            Db.P("@AnfitrionId", anfitrionId),
            Db.P("@Pais", f.Pais),
            Db.P("@Estado", f.Estado),
            Db.P("@Ciudad", f.Ciudad),
            Db.P("@Titulo", f.Titulo),
            Db.P("@Descripcion", f.Descripcion),
            Db.P("@Calle", f.Calle),
            Db.P("@NumeroExterior", f.NumeroExterior),
            Db.P("@Colonia", f.Colonia),
            Db.P("@CodigoPostal", f.CodigoPostal),
            Db.P("@Referencias", f.Referencias),
            Db.P("@PrecioPorNoche", f.PrecioPorNoche),
            Db.P("@CobraLimpieza", f.CobraLimpieza),
            Db.P("@CapacidadMaxima", f.CapacidadMaxima),
            id);
        return (int)id.Value!;
    }

    public async Task ActualizarAsync(int alojamientoId, int usuarioId, PropertyFormViewModel f)
    {
        // Un solo procedimiento: datos del alojamiento y dirección en la misma transacción.
        await _db.EjecutarAsync("sp_ActualizarAlojamientoCompleto",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId),
            Db.P("@Titulo", f.Titulo),
            Db.P("@Descripcion", f.Descripcion),
            Db.P("@PrecioPorNoche", f.PrecioPorNoche),
            Db.P("@CobraLimpieza", f.CobraLimpieza),
            Db.P("@CapacidadMaxima", f.CapacidadMaxima),
            Db.P("@Calle", f.Calle),
            Db.P("@NumeroExterior", f.NumeroExterior),
            Db.P("@Colonia", f.Colonia),
            Db.P("@CodigoPostal", f.CodigoPostal),
            Db.P("@Referencias", f.Referencias));
    }

    /// Ingresos por mes del año (base + limpieza, sin comisión).
    public Task<List<IngresoMes>> IngresosPorMesAsync(int anfitrionId, int anio) =>
        _db.ListarAsync<IngresoMes>("sp_ObtenerIngresosPorMes",
            Db.P("@AnfitrionId", anfitrionId), Db.P("@Anio", anio));

    /// Todas las reseñas de los alojamientos del anfitrión (reputación), en una sola consulta.
    public Task<List<ResenaAnfitrion>> ResenasDeAnfitrionAsync(int anfitrionId) =>
        _db.ListarAsync<ResenaAnfitrion>("sp_ListarResenasDeAnfitrion", Db.P("@AnfitrionId", anfitrionId));

    /// Historial de huéspedes, agrupado por la cuenta del huésped (no por nombre).
    public async Task<List<HuespedHistorial>> HuespedesAsync(int anfitrionId)
    {
        var reservas = await ReservasAsync(anfitrionId);
        return reservas
            .GroupBy(r => r.HuespedId)
            .Select(g => new HuespedHistorial
            {
                Nombre = g.First().Huesped,
                Total = g.Count(),
                Confirmadas = g.Count(x => x.Estado == "Confirmada"),
                Canceladas = g.Count(x => x.Estado == "Cancelada"),
                UltimaEstancia = g.Max(x => x.FechaSalida),
                Reservas = g.OrderByDescending(x => x.FechaEntrada).ToList()
            })
            .OrderByDescending(h => h.UltimaEstancia)
            .ToList();
    }

    public Task CancelarReservaAsync(int reservaId, int anfitrionId, string? motivo) =>
        _db.EjecutarAsync("sp_CancelarReservaAnfitrion",
            Db.P("@ReservaId", reservaId), Db.P("@AnfitrionId", anfitrionId), Db.P("@Motivo", motivo));

    /// Libera el pago retenido de una reserva después del check-in.
    public Task LiberarPagoAsync(int reservaId, int anfitrionId) =>
        _db.EjecutarAsync("sp_LiberarPago", Db.P("@ReservaId", reservaId), Db.P("@AnfitrionId", anfitrionId));

    /// Desglose de una reserva visto por el anfitrión (incluye la comisión).
    public Task<List<CargoItem>> DesgloseAsync(int reservaId, int anfitrionId) =>
        _db.ListarAsync<CargoItem>("sp_ObtenerDesgloseAnfitrion",
            Db.P("@ReservaId", reservaId), Db.P("@AnfitrionId", anfitrionId));

    /// Elimina el alojamiento (borrado lógico). Cancela con reembolso total las reservas activas.
    public Task EliminarAsync(int alojamientoId, int usuarioId) =>
        _db.EjecutarAsync("sp_EliminarAlojamiento",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId));

    public Task CambiarEstadoAsync(int alojamientoId, int usuarioId, bool activo) =>
        _db.EjecutarAsync("sp_CambiarEstadoAlojamiento",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId),
            Db.P("@Activo", activo));

    public Task<List<FotoItem>> FotosAsync(int alojamientoId) =>
        _db.ListarAsync<FotoItem>("sp_ListarFotos", Db.P("@AlojamientoId", alojamientoId));

    public async Task<int> ContarFotosAsync(int alojamientoId)
    {
        var total = Db.Salida("@Total", SqlDbType.Int);
        await _db.EjecutarAsync("sp_ContarFotos", Db.P("@AlojamientoId", alojamientoId), total);
        return (int)total.Value!;
    }

    public async Task AgregarFotoAsync(int alojamientoId, int usuarioId, string ruta)
    {
        var id = Db.Salida("@Id", SqlDbType.Int);
        await _db.EjecutarAsync("sp_AgregarFoto",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId),
            Db.P("@RutaArchivo", ruta),
            id);
    }

    /// Devuelve la ruta del archivo para que el controlador lo borre del disco.
    public async Task<string?> EliminarFotoAsync(int fotoId, int usuarioId)
    {
        var ruta = Db.Salida("@RutaArchivo", SqlDbType.NVarChar, 255);
        await _db.EjecutarAsync("sp_EliminarFoto", Db.P("@FotoId", fotoId), Db.P("@UsuarioId", usuarioId), ruta);
        return ruta.Value as string;
    }

    public Task ReordenarAsync(int alojamientoId, int usuarioId, IEnumerable<int> idsEnOrden)
    {
        var json = "[" + string.Join(",", idsEnOrden) + "]";
        return _db.EjecutarAsync("sp_ReordenarFotos",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId),
            Db.P("@OrdenJson", json));
    }
}
