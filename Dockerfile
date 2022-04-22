FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy everything
COPY ["GingerTalk.Server/GingerTalk.Server.fsproj", "GingerTalk.Server/"]
COPY ["GingerTalk.Lib/GingerTalk.Lib.fsproj", "GingerTalk.Lib/"]
# Restore as distinct layers
RUN dotnet restore "GingerTalk.Server/GingerTalk.Server.fsproj"
COPY . .
WORKDIR "GingerTalk.Server"
RUN dotnet build "GingerTalk.Server.fsproj" -c Release -o /app/build

FROM build-env as publish
# Build and publish a release
RUN dotnet publish "GingerTalk.Server.fsproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GingerTalk.dll"]
