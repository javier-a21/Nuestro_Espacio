import { speciesFlowerVisual, hexColor } from '../scene/flower';

/** Una planta madura en su maceta, dibujada por especie + semilla (para el invernadero). */
export function GardenFlower({ species, seed, size = 84 }: { species: string; seed: number; size?: number }) {
  const v = speciesFlowerVisual(species, seed);
  const w = size, h = size;
  const cx = w / 2;
  const flowerCy = h * 0.30;
  const ring = w * 0.135;
  const rx = w * 0.085 * (v.petalLen / 9);
  const ry = v.pointed ? rx * 0.42 : rx * 0.62;
  const potTop = h * 0.74;
  const potW = w * 0.36;
  const midY = (flowerCy + potTop) / 2;

  const petals = Array.from({ length: v.petalCount }, (_, i) => {
    const angle = (i / v.petalCount) * 360;
    const rad = (angle * Math.PI) / 180;
    return { x: cx + Math.cos(rad) * ring, y: flowerCy + Math.sin(rad) * ring, angle };
  });

  return (
    <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} aria-hidden>
      {/* tallo + hojas */}
      <rect x={cx - 1.5} y={flowerCy} width={3} height={potTop - flowerCy} fill="#3f8f4a" />
      <ellipse cx={cx - 7} cy={midY} rx={7} ry={3} fill="#4aa05a" transform={`rotate(-25 ${cx - 7} ${midY})`} />
      <ellipse cx={cx + 7} cy={midY + 6} rx={7} ry={3} fill="#3f8f4a" transform={`rotate(25 ${cx + 7} ${midY + 6})`} />

      {/* pétalos + corazón */}
      {petals.map((p, i) => (
        <ellipse
          key={i}
          cx={p.x}
          cy={p.y}
          rx={rx}
          ry={ry}
          fill={hexColor(v.petalColor)}
          transform={`rotate(${p.angle} ${p.x} ${p.y})`}
        />
      ))}
      <circle cx={cx} cy={flowerCy} r={ring * 0.66} fill={hexColor(v.centerColor)} />

      {/* maceta */}
      <polygon
        points={`${cx - potW / 2},${potTop} ${cx + potW / 2},${potTop} ${cx + potW / 2 - 4},${h - 3} ${cx - potW / 2 + 4},${h - 3}`}
        fill="#c77a45"
      />
      <rect x={cx - potW / 2 - 2} y={potTop - 4} width={potW + 4} height={5} rx={1} fill="#d98a55" />
    </svg>
  );
}
