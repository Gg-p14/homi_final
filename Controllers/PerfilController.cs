using System.Security.Claims;
using System.Text.RegularExpressions;
using Homi.Data;
using Homi.Helpers;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize]
public class PerfilController : Controller
{
    private readonly UsuarioService _usuarios;
    private readonly ImageStorageService _imagenes;

    public PerfilController(UsuarioService usuarios, ImageStorageService imagenes)
    {
        _usuarios = usuarios;
        _imagenes = imagenes;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sesion = await _usuarios.ObtenerSesionAsync(User.ObtenerUsuarioId());
        if (sesion is null) return NotFound();

        return View(new PerfilViewModel
        {
            Nombre = sesion.Nombre,
            Apellido = sesion.Apellido,
            Telefono = sesion.Telefono,
            Email = sesion.Email,
            FotoPerfil = sesion.FotoPerfil
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PerfilViewModel vm)
    {
        var uid = User.ObtenerUsuarioId();
        var actual = await _usuarios.ObtenerSesionAsync(uid);
        if (actual is null) return NotFound();

        vm.Email = actual.Email;
        vm.FotoPerfil = actual.FotoPerfil;
        ModelState.Remove(nameof(vm.Email));
        if (!ModelState.IsValid) return View(vm);

        string? nuevaRuta = null;
        if (vm.Foto is not null && vm.Foto.Length > 0)
        {
            try
            {
                nuevaRuta = await _imagenes.GuardarAsync(vm.Foto);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.Foto), ex.Message);
                return View(vm);
            }
        }

        // Si sube una foto nueva, no se quita la anterior por la casilla.
        var quitar = vm.QuitarFoto && nuevaRuta is null;

        try
        {
            var anterior = await _usuarios.ActualizarPerfilAsync(
                uid, vm.Nombre.Trim(), vm.Apellido.Trim(), vm.Telefono, nuevaRuta, quitar);

            if (anterior is not null) _imagenes.Borrar(anterior);

            await RefrescarSesionAsync(uid);
            TempData["Exito"] = "Perfil actualizado.";
            return RedirectToAction(nameof(Index));
        }
        catch (ReglaNegocioException ex)
        {
            if (nuevaRuta is not null) _imagenes.Borrar(nuevaRuta);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    /// Cambia la contraseña. Pide la actual, valida la nueva y mantiene la sesión abierta en este dispositivo.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(string passwordActual, string nuevaPassword, string confirmarPassword)
    {
        var uid = User.ObtenerUsuarioId();
        var sesion = await _usuarios.ObtenerSesionAsync(uid);
        if (sesion is null) return NotFound();

        var login = await _usuarios.ObtenerParaLoginAsync(sesion.Email);
        if (login is null || !_usuarios.PasswordCorrecta(login, passwordActual ?? string.Empty))
        {
            TempData["Error"] = "La contraseña actual no es correcta.";
            return RedirectToAction(nameof(Index));
        }
        if (!Regex.IsMatch(nuevaPassword ?? string.Empty, @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$"))
        {
            TempData["Error"] = "La nueva contraseña necesita 8 caracteres, una mayúscula, un número y un carácter especial.";
            return RedirectToAction(nameof(Index));
        }
        if (nuevaPassword != confirmarPassword)
        {
            TempData["Error"] = "Las contraseñas nuevas no coinciden.";
            return RedirectToAction(nameof(Index));
        }

        await _usuarios.CambiarPasswordAsync(uid, nuevaPassword!);
        await RefrescarSesionAsync(uid);
        TempData["Exito"] = "Contraseña actualizada.";
        return RedirectToAction(nameof(Index));
    }

    /// Elimina la cuenta. Pide la contraseña actual. Las reservas activas se cancelan con reembolso total.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCuenta(string password)
    {
        var uid = User.ObtenerUsuarioId();
        var sesion = await _usuarios.ObtenerSesionAsync(uid);
        if (sesion is null) return NotFound();

        var login = await _usuarios.ObtenerParaLoginAsync(sesion.Email);
        if (login is null || !_usuarios.PasswordCorrecta(login, password ?? string.Empty))
        {
            TempData["Error"] = "Escribe tu contraseña actual para eliminar la cuenta.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var ruta = await _usuarios.EliminarCuentaAsync(uid);
            if (ruta is not null) _imagenes.Borrar(ruta);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Exito"] = "Tu cuenta fue eliminada.";
        return RedirectToAction("Index", "Home");
    }

    /// Vuelve a emitir la cookie para que el saludo muestre el nombre nuevo.
    private async Task RefrescarSesionAsync(int uid)
    {
        var sesion = await _usuarios.ObtenerSesionAsync(uid);
        if (sesion is null) return;

        var reclamaciones = User.Claims.Where(c => c.Type != "Nombre" && c.Type != "FotoPerfil" && c.Type != "SecurityStamp").ToList();
        reclamaciones.Add(new Claim("SecurityStamp", sesion.SecurityStamp));
        reclamaciones.Add(new Claim("Nombre", sesion.Nombre));
        if (!string.IsNullOrEmpty(sesion.FotoPerfil))
            reclamaciones.Add(new Claim("FotoPerfil", sesion.FotoPerfil));
        var identidad = new ClaimsIdentity(reclamaciones, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));
    }
}
