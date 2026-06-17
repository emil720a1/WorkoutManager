FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/WorkoutManager.WorkoutAPI/WorkoutManager.WorkoutAPI.csproj", "src/WorkoutManager.WorkoutAPI/"]
COPY ["src/WorkoutManager.Application/WorkoutManager.Application.csproj", "src/WorkoutManager.Application/"]
COPY ["src/WorkoutManager.Domain/WorkoutManager.Domain.csproj", "src/WorkoutManager.Domain/"]
COPY ["src/WorkoutManager.Infrastructure/WorkoutManager.Infrastructure.csproj", "src/WorkoutManager.Infrastructure/"]
COPY ["src/WorkoutManager.Contracts/WorkoutManager.Contracts.csproj", "src/WorkoutManager.Contracts/"]

RUN dotnet restore "src/WorkoutManager.WorkoutAPI/WorkoutManager.WorkoutAPI.csproj"

COPY . .

RUN dotnet publish "src/WorkoutManager.WorkoutAPI/WorkoutManager.WorkoutAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkoutManager.WorkoutAPI.dll"]
