# Establecer la imagen base de .NET SDK
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Usar el SDK de .NET para construir la aplicación
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["UniversidadDB/UniversidadDB.csproj", "UniversidadDB/"]
RUN dotnet restore "UniversidadDB/UniversidadDB.csproj"
COPY . .
WORKDIR "/src/UniversidadDB"
RUN dotnet build "UniversidadDB.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UniversidadDB.csproj" -c Release -o /app/publish

# Crear la imagen final que se ejecutará en el contenedor
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UniversidadDB.dll"]
