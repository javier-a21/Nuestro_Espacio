import { api } from './client';
import type { Bloom, GardenItem, Photo, PlantDetail, RoomState } from '../types';

export const coopApi = {
  create: (name?: string) =>
    api.post<{ cooperativeId: string; inviteCode: string }>('/api/cooperative', { name }),
  join: (inviteCode: string) => api.post<void>('/api/cooperative/join', { inviteCode }),
  state: () => api.get<RoomState>('/api/cooperative/state'),
  blooms: () => api.get<Bloom[]>('/api/cooperative/blooms'),
  garden: () => api.get<GardenItem[]>('/api/cooperative/garden'),
  plant: (id: string) => api.get<PlantDetail>(`/api/cooperative/plant/${id}`),
  photos: () => api.get<Photo[]>('/api/cooperative/photos'),
  setPhoto: (slot: number, dataUrl: string) => api.post<void>('/api/cooperative/photo', { slot, dataUrl }),
  deletePhoto: (slot: number) => api.del<void>(`/api/cooperative/photo/${slot}`),
};
