# ----------------------------
# Build stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src/BotBash.Server

# copy csproj first for better caching
COPY ./BotBash.Server/BotBash.Server.csproj ./
RUN dotnet restore

# copy the rest
COPY . ./
RUN dotnet publish ./BotBash.Server/BotBash.Server.csproj -c Release -o /app/publish /p:UseAppHost=false

# ----------------------------
# Runtime stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

COPY --from=build /app/publish ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "BotBash.Server.dll"]