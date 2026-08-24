import type { MonthKey } from "../api/types";

export default function TimesheetPage({ month }: { month: MonthKey }) {
  return (
    <p>
      Экран «Табель» за {String(month.month).padStart(2, "0")}.{month.year} — будет реализован
      на следующем шаге.
    </p>
  );
}