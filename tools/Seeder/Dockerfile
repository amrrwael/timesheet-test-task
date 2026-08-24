FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Timesheet.Api/Timesheet.Api.csproj backend/Timesheet.Api/
COPY tools/Seeder/Seeder.csproj tools/Seeder/
RUN dotnet restore tools/Seeder/Seeder.csproj
COPY backend/Timesheet.Api/ backend/Timesheet.Api/
COPY tools/Seeder/ tools/Seeder/
RUN dotnet publish tools/Seeder -c Release -o /app

# проект ссылается на Web-SDK (Timesheet.Api), поэтому нужен aspnet-рантайм, а не runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Seeder.dll"]
