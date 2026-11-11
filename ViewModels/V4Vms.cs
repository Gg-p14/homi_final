using System.ComponentModel.DataAnnotations;
using Homi.Models;
using Microsoft.AspNetCore.Http;

namespace Homi.ViewModels;

public class PerfilViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre."), StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu apellido."), StringLength(60)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu teléfono."),
     RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono celular debe tener 10 dígitos, sin espacios.")]
    public string Telefono { get; set; } = string.Empty;

    /// Solo lectura: el correo no se cambia desde el perfil.
    public string Email { get; set; } = string.Empty;

    /// Foto actual (solo lectura en la vista).
    public string? FotoPerfil { get; set; }

    /// Nueva foto opcional (JPG, PNG o WEBP, máximo 1 MB).
    public IFormFile? Foto { get; set; }

    /// Quitar la foto actual sin subir una nueva.
    public bool QuitarFoto { get; set; }
}

public class BuzonViewModel
{
    public List<ConversacionItem> Conversaciones { get; set; } = new();
}

public class ConversacionViewModel
{
    public int ConversacionId { get; set; }
    public ConversacionHeader Header { get; set; } = new();
    public List<MensajeItem> Mensajes { get; set; } = new();

    [Required(ErrorMessage = "Escribe un mensaje."), StringLength(1000)]
    public string Texto { get; set; } = string.Empty;
}

public class PagosViewModel
{
    public List<PagoAnfitrionItem> Pagos { get; set; } = new();
}

public class DesgloseViewModel
{
    public int ReservaId { get; set; }
    public List<Homi.Models.CargoItem> Cargos { get; set; } = new();
}
