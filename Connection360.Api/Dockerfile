# ETAPA 1: Construcción (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copiar TODOS los archivos de proyecto (.csproj) manteniendo la estructura de carpetas
COPY ["Connection360.Api/Connection360.Api.csproj", "Connection360.Api/"]
COPY ["Connection360.Application/Connection360.Application.csproj", "Connection360.Application/"]
COPY ["Connection360.Domain/Connection360.Domain.csproj", "Connection360.Domain/"]
COPY ["Connection360.Infrastructure/Connection360.Infrastructure.csproj", "Connection360.Infrastructure/"]

# Copiar archivos de proyecto y restaurar dependencias
RUN dotnet restore "Connection360.Api/Connection360.Api.csproj"

# Copiar todo el código y publicar la API
COPY . .
WORKDIR "/src/Connection360.Api"
RUN dotnet publish "Connection360.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA 2: Ejecución (Runtime ligero)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app

# 1. Copiamos el certificado directamente en /app (Donde estás parado gracias al WORKDIR)
COPY ["Connection360.Api/aspnetapp.pfx", "./"]

# Copiar los archivos publicados (incluye automáticamente tus appsettings.json)
COPY --from=build /app/publish .

# Forzar a Kestrel a escuchar en el puerto solicitado bajo HTTPS
ENV ASPNETCORE_URLS=https://+:44369
EXPOSE 44369

ENTRYPOINT ["dotnet", "Connection360.Api.dll"]