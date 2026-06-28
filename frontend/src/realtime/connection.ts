import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { HUB_URL } from '../config';
import { useStore } from '../state/store';
import type { Bloom, RoomState } from '../types';

let connection: HubConnection | null = null;

export async function connectHub(): Promise<HubConnection | null> {
  if (!useStore.getState().auth?.token) return null;
  if (connection) return connection;

  connection = new HubConnectionBuilder()
    .withUrl(HUB_URL, { accessTokenFactory: () => useStore.getState().auth?.token ?? '' })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  const { setRoom, patchRoom, bumpAction, setPendingBloom } = useStore.getState();

  connection.on('RoomState', (state: RoomState) => setRoom(state));
  connection.on('NoteUpdated', (note: { text: string | null; authorId: string | null; at: string | null }) =>
    patchRoom({ lastNote: note.text, lastNoteAuthorId: note.authorId, lastNoteAt: note.at }));
  connection.on('PresenceUpdated', (bothOnline: boolean) => patchRoom({ bothOnline }));
  connection.on('PendingTimeZone', (tz: string | null) => patchRoom({ pendingTimeZoneId: tz }));
  // Cada acción dispara el "juice" inmediato en la escena (el RoomState que llega después actualiza el estado).
  connection.on('ActionPerformed', () => bumpAction());
  // Brote del día: ambos han cumplido → celebración + flor visible al instante.
  connection.on('BloomCreated', (bloom: Bloom) => {
    patchRoom({ todayBloom: bloom });
    setPendingBloom(bloom);
  });

  await connection.start();
  return connection;
}

export async function disconnectHub(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}

export const hubActions = {
  performAction: () => connection?.invoke('PerformAction'),
  abonar: () => connection?.invoke('Abonar'),
  protect: () => connection?.invoke('ProtectPlant'),
  sendNote: (text: string) => connection?.invoke('SendNote', text),
  proposeTimeZone: (tz: string) => connection?.invoke('ProposeTimeZone', tz),
};
