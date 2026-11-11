using System.Data;
using Homi.Data;
using Homi.Models;

namespace Homi.Services;

public class FavoritesService
{
    private readonly Db _db;

    public FavoritesService(Db db) => _db = db;

    public Task<List<FavoritoItem>> ListarAsync(int usuarioId) =>
        _db.ListarAsync<FavoritoItem>("sp_ListarFavoritos", Db.P("@UsuarioId", usuarioId));

    public async Task<bool> EsFavoritoAsync(int usuarioId, int alojamientoId)
    {
        var es = Db.Salida("@EsFavorito", SqlDbType.Bit);
        await _db.EjecutarAsync("sp_EsFavorito", Db.P("@UsuarioId", usuarioId), Db.P("@AlojamientoId", alojamientoId), es);
        return es.Value is bool valor && valor;
    }

    public Task AgregarAsync(int usuarioId, int alojamientoId) =>
        _db.EjecutarAsync("sp_AgregarFavorito", Db.P("@UsuarioId", usuarioId), Db.P("@AlojamientoId", alojamientoId));

    public Task QuitarAsync(int usuarioId, int alojamientoId) =>
        _db.EjecutarAsync("sp_QuitarFavorito", Db.P("@UsuarioId", usuarioId), Db.P("@AlojamientoId", alojamientoId));
}
