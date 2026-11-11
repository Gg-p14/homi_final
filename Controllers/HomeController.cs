using Homi.Services;
using Homi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Homi.Controllers;

public class HomeController : Controller
{
    private readonly PropertyService _propiedades;

    public HomeController(PropertyService propiedades) => _propiedades = propiedades;

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            Ciudades = await _propiedades.ListarCiudadesAsync(),
            Destacados = await _propiedades.DestacadosAsync(3)
        };
        return View(vm);
    }

    public IActionResult Error() => View();
}
