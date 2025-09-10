-- Cleanup script to remove subscriptions with invalid station codes
-- These codes (1, 12, 14) return 404 errors from Yandex API

-- First, show what will be deleted
SELECT 
    s."Id" as SubscriptionId,
    s."ExternalStopCode" as StationCode,
    u."TelegramId" as UserId,
    s."CreatedAt"
FROM "Subscriptions" s
INNER JOIN "Users" u ON s."UserId" = u."Id"
WHERE s."ExternalStopCode" IN ('1', '12', '14')
AND s."IsActive" = true;

-- Delete invalid subscriptions
DELETE FROM "Subscriptions" 
WHERE "ExternalStopCode" IN ('1', '12', '14')
AND "IsActive" = true;

-- Show remaining active subscriptions
SELECT 
    s."Id" as SubscriptionId,
    s."ExternalStopCode" as StationCode,
    s."ExternalRouteNumber" as RouteNumber,
    u."TelegramId" as UserId,
    s."NotifyBeforeMinutes",
    s."CreatedAt"
FROM "Subscriptions" s
INNER JOIN "Users" u ON s."UserId" = u."Id"
WHERE s."IsActive" = true
ORDER BY s."CreatedAt" DESC;
