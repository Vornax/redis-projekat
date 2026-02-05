# Faza 1 – build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Faza 2 – runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/Frontend ./Frontend
EXPOSE 5257
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "TrendingApi.dll"]