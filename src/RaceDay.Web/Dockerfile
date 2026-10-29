FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RaceDay.Web/RaceDay.Web.csproj", "src/RaceDay.Web/"]
RUN dotnet restore "src/RaceDay.Web/RaceDay.Web.csproj"

COPY ["src/RaceDay.Web/", "src/RaceDay.Web/"]
WORKDIR "/src/src/RaceDay.Web"
RUN dotnet publish "RaceDay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    API_BASE_URL=http://host.docker.internal:52070

EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]

