FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore to leverage layer caching
COPY ["ChineseAuction.Api/ChineseAuction.Api.csproj", "ChineseAuction.Api/"]
RUN dotnet restore "ChineseAuction.Api/ChineseAuction.Api.csproj"

# Copy everything and publish
COPY . .
WORKDIR /src/ChineseAuction.Api
RUN dotnet publish "ChineseAuction.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ChineseAuction.Api.dll"]
