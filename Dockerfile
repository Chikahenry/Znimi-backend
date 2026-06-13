# ─────────────────────────────────────────────
# Stage 1: Build
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache friendly)
COPY LoanApplication.sln ./
COPY LoanApplication/LoanApplication.csproj LoanApplication/

# Restore dependencies
RUN dotnet restore LoanApplication/LoanApplication.csproj

# Copy the rest of the source code
COPY . .

# Build and publish in Release mode
RUN dotnet publish LoanApplication/LoanApplication.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─────────────────────────────────────────────
# Stage 2: Runtime
# ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published output from build stage
COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app
USER appuser

# Railway injects PORT; fall back to 8080 for local Docker runs
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "LoanApplication.dll"]
