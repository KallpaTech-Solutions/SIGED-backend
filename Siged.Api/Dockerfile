# ETAPA 1: Construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos de proyecto y restaurar dependencias
# (Ajusta los nombres si tus .csproj se llaman diferente)
COPY ["src/SIGED.API/SIGED.API.csproj", "src/SIGED.API/"]
COPY ["src/SIGED.Application/SIGED.Application.csproj", "src/SIGED.Application/"]
COPY ["src/SIGED.Domain/SIGED.Domain.csproj", "src/SIGED.Domain/"]
COPY ["src/SIGED.Infrastructure/SIGED.Infrastructure.csproj", "src/SIGED.Infrastructure/"]

RUN dotnet restore "src/SIGED.API/SIGED.API.csproj"

# Copiar todo el código y compilar
COPY . .
WORKDIR "/app/src/SIGED.API"
RUN dotnet build "SIGED.API.csproj" -c Release -o /app/build

# ETAPA 2: Publicación
FROM build AS publish
RUN dotnet publish "SIGED.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA 3: Imagen Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SIGED.API.dll"]