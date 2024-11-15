# Use the .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY . .

# Build the application
RUN dotnet publish ./test/AdHoc.Locking.AtomicFileLock -f net8.0 -c Release -o /app/publish



# Use the .NET runtime image to run the app
FROM mcr.microsoft.com/dotnet/runtime:8.0

# Set the working directory
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "AdHoc.Locking.AtomicFileLock.dll"]
