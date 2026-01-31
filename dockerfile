# Stage 1: Build the .NET application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source files and build the application
COPY . .
RUN dotnet publish -c Release -o /out

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Set the timezone environment variable
ENV TZ=Europe/Berlin

# Install tzdata damit Systemzeit korrekt ist
RUN apt-get update && apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone && \
    apt-get clean

# Copy the build output from the build stage
COPY --from=build /out .

# Expose the port your application runs on (optional)
EXPOSE 80
EXPOSE 8080

# Specify the entrypoint for the .NET application
ENTRYPOINT ["dotnet", "VisitorApp_EmailService.dll"]
