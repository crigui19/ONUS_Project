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

# Assicura che la cartella per le foto profilo caricate esista ed abbia i permessi di scrittura
RUN mkdir -p /app/wwwroot/Img && chmod -R 775 /app/wwwroot/Img

ENTRYPOINT ["dotnet", "Test_ONUS.dll"]