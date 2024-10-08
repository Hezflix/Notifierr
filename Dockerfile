FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore --use-current-runtime
# Build and publish a release
RUN dotnet publish -c Release -o out --use-current-runtime --self-contained false --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:31099
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "PlexNotifierr.Api.dll"]
