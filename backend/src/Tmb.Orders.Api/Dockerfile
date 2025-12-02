# Imagem do runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Imagem da build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os csproj pra otimizar o cache
COPY src/Tmb.Orders.Api/Tmb.Orders.Api.csproj src/Tmb.Orders.Api/
COPY src/Tmb.Orders.Application/Tmb.Orders.Application.csproj src/Tmb.Orders.Application/
COPY src/Tmb.Orders.Domain/Tmb.Orders.Domain.csproj src/Tmb.Orders.Domain/
COPY src/Tmb.Orders.Infrastructure/Tmb.Orders.Infrastructure.csproj src/Tmb.Orders.Infrastructure/

RUN dotnet restore src/Tmb.Orders.Api/Tmb.Orders.Api.csproj

# Copia o resto do código
COPY . .

WORKDIR /src/src/Tmb.Orders.Api
RUN dotnet publish Tmb.Orders.Api.csproj -c Release -o /app/publish


FROM base AS final
WORKDIR /app

# Instalação do CURL 
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tmb.Orders.Api.dll"]
