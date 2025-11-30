# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Tmb.Orders.Worker/Tmb.Orders.Worker.csproj src/Tmb.Orders.Worker/
COPY src/Tmb.Orders.Application/Tmb.Orders.Application.csproj src/Tmb.Orders.Application/
COPY src/Tmb.Orders.Domain/Tmb.Orders.Domain.csproj src/Tmb.Orders.Domain/
COPY src/Tmb.Orders.Infrastructure/Tmb.Orders.Infrastructure.csproj src/Tmb.Orders.Infrastructure/

RUN dotnet restore src/Tmb.Orders.Worker/Tmb.Orders.Worker.csproj

COPY . .

WORKDIR /src/src/Tmb.Orders.Worker
RUN dotnet publish Tmb.Orders.Worker.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tmb.Orders.Worker.dll"]
