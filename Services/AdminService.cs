using Homi.Data;
using Homi.Models;

namespace Homi.Services;

/// Panel del superadministrador. Cada procedimiento valida que quien llama sea superadministrador.
public class AdminService
{
    private readonly Db _db;

    public AdminService(Db db) => _db = db;

    public async Task<ResumenGeneral> ResumenAsync(int superAdminId) =>
        (await _db.ListarAsync<ResumenGeneral>("sp_ObtenerResumenGeneral", Db.P("@SuperAdminId", superAdminId))).FirstOrDefault()
        ?? new ResumenGeneral();

    public Task<List<UsuarioAdmin>> UsuariosAsync(int superAdminId, string? buscar, bool? activo) =>
        _db.ListarAsync<UsuarioAdmin>("sp_ListarUsuarios",
            Db.P("@SuperAdminId", superAdminId), Db.P("@Buscar", buscar), Db.P("@Activo", activo));

    public Task DesactivarAsync(int usuarioId, int superAdminId) =>
        _db.EjecutarAsync("sp_DesactivarUsuario", Db.P("@UsuarioId", usuarioId), Db.P("@SuperAdminId", superAdminId));

    public Task ActivarAsync(int usuarioId, int superAdminId) =>
        _db.EjecutarAsync("sp_ActivarUsuario", Db.P("@UsuarioId", usuarioId), Db.P("@SuperAdminId", superAdminId));

    public Task EliminarAsync(int usuarioId, int superAdminId) =>
        _db.EjecutarAsync("sp_EliminarUsuarioAdmin", Db.P("@UsuarioId", usuarioId), Db.P("@SuperAdminId", superAdminId));

    public Task AsignarRolAsync(int usuarioId, int superAdminId, bool asignar) =>
        _db.EjecutarAsync("sp_AsignarRolSuperAdmin",
            Db.P("@UsuarioId", usuarioId), Db.P("@SuperAdminId", superAdminId), Db.P("@Asignar", asignar));

    public Task<List<ComentarioAdmin>> ComentariosAsync(int superAdminId, string? estado, string? tipo) =>
        _db.ListarAsync<ComentarioAdmin>("sp_ListarComentarios",
            Db.P("@SuperAdminId", superAdminId), Db.P("@Estado", estado), Db.P("@Tipo", tipo));

    public Task CambiarEstadoComentarioAsync(int comentarioId, int superAdminId, string estado) =>
        _db.EjecutarAsync("sp_CambiarEstadoComentario",
            Db.P("@ComentarioId", comentarioId), Db.P("@SuperAdminId", superAdminId), Db.P("@Estado", estado));

    public Task<List<BitacoraItem>> BitacoraAsync(int superAdminId, DateOnly? desde, DateOnly? hasta, int? usuarioId, string? accion) =>
        _db.ListarAsync<BitacoraItem>("sp_ListarBitacora",
            Db.P("@SuperAdminId", superAdminId), Db.PFecha("@FechaDesde", desde), Db.PFecha("@FechaHasta", hasta),
            Db.P("@UsuarioId", usuarioId), Db.P("@Accion", accion));

    public Task<List<ReporteDia>> ReservasPorDiaAsync(int superAdminId, DateOnly desde, DateOnly hasta) =>
        _db.ListarAsync<ReporteDia>("sp_ReporteReservasPorFecha",
            Db.P("@SuperAdminId", superAdminId), Db.PFecha("@FechaDesde", desde), Db.PFecha("@FechaHasta", hasta));

    public Task<List<ReporteCiudad>> IngresosPorCiudadAsync(int superAdminId, DateOnly desde, DateOnly hasta) =>
        _db.ListarAsync<ReporteCiudad>("sp_ReporteIngresosPorCiudad",
            Db.P("@SuperAdminId", superAdminId), Db.PFecha("@FechaDesde", desde), Db.PFecha("@FechaHasta", hasta));

    public Task<List<AdminAlojamientoItem>> AlojamientosAsync(int uid, string? buscar, bool? activo) =>
        _db.ListarAsync<AdminAlojamientoItem>("sp_ListarTodosLosAlojamientos",
            Db.P("@SuperAdminId", uid), Db.P("@Buscar", buscar), Db.P("@Activo", activo));

    public Task<List<AdminReservaItem>> ReservasAsync(int uid, DateOnly? desde, DateOnly? hasta, int? estado) =>
        _db.ListarAsync<AdminReservaItem>("sp_ListarTodasLasReservas",
            Db.P("@SuperAdminId", uid), Db.PFecha("@FechaDesde", desde), Db.PFecha("@FechaHasta", hasta),
            Db.P("@EstadoReservaId", estado));

    public Task CancelarReservaAsync(int reservaId, int uid, string? motivo) =>
        _db.EjecutarAsync("sp_CancelarReservaAdmin",
            Db.P("@ReservaId", reservaId), Db.P("@SuperAdminId", uid), Db.P("@Motivo", motivo));

    public Task<List<CatalogoItem>> CatalogoAsync(int uid, string catalogo) =>
        _db.ListarAsync<CatalogoItem>("sp_ListarCatalogos", Db.P("@SuperAdminId", uid), Db.P("@Catalogo", catalogo));

    public Task ActualizarCatalogoAsync(int uid, string catalogo, int id, string nombre) =>
        _db.EjecutarAsync("sp_ActualizarCatalogo",
            Db.P("@SuperAdminId", uid), Db.P("@Catalogo", catalogo), Db.P("@Id", id), Db.P("@Nombre", nombre));

    public Task ActualizarUsuarioAsync(int uid, int usuarioId, string nombre, string apellido, string telefono) =>
        _db.EjecutarAsync("sp_ActualizarUsuarioAdmin",
            Db.P("@UsuarioId", usuarioId), Db.P("@SuperAdminId", uid),
            Db.P("@Nombre", nombre), Db.P("@Apellido", apellido), Db.P("@Telefono", telefono));
}
