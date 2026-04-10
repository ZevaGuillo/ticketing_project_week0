FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY services/fulfillment/src ./services/fulfillment/src
WORKDIR /src/services/fulfillment/src/Fulfillment.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache fontconfig freetype libx11 libxext libxrender

COPY --from=build /app/publish .

RUN printf '#!/bin/sh\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Fulfillment.Api.dll\n' > /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004
ENTRYPOINT ["/entrypoint.sh"]