using Homi.Helpers;
using System.Security.Claims;
using Homi.Data;
using Homi.Models;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Homi.Controllers;

public class AccountController : Controller
{
    private const string MensajeGenerico = "Correo o contraseña incorrectos.";

    private readonly UsuarioService _usuarios;
    private readonly LimitadorIntentos _limitador;
    private readonly ConfigSeguridad _seguridad;
    private readonly CorreoService _correo;

    public AccountController(UsuarioService usuarios, LimitadorIntentos limitador,
        ConfigSeguridad seguridad, CorreoService correo)
    {
        _usuarios = usuarios;
        _limitador = limitador;
        _seguridad = seguridad;
        _correo = correo;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            var id = await _usuarios.RegistrarAsync(vm.Correo.Trim(), vm.Password, vm.Nombre.Trim(), vm.Apellido.Trim(), vm.Telefono);
            await EnviarCodigoAsync(id, vm.Correo.Trim(), vm.Nombre.Trim());
            TempData["Exito"] = "Te enviamos un código para confirmar tu correo.";
            return RedirectToAction(nameof(Verificar), new { correo = vm.Correo.Trim() });
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var ip = ObtenerIp();
        if (_limitador.EstaBloqueado(ip, out var minutosEspera))
        {
            ModelState.AddModelError(string.Empty,
                $"Demasiados intentos desde tu conexión. Espera {minutosEspera} minuto(s) e intenta de nuevo.");
            return View(vm);
        }

        var usuario = await _usuarios.ObtenerParaLoginAsync(vm.Correo.Trim());

        // Mismo mensaje si el correo no existe o la contraseña está mal (no revela qué correos tienen cuenta).
        if (usuario is null)
        {
            _usuarios.SimularVerificacion(vm.Password);
            _limitador.RegistrarFallo(ip);
            ModelState.AddModelError(string.Empty, MensajeGenerico);
            return View(vm);
        }

        if (!_usuarios.PasswordCorrecta(usuario, vm.Password))
        {
            await _usuarios.RegistrarFalloAsync(usuario.Id, _seguridad.MaximoIntentosCuenta,
                _seguridad.MinutosBloqueoCuenta, _seguridad.MinutosVentanaCuenta);
            _limitador.RegistrarFallo(ip);
            ModelState.AddModelError(string.Empty, MensajeGenerico);
            return View(vm);
        }

        // Solo quien conoce la contraseña ve el aviso de bloqueo.
        if (usuario.MinutosBloqueado > 0)
        {
            ModelState.AddModelError(string.Empty,
                $"La cuenta está bloqueada temporalmente. Intenta de nuevo en {usuario.MinutosBloqueado} minuto(s).");
            return View(vm);
        }

        if (!usuario.EmailVerificado)
        {
            await EnviarCodigoAsync(usuario.Id, usuario.Email, usuario.Nombre);
            TempData["Exito"] = "Confirma tu correo para continuar. Te enviamos un código nuevo.";
            return RedirectToAction(nameof(Verificar), new { correo = usuario.Email });
        }

        if (!usuario.Activo)
        {
            ModelState.AddModelError(string.Empty, "Tu cuenta no está disponible.");
            return View(vm);
        }

        await _usuarios.RegistrarExitosoAsync(usuario.Id);

        var sesion = await _usuarios.ObtenerSesionAsync(usuario.Id);
        if (sesion is null)
        {
            ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión. Intenta de nuevo.");
            return View(vm);
        }

        await IniciarSesionAsync(sesion);
        TempData["Bienvenida"] = $"¡Bienvenido de nuevo, {sesion.Nombre}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (sesion.EsSuperAdmin)
            return RedirectToAction("Index", "Admin");

        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult Verificar(string? correo = null) =>
        View(new VerificarViewModel { Correo = correo ?? string.Empty });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verificar(VerificarViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            await _usuarios.VerificarCorreoAsync(vm.Correo.Trim(), vm.Codigo.Trim());
            vm.Codigo = string.Empty;
            TempData["Exito"] = "Correo confirmado. Ya puedes iniciar sesión.";
            return View(vm);
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReenviarCodigo(string correo)
    {
        var usuario = await _usuarios.ObtenerParaLoginAsync((correo ?? string.Empty).Trim());
        if (usuario is not null && !usuario.EmailVerificado)
            await EnviarCodigoAsync(usuario.Id, usuario.Email, usuario.Nombre);
        TempData["Exito"] = "Si la cuenta está pendiente de confirmar, te enviamos un código nuevo.";
        return RedirectToAction(nameof(Verificar), new { correo });
    }

    private async Task EnviarCodigoAsync(int usuarioId, string correo, string nombre)
    {
        var codigo = await _usuarios.GenerarCodigoAsync(usuarioId, _seguridad.CodigoVerificacionFijo);
        try
        {
            await _correo.EnviarCodigoAsync(correo, nombre, codigo);
        }
        catch (Exception ex)
        {
            Homi.Helpers.Registro.Error("No se pudo enviar el correo de verificación", ex);
        }
    }

    /// IP real de la conexión. X-Forwarded-For solo se usa si el administrador lo activa (detrás de un proxy).
    private string ObtenerIp()
    {
        if (_seguridad.ConfiarEnEncabezadoProxy && Request.Headers.TryGetValue("X-Forwarded-For", out var valor))
        {
            var primera = valor.ToString().Split(',')[0].Trim();
            if (primera.Length > 0) return primera;
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
    }

    private async Task IniciarSesionAsync(UsuarioSesion sesion)
    {
        var reclamaciones = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.Id.ToString()),
            new(ClaimTypes.Name, sesion.Email),
            new("Nombre", sesion.Nombre),
            new("SecurityStamp", sesion.SecurityStamp)
        };
        if (sesion.EsSuperAdmin)
            reclamaciones.Add(new Claim(ClaimTypes.Role, "SuperAdministrador"));
        if (!string.IsNullOrEmpty(sesion.FotoPerfil))
            reclamaciones.Add(new Claim("FotoPerfil", sesion.FotoPerfil));

        var identidad = new ClaimsIdentity(reclamaciones, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
    }
}
