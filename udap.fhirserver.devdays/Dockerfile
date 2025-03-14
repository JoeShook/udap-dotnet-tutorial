# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
COPY ./CertificateStore/DevDaysCA_1.crt /etc/ssl/certs
RUN update-ca-certificates
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["udap.fhirserver.devdays/udap.fhirserver.devdays.csproj", "udap.fhirserver.devdays/"]
RUN dotnet restore "./udap.fhirserver.devdays/udap.fhirserver.devdays.csproj"
COPY . .
WORKDIR "/src/udap.fhirserver.devdays"
RUN dotnet build "./udap.fhirserver.devdays.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./udap.fhirserver.devdays.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["udap.fhirserver.devdays/DemoFileSystemService", "./DemoFileSystemService"]
RUN chown -R $APP_UID:0 /app/DemoFileSystemService && chmod -R u+w /app/DemoFileSystemService
USER $APP_UID
ENTRYPOINT ["dotnet", "udap.fhirserver.devdays.dll"]