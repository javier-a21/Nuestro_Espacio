import { useEffect, useState } from 'react';
import { coopApi } from '../api/cooperative';
import type { GardenItem } from '../types';
import { GardenFlower } from './GardenFlower';
import { FlowerDetail } from './FlowerDetail';

const SPECIES_LABEL: Record<string, string> = { Rosa: '🌹 Rosa', Girasol: '🌻 Girasol' };

function fmt(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short', year: 'numeric' });
}

/** El invernadero: todas las flores ya crecidas de la cooperativa. Se riegan solas. */
export function Greenhouse({ onClose }: { onClose: () => void }) {
  const [items, setItems] = useState<GardenItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);

  useEffect(() => {
    coopApi.garden().then(setItems).catch((e) => setError(String(e?.message ?? e)));
  }, []);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal greenhouse" onClick={(e) => e.stopPropagation()}>
        <h2>🪴 Invernadero {items ? `(${items.length})` : ''}</h2>
        <p className="muted">Vuestras flores ya crecidas viven aquí y se riegan solas 💧</p>

        {error && <p className="error">{error}</p>}
        {!items && !error && <p>Cargando…</p>}
        {items?.length === 0 && (
          <p className="muted">Aún no habéis criado ninguna flor del todo. ¡La primera está en camino! 🌱</p>
        )}

        {items && items.length > 0 && (
          <div className="green-grid">
            {items.map((it) => (
              <button className="green-card" key={it.id} onClick={() => setDetailId(it.id)} title="Ver ficha">
                <span className="auto-water" />
                <GardenFlower species={it.species} seed={it.seed} size={84} />
                <div className="green-name">{SPECIES_LABEL[it.species] ?? it.species}</div>
                <div className="green-meta">{fmt(it.maturedAt)}</div>
                <div className="green-meta">🌱 {it.actionsCount} · ✍️ {it.notesCount}</div>
              </button>
            ))}
          </div>
        )}

        <button onClick={onClose}>Cerrar</button>
      </div>

      {detailId && <FlowerDetail plantId={detailId} onClose={() => setDetailId(null)} />}
    </div>
  );
}
