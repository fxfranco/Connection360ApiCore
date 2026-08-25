# ETAPA 1: Construcción (SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["Connection360.ApiGateway.csproj", "./"]
RUN dotnet restore "./Connection360.ApiGateway.csproj"

# Copiar todo el código y publicar la API
COPY . .
RUN dotnet publish "Connection360.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAPA 2: Ejecución (Runtime ligero)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app

# 1. Copiamos el certificado directamente en /app (Donde estás parado gracias al WORKDIR)
COPY ["aspnetapp.pfx", "./"]

# Copiar los archivos publicados (incluye automáticamente tus appsettings.json)
COPY --from=build /app/publish .

# Forzar a Kestrel a escuchar en el puerto solicitado bajo HTTPS
ENV ASPNETCORE_URLS=https://+:44368
EXPOSE 44368

ENTRYPOINT ["dotnet", "Connection360.ApiGateway.dll"]