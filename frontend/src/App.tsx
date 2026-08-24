import { useState } from "react";
import TimesheetPage from "./pages/TimesheetPage";
import ReportPage from "./pages/ReportPage";
import type { MonthKey } from "./api/types";

type Tab = "timesheet" | "report";

const now = new Date();

export default function App() {
  const [tab, setTab] = useState<Tab>("timesheet");
  const [month, setMonth] = useState<MonthKey>({
    year: now.getFullYear(),
    month: now.getMonth() + 1,
  });

  const shiftMonth = (delta: number) =>
    setMonth((m) => {
      const d = new Date(m.year, m.month - 1 + delta, 1);
      return { year: d.getFullYear(), month: d.getMonth() + 1 };
    });

  return (
    <div className="app">
      <header className="app-header">
        <h1>Учёт трудозатрат</h1>

        <div className="month-switcher">
          <button aria-label="Предыдущий месяц" onClick={() => shiftMonth(-1)}>←</button>
          <span className="month-label">
            {String(month.month).padStart(2, "0")}.{month.year}
          </span>
          <button aria-label="Следующий месяц" onClick={() => shiftMonth(1)}>→</button>
        </div>

        <nav className="tabs">
          <button
            className={tab === "timesheet" ? "active" : ""}
            onClick={() => setTab("timesheet")}
          >
            Табель
          </button>
          <button
            className={tab === "report" ? "active" : ""}
            onClick={() => setTab("report")}
          >
            Отчёт по проектам
          </button>
        </nav>
      </header>

      <main>
        {tab === "timesheet"
          ? <TimesheetPage month={month} />
          : <ReportPage month={month} />}
      </main>
    </div>
  );
}