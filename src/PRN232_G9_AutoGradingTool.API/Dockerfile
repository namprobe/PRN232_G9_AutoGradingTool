# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj to leverage layer caching
COPY ["src/PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj", "PRN232_G9_AutoGradingTool.API/"]
COPY ["src/PRN232_G9_AutoGradingTool.Application/PRN232_G9_AutoGradingTool.Application.csproj", "PRN232_G9_AutoGradingTool.Application/"]
COPY ["src/PRN232_G9_AutoGradingTool.Domain/PRN232_G9_AutoGradingTool.Domain.csproj", "PRN232_G9_AutoGradingTool.Domain/"]
COPY ["src/PRN232_G9_AutoGradingTool.Infrastructure/PRN232_G9_AutoGradingTool.Infrastructure.csproj", "PRN232_G9_AutoGradingTool.Infrastructure/"]

RUN --mount=type=cache,id=nuget-autograding,target=/root/.nuget/packages \
    dotnet restore "PRN232_G9_AutoGradingTool.API/PRN232_G9_AutoGradingTool.API.csproj"

# Copy all sources (including Resources)
COPY src/ .

# Invalidate cache for Resources layer by creating a timestamp marker
# This RUN layer changes on every build, forcing the next COPY Resources layer to rebuild
# Remove existing Resources and create marker file with current timestamp
RUN rm -rf ./PRN232_G9_AutoGradingTool.Application/Resources && \
    mkdir -p ./PRN232_G9_AutoGradingTool.Application/Resources && \
    echo "Build: $(date -u +%Y%m%d%H%M%S)" > ./PRN232_G9_AutoGradingTool.Application/Resources/.build-marker

# Copy Resources files fresh (always rebuilt because of marker above)
# This ensures localization files (.resx) are always up-to-date and never cached
COPY src/PRN232_G9_AutoGradingTool.Application/Resources/ ./PRN232_G9_AutoGradingTool.Application/Resources/

WORKDIR /src/PRN232_G9_AutoGradingTool.API

RUN --mount=type=cache,id=nuget-autograding,target=/root/.nuget/packages \
    dotnet build "PRN232_G9_AutoGradingTool.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,id=nuget-autograding,target=/root/.nuget/packages \
    dotnet publish "PRN232_G9_AutoGradingTool.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install small tools used by entrypoint
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl netcat-openbsd \
  && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
# Copy entrypoint from the build context where it was added (src/PRN232_G9_AutoGradingTool.API/entrypoint.sh)
COPY --from=build /src/PRN232_G9_AutoGradingTool.API/entrypoint.sh ./entrypoint.sh
RUN chmod +x ./entrypoint.sh && sed -i 's/\r$//' ./entrypoint.sh

# Ensure Logs directory exists for persistent volume mount
# This directory will be mounted to logs_data volume at runtime (see docker-compose.yml)
# Logs are NOT deleted on rebuild; only persisted in the volume
RUN mkdir -p ./Logs && chmod 755 ./Logs

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["./entrypoint.sh"]
CMD ["dotnet", "PRN232_G9_AutoGradingTool.API.dll"]
