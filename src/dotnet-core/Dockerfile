# ---- Etapa de compilacion ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# ---- Etapa de ejecucion ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 5080
ENV ASPNETCORE_URLS=http://0.0.0.0:5080
ENTRYPOINT ["dotnet", "Central.dll"]
