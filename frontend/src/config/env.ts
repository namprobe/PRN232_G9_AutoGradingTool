export function useApiMock(): boolean {
  return import.meta.env.VITE_USE_API_MOCK === "true";
}
