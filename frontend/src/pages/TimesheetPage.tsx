import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deleteEntry, getEmployees, getPeriods, getProjects, getTimeEntries } from "../api/endpoints";
import type { MonthKey, TimeEntry } from "../api/types";
import EntryModal from "../components/EntryModal";
import ErrorBanner from "../components/ErrorBanner";

const PAGE_SIZE = 20;

const money = (value: number) =>
  value.toLocaleString("ru-RU", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const ruDate = (isoDate: string) =>
  new Date(`${isoDate}T00:00:00`).toLocaleDateString("ru-RU");

export default function TimesheetPage({ month }: { month: MonthKey }) {
  const [employeeId, setEmployeeId] = useState("");
  const [projectId, setProjectId] = useState("");
  const [page, setPage] = useState(1);
  const [modalEntry, setModalEntry] = useState<TimeEntry | "new" | null>(null);
  const [actionError, setActionError] = useState<unknown>(null);
  const queryClient = useQueryClient();

  const employeesQuery = useQuery({ queryKey: ["employees"], queryFn: getEmployees });
  const projectsQuery = useQuery({ queryKey: ["projects"], queryFn: getProjects });
  const periodsQuery = useQuery({ queryKey: ["periods"], queryFn: getPeriods });

  const entriesQuery = useQuery({
    queryKey: ["time-entries", month.year, month.month, employeeId, projectId, page],
    queryFn: () =>
      getTimeEntries({
        year: month.year,
        month: month.month,
        employeeId: employeeId || undefined,
        projectId: projectId || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
  });

  const isClosed =
    periodsQuery.data?.some((p) => p.year === month.year && p.month === month.month) ?? false;

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteEntry(id),
    onSuccess: () => {
      setActionError(null);
      queryClient.invalidateQueries({ queryKey: ["time-entries"] });
    },
    onError: (e) => setActionError(e),
  });

  const changeFilter = (setter: (v: string) => void) => (value: string) => {
    setter(value);
    setPage(1);
  };

  const data = entriesQuery.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  const closedHint = "Период закрыт бухгалтерией";

  return (
    <section>
      <div className="toolbar">
        <select
          value={employeeId}
          onChange={(e) => changeFilter(setEmployeeId)(e.target.value)}
        >
          <option value="">Все сотрудники</option>
          {(employeesQuery.data ?? []).map((emp) => (
            <option key={emp.id} value={emp.id}>{emp.name}</option>
          ))}
        </select>

        <select
          value={projectId}
          onChange={(e) => changeFilter(setProjectId)(e.target.value)}
        >
          <option value="">Все проекты</option>
          {(projectsQuery.data ?? []).map((p) => (
            <option key={p.id} value={p.id}>{p.code} — {p.name}</option>
          ))}
        </select>

        <button
          disabled={isClosed}
          title={isClosed ? closedHint : undefined}
          onClick={() => setModalEntry("new")}
        >
          + Добавить запись
        </button>
      </div>

      {isClosed && (
        <div className="notice">
          Период {String(month.month).padStart(2, "0")}.{month.year} закрыт бухгалтерией —
          создание, изменение и удаление записей запрещено.
        </div>
      )}

      <ErrorBanner error={entriesQuery.error} />
      <ErrorBanner error={actionError} />

      {entriesQuery.isLoading && <p>Загрузка…</p>}

      {data && (
        <>
          <table>
            <thead>
              <tr>
                <th>Дата</th>
                <th>Сотрудник</th>
                <th>Проект</th>
                <th className="num">Часы</th>
                <th className="num">Ставка</th>
                <th className="num">Стоимость</th>
                <th>Комментарий</th>
                <th>Переработка</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.items.map((entry) => (
                <tr key={entry.id} className={entry.overtime ? "row-overtime" : undefined}>
                  <td>{ruDate(entry.date)}</td>
                  <td>{entry.employeeName}</td>
                  <td>{entry.projectCode} — {entry.projectName}</td>
                  <td className="num">{entry.hours.toLocaleString("ru-RU")}</td>
                  <td className="num">{money(entry.rate)}</td>
                  <td className="num">{money(entry.amount)}</td>
                  <td>{entry.comment}</td>
                  <td>
                    {entry.overtime && (
                      <span className="badge badge-overtime">переработка</span>
                    )}
                  </td>
                  <td>
                    <button
                      disabled={isClosed}
                      title={isClosed ? closedHint : undefined}
                      onClick={() => setModalEntry(entry)}
                    >
                      Изменить
                    </button>{" "}
                    <button
                      disabled={isClosed}
                      title={isClosed ? closedHint : undefined}
                      onClick={() => handleDelete(entry)}
                    >
                      Удалить
                    </button>
                  </td>
                </tr>
              ))}
              {data.items.length === 0 && (
                <tr>
                  <td colSpan={9}>Записей за выбранный месяц нет.</td>
                </tr>
              )}
            </tbody>
          </table>

          <div className="totals">
            Итого по отфильтрованному списку: часы — {data.totalHours.toLocaleString("ru-RU")},
            стоимость — {money(data.totalAmount)} ₽
          </div>

          <div className="pager">
            <button disabled={page <= 1} onClick={() => setPage(page - 1)}>← Назад</button>
            <span>Страница {data.page} из {totalPages}</span>
            <button disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Вперёд →</button>
            <span className="muted">Всего записей: {data.totalCount}</span>
          </div>
        </>
      )}

      {modalEntry !== null && employeesQuery.data && projectsQuery.data && (
        <EntryModal
          month={month}
          entry={modalEntry === "new" ? null : modalEntry}
          employees={employeesQuery.data}
          projects={projectsQuery.data}
          onClose={() => setModalEntry(null)}
        />
      )}
    </section>
  );

  function handleDelete(entry: TimeEntry) {
    if (window.confirm(`Удалить запись от ${ruDate(entry.date)} (${entry.employeeName})?`)) {
      deleteMutation.mutate(entry.id);
    }
  }
}