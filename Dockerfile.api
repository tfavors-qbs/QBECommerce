FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj", "ShopQualityboltWeb/ShopQualityboltWeb/"]
COPY ["QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj", "QBExternalWebLibrary/QBExternalWebLibrary/"]
RUN dotnet restore "ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj"
COPY . .
WORKDIR "/src/ShopQualityboltWeb/ShopQualityboltWeb"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopQualityboltWeb.dll"]
