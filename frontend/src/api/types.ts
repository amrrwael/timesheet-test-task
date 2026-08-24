export interface MonthKey {
  year: number;
  month: number;
}

export interface Rate {
  value: number;
  from: string;
}

export interface Employee {
  id: string;
  name: string;
  department: string;
  rates: Rate[];
}

export interface Project {
  id: string;
  code: string;
  name: string;
  budget: number;
  startDate: string;
  endDate: string | null;
}

export interface TimeEntry {
  id: string;
  employeeId: string;
  employeeName: string;
  projectId: string;
  projectCode: string;
  projectName: string;
  date: string;
  hours: number;
  rate: number;
  amount: number;
  comment: string | null;
  version: number;
  overtime: boolean;
}

export interface TimeEntriesPage {
  items: TimeEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalHours: number;
  totalAmount: number;
}

export interface TimeEntryFilter {
  year: number;
  month: number;
  employeeId?: string;
  projectId?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateTimeEntryRequest {
  employeeId: string;
  projectId: string;
  date: string;
  hours: number;
  comment?: string | null;
}

export interface UpdateTimeEntryRequest extends CreateTimeEntryRequest {
  version: number;
}

export interface ProjectReportRow {
  projectId: string;
  code: string;
  name: string;
  budget: number;
  hours: number;
  amount: number;
  percent: number | null;
  overspent: boolean;
  atRisk: boolean;
}

export interface ProjectReport {
  projects: ProjectReportRow[];
  totalHours: number;
  totalAmount: number;
}

export interface ClosedPeriod {
  year: number;
  month: number;
  closedAt: string;
}

export interface FieldError {
  field: string;
  message: string;
}