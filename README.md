# Nuestro espacio

Entorno virtual compartido en pixel art: una habitación isométrica que dos personas
vinculadas 1:1 (una "cooperativa") cuidan a diario. La salud de una planta refleja la
constancia de ambas mediante una racha; hay notas fugaces, presencia en tiempo real
(luciérnaga), clima e iluminación día/noche según un huso horario compartido.

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend / API | ASP.NET Core (.NET 10) |
| Tiempo real | SignalR |
| Jobs en segundo plano | Hangfire (almacenado en Postgres) |
| Base de datos | PostgreSQL (Neon) + EF Core |
| Frontend | React + Vite + TypeScript |
| Render | PixiJS (escena en capas, pixel art nearest-neighbor) |
| Auth | ASP.NET Core Identity + JWT |

## Estructura

```
backend/
  Cooperativa.Api/            API REST, SignalR Hub, Hangfire, Program.cs
  Cooperativa.Domain/         Entidades, enums, máquina de 5 estados (lógica pura)
  Cooperativa.Infrastructure/ EF Core DbContext (Identity), migraciones
  Cooperativa.Domain.Tests/   Tests unitarios del dominio
frontend/
  src/scene/                  Escena Pixi (capas: ventana/cortinas, planta, clima, luciérnaga, día/noche)
  src/realtime/               Cliente SignalR
  src/state/                  Store (Zustand) + persistencia local
  src/components/             UI (auth, lobby, habitación, modal de huso)
  src/api/                    Cliente REST
```

## Puesta en marcha (desarrollo)

Requisitos: **.NET 10 SDK**, **Node 20+**, y una base de datos **PostgreSQL** (p.ej. un
proyecto gratuito en [Neon](https://neon.tech)).

1. Configura la cadena de conexión (en formato .NET de Npgsql). Puedes usar una variable
   de entorno o `ConnectionStrings:Default` en `appsettings`:
   ```powershell
   $env:COOPERATIVA_DB = "Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
   ```
2. Aplica la migración inicial:
   ```powershell
   cd backend
   dotnet ef database update -p Cooperativa.Infrastructure -s Cooperativa.Api
   ```
3. Arranca el backend (http://localhost:5174):
   ```powershell
   dotnet run --project Cooperativa.Api
   ```
4. Arranca el frontend (http://localhost:5173):
   ```powershell
   cd frontend
   npm install
   npm run dev
   ```

## Tests

```powershell
cd backend
dotnet test
```

## Pendiente / deuda consciente

- Sustituir los placeholders gráficos de la escena por sprite-sheets de pixel art reales.
- Asegurar el dashboard de Hangfire (`/hangfire`) y mover la clave JWT a secretos en producción.
- Reglas futuras de acciones (el catálogo `ActionType` ya es extensible sin migración).
