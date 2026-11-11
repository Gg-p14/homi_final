using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Homi.Services;

/// Valida y guarda fotos. Revisa el tamaño (1 MB) y el tipo real del archivo por sus primeros bytes.
public class ImageStorageService
{
    public const int TamanoMaximo = 1024 * 1024;

    private readonly IWebHostEnvironment _entorno;

    public ImageStorageService(IWebHostEnvironment entorno) => _entorno = entorno;

    public async Task<string> GuardarAsync(IFormFile archivo)
    {
        if (archivo.Length == 0 || archivo.Length > TamanoMaximo)
            throw new InvalidOperationException($"{archivo.FileName}: la imagen debe pesar máximo 1 MB.");

        byte[] bytes;
        using (var memoria = new MemoryStream())
        {
            await archivo.CopyToAsync(memoria);
            bytes = memoria.ToArray();
        }

        var extension = DetectarExtension(bytes)
            ?? throw new InvalidOperationException($"{archivo.FileName}: solo se permiten imágenes JPG, PNG o WEBP.");

        var carpeta = Path.Combine(_entorno.WebRootPath, "uploads");
        Directory.CreateDirectory(carpeta);
        var nombre = Guid.NewGuid().ToString("N") + extension;
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), bytes);
        return "/uploads/" + nombre;
    }

    public void Borrar(string ruta)
    {
        var fisica = Path.Combine(_entorno.WebRootPath, ruta.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fisica)) File.Delete(fisica);
    }

    private static string? DetectarExtension(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
        if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return ".png";
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return ".webp";
        return null;
    }
}
