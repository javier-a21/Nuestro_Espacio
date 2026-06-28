export interface AuthInfo {
  token: string;
  userId: string;
  displayName: string;
  cooperativeId: string | null;
  role: string | null;
}

export interface MemberStatus {
  id: string;
  displayName: string;
  role: string; // "A" | "B"
  actedToday: boolean;
  online: boolean;
  note: string | null; // post-it personal de ese miembro
}

export interface Bloom {
  date: string; // ISO (yyyy-mm-dd) en el huso compartido
  seed: number;
  weather: string; // "Clear" | "Cloudy" | "Rain" | "Sunny"
  streak: number;
  note: string | null;
  createdAt: string;
}

export interface ActivePlant {
  id: string;
  species: string; // "Rosa" | "Girasol"
  seed: number;
  growthStage: number; // 0..maxStage
  maxStage: number;
  health: string; // "Healthy" | "Wilting" | "Wilted"
  actionsCount: number;
  notesCount: number;
  startedAt: string;
}

export interface GardenItem {
  id: string;
  species: string;
  seed: number;
  actionsCount: number;
  notesCount: number;
  startedAt: string;
  maturedAt: string;
}

export interface ShelfPlant {
  id: string;
  species: string;
  seed: number;
}

export interface WeatherEvent {
  type: string; // "Heatwave" | "ColdSnap"
  stage: string; // "Forecast" | "Active"
  handled: boolean;
}

export interface Photo {
  slot: number; // 0..2
  dataUrl: string; // "data:image/...;base64,..."
}

export interface PlantNote {
  text: string;
  authorName: string;
  createdAt: string;
}

export interface PlantDetail {
  id: string;
  species: string;
  seed: number;
  growthStage: number;
  maxStage: number;
  health: string;
  actionsCount: number;
  notesCount: number;
  startedAt: string;
  maturedAt: string | null;
  notes: PlantNote[];
}

export interface RoomState {
  cooperativeId: string;
  plantLevel: number; // 1..5
  currentStreak: number;
  lastNote: string | null;
  lastNoteAuthorId: string | null;
  lastNoteAt: string | null;
  timeZoneId: string;
  pendingTimeZoneId: string | null;
  weather: string; // "Clear" | "Cloudy" | "Rain" | "Sunny"
  bothOnline: boolean;
  members: MemberStatus[];
  todayBloom: Bloom | null; // brote del día si AMBOS ya cumplieron hoy
  activePlant: ActivePlant | null; // planta en cultivo (fase + salud)
  roomPlants: ShelfPlant[]; // hasta 4 maduras que decoran la sala
  greenhouseCount: number; // nº de flores maduras en el invernadero (excedente >4)
  event: WeatherEvent | null; // evento climático activo (frío/calor)
  photoSlots: number[]; // ranuras del corcho con foto (0..2)
}
