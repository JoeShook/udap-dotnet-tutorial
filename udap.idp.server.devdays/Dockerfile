# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
COPY ../CertificateStore/DevDaysCA_1.crt /etc/ssl/certs
RUN update-ca-certificates
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["udap.idp.server.devdays/udap.idp.server.devdays.csproj", "udap.idp.server.devdays/"]
RUN dotnet restore "./udap.idp.server.devdays/udap.idp.server.devdays.csproj"
COPY . .
WORKDIR "/src/udap.idp.server.devdays"
RUN dotnet build "./udap.idp.server.devdays.csproj" -c $BUILD_CONFIGURATION -o /app/build


# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./udap.idp.server.devdays.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "udap.idp.server.devdays.dll"]