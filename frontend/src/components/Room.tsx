import { useState } from 'react';
import { useStore } from '../state/store';
import { RoomCanvas } from '../scene/RoomCanvas';
import { hubActions } from '../realtime/connection';
import { TimeZoneModal } from './TimeZoneModal';
import { Album } from './Album';
import { Greenhouse } from './Greenhouse';
import { FlowerDetail } from './FlowerDetail';
import { CorkBoard } from './CorkBoard';

const SPECIES_NAMES: Record<string, string> = { Rosa: '🌹 Rosa', Girasol: '🌻 Girasol' };
const HEALTH_NAMES: Record<string, string> = { Healthy: '🌿 Sana', Wilting: '🥀 Marchitándose', Wilted: '🥀 Marchita' };
const EVENT_LABEL: Record<string, string> = { Heatwave: 'ola de calor 🔥', ColdSnap: 'helada ❄️' };
const PROTECT_LABEL: Record<string, string> = { Heatwave: '☀️ Dar sombra', ColdSnap: '🧣 Cubrir' };

export function Room() {
  const room = useStore((s) => s.room)!;
  const auth = useStore((s) => s.auth)!;
  const logout = useStore((s) => s.logout);

  const [tzOpen, setTzOpen] = useState(false);
  const [albumOpen, setAlbumOpen] = useState(false);
  const [greenhouseOpen, setGreenhouseOpen] = useState(false);
  const [corkOpen, setCorkOpen] = useState(false);
  const [detailId, setDetailId] = useState<string | null>(null);

  const me = room.members.find((m) => m.id === auth.userId);
  const myAction = me?.role === 'A' ? 'Regar' : 'Podar';
  const actedToday = me?.actedToday ?? false;
  const actedCount = room.members.filter((m) => m.actedToday).length;
  const bloomedToday = room.todayBloom != null;

  const plant = room.activePlant;
  const health = plant?.health ?? 'Healthy';
  const wilted = health !== 'Healthy';
  const growthPct = plant ? Math.round((plant.growthStage / plant.maxStage) * 100) : 0;

  return (
    <div className="room">
      <div className="stage">
        <RoomCanvas onPlantClick={setDetailId} onBoardClick={() => setCorkOpen(true)} />
      </div>

      <div className="hud">
        {room.event && (
          <div className={`event-banner ${room.event.type === 'Heatwave' ? 'heat' : 'cold'}${room.event.handled ? ' ok' : ''}`}>
            {room.event.handled ? (
              <span>✅ Planta protegida frente a la {EVENT_LABEL[room.event.type] ?? 'inclemencia'}.</span>
            ) : room.event.stage === 'Forecast' ? (
              <>
                <span>⚠️ Pronóstico: se acerca una <b>{EVENT_LABEL[room.event.type]}</b>. Preparad la planta.</span>
                <button className="protect" onClick={() => hubActions.protect()}>{PROTECT_LABEL[room.event.type]}</button>
              </>
            ) : (
              <>
                <span>{room.event.type === 'Heatwave' ? '🔥' : '❄️'} ¡{EVENT_LABEL[room.event.type]} en curso! Protege la planta ya.</span>
                <button className="protect" onClick={() => hubActions.protect()}>{PROTECT_LABEL[room.event.type]}</button>
              </>
            )}
          </div>
        )}

        {plant && (
          <div className={`plant-panel${wilted ? ' wilted' : ''}`}>
            <div className="plant-row">
              <span>{SPECIES_NAMES[plant.species] ?? plant.species}</span>
              <span>Fase <b>{plant.growthStage}/{plant.maxStage}</b></span>
              <span>{HEALTH_NAMES[health] ?? health}</span>
            </div>
            <div className="growth-bar"><div className="growth-fill" style={{ width: `${growthPct}%` }} /></div>
            {wilted && (
              <div className="wilt-warn">
                <span>Tu planta se ha marchitado. Abonadla para que vuelva a crecer.</span>
                <button className="abonar" onClick={() => hubActions.abonar()}>🌱 Abonar</button>
              </div>
            )}
          </div>
        )}

        {bloomedToday ? (
          <div className="bloom-banner">🌸 ¡Brote del día! Hoy ambos habéis cuidado la planta.</div>
        ) : (
          <div className="progress-banner">🌱 Ritual de hoy: {actedCount}/2 — al completarlo, la planta crece una fase.</div>
        )}

        <div className="status">
          <span>Racha: <b>{room.currentStreak}</b></span>
          <span>🪴 Invernadero: <b>{room.greenhouseCount}</b></span>
          <span>{room.bothOnline ? '🟢 Ambos presentes' : '⚪ Esperando…'}</span>
        </div>

        <button disabled={actedToday} onClick={() => hubActions.performAction()}>
          {actedToday ? `✔ ${myAction} hecho hoy` : myAction}
        </button>

        <button onClick={() => setCorkOpen(true)}>📌 Abrir el corcho (notas y fotos)</button>

        <button className="link" onClick={() => setGreenhouseOpen(true)}>🪴 Invernadero ({room.greenhouseCount})</button>
        <button className="link" onClick={() => setAlbumOpen(true)}>🌸 Nuestro ramo</button>
        <button className="link" onClick={() => setTzOpen(true)}>
          Zona horaria: {room.timeZoneId}
          {room.pendingTimeZoneId ? ` → ${room.pendingTimeZoneId} (pendiente)` : ''}
        </button>
        <button className="link" onClick={logout}>Cerrar sesión</button>
      </div>

      {tzOpen && <TimeZoneModal onClose={() => setTzOpen(false)} />}
      {albumOpen && <Album onClose={() => setAlbumOpen(false)} />}
      {greenhouseOpen && <Greenhouse onClose={() => setGreenhouseOpen(false)} />}
      {corkOpen && <CorkBoard onClose={() => setCorkOpen(false)} />}
      {detailId && <FlowerDetail plantId={detailId} onClose={() => setDetailId(null)} />}
    </div>
  );
}
