import { useState } from 'react';
import { coopApi } from '../api/cooperative';
import { useStore } from '../state/store';

export function Lobby() {
  const auth = useStore((s) => s.auth)!;
  const setAuth = useStore((s) => s.setAuth);

  const [code, setCode] = useState('');
  const [created, setCreated] = useState<{ id: string; code: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function enterCoop(cooperativeId: string) {
    setAuth({ ...auth, cooperativeId });
  }

  async function create() {
    setBusy(true);
    setError(null);
    try {
      const res = await coopApi.create();
      setCreated({ id: res.cooperativeId, code: res.inviteCode });
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function join() {
    setBusy(true);
    setError(null);
    try {
      await coopApi.join(code.trim().toUpperCase());
      const state = await coopApi.state();
      enterCoop(state.cooperativeId);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="screen">
      <div className="card">
        <h1>Nuestro espacio</h1>
        {created ? (
          <>
            <p>Comparte este código con la otra persona:</p>
            <p className="code">{created.code}</p>
            <button onClick={() => enterCoop(created.id)}>Entrar a la habitación</button>
          </>
        ) : (
          <>
            <p>Crea una habitación y comparte el código, o únete con el código de tu cooperativa.</p>
            <button onClick={create} disabled={busy}>Crear habitación</button>
            <div className="divider">o</div>
            <input placeholder="Código de invitación" value={code} onChange={(e) => setCode(e.target.value)} />
            <button onClick={join} disabled={busy || !code}>Unirme</button>
          </>
        )}
        {error && <p className="error">{error}</p>}
      </div>
    </div>
  );
}
