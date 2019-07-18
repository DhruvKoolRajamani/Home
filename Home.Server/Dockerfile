FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["Home.Server/Home.Server.csproj", "Home.Server/"]
RUN dotnet restore "Home/Home.csproj"
COPY . .
WORKDIR "/src/Home.Server"
RUN dotnet build "Home.Server.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Home.Server.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Home.Server.dll"]