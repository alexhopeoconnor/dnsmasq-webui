FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DnsmasqWebUI.dll"]
