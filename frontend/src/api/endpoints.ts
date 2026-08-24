import { api } from "./client";
import type {
  ClosedPeriod,
  CreateTimeEntryRequest,
  Employee,
  Project,
  ProjectReport,
  TimeEntriesPage,
  TimeEntry,
  TimeEntryFilter,
  UpdateTimeEntryRequest,
} from "./types";

const qs = (params: Record<string, string | number | undefined>) => {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") search.set(key, String(value));
  }
  const s = search.toString();
  return s ? `?${s}` : "";
};

export const getEmployees = () => api.get<Employee[]>("/api/employees");

export const getProjects = () => api.get<Project[]>("/api/projects");

export const getPeriods = () => api.get<ClosedPeriod[]>("/api/periods");

export const getTimeEntries = (filter: TimeEntryFilter) =>
  api.get<TimeEntriesPage>(
    "/api/time-entries" +
      qs({
        year: filter.year,
        month: filter.month,
        employeeId: filter.employeeId,
        projectId: filter.projectId,
        page: filter.page,
        pageSize: filter.pageSize,
      }),
  );

export const createEntry = (data: CreateTimeEntryRequest) =>
  api.put<TimeEntry>("/api/time-entries", data);

export const updateEntry = (id: string, data: UpdateTimeEntryRequest) =>
  api.post<TimeEntry>(`/api/time-entries/${id}`, data);

export const deleteEntry = (id: string) => api.delete(`/api/time-entries/${id}`);
export const getProjectReport = (year: number, month: number) =>
  api.get<ProjectReport>(`/api/reports/projects${qs({ year, month })}`);