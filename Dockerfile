
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["TrustVault.Api/TrustVault.Api.csproj", "TrustVault.Api/"]
RUN dotnet restore "TrustVault.Api/TrustVault.Api.csproj"

COPY . .
WORKDIR "/src/TrustVault.Api"
RUN dotnet publish "TrustVault.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrustVault.Api.dll"]
