FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Restore dependencies first to cache them between builds
COPY EventsService.csproj ./
RUN dotnet restore

# Copy the source code and publish
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

# Use the smaller ASP.NET runtime image for the final application
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app .

# Expose the application port
EXPOSE 8082

ENTRYPOINT ["dotnet", "EventsService.dll"]
