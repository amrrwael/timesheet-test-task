import type { MonthKey } from "../api/types";

export default function ReportPage({ month }: { month: MonthKey }) {
  return (
    <p>
      Экран «Отчёт по проектам» за {String(month.month).padStart(2, "0")}.{month.year} — будет
      реализован на следующем шаге.
    </p>
  );
}