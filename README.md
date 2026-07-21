# Spotty UTA — Sistema de Gestión de Boxes de Estudio

**Plataforma web para la administración y reserva de boxes de estudio en la Biblioteca Central de la Universidad de Tarapacá, sede Saucache.**

---

## Descripción

Spotty UTA es una aplicación web desarrollada en ASP.NET Core que permite a los estudiantes de la Universidad de Tarapacá reservar boxes de estudio en la biblioteca central, y a los administradores gestionar dichas reservas en tiempo real. El sistema reemplaza el proceso manual de solicitudes con una matriz interactiva que refleja el estado de ocupación de cada sala de forma instantánea.

---

## Tecnologías Utilizadas

| Componente | Tecnología |
|---|---|
| Framework Backend | ASP.NET Core 10 (MVC) |
| ORM | Entity Framework Core (Database-First) |
| Base de Datos | SQL Server |
| Tiempo Real | SignalR (WebSockets) |
| Frontend | Razor Views, Bootstrap 5, CSS personalizado |
| Autenticación | Sesiones de servidor (Cookie-based) |

---

## Estructura del Proyecto

```
SpottyUTA/
├── Controllers/          # Controladores MVC y API REST
├── Data/                 # Contexto de Entity Framework Core
├── Helpers/              # Utilidades del sistema
├── Hubs/                 # Hub de SignalR
├── Models/               # Entidades del dominio
├── Services/             # Lógica de negocio e interfaces
├── Views/                # Vistas Razor (.cshtml)
├── wwwroot/              # Archivos estáticos (CSS, JS)
├── Program.cs            # Punto de entrada de la aplicación
└── ARCHITECTURE_DOC.md   # Documentación técnica detallada
```

Para mayor detalle sobre la arquitectura, diagramas de entidades y flujos del sistema, consultar [ARCHITECTURE_DOC.md](SpottyUTA/ARCHITECTURE_DOC.md).

---

## Funcionalidades

### Estudiantes
- Visualización en tiempo real del estado de todos los boxes organizados por pisos.
- Reserva de boxes con selección de horario (bloques de hasta 2 horas).
- Interfaz responsiva adaptada a dispositivos móviles.

### Administradores
- **Dashboard** con indicadores clave (KPIs) de disponibilidad, reservas y ocupación.
- **Gestión de Salas** — Creación, inhabilitación y eliminación de boxes.
- **Reservas Activas** — Tabla filtrable con vista de ocupación temporal tipo Gantt.
- **Gestión de Usuarios** — Bloqueo, desbloqueo y reinicio de contadores de inasistencia.
- **Notificaciones** sincronizadas entre todas las secciones del panel.

### Reglas de Negocio
- Horarios adaptativos: lunes a viernes (08:00–21:00), sábados (09:00–13:00, solo primer piso), domingos cerrado.
- Protección contra reservas duplicadas y solapamientos horarios.
- Tolerancia de 20 minutos para confirmar asistencia presencial.
- Bloqueo automático de cuenta al acumular 3 inasistencias.
- Duración mínima de 30 minutos por reserva.

### Comunicación en Tiempo Real
- SignalR actualiza todas las pantallas activas sin necesidad de recargar la página.
- Un servicio en segundo plano limpia automáticamente salas expiradas cada 15 segundos.

---

## Instalación y Ejecución

### Prerrequisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server)

### Pasos

```bash
# Clonar el repositorio
git clone https://github.com/PabloLopezDev/Spotty-UTA.git
cd Spotty-UTA/SpottyUTA

# Configurar la cadena de conexión en appsettings.json
# Editar "DefaultConnection" con las credenciales de SQL Server

# Restaurar dependencias y compilar
dotnet restore
dotnet build

# Ejecutar la aplicación
dotnet run
```

La aplicación estará disponible en `https://localhost:5001` o `http://localhost:5000`.

---

## Contribuidores

| | Nombre | GitHub |
|---|---|---|
| <img src="https://github.com/PabloLopezDev.png" width="40"/> | Pablo López | [@PabloLopezDev](https://github.com/PabloLopezDev) |
| <img src="https://github.com/icuevas1014.png" width="40"/> | Ignacio Cuevas | [@icuevas1014](https://github.com/icuevas1014) |
| <img src="https://github.com/Deleriusprohd.png" width="40"/> | Deleriusprohd | [@Deleriusprohd](https://github.com/Deleriusprohd) |

---

## Licencia

Este proyecto está bajo la licencia MIT. Consultar el archivo [LICENSE](LICENSE) para más detalles.

---

*Proyecto académico — Universidad de Tarapacá, Arica, Chile.*
