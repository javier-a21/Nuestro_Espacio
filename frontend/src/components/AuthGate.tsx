import { useState, type FormEvent } from 'react';
import { authApi } from '../api/auth';
import { useStore } from '../state/store';

export function AuthGate() {
  const setAuth = useStore((s) => s.setAuth);
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const auth =
        mode === 'login'
          ? await authApi.login(email, password)
          : await authApi.register(email, password, displayName);
      setAuth(auth);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="screen">
      <form className="card" onSubmit={submit}>
        <h1>Nuestro espacio</h1>
        <div className="tabs">
          <button type="button" className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')}>Entrar</button>
          <button type="button" className={mode === 'register' ? 'active' : ''} onClick={() => setMode('register')}>Crear cuenta</button>
        </div>
        {mode === 'register' && (
          <input placeholder="Nombre" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        )}
        <input type="email" placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <input type="password" placeholder="Contraseña" value={password} onChange={(e) => setPassword(e.target.value)} required />
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={busy}>{busy ? '…' : mode === 'login' ? 'Entrar' : 'Crear cuenta'}</button>
      </form>
    </div>
  );
}
