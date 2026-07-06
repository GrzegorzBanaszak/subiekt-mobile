FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/SubiektMobile.slnx backend/
COPY backend/src/SubiektMobile.Api/SubiektMobile.Api.csproj backend/src/SubiektMobile.Api/
COPY backend/src/SubiektMobile.Application/SubiektMobile.Application.csproj backend/src/SubiektMobile.Application/
COPY backend/src/SubiektMobile.Domain/SubiektMobile.Domain.csproj backend/src/SubiektMobile.Domain/
COPY backend/src/SubiektMobile.Infrastructure/SubiektMobile.Infrastructure.csproj backend/src/SubiektMobile.Infrastructure/
RUN dotnet restore backend/src/SubiektMobile.Api/SubiektMobile.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/SubiektMobile.Api/SubiektMobile.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG APP_UID=1654
ENV APP_UID=$APP_UID
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/subiekt-mobile/data-protection \
    && chown -R "$APP_UID:$APP_UID" /var/lib/subiekt-mobile
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SubiektMobile.Api.dll"]
