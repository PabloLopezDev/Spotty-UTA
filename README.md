# 🟢 Spotty UTA — Sistema de Gestión de Boxes de Estudio

> **Plataforma web para la administración y reserva de boxes de estudio en la Biblioteca Central de la Universidad de Tarapacá, sede Saucache.**

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-purple?logo=dotnet)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-blue?logo=nuget)
![SignalR](https://img.shields.io/badge/SignalR-Tiempo%20Real-green?logo=signalr)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red?logo=microsoftsqlserver)
![License](https://img.shields.io/badge/Licencia-MIT-yellow)

---

## 📋 Descripción

Spotty UTA es una plataforma web que optimiza y automatiza la asignación de boxes de estudio en la biblioteca universitaria. El sistema reemplaza el proceso manual de solicitudes con una **matriz interactiva en tiempo real** que permite a estudiantes reservar salas y a administradores gestionar la operación desde un dashboard centralizado.

---

## ⚡ Tecnologías

| Componente | Tecnología |
|---|---|
| **Framework Backend** | ASP.NET Core 10 (MVC) |
| **ORM** | Entity Framework Core (Database-First) |
| **Base de Datos** | SQL Server |
| **Tiempo Real** | SignalR (WebSockets) |
| **Frontend** | Razor Views + Bootstrap 5 + CSS personalizado |
| **Autenticación** | Sesiones de servidor (Cookie-based) |

---

## 🏗️ Arquitectura

```
SpottyUTA/
├── Controllers/          # Controladores MVC y API REST
│   ├── AdministradorController.cs
│   ├── AuthController.cs
│   ├── ReservasController.cs
│   ├── SalasApiController.cs
│   └── HomeController.cs
├── Data/                 # Contexto EF Core
│   └── SpottyUtaContext.cs
├── Helpers/              # Utilidades del sistema
│   └── SimulationTime.cs
├── Hubs/                 # Hub de SignalR
│   └── SalasHub.cs
├── Models/               # Entidades del dominio
│   ├── Usuario.cs
│   ├── Sala.cs
│   └── Reserva.cs
├── Services/             # Lógica de negocio
│   ├── ISalasService.cs / SalasService.cs
│   ├── IReservasService.cs / ReservasService.cs
│   └── SalasStateBroadcaster.cs
├── Views/                # Vistas Razor (.cshtml)
├── wwwroot/              # Archivos estáticos (CSS, JS)
├── Program.cs            # Punto de entrada
└── ARCHITECTURE_DOC.md   # Documentación técnica completa
```

> 📘 Para documentación técnica detallada con diagramas de arquitectura, ER y flujos, consultar [ARCHITECTURE_DOC.md](SpottyUTA/ARCHITECTURE_DOC.md).

---

## 🎯 Funcionalidades Principales

### Panel de Estudiante
- 🟢 Visualización en tiempo real del estado de todos los boxes por pisos
- 📅 Reserva de boxes con selección de horario
- ⏱️ Duración máxima de 2 horas por bloque
- 📱 Interfaz responsiva (Mobile-First)

### Panel de Administrador
- 📊 **Dashboard** con KPIs en tiempo real (disponibles, reservadas, ocupadas)
- 🏢 **Gestión de Salas** — Agregar, inhabilitar/activar y eliminar boxes
- 📋 **Reservas Activas** — Tabla filtrable + Vista de ocupación temporal (Gantt)
- 👥 **Usuarios** — Bloqueo/desbloqueo manual y reset de inasistencias
- 🔔 **Notificaciones** sincronizadas entre vistas

### Motor de Reglas de Negocio
- ⏰ Horarios adaptativos: L-V (08:00-21:00), Sáb (09:00-13:00, solo 1° piso), Dom (cerrado)
- 🚫 Protección contra reservas duplicadas y solapamientos
- ⚠️ Tolerancia de 20 minutos para confirmar asistencia
- 🔒 Bloqueo automático al acumular 3 inasistencias
- 📐 Duración mínima de 30 minutos por reserva

### Comunicación en Tiempo Real
- 🔄 **SignalR** actualiza todas las pantallas activas sin recargar la página
- 🕐 **SalasStateBroadcaster** limpia automáticamente salas expiradas cada 15 segundos

---

## 🚀 Instalación y Ejecución

### Prerrequisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server)

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/PabloLopezDev/Spotty-UTA.git
cd Spotty-UTA/SpottyUTA

# 2. Configurar la cadena de conexión en appsettings.json
# Editar "DefaultConnection" con tus credenciales de SQL Server

# 3. Restaurar dependencias y compilar
dotnet restore
dotnet build

# 4. Ejecutar la aplicación
dotnet run
```

La aplicación estará disponible en `https://localhost:5001` o `http://localhost:5000`.

---

## 👥 Contribuidores

<table>
  <tr>
    <td align="center">
      <a href="https://github.com/PabloLopezDev">
        <img src="https://github.com/PabloLopezDev.png" width="100px;" alt="PabloLopezDev"/>
        <br /><sub><b>Pablo López</b></sub>
      </a>
    </td>
    <td align="center">
      <a href="https://github.com/icuevas1014">
        <img src="https://github.com/icuevas1014.png" width="100px;" alt="icuevas1014"/>
        <br /><sub><b>Ignacio Cuevas</b></sub>
      </a>
    </td>
    <td align="center">
      <a href="https://github.com/Deleriusprohd">
        <img src="https://github.com/Deleriusprohd.png" width="100px;" alt="Deleriusprohd"/>
        <br /><sub><b>Deleriusprohd</b></sub>
      </a>
    </td>
  </tr>
</table>

---

## 📄 Licencia

Este proyecto está bajo la licencia **MIT**. Consultar el archivo [LICENSE](LICENSE) para más detalles.

---

> *Proyecto académico desarrollado para la Universidad de Tarapacá — Arica, Chile.*
