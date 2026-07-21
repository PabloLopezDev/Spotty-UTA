using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Hubs;
using SpottyUTA.Services;

namespace SpottyUTA
{
    /// <summary>
    /// Punto de entrada principal de la aplicación Spotty UTA.
    /// Configura los servicios de inyección de dependencias, middleware HTTP,
    /// SignalR, sesiones y el pipeline de la aplicación ASP.NET Core.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Método principal que inicia la aplicación web.
        /// </summary>
        /// <param name="args">Argumentos de línea de comandos.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Servicios MVC con vistas Razor
            builder.Services.AddControllersWithViews();

            // Contexto de base de datos Entity Framework Core (SQL Server)
            builder.Services.AddDbContext<SpottyUtaContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // SignalR para comunicación en tiempo real
            builder.Services.AddSignalR();

            // Servicios de negocio (Scoped: una instancia por solicitud HTTP)
            builder.Services.AddScoped<ISalasService, SalasService>();
            builder.Services.AddScoped<IReservasService, ReservasService>();

            // Servicio en segundo plano: broadcasting periódico de estados de salas
            builder.Services.AddHostedService<SalasStateBroadcaster>();

            // Configuración de sesiones de servidor
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // La sesión expira en 30 min de inactividad
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Pipeline HTTP
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseSession();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            // Archivos estáticos (CSS, JS, imágenes)
            app.MapStaticAssets();

            // Endpoint de SignalR
            app.MapHub<SalasHub>("/salasHub");

            // Ruta MVC por defecto
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
