using System.Data;
using Homi.Data;
using Homi.Models;

namespace Homi.Services;

/// Buzón entre huéspedes y anfitriones. Toda la validación de permisos vive en los procedimientos.
public class MensajeriaService
{
    private readonly Db _db;

    public MensajeriaService(Db db) => _db = db;

    public Task<List<ConversacionItem>> BuzonAsync(int usuarioId) =>
        _db.ListarAsync<ConversacionItem>("sp_ListarConversaciones", Db.P("@UsuarioId", usuarioId));

    public async Task<int> AbrirAsync(int alojamientoId, int usuarioId)
    {
        var id = Db.Salida("@ConversacionId", SqlDbType.Int);
        await _db.EjecutarAsync("sp_AbrirConversacion",
            Db.P("@AlojamientoId", alojamientoId),
            Db.P("@UsuarioId", usuarioId),
            id);
        return (int)id.Value!;
    }

    /// Devuelve el encabezado y los mensajes. Los mensajes recibidos se marcan como leídos.
    public async Task<(ConversacionHeader? Header, List<MensajeItem> Mensajes)> VerAsync(int conversacionId, int usuarioId)
    {
        var (cabecera, mensajes) = await _db.ListarDosAsync<ConversacionHeader, MensajeItem>(
            "sp_ObtenerConversacion",
            Db.P("@ConversacionId", conversacionId),
            Db.P("@UsuarioId", usuarioId));
        return (cabecera.FirstOrDefault(), mensajes);
    }

    public async Task EnviarAsync(int conversacionId, int usuarioId, string texto)
    {
        var mensajeId = Db.Salida("@MensajeId", SqlDbType.Int);
        await _db.EjecutarAsync("sp_EnviarMensaje",
            Db.P("@ConversacionId", conversacionId),
            Db.P("@RemitenteId", usuarioId),
            Db.P("@Texto", texto),
            mensajeId);
    }

    public async Task<int> NoLeidosAsync(int usuarioId)
    {
        var total = Db.Salida("@Total", SqlDbType.Int);
        await _db.EjecutarAsync("sp_ContarMensajesNoLeidos", Db.P("@UsuarioId", usuarioId), total);
        return total.Value is int t ? t : 0;
    }
}
