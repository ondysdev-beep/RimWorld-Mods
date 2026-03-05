FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY src/RimVerse.Shared/RimVerse.Shared.csproj RimVerse.Shared/
COPY src/RimVerse.Server/RimVerse.Server.csproj RimVerse.Server/
RUN dotnet restore RimVerse.Server/RimVerse.Server.csproj

COPY src/RimVerse.Shared/ RimVerse.Shared/
COPY src/RimVerse.Server/ RimVerse.Server/
RUN dotnet publish RimVerse.Server/RimVerse.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RimVerse.Server.dll"]
