FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/WorkoutManager.BotGateway/WorkoutManager.BotGateway.csproj", "src/WorkoutManager.BotGateway/"]
COPY ["src/WorkoutManager.Application/WorkoutManager.Application.csproj", "src/WorkoutManager.Application/"]
COPY ["src/WorkoutManager.Domain/WorkoutManager.Domain.csproj", "src/WorkoutManager.Domain/"]
COPY ["src/WorkoutManager.Infrastructure/WorkoutManager.Infrastructure.csproj", "src/WorkoutManager.Infrastructure/"]
COPY ["src/WorkoutManager.Contracts/WorkoutManager.Contracts.csproj", "src/WorkoutManager.Contracts/"]

RUN dotnet restore "src/WorkoutManager.BotGateway/WorkoutManager.BotGateway.csproj"

COPY . .

RUN dotnet publish "src/WorkoutManager.BotGateway/WorkoutManager.BotGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkoutManager.BotGateway.dll"]
