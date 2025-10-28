# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY ["HotelManagement.csproj", "./"]
RUN dotnet restore "HotelManagement.csproj"

# Copy toàn bộ source code và build
COPY . .
RUN dotnet build "HotelManagement.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HotelManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy file đã publish
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "HotelManagement.dll"]