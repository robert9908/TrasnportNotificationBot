# Transport Notification Bot - Руководство по установке и запуску

## Обзор проекта

Transport Notification Bot - это Telegram бот для уведомлений о прибытии общественного транспорта. Пользователи могут:
- Поделиться своей локацией
- Выбрать ближайшие остановки и маршруты
- Подписаться на уведомления о прибытии транспорта
- Получать уведомления за заданное количество минут до прибытия

## Архитектура

Проект построен на Clean Architecture с четырьмя слоями:
- **TransportBot.Core** - доменные сущности и интерфейсы
- **TransportBot.Infrastructure** - Entity Framework, репозитории, база данных
- **TransportBot.Services** - бизнес-логика и сервисы
- **TransportBot.WebApi** - REST API и Telegram webhook

## Технологический стек

- .NET 8
- Entity Framework Core
- PostgreSQL
- Telegram.Bot
- ASP.NET Core Web API

## Предварительные требования

1. **.NET 8 SDK** - [Скачать](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **PostgreSQL** - [Скачать](https://www.postgresql.org/download/)
3. **Telegram Bot Token** - получить у [@BotFather](https://t.me/botfather)

## Установка и настройка

### 1. Клонирование и восстановление пакетов

```bash
cd TransportNotificationBot
dotnet restore
```

### 2. Настройка базы данных

Создайте базу данных PostgreSQL:

```sql
CREATE DATABASE transport_bot;
CREATE USER transport_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE transport_bot TO transport_user;
```

### 3. Настройка конфигурации

Обновите `appsettings.json` в проекте `TransportBot.WebApi`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=transport_bot;Username=transport_user;Password=your_password"
  },
  "TelegramBot": {
    "Token": "YOUR_TELEGRAM_BOT_TOKEN",
    "WebhookUrl": "https://your-domain.com/api/telegram/webhook"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### 4. Применение миграций

```bash
cd TransportBot.WebApi
dotnet ef database update
```

### 5. Создание Telegram бота

1. Найдите [@BotFather](https://t.me/botfather) в Telegram
2. Отправьте команду `/newbot`
3. Следуйте инструкциям для создания бота
4. Скопируйте токен и добавьте в `appsettings.json`

## Запуск приложения

### Локальный запуск

```bash
cd TransportBot.WebApi
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:7000`

### Swagger UI

После запуска откройте браузер и перейдите по адресу:
`https://localhost:7000/swagger`

## Настройка Telegram Webhook

### Для локального тестирования с ngrok

1. Установите [ngrok](https://ngrok.com/)
2. Запустите приложение локально
3. В новом терминале запустите ngrok:

```bash
ngrok http 7000
```

4. Скопируйте HTTPS URL из ngrok (например: `https://abc123.ngrok.io`)
5. Установите webhook:

```bash
curl -X POST "https://api.telegram.org/botYOUR_BOT_TOKEN/setWebhook" \
     -H "Content-Type: application/json" \
     -d '{"url":"https://abc123.ngrok.io/api/telegram/webhook"}'
```

### Для продакшена

Обновите `WebhookUrl` в `appsettings.json` на ваш реальный домен и установите webhook аналогично.

## Тестирование функциональности

### 1. Базовые команды бота

Найдите вашего бота в Telegram и протестируйте команды:

- `/start` - начало работы с ботом
- Поделитесь локацией через кнопку "📍 Поделиться локацией"
- Выберите ближайшую остановку из предложенных
- Выберите маршрут на остановке
- Настройте время уведомлений (5, 10, 15, 20 минут)

### 2. API Endpoints

Используйте Swagger UI или curl для тестирования API:

#### Получить все остановки
```bash
curl -X GET "https://localhost:7000/api/transportstop"
```

#### Найти остановки рядом с координатами
```bash
curl -X GET "https://localhost:7000/api/transportstop/nearby?latitude=55.7558&longitude=37.6176&radiusKm=1"
```

#### Получить маршруты для остановки
```bash
curl -X GET "https://localhost:7000/api/route/by-stop/1"
```

#### Создать подписку
```bash
curl -X POST "https://localhost:7000/api/subscription" \
     -H "Content-Type: application/json" \
     -d '{
       "userId": 1,
       "stopId": 1,
       "routeId": 1,
       "notificationMinutes": 10
     }'
```

### 3. Проверка уведомлений

Фоновый сервис проверяет подписки каждую минуту. Для тестирования:

1. Создайте подписку через бота
2. Подождите до 1 минуты
3. Проверьте логи приложения на наличие отправленных уведомлений

## Структура базы данных

### Основные таблицы

- **Users** - пользователи Telegram
- **TransportStops** - остановки транспорта
- **Routes** - маршруты транспорта
- **RouteStops** - связь маршрутов и остановок
- **Subscriptions** - подписки пользователей на уведомления

### Тестовые данные

При первом запуске автоматически создаются тестовые данные:
- 10 остановок в районе Москвы
- 5 маршрутов (автобусы, троллейбусы, трамваи)
- Связи между маршрутами и остановками

## Логирование и мониторинг

### Логи

Приложение записывает логи в консоль. Основные события:
- Получение сообщений от Telegram
- Создание/обновление подписок
- Отправка уведомлений
- Ошибки и исключения

### Мониторинг уведомлений

Фоновый сервис `NotificationBackgroundService` работает постоянно и:
- Проверяет активные подписки каждую минуту
- Получает данные о прибытии транспорта из мок-сервиса
- Отправляет уведомления пользователям

## Возможные проблемы и решения

### 1. Ошибка подключения к базе данных
- Проверьте, что PostgreSQL запущен
- Убедитесь в правильности строки подключения
- Проверьте права пользователя базы данных

### 2. Telegram webhook не работает
- Убедитесь, что URL доступен извне (для ngrok проверьте статус)
- Проверьте правильность токена бота
- Убедитесь, что webhook установлен правильно

### 3. Уведомления не приходят
- Проверьте логи фонового сервиса
- Убедитесь, что подписки созданы и активны
- Проверьте работу мок-сервиса транспортного API

### 4. Ошибки миграций
```bash
# Сброс миграций (ВНИМАНИЕ: удалит все данные)
dotnet ef database drop
dotnet ef database update
```

## Развитие проекта

### Следующие шаги для улучшения

1. **Интеграция с реальным API транспорта**
   - Замените `TransportApiService` на реальную интеграцию
   - Добавьте обработку различных форматов данных

2. **Docker контейнеризация**
   - Создайте Dockerfile для приложения
   - Настройте docker-compose с PostgreSQL

3. **Аутентификация и авторизация**
   - Добавьте JWT токены для API
   - Реализуйте роли пользователей

4. **Расширенная функциональность**
   - Уведомления о задержках транспорта
   - История поездок
   - Избранные маршруты
   - Геофенсинг для автоматических уведомлений

5. **Тестирование**
   - Unit тесты для сервисов
   - Integration тесты для API
   - End-to-end тесты для Telegram бота

## Поддержка

При возникновении проблем:
1. Проверьте логи приложения
2. Убедитесь в правильности конфигурации
3. Проверьте статус всех внешних сервисов (база данных, Telegram API)

Проект готов к использованию и дальнейшему развитию!
