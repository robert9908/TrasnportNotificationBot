# 🚌 Telegram Transport Bot

Бот для уведомлений о прибытии общественного транспорта с геолокацией и real-time данными

## 📋 Описание

**Telegram Transport Bot** — это интеллектуальный бот для получения уведомлений о прибытии общественного транспорта. Бот интегрируется с API Яндекс.Расписания для получения актуальных данных о транспорте и отправляет персонализированные уведомления пользователям в Telegram.

### ✨ Основные возможности

- **Геолокационный поиск**: Автоматический поиск ближайших остановок по GPS координатам через Yandex Geocoder API
- **Прямой поиск по коду**: Получение расписания по коду станции Яндекса (`/station s9602494`)
- **Real-time уведомления**: Фоновый сервис проверяет расписание через Yandex.Schedules API каждые 30 сек, отправляет push в Telegram за 5-20 мин до прибытия
- **Система подписок**: Пользователи создают персональные уведомления для конкретных маршрутов с выбором времени оповещения
- **Maintenance API**: Автоматическая очистка невалидных станций, мониторинг системы, управление базой данных

## 🏗️ Архитектура

Проект построен по принципам **Clean Architecture** с разделением на слои:

```
├── TransportBot.Core/          # Доменная логика
│   ├── Entities/              # Сущности
│   ├── Interfaces/            # Интерфейсы
│   └── DTOs/                  # Объекты передачи данных
├── TransportBot.Infrastructure/ # Инфраструктура
│   ├── Data/                  # Контекст БД и миграции
│   └── Repositories/          # Реализация репозиториев
├── TransportBot.Services/      # Бизнес-логика
│   ├── Services/              # Сервисы
│   └── BackgroundServices/    # Фоновые сервисы
└── TransportBot.WebApi/        # Веб API
    ├── Controllers/           # Контроллеры
    └── Middleware/            # Промежуточное ПО
```

## 🛠️ Технологический стек

- **.NET 8** - Основной фреймворк
- **ASP.NET Core Web API** - Веб API
- **PostgreSQL** - База данных
- **Entity Framework Core** - ORM
- **Telegram.Bot** - Интеграция с Telegram
- **Docker** - Контейнеризация
- **Yandex.Schedules API** - Данные о транспорте
- **Yandex Geocoder API** - Геокодинг

## 🚀 Быстрый старт

### Предварительные требования

- .NET 8 SDK
- PostgreSQL
- Docker (опционально)
- Telegram Bot Token
- Yandex API Key

### Установка

1. **Клонируйте репозиторий**
```bash
git clone https://github.com/robert9908/TrasnportNotificationBot.git
cd TransportNotificationBot
```

2. **Настройте переменные окружения**
```bash
cp .env.example .env
```

Отредактируйте `.env` файл:
```env
TELEGRAM_BOT_TOKEN=your_telegram_bot_token
YANDEX_API_KEY=your_yandex_api_key
DATABASE_CONNECTION_STRING=your_postgresql_connection_string
```

3. **Запуск с Docker**
```bash
docker-compose up -d
```

4. **Или запуск локально**
```bash
dotnet restore
dotnet ef database update --project TransportBot.Infrastructure
dotnet run --project TransportBot.WebApi
```

## 📱 Использование бота

### Основные команды

- `/start` - Начать работу с ботом
- `/location` - Поделиться геолокацией для поиска остановок
- `/station <код>` - Получить расписание по коду станции
- `/subscriptions` - Управление подписками
- `/help` - Справка по командам

### Пример использования

1. Отправьте `/start` для регистрации
2. Поделитесь геолокацией или используйте `/station s9602494`
3. Выберите остановку и маршрут
4. Настройте время уведомлений (5-20 минут)
5. Получайте автоматические уведомления!

### Где найти коды станций

Коды станций можно найти на сайте [rasp.yandex.ru](https://rasp.yandex.ru) в URL страницы станции.

## 🔧 API Endpoints

### Health Check
```http
GET /api/health
```

### Maintenance
```http
POST /api/maintenance/cleanup-invalid-stations
GET /api/maintenance/active-subscriptions
```

### Routes
```http
GET /api/routes
POST /api/routes
PUT /api/routes/{id}
DELETE /api/routes/{id}
```

## 🗄️ База данных

Проект использует PostgreSQL с Entity Framework Core. Основные сущности:

- **User** - Пользователи Telegram
- **Subscription** - Подписки на уведомления
- **Route** - Маршруты транспорта
- **RouteStop** - Остановки маршрутов
- **TransportStop** - Транспортные остановки

### Миграции

```bash
# Создать миграцию
dotnet ef migrations add MigrationName --project TransportBot.Infrastructure

# Применить миграции
dotnet ef database update --project TransportBot.Infrastructure
```

## 🔄 Background Services

- **NotificationBackgroundService** - Отправка уведомлений каждые 30 секунд
- **TelegramBotPollingService** - Обработка сообщений Telegram

## 🛡️ Безопасность

- Anti-spam защита (5-минутный кулдаун)
- Rate limiting middleware
- Валидация входных данных
- Обработка исключений

## 📊 Мониторинг

- Структурированное логирование
- Health checks
- Maintenance API для диагностики
- Метрики производительности

