import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "../api/client";
import { createEntry, updateEntry } from "../api/endpoints";
import type { Employee, MonthKey, Project, TimeEntry } from "../api/types";

interface Props {
  month: MonthKey;
  /** null — создание, иначе редактирование существующей записи. */
  entry: TimeEntry | null;
  employees: Employee[];
  projects: Project[];
  onClose: () => void;
}

const pad = (n: number) => String(n).padStart(2, "0");

export default function EntryModal({ month, entry, employees, projects, onClose }: Props) {
  const isEdit = entry !== null;

  const [employeeId, setEmployeeId] = useState(entry?.employeeId ?? "");
  const [projectId, setProjectId] = useState(entry?.projectId ?? "");
  const [date, setDate] = useState(entry?.date ?? `${month.year}-${pad(month.month)}-01`);
  const [hours, setHours] = useState(entry ? String(entry.hours) : "");
  const [comment, setComment] = useState(entry?.comment ?? "");
  const [serverError, setServerError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const queryClient = useQueryClient();

  const saveMutation = useMutation({
    mutationFn: () => {
      const hoursNumber = Number(hours.replace(",", "."));
      return isEdit
        ? updateEntry(entry.id, {
            employeeId, projectId, date,
            hours: hoursNumber,
            comment: comment.trim() || null,
            version: entry.version,
          })
        : createEntry({
            employeeId, projectId, date,
            hours: hoursNumber,
            comment: comment.trim() || null,
          });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["time-entries"] });
      onClose();
    },
    onError: (e) => {
      if (e instanceof ApiError) {
        setServerError(e.message);
        const map: Record<string, string> = {};
        for (const fe of e.fieldErrors) map[fe.field] = fe.message;
        setFieldErrors(map);
      } else {
        setServerError(e instanceof Error ? e.message : String(e));
      }
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setServerError(null);
    setFieldErrors({});

    const next: Record<string, string> = {};
    const hoursNumber = Number(hours.replace(",", "."));

    if (!employeeId) next.employeeId = "Выберите сотрудника.";
    if (!projectId) next.projectId = "Выберите проект.";
    if (!date) next.date = "Укажите дату.";
    if (!Number.isFinite(hoursNumber) || hoursNumber <= 0)
      next.hours = "Часы должны быть положительным числом, кратным 0,5 и не больше 24.";

    if (Object.keys(next).length > 0) {
      setFieldErrors(next);
      return;
    }

    saveMutation.mutate();
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <form className="modal" onClick={(e) => e.stopPropagation()} onSubmit={submit}>
        <h3>{isEdit ? "Изменить запись" : "Новая запись"}</h3>

        {serverError && (
          <div className="error-banner" role="alert">{serverError}</div>
        )}

        <label>
          Сотрудник
          <select value={employeeId} onChange={(e) => setEmployeeId(e.target.value)}>
            <option value="">— выберите —</option>
            {employees.map((emp) => (
              <option key={emp.id} value={emp.id}>{emp.name}</option>
            ))}
          </select>
          {fieldErrors.employeeId && <span className="field-error">{fieldErrors.employeeId}</span>}
        </label>

        <label>
          Проект
          <select value={projectId} onChange={(e) => setProjectId(e.target.value)}>
            <option value="">— выберите —</option>
            {projects.map((p) => (
              <option key={p.id} value={p.id}>{p.code} — {p.name}</option>
            ))}
          </select>
          {fieldErrors.projectId && <span className="field-error">{fieldErrors.projectId}</span>}
        </label>

        <label>
          Дата
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          {fieldErrors.date && <span className="field-error">{fieldErrors.date}</span>}
        </label>

        <label>
          Часы (кратно 0,5, максимум 24)
          <input
            type="number"
            step="0.5"
            min="0.5"
            max="24"
            value={hours}
            onChange={(e) => setHours(e.target.value)}
          />
          {fieldErrors.hours && <span className="field-error">{fieldErrors.hours}</span>}
        </label>

        <label>
          Комментарий
          <input
            type="text"
            maxLength={500}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
          />
          {fieldErrors.comment && <span className="field-error">{fieldErrors.comment}</span>}
        </label>

        <div className="modal-actions">
          <button type="button" onClick={onClose}>Отмена</button>
          <button type="submit" disabled={saveMutation.isPending}>
            {saveMutation.isPending ? "Сохранение…" : "Сохранить"}
          </button>
        </div>
      </form>
    </div>
  );
}