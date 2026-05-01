# ETAPA 1: Construcción (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1. Copiar los archivos .csproj con los nombres EXACTOS (PascalCase)
COPY ["Siged.Api/Siged.Api.csproj", "Siged.Api/"]
COPY ["Siged.Application/Siged.Application.csproj", "Siged.Application/"]
COPY ["Siged.Domain/Siged.Domain.csproj", "Siged.Domain/"]
COPY ["Siged.Infrastructure/Siged.Infrastructure.csproj", "Siged.Infrastructure/"]

# 2. Restaurar dependencias
RUN dotnet restore "Siged.Api/Siged.Api.csproj"

# 3. Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/Siged.Api"
RUN dotnet build "Siged.Api.csproj" -c Release -o /app/build

# ETAPA 2: Publicación
FROM build AS publish
RUN dotnet publish "Siged.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA 3: Imagen Final (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Puerto estándar de .NET 8 para contenedores en Render
ENV ASPNETCORE_URLS=http://+:8080
# Sin esto, GetCultureInfo("es-PE") falla (CultureNotFoundException) y el PDF del acta devuelve 500.
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080

ENTRYPOINT ["dotnet", "Siged.Api.dll"]