
# BUILD STAGE
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy backend source
COPY backend/SchoolAdmission.Api/ backend/SchoolAdmission.Api/

WORKDIR /src/backend/SchoolAdmission.Api

# Restore packages and publish
# Publish performs a fresh NuGet restore
RUN dotnet publish SchoolAdmission.Api.csproj \
    -c Release \
    -o /app/publish

# RUNTIME STAGE
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
