# App + dnsmasq in one container. Use this image for running dnsmasq (and the UI) in Docker.
# Both run as root; file permissions work because config dirs are in the container.
# For dnsmasq on the host, use the self-contained publish (scripts/publish-self-contained.sh)
# or run the app on the host; see DnsmasqOptions XML doc for permissions and ReloadCommand scope.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/DnsmasqWebUI/DnsmasqWebUI.csproj", "src/DnsmasqWebUI/"]
RUN dotnet restore "src/DnsmasqWebUI/DnsmasqWebUI.csproj"
COPY . .
WORKDIR "/src/src/DnsmasqWebUI"
RUN dotnet build "DnsmasqWebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DnsmasqWebUI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
# Slow, rarely-changing steps first (stay cached when only .NET code changes)
RUN apt-get update && apt-get install -y --no-install-recommends dnsmasq procps \
    && rm -rf /var/lib/apt/lists/*
COPY scripts/entrypoint.sh scripts/dnsmasq-status.sh .
RUN chmod +x entrypoint.sh dnsmasq-status.sh
# .NET publish last - only this layer invalidates when code changes
COPY --from=publish /app/publish .
ENTRYPOINT ["./entrypoint.sh"]
