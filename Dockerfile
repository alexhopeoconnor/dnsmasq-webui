# App + dnsmasq in one container. Use this image for running dnsmasq (and the UI) in Docker.
# Both run as root; file permissions work because config dirs are in the container.
# For dnsmasq on the host, use the self-contained publish (scripts/publish-self-contained.sh)
# or run the app on the host; see DnsmasqOptions XML doc for permissions and ReloadCommand scope.
#
# Dnsmasq install mode for the test harness:
# - DNSMASQ_VERSION=latest (default): build the latest stable upstream dnsmasq release.
# - DNSMASQ_VERSION=<version>       : build that exact upstream release (e.g. 2.91).
# - DNSMASQ_VERSION=distro          : use the distro package from apt.
#
# This keeps day-to-day harness use close to upstream while still allowing pinned-version testing.

ARG DNSMASQ_VERSION=
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
ARG DNSMASQ_VERSION=latest
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
# Install dnsmasq either from the distro package or by building an upstream release.
RUN if [ "$DNSMASQ_VERSION" = "distro" ]; then \
    apt-get update && apt-get install -y --no-install-recommends dnsmasq procps \
    && rm -rf /var/lib/apt/lists/*; \
    else \
    apt-get update && apt-get install -y --no-install-recommends procps curl build-essential xz-utils ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && if [ "$DNSMASQ_VERSION" = "latest" ]; then \
         DNSMASQ_VERSION="$(curl -fsSL https://thekelleys.org.uk/dnsmasq/ | grep -o 'dnsmasq-[0-9][0-9.]*\.tar\.xz' | sed 's/^dnsmasq-//; s/\.tar\.xz$//' | sort -V | tail -n1)"; \
       fi \
    && test -n "$DNSMASQ_VERSION" \
    && echo "Building dnsmasq $DNSMASQ_VERSION from upstream source" \
    && curl -fsSL "https://thekelleys.org.uk/dnsmasq/dnsmasq-${DNSMASQ_VERSION}.tar.xz" -o /tmp/dnsmasq.tar.xz \
    && tar -C /tmp -xJf /tmp/dnsmasq.tar.xz \
    && make -C /tmp/dnsmasq-${DNSMASQ_VERSION} -j"$(nproc)" \
    && make -C /tmp/dnsmasq-${DNSMASQ_VERSION} install PREFIX=/usr \
    && rm -rf /tmp/dnsmasq-${DNSMASQ_VERSION} /tmp/dnsmasq.tar.xz \
    && apt-get purge -y build-essential curl xz-utils \
    && apt-get autoremove -y --purge \
    && rm -rf /var/lib/apt/lists/*; \
    fi
COPY scripts/entrypoint.sh scripts/dnsmasq-status.sh ./
RUN chmod +x entrypoint.sh dnsmasq-status.sh
# curl for HEALTHCHECK (aspnet image has no curl/wget)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
# .NET publish last - only this layer invalidates when code changes
COPY --from=publish /app/publish .
ENTRYPOINT ["./entrypoint.sh"]
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD curl -f -s http://localhost:8080/healthz/ready || exit 1
