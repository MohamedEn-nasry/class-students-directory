# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Backend/src/ClassDirectory.Api/ClassDirectory.Api.csproj", "Backend/src/ClassDirectory.Api/"]
RUN dotnet restore "Backend/src/ClassDirectory.Api/ClassDirectory.Api.csproj"

# Copy project files and build release
COPY Backend/ Backend/
WORKDIR /src/Backend/src/ClassDirectory.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ClassDirectory.Api.dll"]
