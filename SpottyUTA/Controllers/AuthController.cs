using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpottyUTA.Data;
using SpottyUTA.Models;
using System.Threading.Tasks;

namespace SpottyUTA.Controllers
{
    /// <summary>
    /// Controlador de autenticación y registro de usuarios.
    /// Gestiona el inicio de sesión, registro de nuevas cuentas y cierre de sesión.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly SpottyUtaContext _context;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AuthController"/>.
        /// </summary>
        /// <param name="context">Contexto de acceso a datos de Entity Framework Core.</param>
        public AuthController(SpottyUtaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Muestra el formulario de inicio de sesión. Si el usuario ya tiene una sesión activa,
        /// redirige automáticamente a la página principal.
        /// </summary>
        /// <returns>Vista de login o redirección a Home/Index.</returns>
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UsuarioId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// Procesa la solicitud de inicio de sesión validando el correo institucional UTA.
        /// Determina el rol según el dominio del correo y crea la sesión del usuario.
        /// </summary>
        /// <param name="correo">Correo institucional ingresado por el usuario.</param>
        /// <returns>Redirección a Home/Index si es exitoso, o de vuelta al Login con mensaje de error.</returns>
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

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Muestra el formulario de registro para usuarios nuevos que no existen en la base de datos.
        /// Requiere datos pendientes almacenados en TempData (correo y rol).
        /// </summary>
        /// <returns>Vista de registro o redirección al login si los datos pendientes expiraron.</returns>
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

        /// <summary>
        /// Completa el registro de un nuevo usuario, creando su cuenta en la base de datos
        /// e iniciando sesión automáticamente.
        /// </summary>
        /// <param name="nombreCompleto">Nombre completo del nuevo usuario.</param>
        /// <param name="email">Correo institucional del usuario.</param>
        /// <param name="rol">Rol asignado ("Estudiante" o "Administrador").</param>
        /// <returns>Redirección a Home/Index con mensaje de bienvenida.</returns>
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

        /// <summary>
        /// Destruye la sesión del usuario actual y redirige al formulario de inicio de sesión.
        /// </summary>
        /// <returns>Redirección a la vista de Login.</returns>
        [HttpGet]
        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}