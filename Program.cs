using System.Security.Claims;
using Homi.Data;
using Homi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

var seguridad = builder.Configuration.GetSection("Seguridad").Get<Homi.Helpers.ConfigSeguridad>()
                ?? new Homi.Helpers.ConfigSeguridad();
builder.Services.AddSingleton(seguridad);
builder.Services.AddSingleton<Homi.Services.LimitadorIntentos>();
builder.Services.AddHostedService<Homi.Services.MantenimientoService>();

builder.Services.AddScoped<Db>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<HostService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<FavoritesService>();
builder.Services.AddScoped<ComentarioService>();
builder.Services.AddScoped<ImageStorageService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<MensajeriaService>();
builder.Services.AddScoped<CorreoService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;

        // En cada petición se confirma que la cuenta siga activa y que el sello de seguridad no haya cambiado.
        options.Events.OnValidatePrincipal = async context =>
        {
            var usuarios = context.HttpContext.RequestServices.GetRequiredService<UsuarioService>();
            var idTexto = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var selloSesion = context.Principal?.FindFirst("SecurityStamp")?.Value;

            if (!int.TryParse(idTexto, out var id))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var actual = await usuarios.ObtenerSesionAsync(id);
            if (actual is null || !actual.Activo || actual.SecurityStamp != selloSesion)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            // Si el rol de superadministrador cambió, se reconstruye la identidad sin esperar a un nuevo inicio de sesión.
            if (context.Principal!.IsInRole("SuperAdministrador") != actual.EsSuperAdmin)
            {
                var reclamaciones = context.Principal.Claims.Where(c => c.Type != ClaimTypes.Role).ToList();
                if (actual.EsSuperAdmin)
                    reclamaciones.Add(new Claim(ClaimTypes.Role, "SuperAdministrador"));

                var identidad = new ClaimsIdentity(reclamaciones, CookieAuthenticationDefaults.AuthenticationScheme);
                context.ReplacePrincipal(new ClaimsPrincipal(identidad));
                context.ShouldRenew = true;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Todo error no controlado queda registrado en la carpeta logs.
app.Use(async (contexto, siguiente) =>
{
    try
    {
        await siguiente();
    }
    catch (Exception ex)
    {
        Homi.Helpers.Registro.Error($"{contexto.Request.Method} {contexto.Request.Path}", ex);
        throw;
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("ForzarHttps"))
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var ambito = app.Services.CreateScope())
{
    var resultado = await ambito.ServiceProvider.GetRequiredService<Db>().ProbarConexionAsync();
    Console.WriteLine(resultado);
}

app.Run();
