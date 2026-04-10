FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY services/catalog/src ./services/catalog/src
WORKDIR /src/services/catalog/src/Catalog.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache librdkafka-dev icu-libs krb5-libs

RUN addgroup -g 1000 -S appuser && \
    adduser -u 1000 -S appuser -G appuser

COPY --from=build --chown=appuser:appuser /app/publish .

RUN printf '#!/bin/sh\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning"\necho "Starting application..."\nexec dotnet Catalog.Api.dll\n' > /entrypoint.sh && chmod +x /entrypoint.sh

USER appuser

ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["/entrypoint.sh"]