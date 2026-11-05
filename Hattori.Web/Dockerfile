FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Directory.Build.props changes how every project builds, so it is copied first.
COPY ["Directory.Build.props", "./"]
COPY ["Hattori.Core/Hattori.Core.csproj", "Hattori.Core/"]
COPY ["Hattori.Persistence/Hattori.Persistence.csproj", "Hattori.Persistence/"]
COPY ["Hattori.Web/Hattori.Web.csproj", "Hattori.Web/"]
RUN dotnet restore "Hattori.Web/Hattori.Web.csproj"
COPY . .
WORKDIR "/src/Hattori.Web"
RUN dotnet publish "./Hattori.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Hattori.Web.dll"]
