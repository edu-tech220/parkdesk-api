FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ParkDesk.Api/ParkDesk.Api.csproj src/ParkDesk.Api/
RUN dotnet restore src/ParkDesk.Api/ParkDesk.Api.csproj

COPY src/ParkDesk.Api/ src/ParkDesk.Api/
RUN dotnet publish src/ParkDesk.Api/ParkDesk.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN useradd --uid 1654 --user-group --no-create-home appuser
COPY --from=build /app .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ParkDesk.Api.dll"]
