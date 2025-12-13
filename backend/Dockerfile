FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["InnriGreifi.API.csproj", "./"]
RUN dotnet restore "InnriGreifi.API.csproj"
COPY . .
RUN dotnet build "InnriGreifi.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InnriGreifi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y wget && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InnriGreifi.API.dll"]
