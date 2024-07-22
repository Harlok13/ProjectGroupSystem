FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PGS.TemplatePlaceholderBot/PGS.TemplatePlaceholderBot.csproj", "PGS.TemplatePlaceholderBot/"]
RUN dotnet restore "PGS.TemplatePlaceholderBot/PGS.TemplatePlaceholderBot.csproj"
COPY . .
WORKDIR "/src/PGS.TemplatePlaceholderBot"
RUN dotnet build "PGS.TemplatePlaceholderBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PGS.TemplatePlaceholderBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
RUN mkdir files/templates
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PGS.TemplatePlaceholderBot.dll"]
