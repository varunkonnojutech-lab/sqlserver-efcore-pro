FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY sqlserver-efcore-pro.sln ./
COPY src/SqlServerEfCorePro.Data/SqlServerEfCorePro.Data.csproj src/SqlServerEfCorePro.Data/
COPY src/SqlServerEfCorePro.Api/SqlServerEfCorePro.Api.csproj src/SqlServerEfCorePro.Api/
RUN dotnet restore src/SqlServerEfCorePro.Api/SqlServerEfCorePro.Api.csproj

COPY . .
RUN dotnet publish src/SqlServerEfCorePro.Api/SqlServerEfCorePro.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "SqlServerEfCorePro.Api.dll"]
