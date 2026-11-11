using System.Data;
using Homi.Data;
using Homi.Models;

namespace Homi.Services;

/// Comentarios y problemas enviados desde el pie de página.
public class ComentarioService
{
    private readonly Db _db;

    public ComentarioService(Db db) => _db = db;

    public Task CrearAsync(int usuarioId, string tipo, string asunto, string mensaje)
    {
        var id = Db.Salida("@Id", SqlDbType.Int);
        return _db.EjecutarAsync("sp_CrearComentario",
            Db.P("@UsuarioId", usuarioId),
            Db.P("@Tipo", tipo),
            Db.P("@Asunto", asunto),
            Db.P("@Mensaje", mensaje),
            id);
    }

    public Task<List<ComentarioItem>> ListarAsync(int usuarioId) =>
        _db.ListarAsync<ComentarioItem>("sp_ObtenerComentariosDeUsuario", Db.P("@UsuarioId", usuarioId));
}
