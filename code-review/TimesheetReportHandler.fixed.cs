// Исправленная версия TimesheetReportHandler.cs.
// Исправлены главные проблемы из REVIEW.md:
// 1) вся коллекция больше не грузится в память, фильтр месяца уходит в базу;
// 2) ставка берётся на дату записи, а не первая в истории.
// Заодно при переписывании метода: убран .Result, убраны N+1 запросы,
// добавлены проверки на null и на нулевой бюджет.
// Сознательно не трогал: double для денег и неиспользуемый CancellationToken,
// чтобы правки были минимальными.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;

namespace Demo.Api.Queries.Reports
{
    public class ProjectReportRow
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public double Hours { get; set; }
        public double Amount { get; set; }
        public double Budget { get; set; }
        public double Percent { get; set; }
        public bool Overspent { get; set; }
    }

    public class GetProjectReportQuery : IRequest<List<ProjectReportRow>>
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class TimesheetReportHandler : IRequestHandler<GetProjectReportQuery, List<ProjectReportRow>>
    {
        private readonly IMongoDatabase _db;

        public TimesheetReportHandler(IMongoDatabase db)
        {
            _db = db;
        }

        public async Task<List<ProjectReportRow>> Handle(GetProjectReportQuery request, CancellationToken token)
        {
            // фильтр по месяцу теперь в самом запросе, а не в памяти
            var monthStart = new DateTime(request.Year, request.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            var monthEntries = await _db.GetCollection<TimeEntry>("time_entries")
                .Find(e => e.Date >= monthStart && e.Date < nextMonthStart)
                .ToListAsync();

            if (monthEntries.Count == 0)
            {
                return new List<ProjectReportRow>();
            }

            // справочники грузим один раз списком, а не запросом на каждую запись
            var employeeIds = monthEntries.Select(e => e.EmployeeId).Distinct().ToList();
            var projectIds = monthEntries.Select(e => e.ProjectId).Distinct().ToList();

            var employees = await _db.GetCollection<Employee>("employees")
                .Find(e => employeeIds.Contains(e.Id))
                .ToListAsync();

            var projects = await _db.GetCollection<Project>("projects")
                .Find(p => projectIds.Contains(p.Id))
                .ToListAsync();

            var employeesById = employees.ToDictionary(e => e.Id);
            var projectsById = projects.ToDictionary(p => p.Id);

            var rows = new Dictionary<string, ProjectReportRow>();

            foreach (var entry in monthEntries)
            {
                if (!employeesById.TryGetValue(entry.EmployeeId, out var employee) ||
                    !projectsById.TryGetValue(entry.ProjectId, out var project))
                {
                    // запись без сотрудника или проекта посчитать нельзя,
                    // раньше на этом падал весь отчёт
                    continue;
                }

                var rate = GetRateForDate(employee.Rates, entry.Date);
                if (rate == null)
                {
                    // на дату записи ставки нет, стоимость не считаем
                    continue;
                }

                var amount = Math.Round(entry.Hours * rate.Value, 2);

                if (!rows.TryGetValue(entry.ProjectId, out var row))
                {
                    row = new ProjectReportRow
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        Budget = project.Budget
                    };
                    rows[entry.ProjectId] = row;
                }

                row.Hours += entry.Hours;
                row.Amount += amount;
            }

            foreach (var row in rows.Values)
            {
                // бюджет может быть нулём, делить нельзя
                row.Percent = row.Budget == 0
                    ? 0
                    : Math.Round(row.Amount / row.Budget * 100, 2);
                row.Overspent = row.Percent > 100;
            }

            return rows.Values.OrderBy(r => r.ProjectName).ToList();
        }

        // последняя ставка, которая начала действовать не позже даты записи
        private static Rate GetRateForDate(List<Rate> rates, DateTime date)
        {
            Rate result = null;

            foreach (var rate in rates)
            {
                if (rate.From <= date && (result == null || rate.From > result.From))
                {
                    result = rate;
                }
            }

            return result;
        }
    }

    // --- сущности (те же, что в оригинале) ---

    public class TimeEntry
    {
        public string Id { get; set; }
        public string EmployeeId { get; set; }
        public string ProjectId { get; set; }
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public string Comment { get; set; }
    }

    public class Employee
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Rate> Rates { get; set; }
    }

    public class Rate
    {
        public DateTime From { get; set; }
        public double Value { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Budget { get; set; }
    }
}
