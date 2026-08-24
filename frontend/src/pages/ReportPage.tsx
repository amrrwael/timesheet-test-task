import { useQuery } from "@tanstack/react-query";
import { getProjectReport } from "../api/endpoints";
import type { MonthKey, ProjectReportRow } from "../api/types";
import ErrorBanner from "../components/ErrorBanner";
import { money } from "../utils/format";

const rowClass = (row: ProjectReportRow) =>
  row.overspent ? "row-overspent" : row.atRisk ? "row-atrisk" : undefined;

export default function ReportPage({ month }: { month: MonthKey }) {
  const reportQuery = useQuery({
    queryKey: ["project-report", month.year, month.month],
    queryFn: () => getProjectReport(month.year, month.month),
  });

  const data = reportQuery.data;

  return (
    <section>
      <h2>
        Отчёт по проектам за {String(month.month).padStart(2, "0")}.{month.year}
      </h2>

      <ErrorBanner error={reportQuery.error} />
      {reportQuery.isLoading && <p>Загрузка…</p>}

      {data && (
        <>
          <table>
            <thead>
              <tr>
                <th>Проект</th>
                <th className="num">Часы</th>
                <th className="num">Стоимость</th>
                <th className="num">Бюджет</th>
                <th className="num">Освоено</th>
                <th>Статус</th>
              </tr>
            </thead>
            <tbody>
              {data.projects.map((row) => (
                <tr key={row.projectId} className={rowClass(row)}>
                  <td>{row.code} — {row.name}</td>
                  <td className="num">{row.hours.toLocaleString("ru-RU")}</td>
                  <td className="num">{money(row.amount)}</td>
                  <td className="num">{money(row.budget)}</td>
                  <td className="num">
                    {row.percent === null ? "—" : `${row.percent.toLocaleString("ru-RU")} %`}
                  </td>
                  <td>
                    {row.overspent && <span className="badge badge-overtime">перерасход</span>}
                    {row.atRisk && <span className="badge badge-risk">риск</span>}
                  </td>
                </tr>
              ))}
              {data.projects.length === 0 && (
                <tr>
                  <td colSpan={6}>За выбранный месяц трудозатрат нет.</td>
                </tr>
              )}
              {data.projects.length > 0 && (
                <tr className="total">
                  <td>Итого</td>
                  <td className="num">{data.totalHours.toLocaleString("ru-RU")}</td>
                  <td className="num">{money(data.totalAmount)}</td>
                  <td />
                  <td />
                  <td />
                </tr>
              )}
            </tbody>
          </table>
          <p className="muted">
            Риск — освоено более 80 % бюджета; перерасход — более 100 %.
          </p>
        </>
      )}
    </section>
  );
}