using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;

namespace SpottyUTA.Controllers
{
    public class AuthController : Controller
    {
        private readonly SpottyUtaContext _context;

        public AuthController(SpottyUtaContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Si ya tiene sesión abierta, lo mandamos directo al panel
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                TempData["ErrorMessage"] = "El correo institucional es obligatorio.";
                return RedirectToAction("Login");
            }

            string emailLower = correo.Trim().ToLower();

            // 1. Clasificar el Rol estrictamente por el dominio del correo institucional
            string rolAsignado = "";
            if (emailLower.EndsWith("@gestion.uta.cl"))
            {
                rolAsignado = "Administrador";
            }
            else if (emailLower.EndsWith("@alumnos.uta.cl") || emailLower.EndsWith("@uta.cl"))
            {
                rolAsignado = "Estudiante";
            }
            else
            {
                TempData["ErrorMessage"] = "Acceso denegado: Debe ingresar un correo institucional válido de la UTA.";
                return RedirectToAction("Login");
            }

            // 2. Buscar al usuario en SQL Server usando tu propiedad real: CorreoUta
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoUta.ToLower() == emailLower);

            // Si es un usuario nuevo en tu base local de pruebas, lo creamos de inmediato con tus columnas reales
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    CorreoUta = emailLower,
                    NombreCompleto = rolAsignado == "Administrador" ? "Funcionario Biblioteca" : "Alumno UTA",
                    Rol = rolAsignado,
                    ContadorInasistencias = 0,
                    EstaBloqueado = false
                };
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }

            if (usuario.EstaBloqueado)
            {
                TempData["ErrorMessage"] = $"Acceso denegado: Tu cuenta está suspendida por registrar {usuario.ContadorInasistencias} inasistencias.";
                return RedirectToAction("Login");
            }

            // 3. Guardar los datos del usuario en la Sesión
            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioNombre", usuario.NombreCompleto);

            TempData["SuccessMessage"] = $"¡Bienvenido {usuario.NombreCompleto}!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult CerrarSesion()
        {
            // Limpiar la sesión por completo al salir
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}