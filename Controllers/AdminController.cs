using Homi.Data;
using Homi.Models;
using Homi.Helpers;
using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

[Authorize(Roles = "SuperAdministrador")]
public class AdminController : Controller
{
    private readonly AdminService _admin;

    public AdminController(AdminService admin) => _admin = admin;

    public async Task<IActionResult> Index() =>
        View(await _admin.ResumenAsync(User.ObtenerUsuarioId()));

    [HttpGet]
    public async Task<IActionResult> Usuarios(string? buscar, bool? activo)
    {
        var uid = User.ObtenerUsuarioId();
        var vm = new UsuariosAdminViewModel
        {
            Buscar = buscar,
            Activo = activo,
            ActualId = uid,
            Usuarios = await _admin.UsuariosAsync(uid, buscar, activo)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Desactivar(int id) =>
        Ejecutar(() => _admin.DesactivarAsync(id, User.ObtenerUsuarioId()), "Usuario desactivado. Sus reservas futuras se cancelaron.", nameof(Usuarios));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Activar(int id) =>
        Ejecutar(() => _admin.ActivarAsync(id, User.ObtenerUsuarioId()), "Usuario activado.", nameof(Usuarios));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Eliminar(int id) =>
        Ejecutar(() => _admin.EliminarAsync(id, User.ObtenerUsuarioId()), "Usuario eliminado.", nameof(Usuarios));

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> AsignarSuperAdmin(int id, bool asignar) =>
        Ejecutar(() => _admin.AsignarRolAsync(id, User.ObtenerUsuarioId(), asignar),
            asignar ? "Rol de superadministrador asignado." : "Rol de superadministrador quitado.", nameof(Usuarios));

    [HttpGet]
    public async Task<IActionResult> Comentarios(string? estado, string? tipo)
    {
        var vm = new ComentariosAdminViewModel
        {
            Estado = estado,
            Tipo = tipo,
            Comentarios = await _admin.ComentariosAsync(User.ObtenerUsuarioId(), estado, tipo)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CambiarEstadoComentario(int id, string estado) =>
        Ejecutar(() => _admin.CambiarEstadoComentarioAsync(id, User.ObtenerUsuarioId(), estado), "Estado del comentario actualizado.", nameof(Comentarios));

    [HttpGet]
    public async Task<IActionResult> Bitacora(DateOnly? desde, DateOnly? hasta, int? usuarioId, string? accion)
    {
        var vm = new BitacoraViewModel
        {
            Desde = desde,
            Hasta = hasta,
            UsuarioId = usuarioId,
            Accion = accion,
            Registros = await _admin.BitacoraAsync(User.ObtenerUsuarioId(), desde, hasta, usuarioId, accion)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Reportes(DateOnly? desde, DateOnly? hasta)
    {
        var hoy = Homi.Helpers.Reloj.Hoy;
        var vm = new ReportesViewModel
        {
            Desde = desde ?? new DateOnly(hoy.Year, hoy.Month, 1),
            Hasta = hasta ?? hoy
        };

        if (vm.Hasta < vm.Desde)
        {
            vm.Error = "La fecha final no puede ser anterior a la inicial.";
            return View(vm);
        }

        try
        {
            var uid = User.ObtenerUsuarioId();
            vm.ReservasPorDia = await _admin.ReservasPorDiaAsync(uid, vm.Desde, vm.Hasta);
            vm.IngresosPorCiudad = await _admin.IngresosPorCiudadAsync(uid, vm.Desde, vm.Hasta);
        }
        catch (ReglaNegocioException ex)
        {
            vm.Error = ex.Message;
        }
        return View(vm);
    }

    private async Task<IActionResult> Ejecutar(Func<Task> accion, string mensajeOk, string vista)
    {
        try
        {
            await accion();
            TempData["Exito"] = mensajeOk;
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(vista);
    }

    [HttpGet]
    public async Task<IActionResult> Alojamientos(string? buscar, bool? activo)
    {
        var vm = new AlojamientosAdminViewModel
        {
            Buscar = buscar,
            Activo = activo,
            Alojamientos = await _admin.AlojamientosAsync(User.ObtenerUsuarioId(), buscar, activo)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Reservas(DateOnly? desde, DateOnly? hasta, int? estado)
    {
        var vm = new ReservasAdminViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Estado = estado,
            Reservas = await _admin.ReservasAsync(User.ObtenerUsuarioId(), desde, hasta, estado)
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarReserva(int id, string? motivo)
    {
        try
        {
            await _admin.CancelarReservaAsync(id, User.ObtenerUsuarioId(), motivo);
            TempData["Exito"] = "Reserva cancelada con reembolso total.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Reservas));
    }

    [HttpGet]
    public async Task<IActionResult> Catalogos(string catalogo = "EstadosReserva")
    {
        var vm = new CatalogosAdminViewModel { Catalogo = catalogo };
        try
        {
            vm.Items = await _admin.CatalogoAsync(User.ObtenerUsuarioId(), catalogo);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarCatalogo(string catalogo, int id, string nombre)
    {
        try
        {
            await _admin.ActualizarCatalogoAsync(User.ObtenerUsuarioId(), catalogo, id, nombre.Trim());
            TempData["Exito"] = "Catálogo actualizado.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Catalogos), new { catalogo });
    }

    [HttpGet]
    public async Task<IActionResult> EditarUsuario(int id)
    {
        var usuario = (await _admin.UsuariosAsync(User.ObtenerUsuarioId(), null, null)).FirstOrDefault(u => u.Id == id);
        if (usuario is null) return NotFound();
        return View(new EditarUsuarioViewModel
        {
            Id = usuario.Id,
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Telefono = usuario.Telefono
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        try
        {
            await _admin.ActualizarUsuarioAsync(User.ObtenerUsuarioId(), vm.Id, vm.Nombre.Trim(), vm.Apellido.Trim(), vm.Telefono);
            TempData["Exito"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Usuarios));
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }
}
