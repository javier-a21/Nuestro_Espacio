import { API_BASE } from '../config';
import { useStore } from '../state/store';

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = useStore.getState().auth?.token;
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {}),
    },
  });

  if (!res.ok) {
    let message = `Error ${res.status}`;
    try {
      const body = await res.json();
      if (typeof body === 'string') message = body;
      else if (Array.isArray(body)) message = body.join(', ');
    } catch {
      // respuesta sin cuerpo JSON
    }
    throw new Error(message);
  }

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: 'POST', body: body !== undefined ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
};
