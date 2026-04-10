FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY services/gateway/src ./services/gateway/src
COPY services/shared/src ./services/shared/src
WORKDIR /src/services/gateway/src/Gateway.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

RUN printf '#!/bin/sh\nset -e\necho "Starting application..."\nexec dotnet Gateway.Api.dll\n' > /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["/entrypoint.sh"]