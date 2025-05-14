# Stage 1: Build .NET application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj files and restore dependencies
COPY *.sln .
COPY src/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done
COPY tests/*/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p tests/${file%.*}/ && mv $file tests/${file%.*}/; done

# Restore dependencies
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build -c Release --no-restore

# Run tests
RUN dotnet test -c Release --no-build --verbosity normal

# Publish the application
RUN dotnet publish src/SmartInsight.API/SmartInsight.API.csproj -c Release -o /app --no-build

# Stage 2: Build React SPA
FROM node:18-alpine AS ui-build
WORKDIR /src

# Copy package.json and install dependencies
COPY src/SmartInsight.UI/package*.json ./
RUN npm ci

# Copy UI source code
COPY src/SmartInsight.UI/ ./

# Build the UI
RUN npm run build

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published API
COPY --from=build /app ./

# Copy the built UI files to the wwwroot directory
COPY --from=ui-build /src/build ./wwwroot

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "SmartInsight.API.dll"] 