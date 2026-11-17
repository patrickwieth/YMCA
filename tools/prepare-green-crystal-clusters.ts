// External tsx/pngjs; see docs/green-crystal-clusters.md.
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
import { withBlackOutline } from './crystal-outlines';
import { withEarthBase } from './crystal-earth';
import { repairGreenCrystalContrast } from './repair-green-crystal-contrast';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const source = resolve(process.argv[2] ?? '../11 green crystals.png');
const root = resolve(process.argv[3] ?? 'mods/ca/bits/resources');
const output = resolve(process.argv[4] ?? join(root, 'green-crystal-clusters'));
const original = readFileSync(source);
const hash = createHash('sha256').update(original).digest('hex');
if (hash !== '199747faee651337d408fc8f3e994a3a7c51735e3d773444ebddab178185704f') throw new Error('Unexpected source artwork.');
const image: Image = PNG.sync.read(original);
if (image.width !== 256 || image.height !== 256) throw new Error('Expected 256x256 source.');
const blank = (width: number, height: number): Image => ({ width, height, data: Buffer.alloc(width * height * 4) });
function chunk(key: string, value: string): Buffer {
  const payload = Buffer.from(key + '\0' + value), data = Buffer.concat([Buffer.from('tEXt'), payload]);
  let crc = 0xffffffff;
  for (const byte of data) { crc ^= byte; for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  const size = Buffer.alloc(4), checksum = Buffer.alloc(4);
  size.writeUInt32BE(payload.length); checksum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
  return Buffer.concat([size, data, checksum]);
}
function save(name: string, art: Image, metadata: Record<string, string> = {}) {
  const bytes: Buffer = PNG.sync.write(art, { colorType: 6 });
  writeFileSync(join(output, name), Buffer.concat([bytes.subarray(0, -12),
    ...Object.entries(metadata).map(([key, value]) => chunk(key, value)), bytes.subarray(-12)]));
}
const labels = new Int32Array(256 * 256).fill(-1), components: number[][] = [];
for (let i = 0; i < labels.length; i++) {
  if (!image.data[i * 4 + 3] || labels[i] >= 0) continue;
  const pixels = [i]; labels[i] = components.length;
  for (let j = 0; j < pixels.length; j++) {
    const x = pixels[j] % 256, y = Math.floor(pixels[j] / 256);
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, ny = y + dy, next = ny * 256 + nx;
      if (nx < 0 || ny < 0 || nx >= 256 || ny >= 256 || labels[next] >= 0 || !image.data[next * 4 + 3]) continue;
      labels[next] = components.length; pixels.push(next);
    }
  }
  components.push(pixels);
}
const anchors = [[53,100],[97,96],[193,84],[23,136],[130,146],[218,130],[58,183],[188,203],[32,235],[99,244],[232,246]];
const main = anchors.map(([x,y]) => labels[y * 256 + x]);
if (new Set(main).size !== 11 || main.some(i => i < 0 || components[i].length < 150)) throw new Error('Expected eleven bodies.');
const groups = main.map(i => [...components[i]]);
for (let i = 0; i < components.length; i++) {
  if (main.includes(i)) continue;
  let nearest = -1, distance = Infinity;
  for (const p of components[i]) for (let v = 0; v < main.length; v++) for (const q of components[main[v]]) {
    const d = (p % 256 - q % 256) ** 2 + (Math.floor(p / 256) - Math.floor(q / 256)) ** 2;
    if (d < distance) { nearest = v; distance = d; }
  }
  groups[nearest].push(...components[i]);
}
mkdirSync(output, { recursive: true });
const pool: { name: string; art: Image }[] = [];
const owned = new Set<number>();
const records = [`Source SHA256: ${hash}`, 'Source anchors: ' + JSON.stringify(anchors),
  'Excluded source motifs: 5,6,7,8. Seven small motifs used at native size; motif 6 removed in the height review.',
  'New green hues: 95 + (source hue - 120) * 0.35; HSL lightness/saturation preserved.',
  'Density: frames 0-2 one motif, 3-5 two, 6-8 three, 9-11 four. Fixed positions and sizes.'];
for (let v = 0; v < anchors.length; v++) {
  const art = blank(160,128), [ax,ay] = anchors[v];
  for (const p of groups[v]) {
    if (owned.has(p)) throw new Error('Duplicate source pixel.');
    owned.add(p);
    const x = p % 256 - ax + 80, y = Math.floor(p / 256) - ay + 80;
    if (x < 0 || x >= 160 || y < 0 || y >= 128) throw new Error('Clipped motif.');
    image.data.copy(art.data, (y * 160 + x) * 4, p * 4, p * 4 + 4);
  }
  save(`source-${v + 1}-body.png`, art);
  if ([4,5,6,7].includes(v)) continue;
  for (let p = 0; p < art.data.length; p += 4) {
    const r = art.data[p], g = art.data[p+1], b = art.data[p+2], low = Math.min(r,g,b), chroma = g-low;
    if (!art.data[p+3] || g < 40 || g <= r || g <= b || chroma < 15) continue;
    const hue = 60 * ((b-r)/chroma+2);
    if (hue < 100 || hue > 170) continue;
    const h = (95 + (hue-120)*0.35)/60;
    art.data[p] = Math.round(chroma*(2-h)+low); art.data[p+2] = low;
  }
  save(`harmonized-${v + 1}-body.png`, art);
  pool.push({ name: `eleven-${v+1}`, art });
}
let sourceCount = 0;
for (let p = 3; p < image.data.length; p += 4) if (image.data[p]) sourceCount++;
if (owned.size !== sourceCount) throw new Error('Lost source pixels.');
records.push(`All ${sourceCount} source pixels retained exactly once in the eleven raw bodies.`);
for (const [directory, variant] of [['green-crystals-harmonized',1], ...[1,2,3,5].map(v => ['green-crystals-seven',v])] as [string,number][]) {
  pool.push({ name: `${directory}-${variant}`, art: PNG.sync.read(readFileSync(join(root,directory,`green-crystal-${variant}-body.png`))) });
}
const sceneRoot = join(root,'green-scene-crystals');
const sceneManifest = JSON.parse(readFileSync(join(sceneRoot,'manifest.json'),'utf8')) as {name:string}[];
for (const {name} of sceneManifest)
  pool.push({name,art:PNG.sync.read(readFileSync(join(sceneRoot,name+'-body.png')))});
records.push(`Additional small green scene motifs: ${sceneManifest.map(m=>m.name).join(', ')}. See green-scene-crystals/manifest.json for source hashes and anchors.`);
// Use one actual in-game, soil-bearing legacy motif as the color authority.
// Match the lightness distribution as well as the colors. A hue-only match
// still leaves the newer sheets much brighter than the earthy reference.
// Quantile matching preserves shade ordering without changing any geometry.
const paletteReference = 'green-crystals-harmonized/green-crystal-1-body.png';
const referenceBytes = readFileSync(join(root,paletteReference));
const reference: Image = PNG.sync.read(referenceBytes);
function isCrystalGreen(r: number,g: number,b: number): boolean {
  const chroma = g-Math.min(r,g,b);
  if (g<40 || g<=r || g<=b || chroma<15) return false;
  const hue = 60*((b-r)/chroma+2);
  return hue>=70 && hue<=170;
}
const shades = new Map<string,{rgb:number[];lightness:number;count:number}>();
for (let p=0;p<reference.data.length;p+=4) {
  const rgb = [...reference.data.subarray(p,p+3)];
  if (!reference.data[p+3] || !isCrystalGreen(rgb[0],rgb[1],rgb[2])) continue;
  const key = rgb.join(','), existing = shades.get(key);
  if (existing) existing.count++;
  else shades.set(key,{rgb,lightness:(Math.max(...rgb)+Math.min(...rgb))/2,count:1});
}
if (shades.size<8) throw new Error('Insufficient legacy green palette.');
const palette = [...shades.values()].sort((a,b)=>a.lightness-b.lightness || b.count-a.count);
const referenceTotal = palette.reduce((sum,p)=>sum+p.count,0);
const paletteBodies: Record<string,string> = {};
const contrastRepaired = pool.filter(m=>/^eleven-[1-4]$|^scene-[BC]-/.test(m.name)).map(m=>m.name);
for (const {name,art} of pool) {
  if (contrastRepaired.includes(name)) {
    repairGreenCrystalContrast(art,palette.map(p=>p.rgb));
    paletteBodies[name]=`palette-${name}-body.png`;
    save(paletteBodies[name],art);
    continue;
  }
  const histogram = new Map<number,number>();
  for (let p=0;p<art.data.length;p+=4) {
    const r=art.data[p],g=art.data[p+1],b=art.data[p+2];
    if (!art.data[p+3] || !isCrystalGreen(r,g,b)) continue;
    const lightness=(g+Math.min(r,b))/2;
    histogram.set(lightness,(histogram.get(lightness)??0)+1);
  }
  const total=[...histogram.values()].reduce((a,b)=>a+b,0), mapping=new Map<number,number[]>();
  let cumulative=0;
  for (const [lightness,count] of [...histogram.entries()].sort((a,b)=>a[0]-b[0])) {
    const quantile=(cumulative+count/2)/total;
    let referenceCumulative=0, match=palette[palette.length-1];
    for (const shade of palette) {
      referenceCumulative+=shade.count/referenceTotal;
      if (quantile<=referenceCumulative) { match=shade; break; }
    }
    mapping.set(lightness,match.rgb); cumulative+=count;
  }
  for (let p=0;p<art.data.length;p+=4) {
    const r=art.data[p],g=art.data[p+1],b=art.data[p+2];
    if (name==='green-crystals-harmonized-1' || !art.data[p+3] || !isCrystalGreen(r,g,b)) continue;
    const rgb=mapping.get((g+Math.min(r,b))/2)!;
    for (let c=0;c<3;c++) art.data[p+c]=rgb[c];
  }
  paletteBodies[name]=`palette-${name}-body.png`;
  save(paletteBodies[name],art);
}
const paletteReferenceSHA256 = createHash('sha256').update(referenceBytes).digest('hex');
records.push(`Final runtime palette: ${paletteReference}; SHA256 ${paletteReferenceSHA256}; ${palette.length} authored green shades. Original files retained; targeted contrast/edge treatment recorded below.`);
records.push(`Contrast/fringe repair: ${contrastRepaired.join(', ')}. Preserve facet shade ordering with 0.85 lightness compression; exclude muted earth colors; replace existing dark green boundary pixels with black without expanding alpha.`);
const earthBodies: Record<string,string> = {};
const outlined = new Map(pool.map(m=>{
  let art=m.art;
  // The older sheets already contain their own broken ground; preserve it.
  if (m.name.startsWith('scene-')) {
    art=withEarthBase(art);
    earthBodies[m.name]=`grounded-${m.name}-body.png`;
    save(earthBodies[m.name],art);
  }
  return [m.name,withBlackOutline(art)];
}));
records.push(`Small broken-earth bases added only to ${Object.keys(earthBodies).length} previously bare scene motifs; original soil-bearing motifs unchanged.`);
const outlineBodies: Record<string,string> = {};
const handEdited: Record<string,{file:string;sha256:string}> = {};
for (const [name,art] of outlined) {
  outlineBodies[name]=`outlined-${name}-body.png`;
  // Hand corrections are final art: bypass every color, earth and outline pass.
  // Always read from the source directory, including when output is staged elsewhere.
  const file=join('hand-edited',outlineBodies[name]);
  const path=join(root,'green-crystal-clusters',file);
  if (existsSync(path)) {
    const bytes=readFileSync(path), corrected:Image=PNG.sync.read(bytes);
    if (corrected.width!==160 || corrected.height!==128 || !corrected.data.some((v,i)=>i%4===3&&v>0))
      throw new Error(`Invalid hand-edited motif (expected nonempty 160x128 PNG): ${path}`);
    outlined.set(name,corrected);
    handEdited[name]={file:file.replace(/\\/g,'/'),sha256:createHash('sha256').update(bytes).digest('hex')};
    writeFileSync(join(output,outlineBodies[name]),bytes);
  } else save(outlineBodies[name],art);
}
records.push(`Final hand-edited overrides: ${Object.keys(handEdited).length}. See layouts.json for source files/hashes. Copied byte-for-byte after all generated processing.`);
records.push('Final outline: opaque black, one native pixel (4-neighbor mask), existing dark outlines not expanded; per motif before depth compositing; existing RGBA pixels preserved.');
const positions = [[0,-10],[-22,2],[22,2],[0,16]];
function fits(art: Image, dx: number, dy: number, outlineMargin = 0) {
  for (let y = 0; y < 128; y++) for (let x = 0; x < 160; x++) if (art.data[(y*160+x)*4+3])
    if (x+dx < 16-outlineMargin || x+dx >= 144+outlineMargin || y+dy < 0 || y+dy > 112+outlineMargin) return false;
  return true;
}
function stamp(target: Image, art: Image, dx: number, dy: number) {
  for (let y = 0; y < art.height; y++) for (let x = 0; x < art.width; x++) {
    const p = (y*art.width+x)*4;
    if (!art.data[p+3]) continue;
    const dest = ((y+dy)*target.width+x+dx)*4;
    if (art.data[p+3] === 255 || !target.data[dest+3]) {
      art.data.copy(target.data,dest,p,p+4);
      continue;
    }
    const alpha = art.data[p+3]/255, underneath = target.data[dest+3]/255;
    const combined = alpha + underneath*(1-alpha);
    for (let c = 0; c < 3; c++) target.data[dest+c] = Math.round((art.data[p+c]*alpha + target.data[dest+c]*underneath*(1-alpha))/combined);
    target.data[dest+3] = Math.round(combined*255);
  }
}
const preview = blank(640,384), used = new Set<string>();
const layouts: string[][] = [];
for (let v = 0; v < 12; v++) {
  const selected: typeof pool = [];
  for (let slot = 0; slot < 4; slot++) {
    const [dx,dy] = positions[slot];
    const candidates = Array.from({ length: pool.length }, (_, i) => pool[(v*4+slot+i)%pool.length]);
    // Give every eligible motif a slot, including wider bodies that fit only
    // the center positions. Stable sorting preserves deterministic tie order.
    candidates.sort((a,b) => Number(used.has(a.name))-Number(used.has(b.name)));
    const motif = candidates.find(m => !selected.includes(m) && fits(m.art,dx,dy));
    if (!motif) throw new Error('No bounded cluster arrangement.');
    if (!fits(outlined.get(motif.name)!,dx,dy,1)) throw new Error('Outlined motif exceeds layout bounds.');
    selected.push(motif); used.add(motif.name);
  }
  layouts.push(selected.map(m => m.name));
  const atlas = blank(1920,128);
  for (let frame = 0; frame < 12; frame++) {
    const art = blank(160,128);
    for (let slot = 0; slot <= Math.floor(frame/3); slot++) stamp(art,outlined.get(selected[slot].name)!,...positions[slot] as [number,number]);
    stamp(atlas,art,frame*160,0);
    if (frame === 11) stamp(preview,art,(v%4)*160,Math.floor(v/4)*128);
  }
  save(`cluster-${v+1}-growth.png`,atlas,{FrameSize:'160,128',FrameAmount:'12',Offset:'0,-16'});
}
if (used.size !== pool.length) throw new Error('Some eligible motifs are missing from the layouts.');
for (let p = 0; p < preview.data.length; p += 4) {
  const alpha = preview.data[p+3]/255, background = [101,126,75];
  for (let c = 0; c < 3; c++) preview.data[p+c] = Math.round(preview.data[p+c]*alpha + background[c]*(1-alpha));
  preview.data[p+3] = 255;
}
save('preview.png',preview);
writeFileSync(join(output,'layouts.json'),JSON.stringify({ positions, layouts, paletteReference, paletteReferenceSHA256, palette:palette.map(p=>p.rgb), paletteBodies, contrastRepaired, earthBodies, outlineBodies, handEdited, outline:{color:[0,0,0,255],radius:1,neighbors:4,preserveExistingDark:true} },null,2)+'\n');
writeFileSync(join(output,'provenance.txt'),records.join('\n')+'\n');
if (!readFileSync(source).equals(original)) throw new Error('Source modified.');
console.log(`PASS: eleven exact extractions (${sourceCount} pixels), ${pool.length} small native-size motifs, twelve stable density layouts.`);
