
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY backend/SchoolAdmission.Api/SchoolAdmission.Api.csproj backend/SchoolAdmission.Api/

RUN dotnet restore backend/SchoolAdmission.Api/SchoolAdmission.Api.csproj

COPY backend/SchoolAdmission.Api/ backend/SchoolAdmission.Api/

WORKDIR /src/backend/SchoolAdmission.Api

RUN dotnet publish SchoolAdmission.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
