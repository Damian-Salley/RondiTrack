# Build the application.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to cache package restoration.
COPY RondiTrack.csproj ./
COPY RondiTrack.Domain/RondiTrack.Domain.csproj RondiTrack.Domain/
COPY RondiTrack.Infrastructure/RondiTrack.Infrastructure.csproj RondiTrack.Infrastructure/

RUN dotnet restore RondiTrack.csproj

# Copy source and publish.
COPY . .
RUN dotnet publish RondiTrack.csproj -c Release -o /app/publish --no-restore

# Run the application.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Use the built-in non-root user.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RondiTrack.dll"]