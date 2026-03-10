using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siged.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dependencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Siglas = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Abreviatura = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Facultad"),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Lema = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", maxLength: 500, nullable: true),
                    PortadaUrl = table.Column<string>(type: "text", maxLength: 500, nullable: true),
                    ColorRepresentativo = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    IdPermiso = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Categoria = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.IdPermiso);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Personas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DNI = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "text", nullable: false),
                    Apellidos = table.Column<string>(type: "text", nullable: false),
                    FotoPath = table.Column<string>(type: "text", nullable: true),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Correo = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    DependenciaId = table.Column<int>(type: "integer", nullable: true),
                    EsPersonalInterno = table.Column<bool>(type: "boolean", nullable: true),
                    Cargo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Oficina = table.Column<string>(type: "text", nullable: true),
                    CodigoEstudiante = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EstaMatriculado = table.Column<bool>(type: "boolean", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personas_Dependencias_DependenciaId",
                        column: x => x.DependenciaId,
                        principalTable: "Dependencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    PermisosIdPermiso = table.Column<string>(type: "text", nullable: false),
                    RolesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => new { x.PermisosIdPermiso, x.RolesId });
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermisosIdPermiso",
                        column: x => x.PermisosIdPermiso,
                        principalTable: "Permisos",
                        principalColumn: "IdPermiso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    EstaActivo = table.Column<bool>(type: "boolean", nullable: false),
                    RolId = table.Column<int>(type: "integer", nullable: false),
                    PersonaId = table.Column<int>(type: "integer", nullable: false),
                    OrganizacionId = table.Column<int>(type: "integer", nullable: true),
                    CreadoPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    RequiereCambioPassword = table.Column<bool>(type: "boolean", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Organizaciones_OrganizacionId",
                        column: x => x.OrganizacionId,
                        principalTable: "Organizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuarios_Personas_PersonaId",
                        column: x => x.PersonaId,
                        principalTable: "Personas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Usuarios_Usuarios_CreadoPorUsuarioId",
                        column: x => x.CreadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosPermisos",
                columns: table => new
                {
                    PermisosEspecialesIdPermiso = table.Column<string>(type: "text", nullable: false),
                    UsuariosId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosPermisos", x => new { x.PermisosEspecialesIdPermiso, x.UsuariosId });
                    table.ForeignKey(
                        name: "FK_UsuariosPermisos_Permisos_PermisosEspecialesIdPermiso",
                        column: x => x.PermisosEspecialesIdPermiso,
                        principalTable: "Permisos",
                        principalColumn: "IdPermiso",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosPermisos_Usuarios_UsuariosId",
                        column: x => x.UsuariosId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Dependencias",
                columns: new[] { "Id", "Nombre", "Siglas" },
                values: new object[,]
                {
                    { 1, "Oficina de Tecnología de Información", "OTI" },
                    { 2, "Rectorado Central", "RECT" },
                    { 3, "Bienestar Universitario", "DBU" },
                    { 4, "Dirección de Admisión", "ADM" }
                });

            migrationBuilder.InsertData(
                table: "Organizaciones",
                columns: new[] { "Id", "Abreviatura", "ColorRepresentativo", "Descripcion", "EstaActivo", "FechaCreacion", "Lema", "LogoUrl", "Nombre", "PortadaUrl", "Tipo" },
                values: new object[,]
                {
                    { 1, "FIIS", "#0284c7", null, true, null, null, null, "Facultad de Ingeniería en Informática y Sistemas", null, "Facultad" },
                    { 2, "FZ", "#d97706", null, true, null, null, null, "Facultad de Zootecnia", null, "Facultad" },
                    { 3, "FA", "#16a34a", null, true, null, null, null, "Facultad de Agronomía", null, "Facultad" },
                    { 4, "FCEA", "#9333ea", null, true, null, null, null, "Facultad de Ciencias Económicas y Administrativas", null, "Facultad" }
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "IdPermiso", "Categoria", "Descripcion" },
                values: new object[,]
                {
                    { "comp.tourn.manage", "COMPETENCIAS", "Gestionar fechas y torneos" },
                    { "comp.tourn.view", "COMPETENCIAS", "Ver torneos y resultados" },
                    { "core.org.manage", "CORE", "Administrar facultades" },
                    { "core.org.view", "CORE", "Ver facultades y sedes" },
                    { "security.role.manage", "SEGURIDAD", "Gestionar roles y permisos" },
                    { "security.user.manage", "SEGURIDAD", "Crear y editar usuarios" },
                    { "security.user.view", "SEGURIDAD", "Ver lista de usuarios" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Control total del sistema (OTI)", "SuperAdmin" },
                    { 2, "Personal administrativo de oficinas centrales", "Administrador" },
                    { 3, "Docente o administrativo encargado de facultad", "Encargado" },
                    { 4, "Alumno regular de la UNAS", "Estudiante" }
                });

            migrationBuilder.InsertData(
                table: "Personas",
                columns: new[] { "Id", "Apellidos", "Correo", "DNI", "DependenciaId", "Discriminator", "EsPersonalInterno", "FotoPath", "Nombres", "Telefono" },
                values: new object[,]
                {
                    { 1, "Admin", null, "76063362", 1, "Administrador", true, null, "Benjamín", null },
                    { 2, "Apoyo", null, "12345678", 2, "Administrador", true, null, "Juan", null }
                });

            migrationBuilder.InsertData(
                table: "RolPermisos",
                columns: new[] { "PermisosIdPermiso", "RolesId" },
                values: new object[,]
                {
                    { "comp.tourn.manage", 1 },
                    { "comp.tourn.manage", 3 },
                    { "comp.tourn.view", 1 },
                    { "comp.tourn.view", 2 },
                    { "comp.tourn.view", 3 },
                    { "comp.tourn.view", 4 },
                    { "core.org.manage", 1 },
                    { "core.org.view", 1 },
                    { "core.org.view", 2 },
                    { "core.org.view", 3 },
                    { "core.org.view", 4 },
                    { "security.role.manage", 1 },
                    { "security.user.manage", 1 },
                    { "security.user.manage", 2 },
                    { "security.user.view", 1 },
                    { "security.user.view", 2 }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "CreadoPorUsuarioId", "EstaActivo", "FechaRegistro", "OrganizacionId", "PasswordHash", "PersonaId", "RequiereCambioPassword", "RolId", "Username" },
                values: new object[,]
                {
                    { 1, null, true, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe", 1, false, 1, "admin_benjamin" },
                    { 2, null, true, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Utc), null, "$2a$12$eL7jdgl1iOF508ePe1ScyOrYqdzdDedn1yo5WuGrj1E1.ZovUk0Xe", 2, true, 2, "apoyo_juan" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizaciones_Abreviatura",
                table: "Organizaciones",
                column: "Abreviatura",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizaciones_Nombre",
                table: "Organizaciones",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personas_DependenciaId",
                table: "Personas",
                column: "DependenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Personas_DNI",
                table: "Personas",
                column: "DNI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolesId",
                table: "RolPermisos",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CreadoPorUsuarioId",
                table: "Usuarios",
                column: "CreadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_OrganizacionId",
                table: "Usuarios",
                column: "OrganizacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_PersonaId",
                table: "Usuarios",
                column: "PersonaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosPermisos_UsuariosId",
                table: "UsuariosPermisos",
                column: "UsuariosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "UsuariosPermisos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Organizaciones");

            migrationBuilder.DropTable(
                name: "Personas");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Dependencias");
        }
    }
}
