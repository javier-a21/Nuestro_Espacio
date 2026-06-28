import { useEffect } from 'react';
import { useStore } from './state/store';
import { AuthGate } from './components/AuthGate';
import { Lobby } from './components/Lobby';
import { Room } from './components/Room';
import { coopApi } from './api/cooperative';
import { connectHub, disconnectHub } from './realtime/connection';

export default function App() {
  const auth = useStore((s) => s.auth);
  const room = useStore((s) => s.room);
  const setRoom = useStore((s) => s.setRoom);

  useEffect(() => {
    if (!auth?.cooperativeId) return;
    let active = true;
    (async () => {
      try {
        const state = await coopApi.state();
        if (active) setRoom(state);
        await connectHub();
      } catch (e) {
        console.error(e);
      }
    })();
    return () => {
      active = false;
      void disconnectHub();
    };
  }, [auth?.cooperativeId, setRoom]);

  if (!auth) return <AuthGate />;
  if (!auth.cooperativeId) return <Lobby />;
  if (!room) return <div className="screen">Cargando habitación…</div>;
  return <Room />;
}
