FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/Directory.Build.props", "backend/Directory.Packages.props", "./backend/"]
COPY ["backend/src/AnalyticsPlatform.McpServer/AnalyticsPlatform.McpServer.csproj", "backend/src/AnalyticsPlatform.McpServer/"]
COPY ["backend/src/AnalyticsPlatform.Application/AnalyticsPlatform.Application.csproj", "backend/src/AnalyticsPlatform.Application/"]
COPY ["backend/src/AnalyticsPlatform.Domain/AnalyticsPlatform.Domain.csproj", "backend/src/AnalyticsPlatform.Domain/"]
COPY ["backend/src/AnalyticsPlatform.Infrastructure/AnalyticsPlatform.Infrastructure.csproj", "backend/src/AnalyticsPlatform.Infrastructure/"]
RUN dotnet restore "backend/src/AnalyticsPlatform.McpServer/AnalyticsPlatform.McpServer.csproj"

COPY backend/ ./backend/
WORKDIR "/src/backend/src/AnalyticsPlatform.McpServer"
RUN dotnet publish "AnalyticsPlatform.McpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5005
ENTRYPOINT ["dotnet", "AnalyticsPlatform.McpServer.dll"]

