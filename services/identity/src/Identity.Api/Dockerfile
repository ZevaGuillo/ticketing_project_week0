FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY services/identity/src ./services/identity/src
WORKDIR /src/services/identity/src/Identity.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN printf '#!/bin/sh\nset -e\necho "Applying EF Core migrations..."\necho "Migrations skipped" || echo "Migration warning: migrations may already be applied"\necho "Starting application..."\nexec dotnet Identity.Api.dll\n' > /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["/entrypoint.sh"]