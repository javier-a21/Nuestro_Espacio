import { useEffect, useRef, useState } from 'react';
import { coopApi } from '../api/cooperative';
import { hubActions } from '../realtime/connection';
import { useStore } from '../state/store';
import type { Photo } from '../types';

const SLOTS = [0, 1, 2];

/** Reduce una imagen en el navegador antes de subirla (lado máx + JPEG con calidad). */
async function fileToDataUrl(file: File, max = 640, quality = 0.82): Promise<string> {
  const url = URL.createObjectURL(file);
  try {
    const img = await new Promise<HTMLImageElement>((resolve, reject) => {
      const i = new Image();
      i.onload = () => resolve(i);
      i.onerror = reject;
      i.src = url;
    });
    const scale = Math.min(1, max / Math.max(img.width, img.height));
    const w = Math.max(1, Math.round(img.width * scale));
    const h = Math.max(1, Math.round(img.height * scale));
    const canvas = document.createElement('canvas');
    canvas.width = w;
    canvas.height = h;
    canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
    return canvas.toDataURL('image/jpeg', quality);
  } finally {
    URL.revokeObjectURL(url);
  }
}

/** El corcho en primer plano: dos post-its (uno por miembro) y tres polaroids con chincheta. */
export function CorkBoard({ onClose }: { onClose: () => void }) {
  const room = useStore((s) => s.room)!;
  const auth = useStore((s) => s.auth)!;

  const me = room.members.find((m) => m.id === auth.userId) ?? null;
  const partner = room.members.find((m) => m.id !== auth.userId) ?? null;

  const [photos, setPhotos] = useState<Photo[] | null>(null);
  const [busySlot, setBusySlot] = useState<number | null>(null);
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(me?.note ?? '');
  const [error, setError] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);
  const pendingSlot = useRef<number | null>(null);

  const loadPhotos = () => coopApi.photos().then(setPhotos).catch(() => setPhotos([]));
  useEffect(() => { loadPhotos(); }, []);

  const photoAt = (slot: number) => photos?.find((p) => p.slot === slot) ?? null;

  function pick(slot: number) {
    pendingSlot.current = slot;
    fileRef.current?.click();
  }

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ''; // permite re-elegir el mismo archivo
    const slot = pendingSlot.current;
    if (!file || slot == null) return;
    setBusySlot(slot);
    setError(null);
    try {
      const dataUrl = await fileToDataUrl(file);
      await coopApi.setPhoto(slot, dataUrl);
      await loadPhotos();
    } catch (err) {
      setError(`No se pudo subir la foto: ${err instanceof Error ? err.message : String(err)}`);
    } finally {
      setBusySlot(null);
    }
  }

  async function removePhoto(slot: number) {
    setBusySlot(slot);
    try {
      await coopApi.deletePhoto(slot);
      await loadPhotos();
    } finally {
      setBusySlot(null);
    }
  }

  function saveNote() {
    hubActions.sendNote(draft);
    setEditing(false);
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="corkboard" onClick={(e) => e.stopPropagation()}>
        {/* El input vive DENTRO del corcho: así su click programático no cierra el modal. */}
        <input ref={fileRef} type="file" accept="image/*" hidden onChange={onFile} />
        <button className="cork-close" onClick={onClose} aria-label="Cerrar">✕</button>
        {error && <div className="cork-error">{error}</div>}

        {/* Post-its: uno por miembro */}
        <div className="postits">
          <div className="postit mine">
            <span className="tack red" />
            <div className="postit-author">{me?.displayName ?? 'Tú'}</div>
            {editing ? (
              <div className="postit-edit">
                <textarea maxLength={50} value={draft} autoFocus
                  onChange={(e) => setDraft(e.target.value)} placeholder="Tu nota (máx 50)" />
                <div className="row">
                  <button onClick={saveNote}>Guardar</button>
                  <button className="ghost" onClick={() => { setDraft(me?.note ?? ''); setEditing(false); }}>Cancelar</button>
                </div>
              </div>
            ) : (
              <button className="postit-text" onClick={() => { setDraft(me?.note ?? ''); setEditing(true); }}>
                {me?.note || <span className="muted">Toca para escribir tu nota ✎</span>}
              </button>
            )}
          </div>

          <div className="postit partner">
            <span className="tack blue" />
            <div className="postit-author">{partner?.displayName ?? 'Tu pareja'}</div>
            <div className="postit-text readonly">
              {partner ? (partner.note || <span className="muted">Sin nota todavía</span>) : <span className="muted">Esperando a tu pareja…</span>}
            </div>
          </div>
        </div>

        {/* Polaroids: 3 ranuras con chincheta */}
        <div className="polaroids">
          {SLOTS.map((slot) => {
            const photo = photoAt(slot);
            const busy = busySlot === slot;
            return (
              <div className={`polaroid r${slot}`} key={slot}>
                <span className="tack" />
                {photo ? (
                  <>
                    <button className="polaroid-img" onClick={() => pick(slot)} title="Cambiar foto" disabled={busy}>
                      <img src={photo.dataUrl} alt="" />
                    </button>
                    <button className="polaroid-x" onClick={() => removePhoto(slot)} disabled={busy} aria-label="Quitar">✕</button>
                  </>
                ) : (
                  <button className="polaroid-empty" onClick={() => pick(slot)} disabled={busy}>
                    {busy ? '…' : '+'}
                  </button>
                )}
                <div className="polaroid-caption">{busy ? 'Subiendo…' : photo ? '' : 'Añadir foto'}</div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
