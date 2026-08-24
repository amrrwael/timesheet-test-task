import type { FieldError } from "./types";

/** Ошибка бизнес-правил/валидации с сервера — машиночитаемый код + русский текст. */
export class ApiError extends Error {
  code: string;
  fieldErrors: FieldError[];

  constructor(code: string, message: string, fieldErrors: FieldError[] = []) {
    super(message);
    this.name = "ApiError";
    this.code = code;
    this.fieldErrors = fieldErrors;
  }
}

interface ErrorBody {
  code?: string;
  message?: string;
  errors?: FieldError[];
}

async function request<T>(url: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(url, init);
  } catch {
    throw new Error("Сервер недоступен. Проверьте, что API запущен (http://localhost:5000).");
  }

  if (response.status === 204) return undefined as T;

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    const error = body as ErrorBody | null;
    if (error?.code) {
      throw new ApiError(error.code, error.message ?? "Ошибка запроса", error.errors ?? []);
    }
    throw new Error(`Ошибка HTTP ${response.status}`);
  }

  return body as T;
}

function jsonInit(method: string, data: unknown): RequestInit {
  return {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(data),
  };
}

export const api = {
  get: <T>(url: string) => request<T>(url),
  put: <T>(url: string, data: unknown) => request<T>(url, jsonInit("PUT", data)),
  post: <T>(url: string, data: unknown) => request<T>(url, jsonInit("POST", data)),
  delete: (url: string) => request<void>(url, { method: "DELETE" }),
};