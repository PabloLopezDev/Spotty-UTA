using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;
using System.Threading.Tasks;

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

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.CorreoUta.ToLower() == emailLower);

            if (usuario == null)
            {
                TempData["PendingEmail"] = emailLower;
                TempData["PendingRol"] = rolAsignado;
                return RedirectToAction("Register");
            }

            if (usuario.EstaBloqueado)
            {
                TempData["ErrorMessage"] = $"Acceso denegado: Tu cuenta está suspendida por registrar {usuario.ContadorInasistencias} inasistencias.";
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioNombre", usuario.NombreCompleto);

            TempData["SuccessMessage"] = $"¡Bienvenido {usuario.NombreCompleto}!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            var email = TempData["PendingEmail"] as string;
            var rol = TempData["PendingRol"] as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(rol))
            {
                return RedirectToAction("Login");
            }

            TempData.Keep("PendingEmail");
            TempData.Keep("PendingRol");

            ViewBag.Email = email;
            ViewBag.Rol = rol;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletarRegistro(string nombreCompleto, string email, string rol)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(rol))
            {
                TempData["ErrorMessage"] = "Sesión de registro expirada o inválida.";
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                TempData["PendingEmail"] = email;
                TempData["PendingRol"] = rol;
                TempData["ErrorMessage"] = "El nombre completo es obligatorio.";
                return RedirectToAction("Register");
            }

            var usuario = new Usuario
            {
                CorreoUta = email,
                NombreCompleto = nombreCompleto.Trim(),
                Rol = rol,
                ContadorInasistencias = 0,
                EstaBloqueado = false
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioNombre", usuario.NombreCompleto);

            TempData["SuccessMessage"] = $"¡Bienvenido {usuario.NombreCompleto}! Tu cuenta ha sido registrada con éxito.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}