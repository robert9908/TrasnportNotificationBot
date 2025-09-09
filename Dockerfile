# Многоэтапная сборка для оптимизации размера образа
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проекта и восстанавливаем зависимости
COPY ["TransportBot.WebApi/TransportBot.WebApi.csproj", "TransportBot.WebApi/"]
COPY ["TransportBot.Core/TransportBot.Core.csproj", "TransportBot.Core/"]
COPY ["TransportBot.Infrastructure/TransportBot.Infrastructure.csproj", "TransportBot.Infrastructure/"]
COPY ["TransportBot.Services/TransportBot.Services.csproj", "TransportBot.Services/"]

RUN dotnet restore "TransportBot.WebApi/TransportBot.WebApi.csproj"

# Копируем весь исходный код и собираем приложение
COPY . .
WORKDIR "/src/TransportBot.WebApi"
RUN dotnet build "TransportBot.WebApi.csproj" -c Release -o /app/build

# Публикуем приложение с оптимизациями
FROM build AS publish
RUN dotnet publish "TransportBot.WebApi.csproj" -c Release -o /app/publish \
    --no-restore \
    --self-contained false \
    --verbosity minimal

# Используем runtime образ для финального контейнера
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Устанавливаем необходимые пакеты
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Создаем директории для логов
RUN mkdir -p /app/logs

# Создаем пользователя для безопасности
RUN adduser --disabled-password --gecos '' appuser && \
    chown -R appuser:appuser /app

# Копируем опубликованное приложение
COPY --from=publish /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

# Настройка переменных окружения
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TransportBot.WebApi.dll"]
