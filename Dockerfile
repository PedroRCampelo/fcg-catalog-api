# syntax=docker/dockerfile:1

# Estágio de build: roda sempre na arquitetura da máquina que está construindo ($BUILDPLATFORM) e compila
# para a arquitetura de destino ($TARGETARCH) com `-a`. Assim a imagem arm64 e a amd64 saem do mesmo
# Dockerfile, sem emulação (QEMU) durante o build.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# Primeiro só os .csproj: a camada do restore só é refeita quando os pacotes mudam, não a cada edição de código.
COPY src/FCG.Catalog.Domain/FCG.Catalog.Domain.csproj src/FCG.Catalog.Domain/
COPY src/FCG.Catalog.Application/FCG.Catalog.Application.csproj src/FCG.Catalog.Application/
COPY src/FCG.Catalog.Infrastructure/FCG.Catalog.Infrastructure.csproj src/FCG.Catalog.Infrastructure/
COPY src/FCG.Catalog.Api/FCG.Catalog.Api.csproj src/FCG.Catalog.Api/
RUN dotnet restore src/FCG.Catalog.Api/FCG.Catalog.Api.csproj -a $TARGETARCH

COPY src/ src/
RUN dotnet publish src/FCG.Catalog.Api/FCG.Catalog.Api.csproj -a $TARGETARCH -c Release --no-restore -p:UseAppHost=false -o /app/publish

# Estágio final: só o runtime do ASP.NET, sem SDK nem código-fonte. Não há nenhum RUN aqui de propósito,
# porque um RUN rodaria na arquitetura de destino e exigiria emulação ao construir para outra arquitetura.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# A imagem base já traz o usuário sem privilégios `app` e escuta na porta 8080 (ASPNETCORE_HTTP_PORTS).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "FCG.Catalog.Api.dll"]
