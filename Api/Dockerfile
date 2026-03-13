ARG APPLICATION_PORT=4200

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APPLICATION_PORT

USER $APP_UID
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:${ORDER_SERVICE_PORT}
EXPOSE ${APPLICATION_PORT}

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["Api/Api.csproj", "Api/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["Application/Application.csproj", "Application/"]

RUN dotnet restore "Api/Api.csproj"
COPY . .
WORKDIR "/src/Api"
RUN dotnet build "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]
