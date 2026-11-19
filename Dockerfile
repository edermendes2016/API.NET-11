# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY Alibi.slnx ./
COPY src/Alibi.Domain/Alibi.Domain.csproj src/Alibi.Domain/
COPY src/Alibi.Infrastructure/Alibi.Infrastructure.csproj src/Alibi.Infrastructure/
COPY src/Alibi.Api/Alibi.Api.csproj src/Alibi.Api/
RUN dotnet restore src/Alibi.Api/Alibi.Api.csproj
COPY src/ src/
RUN dotnet publish src/Alibi.Api/Alibi.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Alibi.Api.dll"]
