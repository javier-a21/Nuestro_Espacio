import { Application, Container, Graphics, Rectangle, Text, TextStyle, TextureSource } from 'pixi.js';
import type { Bloom, MemberStatus, RoomState, ShelfPlant } from '../types';
import { flowerVisual, speciesFlowerVisual } from './flower';

// Pixel art: vecino más cercano por defecto.
TextureSource.defaultOptions.scaleMode = 'nearest';

export const SCENE_W = 440;
export const SCENE_H = 240;
const FLOOR_Y = 158;

// Planta en cultivo: encima de una mesita (junto a la ventana). Centro de los efectos.
const PLANT_X = 92;
const PLANT_Y = 124;
const STAND_TOP = 150; // superficie de la mesita donde se apoya la maceta

// Sitios repartidos donde se posan las flores maduras de la sala (hasta 4).
const FLOWER_SPOTS = [
  { x: 419, baseY: STAND_TOP }, // sobre la mesilla, junto a la cama (lado almohada)
  { x: 340, baseY: 94 },        // en el estante de pared
  { x: 52, baseY: 110 },        // en el alféizar
  { x: 195, baseY: 216 },       // en el suelo
];

// Corcho de la pared (zona clicable).
const BOARD = { x: 158, y: 34, w: 112, h: 72 };

function lerp(a: number, b: number, t: number): number { return a + (b - a) * t; }
function lerpColor(c1: number, c2: number, t: number): number {
  const r = lerp((c1 >> 16) & 255, (c2 >> 16) & 255, t);
  const g = lerp((c1 >> 8) & 255, (c2 >> 8) & 255, t);
  const b = lerp(c1 & 255, c2 & 255, t);
  return (Math.round(r) << 16) | (Math.round(g) << 8) | Math.round(b);
}
function vGrad(g: Graphics, x: number, y: number, w: number, h: number, top: number, bottom: number, steps = 14): void {
  const bh = h / steps;
  for (let i = 0; i < steps; i++) g.rect(x, y + i * bh, w, Math.ceil(bh) + 1).fill(lerpColor(top, bottom, i / (steps - 1)));
}

type ParticleShape = 'dot' | 'star' | 'petal';
interface Particle { g: Graphics; vx: number; vy: number; life: number; maxLife: number; maxAlpha: number; gravity: number; spin: number; }

/** Escena de la habitación (pixel-art), ahora más amplia: ventana, corcho, cama y flores repartidas. */
export class RoomScene {
  readonly app = new Application();

  private root = new Container();
  private decorTop = new Graphics();
  private curtains = new Graphics();
  private boardGfx = new Graphics();   // contenido decorativo del corcho (garabatos + fotos)
  private board = new Container();      // corcho clicable
  private plant = new Container();
  private plantGfx = new Graphics();
  private firefly = new Graphics();
  private flowers = new Container();    // flores maduras repartidas (clicables)
  private weatherLayer = new Container();
  private rainLines: Graphics[] = [];
  private eventTint = new Graphics();
  private vignette = new Graphics();
  private dayNight = new Graphics();
  private streakText: Text;

  private onPlantClick?: (id: string) => void;
  private onBoardClick?: () => void;

  private particles: Particle[] = [];
  private state: RoomState | null = null;
  private todayBloom: Bloom | null = null;
  private growthStage = 0;
  private health = 'Healthy';
  private pop = 0;
  private t = 0;

  constructor() {
    this.streakText = new Text({ text: '', style: new TextStyle({ fontFamily: 'monospace', fontSize: 11, fill: 0xfff3e0, fontWeight: 'bold' }) });
  }

  async init(parent: HTMLElement): Promise<void> {
    await this.app.init({ width: SCENE_W, height: SCENE_H, background: 0x14101c, antialias: false });
    parent.appendChild(this.app.canvas);
    this.build();
    this.app.ticker.add(() => this.update());
  }

  destroy(): void { this.app.destroy(true, { children: true }); }

  setPlantClickHandler(cb: (id: string) => void): void { this.onPlantClick = cb; }
  setBoardClickHandler(cb: () => void): void { this.onBoardClick = cb; }

  setState(state: RoomState): void {
    this.state = state;
    this.todayBloom = state.todayBloom ?? null;
    this.growthStage = state.activePlant?.growthStage ?? 0;
    this.health = state.activePlant?.health ?? 'Healthy';
    this.drawPlant();
    this.drawFlowers(state.roomPlants ?? []);
    this.drawBoard(state.members ?? [], state.photoSlots ?? []);
    this.drawEventTint(state.event);
    this.streakText.text = `fase ${this.growthStage}/10`;
    this.firefly.visible = state.bothOnline;
    this.buildWeather(state.weather);
  }

  playActionJuice(): void {
    this.pop = 1;
    for (let i = 0; i < 14; i++) {
      const a = -Math.PI / 2 + (Math.random() - 0.5) * 2.2;
      const speed = 28 + Math.random() * 38;
      this.spawnParticle(PLANT_X + (Math.random() * 16 - 8), PLANT_Y - 22, {
        color: i % 3 === 0 ? 0xbfeec8 : 0x8fd6ff, shape: 'dot',
        vx: Math.cos(a) * speed, vy: Math.sin(a) * speed, life: 0.7 + Math.random() * 0.4, gravity: 48, size: 1.5,
      });
    }
  }

  playBloom(bloom: Bloom): void {
    this.todayBloom = bloom;
    this.drawPlant();
    this.pop = 1.4;
    const v = flowerVisual(bloom);
    for (let i = 0; i < 30; i++) {
      const a = Math.random() * Math.PI * 2;
      const speed = 20 + Math.random() * 58;
      this.spawnParticle(PLANT_X, PLANT_Y - 30, {
        color: i % 4 === 0 ? v.centerColor : v.petalColor, shape: 'petal',
        vx: Math.cos(a) * speed, vy: Math.sin(a) * speed - 28, life: 1.2 + Math.random() * 0.9, gravity: 30, size: 3, spin: (Math.random() - 0.5) * 6,
      });
    }
  }

  // ---------------------------------------------------------------------------
  private build(): void {
    this.app.stage.addChild(this.root);

    const bg = new Graphics();
    this.drawWall(bg);
    this.drawWindow(bg);
    this.drawCorkFrame(bg);
    this.drawFloor(bg);
    this.drawRug(bg);
    this.drawContactShadows(bg);
    this.drawBed(bg);
    this.drawBedsideTable(bg);
    this.drawWallShelf(bg);
    this.drawPlantStand(bg);
    this.root.addChild(bg);

    this.root.addChild(this.curtains);
    this.root.addChild(this.decorTop);
    this.drawValance(this.decorTop);

    // Corcho clicable (contenido decorativo dentro).
    this.board.eventMode = 'static';
    this.board.cursor = 'pointer';
    this.board.hitArea = new Rectangle(BOARD.x, BOARD.y, BOARD.w, BOARD.h);
    this.board.on('pointertap', () => this.onBoardClick?.());
    this.board.addChild(this.boardGfx);
    this.root.addChild(this.board);

    // Flores repartidas (clicables)
    this.root.addChild(this.flowers);

    // Planta en cultivo
    this.plant.position.set(PLANT_X, PLANT_Y);
    this.plant.addChild(this.plantGfx);
    this.root.addChild(this.plant);

    // Luciérnaga
    this.firefly.circle(0, 0, 4).fill({ color: 0xffef9c, alpha: 0.30 });
    this.firefly.circle(0, 0, 2).fill(0xfff6c8);
    this.firefly.visible = false;
    this.root.addChild(this.firefly);

    this.root.addChild(this.weatherLayer);
    this.root.addChild(this.eventTint);

    this.drawVignette(this.vignette);
    this.root.addChild(this.vignette);
    this.dayNight.rect(0, 0, SCENE_W, SCENE_H).fill(0x0a0a22);
    this.dayNight.alpha = 0;
    this.root.addChild(this.dayNight);

    // HUD: insignia de fase
    const hud = new Graphics();
    hud.roundRect(6, 6, 104, 22, 8).fill({ color: 0x241d2e, alpha: 0.82 });
    hud.roundRect(6, 6, 104, 22, 8).stroke({ width: 1, color: 0x5a4c63 });
    hud.rect(20, 16, 1.4, 6).fill(0x4a8f54);
    hud.ellipse(18, 16, 4, 2.5).fill(0x5fbf6a);
    hud.ellipse(22, 13.5, 3, 2).fill(0x83de8e);
    this.app.stage.addChild(hud);
    this.streakText.position.set(30, 10);
    this.app.stage.addChild(this.streakText);

    this.drawPlant();
  }

  private drawWall(g: Graphics): void {
    vGrad(g, 0, 0, SCENE_W, FLOOR_Y, 0x564860, 0x6f5f78, 16);
    for (let i = 7; i > 0; i--) g.ellipse(74, 64, (74 * i) / 7, (60 * i) / 7).fill({ color: 0xffe6b0, alpha: 0.012 });
    g.rect(0, 120, SCENE_W, 4).fill(0x47394f);
    g.rect(0, 120, SCENE_W, 1).fill(0x8a7a94);
    g.rect(0, 123, SCENE_W, 1).fill(0x322a3c);
  }

  private drawWindow(g: Graphics): void {
    const x = 24, y = 30, w = 92, h = 72;
    vGrad(g, x, y, w, h, 0xbfe0ff, 0xfcd9a8, 10);
    g.ellipse(x + 28, y + 20, 11, 5).fill({ color: 0xffffff, alpha: 0.85 });
    g.ellipse(x + 38, y + 22, 9, 5).fill({ color: 0xffffff, alpha: 0.85 });
    g.ellipse(x + w * 0.7, y + h, 38, 18).fill({ color: 0x9ec6a0, alpha: 0.55 });
    g.rect(x + w / 2 - 1.5, y, 3, h).fill(0x6a5238);
    g.rect(x, y + h / 2 - 1.5, w, 3).fill(0x6a5238);
    g.rect(x - 5, y - 5, w + 10, 5).fill(0x8a6b48);
    g.rect(x - 5, y - 5, 5, h + 10).fill(0x7a5d3e);
    g.rect(x - 5, y + h, w + 10, 5).fill(0x4f3c28);
    g.rect(x + w, y - 5, 5, h + 10).fill(0x5b4630);
    g.rect(x - 5, y - 5, w + 10, h + 10).stroke({ width: 1, color: 0x3a2c1d });
    g.rect(x - 9, y + h + 5, w + 18, 6).fill(0x6a5038);
    g.rect(x - 9, y + h + 5, w + 18, 1.5).fill(0x8a6c48);
    g.rect(x - 9, y + h + 11, w + 18, 1.5).fill(0x3f2e1f);
  }

  private drawCorkFrame(g: Graphics): void {
    const { x, y, w, h } = BOARD;
    g.rect(x - 4, y - 4, w + 8, h + 8).fill(0x6e4a2c);          // marco de madera
    g.rect(x - 4, y - 4, w + 8, 2).fill(0x86562f);
    g.rect(x, y, w, h).fill(0xc39256);                          // corcho
    // textura punteada del corcho
    for (let yy = y + 4; yy < y + h - 2; yy += 6)
      for (let xx = x + 4; xx < x + w - 2; xx += 6)
        g.rect(xx, yy, 1, 1).fill({ color: 0x000000, alpha: 0.13 });
  }

  private drawFloor(g: Graphics): void {
    vGrad(g, 0, FLOOR_Y, SCENE_W, SCENE_H - FLOOR_Y, 0x9a7856, 0x6c5036, 10);
    g.rect(0, FLOOR_Y - 4, SCENE_W, 4).fill(0x46394d);
    g.rect(0, FLOOR_Y - 1, SCENE_W, 1).fill(0x6a5c70);
    for (const py of [FLOOR_Y + 20, FLOOR_Y + 40, FLOOR_Y + 60]) {
      g.rect(0, py, SCENE_W, 1).fill({ color: 0x000000, alpha: 0.18 });
      g.rect(0, py + 1, SCENE_W, 1).fill({ color: 0xffffff, alpha: 0.05 });
    }
    [40, 150, 250, 360, 95, 205, 320, 420, 20, 130, 235, 300, 70, 180, 285, 390].forEach((jx, i) => {
      g.rect(jx, FLOOR_Y + (i % 4) * 20, 1, 20).fill({ color: 0x000000, alpha: 0.14 });
    });
  }

  private drawRug(g: Graphics): void {
    const cx = PLANT_X, cy = 196;
    g.ellipse(cx, cy, 54, 14).fill(0x9a4a3e);
    g.ellipse(cx, cy, 47, 11).fill(0xc06a52);
    g.ellipse(cx, cy, 39, 8).stroke({ width: 1.5, color: 0xe0aa78 });
  }

  /** Mesita redonda donde se apoya la planta en cultivo. */
  private drawPlantStand(g: Graphics): void {
    const x = PLANT_X, top = STAND_TOP;
    g.rect(x - 16, top, 5, 40).fill(0x4a3320);       // patas
    g.rect(x + 11, top, 5, 40).fill(0x4a3320);
    g.rect(x - 14, top + 24, 28, 4).fill(0x3a2718);  // travesaño
    g.ellipse(x, top + 2, 22, 5).fill(0x5a3f28);     // canto del tablero
    g.ellipse(x, top - 1, 22, 5).fill(0x6e4a2c);     // tablero
    g.ellipse(x, top - 2, 20, 4).fill(0x82542f);     // brillo
  }

  private drawBed(g: Graphics): void {
    // Cama: cabecero y almohada a la DERECHA (junto a la mesilla); manta a la izquierda.
    const bx = 278, bw = 120, mt = 176;
    g.rect(bx, mt - 6, 8, 38).fill(0x5a3f28);                  // piecero (izq, corto)
    g.rect(bx + bw - 9, mt - 22, 9, 54).fill(0x5a3f28);        // cabecero (der, alto)
    g.rect(bx + bw - 9, mt - 22, 9, 3).fill(0x6e4f33);
    g.rect(bx, mt + 16, bw, 8).fill(0x4a3320);                 // somier
    g.rect(bx + 5, mt + 24, 8, 14).fill(0x3a2718);             // patas
    g.rect(bx + bw - 13, mt + 24, 8, 14).fill(0x3a2718);
    g.rect(bx + 6, mt, bw - 12, 18).fill(0xe7e0d0);            // colchón
    g.rect(bx + bw - 47, mt - 4, 36, 14).fill(0xfbf6ec);       // almohada (derecha, junto al cabecero)
    g.rect(bx + bw - 47, mt - 4, 36, 3).fill(0xffffff);
    const bend = bx + bw - 50;                                 // la manta cubre la izquierda
    g.rect(bx + 6, mt + 4, bend - (bx + 6), 14).fill(0x6f96c4);
    g.rect(bx + 6, mt + 4, bend - (bx + 6), 2).fill(0x9cbfe0);
  }

  private drawBedsideTable(g: Graphics): void {
    // Mesilla a la DERECHA de la cama (lado almohada). Superficie a la altura de STAND_TOP.
    const x = 402, top = STAND_TOP + 18, w = 34;
    g.rect(x + 4, top + 4, 6, 22).fill(0x4a3320);
    g.rect(x + w - 10, top + 4, 6, 22).fill(0x4a3320);
    g.rect(x, top - 16, w, 22).fill(0x6e4a2c);
    g.rect(x - 2, top - 18, w + 4, 4).fill(0x82542f);
    g.rect(x - 2, top - 18, w + 4, 1.5).fill(0xa06a3c);
    g.rect(x + 6, top - 10, w - 12, 9).fill(0x5a3c22);
    g.circle(x + w / 2, top - 5.5, 1.4).fill(0xd9b070);
  }

  private drawWallShelf(g: Graphics): void {
    // Pequeño estante de pared para una flor (spot C).
    const x = 318, y = 94, w = 46;
    g.poly([x + 6, y, x + 14, y, x + 8, y + 8]).fill(0x4a3320);
    g.poly([x + w - 14, y, x + w - 6, y, x + w - 8, y + 8]).fill(0x4a3320);
    g.rect(x, y, w, 4).fill(0x6e4a2c);
    g.rect(x, y, w, 1.5).fill(0x86562f);
  }

  private drawContactShadows(g: Graphics): void {
    g.ellipse(PLANT_X, 192, 22, 5).fill({ color: 0x000000, alpha: 0.20 }); // mesita planta
    g.ellipse(338, 216, 66, 8).fill({ color: 0x000000, alpha: 0.16 });     // cama
    g.ellipse(419, 196, 22, 5).fill({ color: 0x000000, alpha: 0.14 });     // mesilla
  }

  private drawValance(g: Graphics): void {
    g.clear();
    const x = 16, w = 112;
    g.rect(x - 4, 22, w + 8, 3).fill(0x6b4a2f);
    g.circle(x - 4, 23.5, 3).fill(0x8a6038);
    g.circle(x + w + 4, 23.5, 3).fill(0x8a6038);
    g.rect(x, 25, w, 12).fill(0xb24a63);
    g.rect(x, 25, w, 2).fill(0xcf6480);
    const n = 9, sw = w / n;
    for (let i = 0; i < n; i++) g.ellipse(x + sw * (i + 0.5), 37, sw / 2, 4).fill(0xb24a63);
  }

  // --- Flores repartidas (clicables) ---
  private drawFlowers(plants: ShelfPlant[]): void {
    this.flowers.removeChildren();
    plants.slice(0, FLOWER_SPOTS.length).forEach((p, i) => {
      const spot = FLOWER_SPOTS[i];
      const item = new Container();
      const g = new Graphics();
      this.drawMiniPlant(g, spot.x, spot.baseY, p.species, p.seed);
      item.addChild(g);
      item.eventMode = 'static';
      item.cursor = 'pointer';
      item.hitArea = new Rectangle(spot.x - 14, spot.baseY - 48, 28, 50);
      item.on('pointertap', () => this.onPlantClick?.(p.id));
      this.flowers.addChild(item);
    });
  }

  /** Flor madura en maceta, al tamaño de la planta principal. */
  private drawMiniPlant(g: Graphics, x: number, baseY: number, species: string, seed: number): void {
    const v = speciesFlowerVisual(species, seed);
    // Maceta (trapecio, ≈ tamaño de la principal)
    g.poly([x - 11, baseY - 17, x + 11, baseY - 17, x + 8, baseY - 1, x - 8, baseY - 1]).fill(0xc77a45);
    g.poly([x + 3, baseY - 17, x + 11, baseY - 17, x + 8, baseY - 1, x + 3, baseY - 1]).fill(0xa85f33);
    g.rect(x - 12, baseY - 19, 24, 4).fill(0xd98a55);
    g.rect(x - 12, baseY - 19, 24, 1.4).fill(0xe8a468);
    g.ellipse(x, baseY - 18, 9, 2).fill(0x4a3526);
    // Tallo + hojas
    const top = baseY - 38;
    g.moveTo(x, baseY - 17).lineTo(x, top + 3).stroke({ width: 2, color: 0x3f8f4a });
    g.ellipse(x - 6, baseY - 26, 5.5, 2.4).fill(0x4aa05a);
    g.ellipse(x + 6, baseY - 29, 5.5, 2.4).fill(0x3f8f4a);
    // Flor
    const cy = top, ring = 4.6, pc = Math.min(v.petalCount, 14);
    const rx = 3.2, ry = v.pointed ? rx * 0.5 : rx * 0.72;
    for (let i = 0; i < pc; i++) { const a = (i / pc) * Math.PI * 2; g.ellipse(x + Math.cos(a) * ring, cy + Math.sin(a) * ring, rx, ry).fill(v.petalColor); }
    g.circle(x, cy, ring * 0.7).fill(v.centerColor);
    g.circle(x - 0.8, cy - 0.8, 1).fill(lerpColor(v.centerColor, 0xffffff, 0.5));
  }

  // --- Corcho "de lejos": post-its de garabatos + fotos decorativas ---
  private drawBoard(members: MemberStatus[], photoSlots: number[]): void {
    const g = this.boardGfx;
    g.clear();
    // Post-its (uno por miembro) con líneas ilegibles si tiene nota.
    const postSlots = [[BOARD.x + 30, BOARD.y + 24], [BOARD.x + 80, BOARD.y + 26]];
    members.slice(0, 2).forEach((m, i) => {
      const [px, py] = postSlots[i];
      g.rect(px - 13, py - 12, 26, 24).fill(i === 0 ? 0xfff3a8 : 0xffd7e6);
      g.circle(px, py - 12, 1.6).fill(i === 0 ? 0xd0354c : 0x3a6fb0);
      if (m.note) for (let k = 0; k < 3; k++) g.rect(px - 9, py - 6 + k * 6, 18 - (k % 2) * 5, 1.4).fill({ color: 0x6a5a30, alpha: 0.55 });
    });
    // Fotos (polaroids decorativos) en las ranuras con foto.
    const photoPos = [[BOARD.x + 26, BOARD.y + 56], [BOARD.x + 56, BOARD.y + 58], [BOARD.x + 86, BOARD.y + 55]];
    for (const slot of photoSlots) {
      const [px, py] = photoPos[slot] ?? [BOARD.x + 56, BOARD.y + 56];
      g.rect(px - 10, py - 9, 20, 22).fill(0xfdfdfa);
      g.rect(px - 8, py - 7, 16, 13).fill(0xb8b0a4);
      g.circle(px, py - 9, 1.4).fill(0x3a6fb0);
    }
  }

  private drawPlant(): void {
    const g = this.plantGfx;
    g.clear();
    g.ellipse(0, 27, 14, 3).fill(0x7a4a2c);
    g.poly([-12, 8, 12, 8, 9, 26, -9, 26]).fill(0xc77a45);
    g.poly([4, 8, 12, 8, 9, 26, 3, 26]).fill(0xa85f33);
    g.rect(-14, 5, 28, 5).fill(0xd98a55);
    g.rect(-14, 5, 28, 1.5).fill(0xe8a468);
    g.ellipse(0, 7, 11, 2.4).fill(0x4a3526);

    const t = Math.max(0, Math.min(1, this.growthStage / 10));
    const wilting = this.health === 'Wilting', wilted = this.health === 'Wilted';
    const wiltMix = wilted ? 0.85 : wilting ? 0.45 : 0;
    const color = lerpColor(0x3fbf5a, 0x9a7b3a, wiltMix);
    const dark = lerpColor(color, 0x123018, 0.5);
    const light = lerpColor(color, 0xffffff, 0.42);
    const stemCol = lerpColor(color, 0x2a4a22, 0.4);
    const droop = wilted ? 9 : wilting ? 5 : 0;

    if (this.growthStage <= 0) {
      g.ellipse(-2.5, 4, 3, 1.6).fill(color);
      g.ellipse(2.5, 4, 3, 1.6).fill(color);
      g.moveTo(0, 7).lineTo(0, 2).stroke({ width: 1.5, color: stemCol });
      return;
    }
    const stemH = 8 + 30 * t;
    const top = 8 - stemH + droop;
    g.moveTo(0, 8).lineTo(0, top).stroke({ width: 2 + 1.5 * t, color: stemCol });
    const pairs = 1 + Math.floor(t * 4), lw = 4.5 + 4 * t;
    for (let i = 0; i < pairs; i++) {
      const f = (i + 1) / (pairs + 1), ly = 8 - stemH * f + droop * f;
      g.ellipse(-3 - lw * 0.4, ly, lw, 2.5 + 1.5 * t).fill(dark);
      g.ellipse(3 + lw * 0.4, ly + 1, lw, 2.5 + 1.5 * t).fill(color);
    }
    const tb = 4 + 7 * t;
    g.ellipse(0, top + 1, tb, tb * 0.7).fill(dark);
    g.ellipse(-1, top, tb * 0.8, tb * 0.6).fill(color);
    g.ellipse(-1.5, top - 1, tb * 0.4, tb * 0.3).fill(light);

    if (this.todayBloom && this.growthStage >= 3 && !wilted) {
      const v = flowerVisual(this.todayBloom), cx = 0, cy = top - 1;
      for (let i = 0; i < v.petalCount; i++) { const a = (i / v.petalCount) * Math.PI * 2; g.ellipse(cx + Math.cos(a) * 4, cy + Math.sin(a) * 4, 3, 1.8).fill(v.petalColor); }
      g.circle(cx, cy, 2.4).fill(v.centerColor);
      g.circle(cx - 0.7, cy - 0.7, 1).fill(lerpColor(v.centerColor, 0xffffff, 0.5));
    }
  }

  private drawVignette(g: Graphics): void {
    for (let i = 0; i < 14; i++) g.rect(i, i, SCENE_W - 2 * i, SCENE_H - 2 * i).stroke({ width: 1, color: 0x000000, alpha: 0.035 });
  }

  private drawEventTint(ev: RoomState['event']): void {
    const g = this.eventTint;
    g.clear();
    if (!ev || ev.handled) return;
    const active = ev.stage === 'Active';
    const color = ev.type === 'Heatwave' ? 0xff7a3a : 0x6aa8ff;
    const alpha = active ? (ev.type === 'Heatwave' ? 0.18 : 0.2) : 0.07;
    g.rect(0, 0, SCENE_W, SCENE_H).fill({ color, alpha });
  }

  private buildWeather(weather: string): void {
    this.weatherLayer.removeChildren();
    this.rainLines = [];
    if (weather === 'Rain') {
      for (let i = 0; i < 55; i++) {
        const line = new Graphics();
        line.rect(0, 0, 1, 8).fill({ color: 0xaad4ff, alpha: 0.7 });
        line.position.set(Math.random() * SCENE_W, Math.random() * SCENE_H);
        this.weatherLayer.addChild(line);
        this.rainLines.push(line);
      }
    } else if (weather === 'Cloudy') {
      const c = new Graphics(); c.rect(0, 0, SCENE_W, SCENE_H).fill(0x9aa0aa); c.alpha = 0.22; this.weatherLayer.addChild(c);
    } else if (weather === 'Sunny') {
      const c = new Graphics(); c.rect(0, 0, SCENE_W, SCENE_H).fill(0xfff2b0); c.alpha = 0.12; this.weatherLayer.addChild(c);
    }
  }

  private update(): void {
    const dt = this.app.ticker.deltaMS / 1000;
    this.t += dt;

    this.plant.rotation = Math.sin(this.t * 1.5) * 0.035;
    this.pop = Math.max(0, this.pop - dt * 3);
    this.plant.scale.set(1 + this.pop * 0.22);

    this.drawCurtains();

    if (this.firefly.visible) {
      this.firefly.position.set(
        200 + Math.sin(this.t * 1.1) * 80 + Math.cos(this.t * 2.3) * 12,
        130 + Math.cos(this.t * 0.9) * 30 + Math.sin(this.t * 2.7) * 8,
      );
      this.firefly.alpha = 0.6 + Math.sin(this.t * 6) * 0.4;
    }

    for (const line of this.rainLines) { line.y += 220 * dt; if (line.y > SCENE_H) { line.y = -8; line.x = Math.random() * SCENE_W; } }

    if (this.dayNight.alpha < 0.4 && Math.random() < 0.03) {
      this.spawnParticle(30 + Math.random() * 110, 40 + Math.random() * 110, {
        color: 0xfff3d0, shape: 'dot', size: 1, vx: (Math.random() - 0.5) * 5, vy: -3 - Math.random() * 4, life: 3 + Math.random() * 2, maxAlpha: 0.45,
      });
    }
    if (this.growthStage >= 8 && this.health === 'Healthy' && Math.random() < 0.2) {
      this.spawnParticle(PLANT_X + (Math.random() * 30 - 15), PLANT_Y - 22 + (Math.random() * 20 - 10), { color: 0xffffaa, shape: 'star', size: 2, vx: (Math.random() - 0.5) * 10, vy: -15 - Math.random() * 10, life: 1 });
    }
    this.updateParticles(dt);

    if (this.state) this.applyDayNight(this.state.timeZoneId);
  }

  private drawCurtains(): void {
    const g = this.curtains;
    g.clear();
    const sway = Math.sin(this.t * 1.4) * 2;
    this.curtainPanel(g, 18 + sway, 28, 22);
    this.curtainPanel(g, 100 - sway, 28, 22);
  }

  private curtainPanel(g: Graphics, x: number, top: number, w: number): void {
    const bottom = 100, folds = 4, fw = w / folds;
    for (let i = 0; i < folds; i++) {
      const fx = x + i * fw, lightFold = i % 2 === 0, col = lightFold ? 0xcf6480 : 0xa8455e;
      g.rect(fx, top, fw + 0.5, bottom - top).fill(col);
      g.ellipse(fx + fw / 2, bottom, fw / 2 + 0.5, 4).fill(col);
      if (lightFold) g.rect(fx + 0.8, top, 1.4, bottom - top).fill(0xe88aa0);
    }
    const gy = top + (bottom - top) * 0.55;
    g.rect(x - 1, gy, w + 2, 4).fill(0x8a3850);
    g.rect(x - 1, gy, w + 2, 1).fill(0xb05a72);
  }

  private spawnParticle(x: number, y: number, opts: { color: number; shape?: ParticleShape; vx?: number; vy?: number; life?: number; gravity?: number; size?: number; spin?: number; maxAlpha?: number }): void {
    const g = new Graphics();
    const size = opts.size ?? 2;
    if (opts.shape === 'star') g.star(0, 0, 4, size).fill(opts.color);
    else if (opts.shape === 'petal') g.ellipse(0, 0, size, size * 0.55).fill(opts.color);
    else g.circle(0, 0, size).fill(opts.color);
    g.position.set(x, y);
    this.root.addChild(g);
    const life = opts.life ?? 1;
    this.particles.push({ g, vx: opts.vx ?? 0, vy: opts.vy ?? 0, life, maxLife: life, maxAlpha: opts.maxAlpha ?? 1, gravity: opts.gravity ?? 0, spin: opts.spin ?? 0 });
  }

  private updateParticles(dt: number): void {
    for (const p of this.particles) {
      p.life -= dt; p.vy += p.gravity * dt; p.g.x += p.vx * dt; p.g.y += p.vy * dt; p.g.rotation += p.spin * dt;
      p.g.alpha = Math.max(0, p.life / p.maxLife) * p.maxAlpha;
    }
    this.particles = this.particles.filter((p) => { if (p.life <= 0) { p.g.destroy(); return false; } return true; });
  }

  private applyDayNight(tz: string): void {
    let hour = new Date().getHours();
    try { hour = parseInt(new Intl.DateTimeFormat('en-GB', { hour: '2-digit', hour12: false, timeZone: tz }).format(new Date()), 10); } catch { /* zona inválida */ }
    let target = 0;
    if (hour < 6 || hour >= 21) target = 0.6;
    else if (hour < 8) target = 0.35;
    else if (hour < 18) target = 0;
    else target = 0.3;
    this.dayNight.alpha += (target - this.dayNight.alpha) * 0.05;
  }
}
