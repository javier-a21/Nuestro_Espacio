import { useEffect, useRef } from 'react';
import { RoomScene } from './RoomScene';
import { useStore } from '../state/store';

export function RoomCanvas({ onPlantClick, onBoardClick }: { onPlantClick?: (id: string) => void; onBoardClick?: () => void }) {
  const hostRef = useRef<HTMLDivElement>(null);
  const sceneRef = useRef<RoomScene | null>(null);
  const room = useStore((s) => s.room);
  const actionPulse = useStore((s) => s.actionPulse);
  const pendingBloom = useStore((s) => s.pendingBloom);
  const setPendingBloom = useStore((s) => s.setPendingBloom);
  const clickRef = useRef(onPlantClick);
  clickRef.current = onPlantClick;
  const boardRef = useRef(onBoardClick);
  boardRef.current = onBoardClick;

  useEffect(() => {
    let disposed = false;
    const scene = new RoomScene();
    scene.setPlantClickHandler((id) => clickRef.current?.(id));
    scene.setBoardClickHandler(() => boardRef.current?.());

    (async () => {
      if (!hostRef.current) return;
      await scene.init(hostRef.current);
      if (disposed) { scene.destroy(); return; }
      sceneRef.current = scene;
      const current = useStore.getState().room;
      if (current) scene.setState(current);
    })();

    return () => {
      disposed = true;
      sceneRef.current?.destroy();
      sceneRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (room && sceneRef.current) sceneRef.current.setState(room);
  }, [room]);

  // "Juice" al actuar (ignora el valor inicial 0).
  useEffect(() => {
    if (actionPulse > 0) sceneRef.current?.playActionJuice();
  }, [actionPulse]);

  // Celebración del brote del día (de un solo uso: se consume y se limpia).
  useEffect(() => {
    if (pendingBloom && sceneRef.current) {
      sceneRef.current.playBloom(pendingBloom);
      setPendingBloom(null);
    }
  }, [pendingBloom, setPendingBloom]);

  return <div className="room-canvas" ref={hostRef} />;
}
