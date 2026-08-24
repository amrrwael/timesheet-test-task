# Учёт трудозатрат и стоимость работ по проектам / Project Timesheet & Cost Tracking

[Русский](#русский) | [English](#english)

## Русский

Тестовое задание fullstack (.NET 8 + MongoDB + React/TypeScript).

Небольшая система учёта трудозатрат: табель с почасовыми ставками
(с историей, ставки можно менять задним числом), лимит 24 часа в день,
пометки переработки, закрытые бухгалтерией месяцы и отчёт по освоению
бюджетов проектов.

### Как запустить

Нужен только Docker.

    git clone https://github.com/amrrwael/timesheet-test-task.git
    cd timesheet-test-task
    docker compose up --build

Поднимутся MongoDB, API, сидер и фронтенд. Когда сидер в логах напишет
«Готово», открывайте:

- интерфейс: http://localhost:8080
- swagger: http://localhost:5000/swagger

Тестовые данные (сотрудники, проекты, записи из раздела «Приёмочные
проверки» задания) загружаются сидером автоматически. Полный сброс:

    docker compose down -v
    docker compose up --build

### Как запустить в режиме разработки

Если удобнее без контейнеров (понадобятся .NET 8 SDK, Node 18+, Docker
для MongoDB):

    docker compose up -d mongo
    dotnet run --project backend/Timesheet.Api
    cd frontend && npm install && npm run dev

API: http://localhost:5000, интерфейс: http://localhost:5173.
Тестовые данные: `dotnet run --project tools/Seeder`.

### Тесты

    dotnet test backend/Timesheet.sln

Юнит-тесты бизнес-правил: выбор ставки на дату записи, лимит часов,
кратность 0,5, границы периода проекта, закрытый период, округление денег.

### Что внутри

Коротко об API — в таблице ниже, подробно о принятых решениях
и допущениях — в [NOTES.md](NOTES.md).

| Метод | Путь | Что делает |
|---|---|---|
| GET | /api/time-entries | список записей за месяц, с пагинацией и фильтрами |
| PUT | /api/time-entries | создать запись |
| POST | /api/time-entries/{id} | изменить запись |
| DELETE | /api/time-entries/{id} | удалить запись |
| GET | /api/reports/projects | отчёт по проектам за месяц |
| GET | /api/employees, /api/projects | справочники |
| POST | /api/employees/{id}/rates | добавить или заменить ставку с даты |
| GET | /api/periods | список закрытых месяцев |
| POST | /api/periods/close, /api/periods/open | закрыть / открыть месяц |

Ошибки бизнес-правил приходят с кодом 400/409 и телом
`{ code, message, errors? }` — машиночитаемый код плюс понятный текст
на русском, никаких голых 500.

## English

A fullstack test assignment (.NET 8 + MongoDB + React/TypeScript).

A small timesheet system: hourly rates with history (rates can be changed
retroactively), a 24-hour daily limit, overtime flags, months closed by
accounting, and a report on project budget usage.

### How to run

All you need is Docker.

    git clone https://github.com/amrrwael/timesheet-test-task.git
    cd timesheet-test-task
    docker compose up --build

This starts MongoDB, the API, a seeder and the frontend. Once the seeder
logs «Готово» (done), open:

- UI: http://localhost:8080
- Swagger: http://localhost:5000/swagger

Test data (employees, projects and timesheet entries matching the
assignment's acceptance tables) is loaded automatically. Full reset:

    docker compose down -v
    docker compose up --build

### Running in development mode

If you prefer running without containers (.NET 8 SDK, Node 18+ and Docker
for MongoDB required):

    docker compose up -d mongo
    dotnet run --project backend/Timesheet.Api
    cd frontend && npm install && npm run dev

API: http://localhost:5000, UI: http://localhost:5173.
Test data: `dotnet run --project tools/Seeder`.

### Tests

    dotnet test backend/Timesheet.sln

Unit tests cover the business rules: rate selection by entry date, the
daily hours limit, the 0.5-hours step, project period boundaries, closed
periods and money rounding.

### API

| Method | Path | Purpose |
|---|---|---|
| GET | /api/time-entries | paged monthly list with filters |
| PUT | /api/time-entries | create an entry |
| POST | /api/time-entries/{id} | update an entry |
| DELETE | /api/time-entries/{id} | delete an entry |
| GET | /api/reports/projects | monthly project report |
| GET | /api/employees, /api/projects | dictionaries |
| POST | /api/employees/{id}/rates | add or replace a rate from a date |
| GET | /api/periods | closed months |
| POST | /api/periods/close, /api/periods/open | close / open a month |

Business-rule errors return 400/409 with a `{ code, message, errors? }`
body — a machine-readable code plus a human-readable message (in Russian,
per the assignment). No bare 500s.