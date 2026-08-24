using MongoDB.Driver;
using Timesheet.Api.Domain.Entities;
using Timesheet.Api.Infrastructure;

var connectionString =
    Environment.GetEnvironmentVariable("SEED_CONNECTION_STRING")
    ?? (args.Length > 0 ? args[0] : "mongodb://localhost:27017");

const string databaseName = "timesheet";

var client = new MongoClient(connectionString);
var db = client.GetDatabase(databaseName);

Console.WriteLine($"Сидирование базы «{databaseName}» на {connectionString}");

await MongoIndexInitializer.EnsureIndexesAsync(db);

// чистим документы, коллекции и индексы остаются
await db.GetCollection<Employee>(MongoCollections.Employees)
    .DeleteManyAsync(FilterDefinition<Employee>.Empty);
await db.GetCollection<Project>(MongoCollections.Projects)
    .DeleteManyAsync(FilterDefinition<Project>.Empty);
await db.GetCollection<TimeEntry>(MongoCollections.TimeEntries)
    .DeleteManyAsync(FilterDefinition<TimeEntry>.Empty);
await db.GetCollection<ClosedPeriod>(MongoCollections.ClosedPeriods)
    .DeleteManyAsync(FilterDefinition<ClosedPeriod>.Empty);

static DateTime Utc(int year, int month, int day) =>
    DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

// --- сотрудники (приёмочные данные) ---

var ivanov = new Employee
{
    Id = string.Empty,
    Name = "Иванов И. И.",
    Department = "Проектный",
    Rates = new List<Rate>
    {
        new() { From = Utc(2026, 1, 1), Value = 500m },
        new() { From = Utc(2026, 3, 1), Value = 600m }
    }
};

var petrova = new Employee
{
    Id = string.Empty,
    Name = "Петрова А. С.",
    Department = "Проектный",
    Rates = new List<Rate>
    {
        new() { From = Utc(2026, 2, 1), Value = 700m }
    }
};

var employees = db.GetCollection<Employee>(MongoCollections.Employees);
await employees.InsertOneAsync(ivanov);
await employees.InsertOneAsync(petrova);

// --- проекты ---

var p001 = new Project
{
    Id = string.Empty,
    Code = "П-001",
    Name = "Реконструкция цеха",
    Budget = 20000m,
    StartDate = Utc(2026, 1, 1),
    EndDate = Utc(2026, 3, 31)
};

var p002 = new Project
{
    Id = string.Empty,
    Code = "П-002",
    Name = "Инженерные сети",
    Budget = 5000m,
    StartDate = Utc(2026, 3, 1),
    EndDate = null // бессрочный
};

var projects = db.GetCollection<Project>(MongoCollections.Projects);
await projects.InsertOneAsync(p001);
await projects.InsertOneAsync(p002);

// --- записи табеля (ожидаемые стоимости: 4000, 4800, 2800, 7000) ---

var now = DateTime.UtcNow;
TimeEntry Entry(string emp, string prj, int month, int day, double hours) => new()
{
    Id = string.Empty,
    EmployeeId = emp,
    ProjectId = prj,
    Date = Utc(2026, month, day),
    Hours = hours,
    Comment = null,
    Version = 1,
    CreatedAt = now,
    CreatedBy = "seeder"
};

var entries = db.GetCollection<TimeEntry>(MongoCollections.TimeEntries);
await entries.InsertOneAsync(Entry(ivanov.Id, p001.Id, month: 2, day: 20, hours: 8));  // 8 × 500
await entries.InsertOneAsync(Entry(ivanov.Id, p001.Id, month: 3, day: 5, hours: 8));   // 8 × 600
await entries.InsertOneAsync(Entry(petrova.Id, p001.Id, month: 3, day: 5, hours: 4));  // 4 × 700
await entries.InsertOneAsync(Entry(petrova.Id, p002.Id, month: 3, day: 6, hours: 10)); // 10 × 700

Console.WriteLine("Готово:");
Console.WriteLine($"  employees:    2  (Иванов {ivanov.Id}, Петрова {petrova.Id})");
Console.WriteLine($"  projects:     2  (П-001 {p001.Id}, П-002 {p002.Id})");
Console.WriteLine("  time_entries: 4");
Console.WriteLine("Ожидаемо: март — П-001 12ч/7600₽/38%, П-002 10ч/7000₽/140%, итого 22ч/14600₽;");
Console.WriteLine("          февраль — П-001 8ч/4000₽/20%.");