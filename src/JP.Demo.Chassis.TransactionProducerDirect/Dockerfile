#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/JP.Demo.Chassis.TransactionProducerDirect/JP.Demo.Chassis.TransactionProducerDirect.csproj", "src/JP.Demo.Chassis.TransactionProducerDirect/"]
RUN dotnet restore "src/JP.Demo.Chassis.TransactionProducerDirect/JP.Demo.Chassis.TransactionProducerDirect.csproj"
COPY . .
WORKDIR "/src/src/JP.Demo.Chassis.TransactionProducerDirect"
RUN dotnet build "JP.Demo.Chassis.TransactionProducerDirect.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JP.Demo.Chassis.TransactionProducerDirect.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JP.Demo.Chassis.TransactionProducerDirect.dll"]