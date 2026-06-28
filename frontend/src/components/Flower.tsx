import { flowerVisual, hexColor, type BloomLike } from '../scene/flower';

/** Dibuja la flor de un brote como SVG, idéntica en forma a la que aparece en la escena. */
export function Flower({ bloom, size = 56 }: { bloom: BloomLike; size?: number }) {
  const v = flowerVisual(bloom);
  const c = size / 2;
  const ring = size * 0.22; // separación de los pétalos respecto al centro
  const rx = (size * v.petalLen) / 26; // escalado del pétalo al tamaño pedido
  const ry = v.pointed ? rx * 0.42 : rx * 0.62;

  const petals = Array.from({ length: v.petalCount }, (_, i) => {
    const angle = (i / v.petalCount) * 360;
    const rad = (angle * Math.PI) / 180;
    return {
      x: c + Math.cos(rad) * ring,
      y: c + Math.sin(rad) * ring,
      angle,
    };
  });

  return (
    <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} aria-hidden>
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
      <circle cx={c} cy={c} r={ring * 0.7} fill={hexColor(v.centerColor)} />
    </svg>
  );
}
