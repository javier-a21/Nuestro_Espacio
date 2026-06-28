import { useEffect, useState } from 'react';
import { coopApi } from '../api/cooperative';
import type { Bloom } from '../types';
import { Flower } from './Flower';

function formatDate(iso: string): string {
  // iso = "yyyy-mm-dd"; lo mostramos en formato local corto.
  const [y, m, d] = iso.split('-').map(Number);
  if (!y || !m || !d) return iso;
  return new Date(y, m - 1, d).toLocaleDateString('es-ES', { day: 'numeric', month: 'short', year: 'numeric' });
}

/** El "ramo": colección de brotes (recuerdos) acumulados por la cooperativa. */
export function Album({ onClose }: { onClose: () => void }) {
  const [blooms, setBlooms] = useState<Bloom[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    coopApi.blooms().then(setBlooms).catch((e) => setError(String(e?.message ?? e)));
  }, []);

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal album" onClick={(e) => e.stopPropagation()}>
        <h2>Nuestro ramo {blooms ? `(${blooms.length})` : ''}</h2>

        {error && <p className="error">{error}</p>}
        {!blooms && !error && <p>Cargando…</p>}
        {blooms?.length === 0 && <p className="muted">Aún no hay flores. Cuidad la planta juntos hoy 🌱</p>}

        {blooms && blooms.length > 0 && (
          <div className="bouquet">
            {blooms.map((b) => (
              <div className="bloom-card" key={b.date} title={`Racha ${b.streak} · ${b.weather}`}>
                <Flower bloom={b} size={56} />
                <div className="bloom-date">{formatDate(b.date)}</div>
                {b.note && <div className="bloom-note">“{b.note}”</div>}
              </div>
            ))}
          </div>
        )}

        <button onClick={onClose}>Cerrar</button>
      </div>
    </div>
  );
}
