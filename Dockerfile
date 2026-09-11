FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ARG PORT=8080
ARG BUILD_ID=None
ENV BUILD_ID=$BUILD_ID
ENV ASPNETCORE_HTTP_PORTS=$PORT
EXPOSE $PORT

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/OpenKoqis.Api/OpenKoqis.Api.csproj", "OpenKoqis.Api/"]
COPY ["src/OpenKoqis.Infrastructure/OpenKoqis.Infrastructure.csproj", "OpenKoqis.Infrastructure/"]
COPY ["src/OpenKoqis.Application/OpenKoqis.Application.csproj", "OpenKoqis.Application/"]
COPY ["src/OpenKoqis.Domain/OpenKoqis.Domain.csproj", "OpenKoqis.Domain/"]
RUN dotnet restore "OpenKoqis.Api/OpenKoqis.Api.csproj"
COPY src/ .
WORKDIR "/src/OpenKoqis.Api"
RUN dotnet build "OpenKoqis.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OpenKoqis.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY global-bundle.pem .
ARG ENVIRONMENT=Production
ENV ASPNETCORE_ENVIRONMENT=$ENVIRONMENT

ENTRYPOINT ["dotnet", "OpenKoqis.Api.dll"]
