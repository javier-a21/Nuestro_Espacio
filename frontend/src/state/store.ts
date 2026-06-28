import { create } from 'zustand';
import type { AuthInfo, Bloom, RoomState } from '../types';

interface AppState {
  auth: AuthInfo | null;
  room: RoomState | null;
  // Canales efímeros de evento (la escena los consume para animar):
  actionPulse: number; // se incrementa cada vez que alguien hace su acción
  pendingBloom: Bloom | null; // brote recién acuñado pendiente de celebrar
  setAuth: (auth: AuthInfo | null) => void;
  setRoom: (room: RoomState | null) => void;
  patchRoom: (patch: Partial<RoomState>) => void;
  bumpAction: () => void;
  setPendingBloom: (bloom: Bloom | null) => void;
  logout: () => void;
}

const AUTH_KEY = 'coop.auth';

function loadAuth(): AuthInfo | null {
  try {
    const raw = localStorage.getItem(AUTH_KEY);
    return raw ? (JSON.parse(raw) as AuthInfo) : null;
  } catch {
    return null;
  }
}

export const useStore = create<AppState>()((set) => ({
  auth: loadAuth(),
  room: null,
  actionPulse: 0,
  pendingBloom: null,
  setAuth: (auth) => {
    if (auth) localStorage.setItem(AUTH_KEY, JSON.stringify(auth));
    else localStorage.removeItem(AUTH_KEY);
    set({ auth });
  },
  setRoom: (room) => set({ room }),
  patchRoom: (patch) => set((s) => ({ room: s.room ? { ...s.room, ...patch } : s.room })),
  bumpAction: () => set((s) => ({ actionPulse: s.actionPulse + 1 })),
  setPendingBloom: (bloom) => set({ pendingBloom: bloom }),
  logout: () => {
    localStorage.removeItem(AUTH_KEY);
    set({ auth: null, room: null, pendingBloom: null });
  },
}));
