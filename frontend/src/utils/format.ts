export const money = (value: number) =>
  value.toLocaleString("ru-RU", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export const ruDate = (isoDate: string) =>
  new Date(`${isoDate}T00:00:00`).toLocaleDateString("ru-RU");

export const pad = (n: number) => String(n).padStart(2, "0");