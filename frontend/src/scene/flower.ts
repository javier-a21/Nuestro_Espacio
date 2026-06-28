// Descriptor visual de una flor, derivado de forma DETERMINISTA de (semilla, clima, racha).
// Única fuente de verdad compartida por la escena Pixi (brote en la planta) y el álbum SVG.

export interface FlowerVisual {
  petalCount: number;
  petalColor: number; // 0xRRGGBB
  centerColor: number;
  petalLen: number; // radio del pétalo, en px de escena
  pointed: boolean; // pétalo afilado vs redondeado
}

export interface BloomLike {
  seed: number;
  weather: string; // "Clear" | "Cloudy" | "Rain" | "Sunny"
  streak: number;
}

// Color base según el clima del día en que floreció.
const WEATHER_HUE: Record<string, number> = {
  Sunny: 0xffd24a, // sol → cálido dorado
  Clear: 0xff7eb6, // despejado → rosa
  Cloudy: 0xc9a0ff, // nublado → lavanda
  Rain: 0x7ec8ff, // lluvia → azul
};

// PRNG determinista (mulberry32): misma semilla → misma flor, siempre.
function mulberry32(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// Desplaza un color hacia blanco (f>0) o negro (f<0). f en [-1, 1].
function tintShift(color: number, f: number): number {
  const r = (color >> 16) & 0xff;
  const g = (color >> 8) & 0xff;
  const b = color & 0xff;
  const mix = (c: number) => {
    const target = f >= 0 ? 255 : 0;
    const v = Math.round(c + (target - c) * Math.abs(f));
    return Math.max(0, Math.min(255, v));
  };
  return (mix(r) << 16) | (mix(g) << 8) | mix(b);
}

export function flowerVisual(b: BloomLike): FlowerVisual {
  const rng = mulberry32(b.seed);
  const base = WEATHER_HUE[b.weather] ?? WEATHER_HUE.Clear;

  const petalColor = tintShift(base, (rng() - 0.5) * 0.4); // ±20% claro/oscuro
  const centerColor = 0xfff1a8;
  // Más racha → más pétalos (5..12).
  const petalCount = Math.max(5, Math.min(12, 5 + Math.floor(b.streak / 2)));
  const pointed = rng() > 0.5;
  const petalLen = 7 + Math.floor(rng() * 4); // 7..10

  return { petalCount, petalColor, centerColor, petalLen, pointed };
}

// "#rrggbb" para SVG/CSS.
export function hexColor(n: number): string {
  return `#${(n & 0xffffff).toString(16).padStart(6, '0')}`;
}

// Aspecto de una flor MADURA según su especie (color base) + semilla (variación).
// Usado por el invernadero; distinto del brote diario (que va por clima/racha).
const SPECIES_HUE: Record<string, number> = {
  Rosa: 0xe24a63, // rosa/rojo
  Girasol: 0xffc83a, // amarillo girasol
};

export function speciesFlowerVisual(species: string, seed: number): FlowerVisual {
  const rng = mulberry32(seed);
  const girasol = species === 'Girasol';
  const base = SPECIES_HUE[species] ?? 0xff7eb6;

  const petalColor = tintShift(base, (rng() - 0.5) * 0.3);
  const centerColor = girasol ? 0x7a4a22 : 0xfff1a8; // corazón oscuro del girasol
  const petalCount = girasol ? 16 : Math.max(6, Math.min(10, 6 + Math.floor(rng() * 5)));
  const pointed = girasol ? true : rng() > 0.5;
  const petalLen = 8 + Math.floor(rng() * 3);

  return { petalCount, petalColor, centerColor, petalLen, pointed };
}
