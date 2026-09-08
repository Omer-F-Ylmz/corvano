# --- CSS (Tailwind) ---
FROM node:22-alpine AS css
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts
COPY src ./src
COPY Corvano.Web/Views ./Corvano.Web/Views
RUN npm run css:build

# --- .NET build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Corvano.sln ./
COPY Corvano.Core/Corvano.Core.csproj Corvano.Core/
COPY Corvano.Entities/Corvano.Entities.csproj Corvano.Entities/
COPY Corvano.DataAccess/Corvano.DataAccess.csproj Corvano.DataAccess/
COPY Corvano.Business/Corvano.Business.csproj Corvano.Business/
COPY Corvano.Web/Corvano.Web.csproj Corvano.Web/
RUN dotnet restore Corvano.Web/Corvano.Web.csproj
COPY . .
COPY --from=css /src/Corvano.Web/wwwroot/css/site.css Corvano.Web/wwwroot/css/site.css
RUN dotnet publish Corvano.Web/Corvano.Web.csproj -c Release -o /app --no-restore

# --- Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Corvano.Web.dll"]
