# Spotty UTA - Sistema de Gestión de Boxes de Estudio (Saucache)

Spotty UTA es una plataforma web diseñada para optimizar y automatizar la administración y asignación de los boxes de estudio en la biblioteca de la Universidad de Tarapacá, sede Saucache. El sistema soluciona los problemas de ineficiencia de las solicitudes manuales tradicionales, proporcionando una matriz interactiva y reactiva en tiempo real.

## Características del Proyecto (Sprint 1)

### 1. Interfaz de Usuario Adaptativa (UI/UX)
* **Matriz de Control para Escritorio:** Grilla estructurada y optimizada que permite visualizar de un vistazo el estado de todos los boxes distribuidos por pisos.
* **Diseño Mobile-First:** Interfaz totalmente responsiva pensada para smartphones, equipada con selectores rápidos de piso y tarjetas adaptativas para facilitar el uso de los estudiantes en el campus.

### 2. Motor de Reglas de Negocio y Restricciones
* **Duración Máxima de Ocupación:** Asignaciones automáticas con un límite estricto de hasta 2 horas continuas por solicitud.
* **Recorte Horario Inteligente:** Algoritmo que detecta reservas futuras y ajusta automáticamente la hora de término del bloque actual para evitar solapamientos.
* **Protección de Tiempos Pasados:** Restricción en la interfaz que impide realizar solicitudes extemporáneas (con un margen de cortesía técnica de 5 minutos).
* **Margen de Utilidad de Estudio:** Exigencia de un mínimo de 30 minutos disponibles antes del cierre de las dependencias.
* **Horarios Adaptativos de la Institución:** 
  * Lunes a Viernes: Cierre automatizado a las 21:00 hrs.
  * Sábados: Operación restringida de 09:00 a 13:00 hrs, inhabilitando de forma automática los pisos 2, 3 y 4 (sólo primer piso disponible).
  * Domingos: Bloqueo perentorio de la matriz por cierre del recinto.
* **Tolerancia de Asistencia:** Margen estricto de 15 minutos de espera para que el estudiante confirme su llegada en el mesón antes de liberar el espacio.

### 3. Arquitectura Tecnológica y Tiempo Real
* **Backend:** Desarrollado en **.NET 10** utilizando ASP.NET Core MVC.
* **Persistencia de Datos:** Mapeo e integración de base de datos SQL Server mediante **Entity Framework Core (Database-First)**.
* **Sincronización Reactiva:** Implementación de **SignalR** y Fetch API para actualizar los estados de los boxes (Disponible, Reservado, Ocupado, Inactiva) de forma simultánea en todas las pantallas activas, sin requerir recargas manuales (F5).

---
*Desarrollado como proyecto académico para la Universidad de Tarapacá.*
