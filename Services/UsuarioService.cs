using System.Data;
using Homi.Data;
using Homi.Models;
using Microsoft.AspNetCore.Identity;

namespace Homi.Services;

/// Registro, login, bloqueo y perfil. La contraseña se guarda como hash con PasswordHasher.
public class UsuarioService
{
    private readonly Db _db;
    private readonly PasswordHasher<UsuarioLogin> _hasher = new();

    public UsuarioService(Db db) => _db = db;

    public async Task<int> RegistrarAsync(string email, string password, string nombre, string apellido, string telefono)
    {
        var hash = _hasher.HashPassword(new UsuarioLogin(), password);
        var id = Db.Salida("@Id", SqlDbType.Int);
        await _db.EjecutarAsync("sp_CrearUsuario",
            Db.P("@Email", email),
            Db.P("@PasswordHash", hash),
            Db.P("@SecurityStamp", Guid.NewGuid().ToString("N")),
            Db.P("@Nombre", nombre),
            Db.P("@Apellido", apellido),
            Db.P("@Telefono", telefono),
            id);
        return (int)id.Value!;
    }

    public async Task<UsuarioLogin?> ObtenerParaLoginAsync(string email) =>
        (await _db.ListarAsync<UsuarioLogin>("sp_ObtenerUsuarioParaLogin", Db.P("@Email", email))).FirstOrDefault();

    public bool PasswordCorrecta(UsuarioLogin usuario, string password) =>
        _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password) != PasswordVerificationResult.Failed;

    public Task RegistrarFalloAsync(int usuarioId, int maximoIntentos, int minutosBloqueo, int minutosVentana)
    {
        var fallos = Db.Salida("@Fallos", SqlDbType.Int);
        var bloqueo = Db.Salida("@LockoutEnd", SqlDbType.DateTime2);
        return _db.EjecutarAsync("sp_RegistrarLoginFallido",
            Db.P("@UsuarioId", usuarioId), Db.P("@MaximoIntentos", maximoIntentos),
            Db.P("@MinutosBloqueo", minutosBloqueo), Db.P("@MinutosVentana", minutosVentana),
            fallos, bloqueo);
    }

    public Task RegistrarExitosoAsync(int usuarioId) =>
        _db.EjecutarAsync("sp_RegistrarLoginExitoso", Db.P("@UsuarioId", usuarioId));

    public async Task<UsuarioSesion?> ObtenerSesionAsync(int usuarioId) =>
        (await _db.ListarAsync<UsuarioSesion>("sp_ObtenerUsuarioPorId", Db.P("@Id", usuarioId))).FirstOrDefault();

    /// Actualiza nombre, apellido, teléfono y foto. Devuelve la ruta de la foto anterior
    /// si cambió (la aplicación debe borrarla del disco), o null si no hubo cambio de foto.
    public async Task<string?> ActualizarPerfilAsync(int id, string nombre, string apellido, string telefono,
        string? nuevaFoto, bool quitarFoto)
    {
        var anterior = Db.Salida("@FotoAnterior", SqlDbType.NVarChar, 255);
        await _db.EjecutarAsync("sp_ActualizarPerfil",
            Db.P("@Id", id),
            Db.P("@Nombre", nombre),
            Db.P("@Apellido", apellido),
            Db.P("@Telefono", telefono),
            Db.P("@FotoPerfil", nuevaFoto),
            Db.P("@QuitarFoto", quitarFoto),
            anterior);
        return anterior.Value as string;
    }

    /// Genera un código de 6 dígitos, lo guarda (vence en 15 minutos) y lo devuelve para enviarlo.
    public async Task<string> GenerarCodigoAsync(int usuarioId, string? codigoFijo = null)
    {
        var codigo = string.IsNullOrWhiteSpace(codigoFijo)
            ? System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6")
            : codigoFijo.Trim();
        await _db.EjecutarAsync("sp_GuardarCodigoVerificacion",
            Db.P("@UsuarioId", usuarioId), Db.P("@Codigo", codigo));
        return codigo;
    }

    /// Confirma el correo con el código. Lanza ReglaNegocioException si el código no sirve.
    public Task VerificarCorreoAsync(string correo, string codigo) =>
        _db.EjecutarAsync("sp_VerificarCorreo", Db.P("@Email", correo), Db.P("@Codigo", codigo));

    /// Cancela una reserva desde el panel del anfitrión (reembolso total y aviso al huésped).
    public Task CancelarReservaAsync(int reservaId, int anfitrionId, string? motivo) =>
        _db.EjecutarAsync("sp_CancelarReservaAnfitrion",
            Db.P("@ReservaId", reservaId), Db.P("@AnfitrionId", anfitrionId), Db.P("@Motivo", motivo));

    /// Hash de referencia: se usa para que un correo inexistente tarde lo mismo que uno real.
    private static readonly string HashFicticio =
        new PasswordHasher<UsuarioLogin>().HashPassword(new UsuarioLogin(), "referencia-" + Guid.NewGuid());

    public void SimularVerificacion(string password) =>
        _hasher.VerifyHashedPassword(new UsuarioLogin(), HashFicticio, password ?? string.Empty);

    /// Cambia la contraseña y el sello de seguridad (cierra sesiones en otros dispositivos). Devuelve el sello nuevo.
    public async Task<string> CambiarPasswordAsync(int id, string nueva)
    {
        var hash = _hasher.HashPassword(new UsuarioLogin(), nueva);
        var sello = Guid.NewGuid().ToString("N");
        await _db.EjecutarAsync("sp_CambiarPassword",
            Db.P("@Id", id), Db.P("@PasswordHash", hash), Db.P("@SecurityStamp", sello));
        return sello;
    }

    /// Elimina la cuenta del usuario. Devuelve la ruta de la foto de perfil para borrarla del disco.
    public async Task<string?> EliminarCuentaAsync(int usuarioId)
    {
        var ruta = Db.Salida("@Ruta", SqlDbType.NVarChar, 255);
        await _db.EjecutarAsync("sp_EliminarMiCuenta", Db.P("@UsuarioId", usuarioId), ruta);
        return ruta.Value as string;
    }
}
