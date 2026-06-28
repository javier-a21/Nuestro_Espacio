# CLAUDE.md — Cooperativa

Contexto para Claude al trabajar en este repositorio. (El usuario habla español; responde en español.)

## Qué es

Entorno virtual compartido en **pixel art**: una habitación isométrica que dos personas
vinculadas 1:1 (una "**cooperativa**" — NO llamarlo "app de pareja") cuidan a diario.
Una planta de 5 estados refleja la constancia mediante una racha; hay notas fugaces (≤50
chars), presencia en tiempo real (luciérnaga), clima escriptado e iluminación día/noche
según un **huso horario compartido**.

## Stack

- **Backend**: ASP.NET Core (.NET 10), 3 capas: `Cooperativa.Api`, `Cooperativa.Domain`, `Cooperativa.Infrastructure` (+ `Cooperativa.Domain.Tests`). Solución `.slnx`.
- **Tiempo real**: SignalR (`/hubs/cooperative`, `CooperativeHub`).
- **Jobs**: Hangfire (tick horario: racha, decadencia 24h/48h, clima, cambio de huso de madrugada).
- **BD**: EF Core. **Postgres (Neon) en producción**; **SQLite local en dev** (fallback automático).
- **Auth**: ASP.NET Core Identity + JWT (el token autentica también SignalR vía query `access_token`).
- **Frontend**: React + Vite + TypeScript; **PixiJS imperativo** (escena en capas, pixel art nearest-neighbor). Estado con Zustand, cliente SignalR.

## Estructura

```
backend/
  Cooperativa.Api/             Program.cs, Controllers/, Realtime/ (Hub, PresenceTracker, IRoomNotifier),
                               Services/ (CooperativeService, RoomService), Jobs/ (RoomTickJob), Auth/ (JWT), Contracts/
  Cooperativa.Domain/          Entities/ (Cooperative, AppUser, RoomState, DailyAction, ActionType), Enums.cs, PlantEvaluator.cs
  Cooperativa.Infrastructure/  Persistence/ (AppDbContext, DesignTimeDbContextFactory), DependencyInjection.cs, Migrations/
  Cooperativa.Domain.Tests/    PlantEvaluatorTests.cs (xUnit)
frontend/src/
  scene/ (RoomScene.ts, RoomCanvas.tsx)  realtime/ (connection.ts)  state/ (store.ts)
  components/ (AuthGate, Lobby, Room, TimeZoneModal)  api/  config.ts  types.ts
```

## Cómo arrancar (dev, sin dependencias externas)

Dos terminales. El backend usa SQLite automáticamente si no hay cadena Postgres.

```powershell
# Backend -> http://localhost:5174  (panel Hangfire en /hangfire)
cd backend
dotnet run --project Cooperativa.Api --launch-profile http

# Frontend -> http://localhost:5173
cd frontend
npm run dev
```

Otros comandos:
```powershell
dotnet build backend/Cooperativa.slnx          # compilar todo
dotnet test  backend/Cooperativa.slnx          # tests
# Migraciones (solo modo Postgres):
dotnet ef migrations add <Nombre> -p backend/Cooperativa.Infrastructure -s backend/Cooperativa.Api
```

Para usar Postgres/Neon: define `ConnectionStrings:Default` o la variable `COOPERATIVA_DB`
(formato Npgsql). Al arrancar, en Postgres aplica migraciones; en SQLite hace EnsureCreated.

## Decisiones / convenciones (importante)

- **Identificadores de tipos en inglés** (`Cooperative`, `RoomState`, `ActionType`, `Role`, `WeatherType`, `PlantLevel`) para evitar choque con el namespace `Cooperativa`. Comentarios y textos de UI en español.
- **Roles asimétricos**: A = Riego (creador), B = Poda (invitado). Emparejamiento por **código de invitación**; la cooperativa es 1:1 (rechaza un tercero).
- **Acciones extensibles**: `ActionType` es un catálogo (seed RIEGO/PODA); añadir acciones = insertar fila, sin migración.
- **Huso horario COMPARTIDO** en `RoomState`. Cambiarlo es **destructivo** (reinicia planta y racha); se marca como pendiente y el `RoomTickJob` lo aplica de madrugada. El frontend avisa con modal.
- **Clima escriptado** (opción B): lo controla la app, no una API meteorológica real.
- **Máquina de 5 estados** en `PlantEvaluator.Evaluate` (lógica pura, testeada): Marchita(1, ≥48h sin interacción) / Alicaída(2, ≥24h) / Estable(3, racha 0) / Sana(4, racha 1-6) / Radiante(5, racha ≥7 y ambos actuaron hoy). La decadencia tiene prioridad sobre la racha.

## Estado actual (verificado)

- Backend y frontend **compilan limpios**. Tests del dominio **8/8**. Smoke test REST y **prueba SignalR en tiempo real** (acciones, notas, presencia) **OK** en modo SQLite.
- Repo git en `main`. Assets de la escena son **placeholders gráficos** (faltan sprites de pixel art reales).

## Pendiente / próximos pasos

- **Despliegue VPS** (plan acordado): `Dockerfile` backend + `docker-compose` (app + Postgres + **Caddy** con HTTPS automático) + `Caddyfile`. Objetivo: VPS Hetzner (~€4-5/mes), Postgres local en la misma caja (no hace falta Neon).
- **Assets de pixel art** reales sustituyendo placeholders, capa por capa en `RoomScene`.
- **Seguridad prod**: mover la clave JWT de `appsettings.json` a secretos; proteger el dashboard `/hangfire`.
- Acciones del Hub y job de Hangfire: probados por script; verás el comportamiento completo usando el frontend.
```
