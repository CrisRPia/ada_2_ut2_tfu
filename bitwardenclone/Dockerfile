FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
EXPOSE 8080
ENTRYPOINT ["dotnet", "run", "--urls", "http://0.0.0.0:8080"]

