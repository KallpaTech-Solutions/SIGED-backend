# 🏆 SIGED - Sistema de Gestión Deportiva UNAS

Bienvenido al Backend del **SIGED**. Este proyecto utiliza **Clean Architecture** y **.NET 8** con un sistema de seguridad **RBAC** (Control de Acceso Basado en Roles).

## 🚀 Configuración Inicial para el Equipo

Para que el proyecto corra en tu máquina local, sigue estos pasos:

1. **Clonar el repositorio:**
   `git clone https://github.com/KallpaTech-Solutions/SIGED-backend.git`

2. **Configurar el entorno:**
   - Ve a la carpeta `Siged.Api`.
   - Copia el archivo `appsettings.json` y renómbralo a `appsettings.Development.json`.
   - Cambia los valores en **ConnectionStrings** con tu usuario y contraseña de PostgreSQL local (Puerto sugerido: **5433**).

3. **Base de Datos:**
   Abre la **Consola del Administrador de Paquetes** en Visual Studio y ejecuta:
   `Update-Database`

4. **Ejecutar:**
   Selecciona `Siged.Api` como proyecto de inicio y presiona **F5**. ¡Swagger se abrirá automáticamente!

## 🏗️ Estructura del Proyecto
- **Siged.Domain:** Entidades y reglas de negocio puras.
- **Siged.Application:** Casos de uso, DTOs e Interfaces.
- **Siged.Infrastructure:** Acceso a datos (EF Core) y servicios externos.
- **Siged.Api:** Endpoints, Seguridad JWT y Controllers.
