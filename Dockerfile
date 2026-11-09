# ==============================================================================
# Stage 1: Build & Publish the ASP.NET Core MVC Web Application (RaceDay.Web)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first to leverage Docker layer caching during restore
COPY ["src/RaceDay.Web/RaceDay.Web.csproj", "src/RaceDay.Web/"]
RUN dotnet restore "src/RaceDay.Web/RaceDay.Web.csproj"

# Copy application source and publish optimised release artifacts
COPY ["src/RaceDay.Web/", "src/RaceDay.Web/"]
WORKDIR "/src/src/RaceDay.Web"
RUN dotnet publish "RaceDay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# Stage 2: Minimal Production Runtime Image
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Configure ASP.NET Core to listen on container port 8080 and point to host API by default
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    API_BASE_URL=http://host.docker.internal:52070

EXPOSE 8080

# Run as non-root container user (best practice)
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]

