FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG DEPLOYABLE_VERSION
ARG TARGETPLATFORM

WORKDIR /app

# copy csproj and restore as distinct layers
COPY ./Huna.Signalr.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c Release -o /dist --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /dist ./
ENTRYPOINT ["dotnet", "Huna.Signalr.dll"]

EXPOSE 3000
LABEL org.opencontainers.image.source=https://github.com/mars-office/huna-signalr


