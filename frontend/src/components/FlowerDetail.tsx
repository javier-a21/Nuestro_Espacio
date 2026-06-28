import { useEffect, useState } from 'react';
import { coopApi } from '../api/cooperative';
import type { PlantDetail } from '../types';
import { GardenFlower } from './GardenFlower';

const SPECIES_LABEL: Record<string, string> = { Rosa: '🌹 Rosa', Girasol: '🌻 Girasol' };

function fmtDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short', year: 'numeric' });
}
function daysBetween(a: string, b: string): number {
  const d1 = new Date(a).getTime();
  const d2 = new Date(b).getTime();
  if (Number.isNaN(d1) || Number.isNaN(d2)) return 0;
  return Math.max(1, Math.round((d2 - d1) / 86_400_000));
}
// Asigna un color de autor estable: el primer autor distinto = A, el segundo = B.
function authorClass(notes: { authorName: string }[], author: string): string {
  const authors = [...new Set(notes.map((n) => n.authorName))];
  return authors.indexOf(author) === 0 ? 'by-a' : 'by-b';
}

/** Ficha de una flor: portada + stats + librito de notas de la pareja. */
export function FlowerDetail({ plantId, onClose }: { plantId: string; onClose: () => void }) {
  const [p, setP] = useState<PlantDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    coopApi.plant(plantId).then(setP).catch((e) => setError(String(e?.message ?? e)));
  }, [plantId]);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal flower-detail" onClick={(e) => e.stopPropagation()}>
        {error && <p className="error">{error}</p>}
        {!p && !error && <p>Cargando…</p>}

        {p && (
          <>
            <div className="fd-head">
              <GardenFlower species={p.species} seed={p.seed} size={84} />
              <div>
                <h2>{SPECIES_LABEL[p.species] ?? p.species}</h2>
                <p className="muted">{p.maturedAt ? 'Crecida del todo 🌼' : `En cultivo · fase ${p.growthStage}/${p.maxStage}`}</p>
              </div>
            </div>

            <div className="fd-stats">
              <div><span>Inicio</span><b>{fmtDate(p.startedAt)}</b></div>
              <div><span>Madurez</span><b>{p.maturedAt ? fmtDate(p.maturedAt) : '—'}</b></div>
              <div><span>Días</span><b>{p.maturedAt ? daysBetween(p.startedAt, p.maturedAt) : '—'}</b></div>
              <div><span>🌱 Acciones</span><b>{p.actionsCount}</b></div>
              <div><span>✍️ Notas</span><b>{p.notesCount}</b></div>
            </div>

            <div className="fd-book">
              <h3>📖 Librito de notas</h3>
              {p.notes.length === 0 ? (
                <p className="muted">No se dejaron notas durante su crianza.</p>
              ) : (
                <ul className="note-list">
                  {p.notes.map((n, i) => (
                    <li key={i} className={authorClass(p.notes, n.authorName)}>
                      <span className="nl-author">{n.authorName}</span>
                      <span className="nl-text">“{n.text}”</span>
                      <span className="nl-meta">{fmtDate(n.createdAt)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <button onClick={onClose}>Cerrar</button>
          </>
        )}
      </div>
    </div>
  );
}
