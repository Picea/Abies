FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Abies.Conduit.Apphost/Abies.Conduit.Apphost.csproj", "Abies.Conduit.Apphost/"]
COPY ["Abies.Conduit/Abies.Conduit.csproj", "Abies.Conduit/"]
COPY ["Abies/Abies.csproj", "Abies/"]
COPY ["Conduit.Api/Conduit.Api.csproj", "Conduit.Api/"]
RUN dotnet restore "Abies.Conduit.Apphost/Abies.Conduit.Apphost.csproj"
COPY . .
WORKDIR "/src/Abies.Conduit.Apphost"
RUN dotnet build "./Abies.Conduit.Apphost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Abies.Conduit.Apphost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Abies.Conduit.Apphost.dll"]
