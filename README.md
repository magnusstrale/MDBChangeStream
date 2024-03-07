# MDBChangeStream

## Disclaimer
The code is not production-ready, it is a simple proof-of-concept. It is lacking error handling, needs general hardening, has primitive multithreading etc. This code is not officially endorsed by MongoDB Inc.

## Description
The code illustrates using change streams to move data from one MongoDB cluster to another. It batches data writes to avoid excessive network roundtrips.

## Prerequisites
- .NET 8.0 SDK. See https://learn.microsoft.com/en-us/dotnet/core/install/ for installation instructions.
- (Optional) Docker for building and running it as a docker container. See https://www.docker.com/products/docker-desktop/ for installation instructions.
- (Optional) Visual Studio Code for development & testing. See https://code.visualstudio.com/download for installation instructions.

## Getting it to run
1. Modify the settings in appsettings.json. Change the Source and Target connection strings to match the clusters you want to use.
2. Modify setenv.sh by adding user name and passwords for the database users to connect to source and target clusters.
3. If developing - start VS Code by executing ./start.sh
 
   -or-

4. If you want to run under Docker, build with builddocker.sh and run int with rundocker.sh. Note that rundocker uses the --rm parameter so the container will be deleted on exit.