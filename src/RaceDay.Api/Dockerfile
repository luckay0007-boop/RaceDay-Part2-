FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/RaceDay.Api/RaceDay.Api.csproj", "src/RaceDay.Api/"]
RUN dotnet restore "src/RaceDay.Api/RaceDay.Api.csproj"

COPY ["src/RaceDay.Api/", "src/RaceDay.Api/"]
WORKDIR "/src/src/RaceDay.Api"
RUN dotnet publish "RaceDay.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS=http://+:52070

EXPOSE 52070
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]

