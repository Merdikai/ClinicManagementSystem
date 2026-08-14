FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ClinicManagementSystem.API/ClinicManagementSystem.API.csproj", "src/ClinicManagementSystem.API/"]
COPY ["src/ClinicManagementSystem.Application/ClinicManagementSystem.Application.csproj", "src/ClinicManagementSystem.Application/"]
COPY ["src/ClinicManagementSystem.Domain/ClinicManagementSystem.Domain.csproj", "src/ClinicManagementSystem.Domain/"]
COPY ["src/ClinicManagementSystem.Infrastructure/ClinicManagementSystem.Infrastructure.csproj", "src/ClinicManagementSystem.Infrastructure/"]
RUN dotnet restore "src/ClinicManagementSystem.API/ClinicManagementSystem.API.csproj"
COPY . .
WORKDIR "/src/src/ClinicManagementSystem.API"
RUN dotnet build "ClinicManagementSystem.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClinicManagementSystem.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClinicManagementSystem.API.dll"]
