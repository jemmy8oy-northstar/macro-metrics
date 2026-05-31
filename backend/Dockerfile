# Stage 1: Build .NET app
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

# Copy solution and project files first for layer-caching of dotnet restore
COPY MacroMetrics.slnx .
COPY MacroMetrics.WebApi/*.csproj ./MacroMetrics.WebApi/
COPY MacroMetrics.Abstractions/*.csproj ./MacroMetrics.Abstractions/
COPY MacroMetrics.DataModels/*.csproj ./MacroMetrics.DataModels/
COPY MacroMetrics.DomainModels/*.csproj ./MacroMetrics.DomainModels/
COPY MacroMetrics.Services/*.csproj ./MacroMetrics.Services/
COPY MacroMetrics.EntityModels/*.csproj ./MacroMetrics.EntityModels/
COPY MacroMetrics.Database/*.csproj ./MacroMetrics.Database/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish
RUN dotnet publish MacroMetrics.WebApi/MacroMetrics.WebApi.csproj -c Release -o /app/publish

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (default ASP.NET Core port is 8080 in .NET 8+)
EXPOSE 8080

ENTRYPOINT ["dotnet", "MacroMetrics.WebApi.dll"]
