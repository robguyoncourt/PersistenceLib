FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln ./
COPY PersistenceLib/*.csproj ./PersistenceLib/
COPY PersistenceTest/*.csproj ./PersistenceTest/
WORKDIR /app/
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY PersistenceLib/. ./PersistenceLib/
COPY PersistenceTest/. ./PersistenceTest/
WORKDIR /app/
RUN dotnet publish -c Release -o out

ENTRYPOINT ["dotnet", "out/PersistenceLib.dll"]

ENTRYPOINT ["dotnet", "out/PersistenceTest.dll"]

# test application -- see: dotnet-docker-unit-testing.md
FROM build AS testrunner
WORKDIR /app/PersistenceTest
ENTRYPOINT ["dotnet", "test", "--logger:trx"]
