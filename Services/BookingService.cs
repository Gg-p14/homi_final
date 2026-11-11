using System.Data;
using Homi.Data;
using Homi.Models;

namespace Homi.Services;

/// Reservas, pagos, cancelaciones, comprobantes y reseñas.
public class BookingService
{
    private readonly Db _db;

    public BookingService(Db db) => _db = db;

    public async Task<int> CrearReservaAsync(int alojamientoId, int huespedId, DateOnly entrada, DateOnly salida, int huespedes)
    {
        var id = Db.Salida("@ReservaId", SqlDbType.Int);
        await _db.EjecutarAsync("sp_CrearReserva",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@HuespedId", huespedId),
            Db.PFecha("@FechaEntrada", entrada),
            Db.PFecha("@FechaSalida", salida),
            Db.P("@NumeroHuespedes", huespedes),
            id);
        return (int)id.Value!;
    }

    /// Marca como expiradas las reservas pendientes que pasaron los 30 minutos y libera sus noches.
    public Task ExpirarPendientesAsync() =>
        _db.EjecutarAsync("sp_ExpirarReservasPendientes");

    public Task PagarAsync(int reservaId, int usuarioId) =>
        _db.EjecutarAsync("sp_ProcesarPago", Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId));

    public async Task<ReservaDetalle?> DetalleAsync(int reservaId, int usuarioId) =>
        (await _db.ListarAsync<ReservaDetalle>("sp_ObtenerDetalleReserva",
            Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId))).FirstOrDefault();

    public Task<List<CargoItem>> CargosAsync(int reservaId, int usuarioId) =>
        _db.ListarAsync<CargoItem>("sp_ObtenerCargosReserva", Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId));

    public Task<List<ReservaLista>> ListarAsync(int huespedId, string filtro) =>
        _db.ListarAsync<ReservaLista>("sp_ListarReservasDeHuesped", Db.P("@HuespedId", huespedId), Db.P("@Filtro", filtro));

    public Task CancelarAsync(int reservaId, int usuarioId) =>
        _db.EjecutarAsync("sp_CancelarReserva", Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId));

    public Task ResenarAsync(int reservaId, int huespedId, int calificacion, string? comentario) =>
        _db.EjecutarAsync("sp_CrearResena",
            Db.P("@ReservaId", reservaId),
            Db.P("@HuespedId", huespedId),
            Db.P("@Calificacion", calificacion),
            Db.P("@Comentario", comentario));

    public async Task<ComprobanteItem?> ComprobanteAsync(int reservaId, int usuarioId) =>
        (await _db.ListarAsync<ComprobanteItem>("sp_ObtenerComprobante",
            Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId))).FirstOrDefault();

    public Task<List<ComprobanteResumen>> ComprobantesDeHuespedAsync(int huespedId) =>
        _db.ListarAsync<ComprobanteResumen>("sp_ListarComprobantesDeHuesped", Db.P("@HuespedId", huespedId));

    public async Task<PagoReservaItem?> PagoDeReservaAsync(int reservaId, int usuarioId) =>
        (await _db.ListarAsync<PagoReservaItem>("sp_ObtenerPagoReserva",
            Db.P("@ReservaId", reservaId), Db.P("@UsuarioId", usuarioId))).FirstOrDefault();

    public async Task<ResenaReserva?> ResenaDeReservaAsync(int reservaId) =>
        (await _db.ListarAsync<ResenaReserva>("sp_ObtenerResenaPorReserva", Db.P("@ReservaId", reservaId))).FirstOrDefault();
}
