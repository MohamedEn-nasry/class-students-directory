# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ClassDirectory.Api/ClassDirectory.Api.csproj", "src/ClassDirectory.Api/"]
RUN dotnet restore "src/ClassDirectory.Api/ClassDirectory.Api.csproj"
COPY . .
WORKDIR /src/src/ClassDirectory.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClassDirectory.Api.dll"]
