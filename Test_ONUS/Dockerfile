# Usa l'immagine ufficiale di .NET 8 per far girare l'app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Usa l'SDK per compilare il codice
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Test_ONUS.csproj", "."]
RUN dotnet restore "./Test_ONUS.csproj"
COPY . .
RUN dotnet build "Test_ONUS.csproj" -c Release -o /app/build

# Pubblica i file ottimizzati
FROM build AS publish
RUN dotnet publish "Test_ONUS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Fase finale: prepara il server
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Crea la cartella /data per SQLite e dai i permessi massimi di scrittura
USER root
RUN mkdir -p /data && chmod 777 /data

ENTRYPOINT ["dotnet", "Test_ONUS.dll"]