FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Picea.Abies.Conduit.AppHost/Picea.Abies.Conduit.AppHost.csproj", "Picea.Abies.Conduit.AppHost/"]
COPY ["Picea.Abies.Conduit/Picea.Abies.Conduit.csproj", "Picea.Abies.Conduit/"]
COPY ["Picea.Abies/Picea.Abies.csproj", "Picea.Abies/"]
COPY ["Picea.Abies.Conduit.Api/Picea.Abies.Conduit.Api.csproj", "Picea.Abies.Conduit.Api/"]
RUN dotnet restore "Picea.Abies.Conduit.AppHost/Picea.Abies.Conduit.AppHost.csproj"
RUN dotnet workload restore "Picea.Abies.Conduit.AppHost/Picea.Abies.Conduit.AppHost.csproj"
COPY . .
WORKDIR "/src/Picea.Abies.Conduit.AppHost"
RUN dotnet build "./Picea.Abies.Conduit.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Picea.Abies.Conduit.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Picea.Abies.Conduit.AppHost.dll"]