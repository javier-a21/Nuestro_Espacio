import { useState } from 'react';
import { useStore } from '../state/store';
import { hubActions } from '../realtime/connection';

const ZONES = [
  'Europe/Madrid',
  'Europe/London',
  'America/New_York',
  'America/Mexico_City',
  'America/Argentina/Buenos_Aires',
  'America/Bogota',
  'Asia/Tokyo',
];

export function TimeZoneModal({ onClose }: { onClose: () => void }) {
  const room = useStore((s) => s.room)!;
  const [tz, setTz] = useState(room.timeZoneId);
  const [confirming, setConfirming] = useState(false);

  function apply() {
    hubActions.proposeTimeZone(tz);
    onClose();
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h2>Cambiar zona horaria</h2>
        <select value={tz} onChange={(e) => setTz(e.target.value)}>
          {ZONES.map((z) => (
            <option key={z} value={z}>{z}</option>
          ))}
        </select>

        {!confirming ? (
          <button onClick={() => setConfirming(true)} disabled={tz === room.timeZoneId}>Continuar</button>
        ) : (
          <div className="warn">
            <p>⚠️ Cambiar la zona horaria <b>reinicia la planta y la racha a cero</b>. El cambio se aplicará de madrugada. ¿Seguro que quieres continuar?</p>
            <div className="row">
              <button className="danger" onClick={apply}>Sí, cambiar</button>
              <button onClick={onClose}>Cancelar</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
