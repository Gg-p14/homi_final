using System.Data;
using Homi.Data;
using Homi.Models;
using Microsoft.Data.SqlClient;

namespace Homi.Services;

/// Búsqueda, detalle, tarifas y reseñas. Todo sale de los procedimientos de la v3.
public class PropertyService
{
    private readonly Db _db;

    public PropertyService(Db db) => _db = db;

    public Task<List<CiudadItem>> ListarCiudadesAsync() =>
        _db.ListarAsync<CiudadItem>("sp_ListarCiudades");

    public Task<List<AlojamientoResumen>> BuscarAsync(int? ciudadId, DateOnly? entrada, DateOnly? salida, int huespedes) =>
        _db.ListarAsync<AlojamientoResumen>("sp_BuscarAlojamientosDisponibles",
            Db.P("@CiudadId", ciudadId),
            Db.PFecha("@Entrada", entrada),
            Db.PFecha("@Salida", salida),
            Db.P("@Huespedes", huespedes));

    public Task<List<AlojamientoResumen>> DestacadosAsync(int cantidad = 3) =>
        _db.ListarAsync<AlojamientoResumen>("sp_ListarDestacados", Db.P("@Cantidad", cantidad));

    /// Si el usuario es el anfitrión o superadministrador, el procedimiento también devuelve alojamientos pausados.
    public async Task<(AlojamientoDetalle? Detalle, List<FotoItem> Fotos)> ObtenerDetalleAsync(int id, int? usuarioId)
    {
        var (detalles, fotos) = await _db.ListarDosAsync<AlojamientoDetalle, FotoItem>("sp_ObtenerDetalleAlojamiento",
            Db.P("@AlojamientoId", id),
            Db.P("@UsuarioId", usuarioId));
        return (detalles.FirstOrDefault(), fotos);
    }

    public async Task<DesgloseTarifa?> CalcularDesgloseAsync(int id, DateOnly entrada, DateOnly salida) =>
        (await _db.ListarAsync<DesgloseTarifa>("sp_CalcularDesglose",
            Db.P("@AlojamientoId", id),
            Db.PFecha("@Entrada", entrada),
            Db.PFecha("@Salida", salida))).FirstOrDefault();

    public async Task<decimal?> PromedioAsync(int id)
    {
        var promedio = new SqlParameter("@Promedio", SqlDbType.Decimal) { Precision = 3, Scale = 2, Direction = ParameterDirection.Output };
        await _db.EjecutarAsync("sp_PromedioCalificacion", Db.P("@AlojamientoId", id), promedio);
        return promedio.Value is decimal d ? d : null;
    }

    /// Noches ocupadas en un rango de fechas (para el calendario).
    public Task<List<FechaOcupada>> OcupadasAsync(int id, DateOnly desde, DateOnly hasta) =>
        _db.ListarAsync<FechaOcupada>("sp_ListarFechasOcupadas",
            Db.P("@AlojamientoId", id), Db.PFecha("@Desde", desde), Db.PFecha("@Hasta", hasta));

    public Task<List<ResenaItem>> ResenasAsync(int id) =>
        _db.ListarAsync<ResenaItem>("sp_ListarResenasDeAlojamiento", Db.P("@AlojamientoId", id));

    public Task<List<CatalogoItem>> PaisesAsync() => _db.ListarAsync<CatalogoItem>("sp_ListarPaises");

    public Task<List<CatalogoItem>> EstadosAsync() => _db.ListarAsync<CatalogoItem>("sp_ListarEstados");
}
