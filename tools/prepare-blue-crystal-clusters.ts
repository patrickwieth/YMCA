// Run from Game with external tsx/pngjs. Approved cutouts are retained in source/.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
import { withBlackOutline } from './crystal-outlines';
import { withEarthBase } from './crystal-earth';
const { PNG } = require('pngjs');
type Image = { width: number; height: number; data: Buffer };
const root = resolve(process.argv[2] ?? 'mods/ca/bits/resources');
const input = join(root,'blue-crystal-clusters','source');
const output = resolve(process.argv[3] ?? join(root,'blue-crystal-clusters'));
const blank = (width: number,height: number): Image => ({width,height,data:Buffer.alloc(width*height*4)});
const manifest = JSON.parse(readFileSync(join(input,'manifest.json'),'utf8')) as {name:string;sourceRectangle:number[]}[];
const anchors: Record<string,[number,number]> = {
  'A-01':[32,107], 'A-02':[97,102], 'A-03':[192,94], 'A-04':[228,94],
  'A-05':[170,119], 'A-06':[225,139], 'A-08':[35,185], 'A-09':[191,196],
  'A-11':[72,242], 'B-01':[54,78], 'B-05':[215,138],
  'C-01':[15,107], 'C-02':[116,93], 'C-03':[165,107], 'C-04':[224,126],
  'C-07':[218,220],
  'D-01':[125,35], 'D-02':[112,43], 'D-03':[92,55], 'D-04':[79,69], 'D-05':[13,96],
  'E-01':[156,43], 'E-04':[160,106], 'E-06':[72,139], 'E-07':[223,147],
  'E-09':[19,214], 'E-11':[204,246],
};
function chunk(key:string,value:string) {
  const payload=Buffer.from(key+'\0'+value), data=Buffer.concat([Buffer.from('tEXt'),payload]);
  let crc=0xffffffff;
  for(const byte of data) {crc^=byte;for(let bit=0;bit<8;bit++) crc=(crc>>>1)^((crc&1)?0xedb88320:0);}
  const size=Buffer.alloc(4),checksum=Buffer.alloc(4);size.writeUInt32BE(payload.length);checksum.writeUInt32BE((crc^0xffffffff)>>>0);
  return Buffer.concat([size,data,checksum]);
}
function save(name:string,image:Image,metadata:Record<string,string>={}) {
  const bytes:Buffer=PNG.sync.write(image,{colorType:6});
  writeFileSync(join(output,name),Buffer.concat([bytes.subarray(0,-12),...Object.entries(metadata).map(([k,v])=>chunk(k,v)),bytes.subarray(-12)]));
}
function stamp(target:Image,source:Image,dx:number,dy:number) {
  for(let y=0;y<source.height;y++) for(let x=0;x<source.width;x++) {
    const p=(y*source.width+x)*4;
    if(!source.data[p+3]) continue;
    if(x+dx<0||x+dx>=target.width||y+dy<0||y+dy>=target.height) throw new Error('Clipped source motif.');
    const q=((y+dy)*target.width+x+dx)*4;
    if(source.data[p+3]===255||!target.data[q+3]) {source.data.copy(target.data,q,p,p+4);continue;}
    const a=source.data[p+3]/255,b=target.data[q+3]/255,c=a+b*(1-a);
    for(let channel=0;channel<3;channel++) target.data[q+channel]=Math.round((source.data[p+channel]*a+target.data[q+channel]*b*(1-a))/c);
    target.data[q+3]=Math.round(c*255);
  }
}
mkdirSync(output,{recursive:true});
const pool:{name:string;art:Image}[]=[], hashes:Record<string,string>={};
for(const [name,anchor] of Object.entries(anchors)) {
  const entry=manifest.find(m=>m.name===name+'.png');
  if(!entry) throw new Error('Missing reviewed source '+name);
  const bytes=readFileSync(join(input,entry.name)),source:Image=PNG.sync.read(bytes);
  hashes[entry.name]=createHash('sha256').update(bytes).digest('hex');
  const [x,y,w,h]=entry.sourceRectangle;
  if(source.width!==w||source.height!==h) throw new Error('Reviewed source dimensions changed.');
  const art=blank(160,128);stamp(art,source,80-(anchor[0]-x),80-(anchor[1]-y));
  // Keep the height ceiling established when the two tallest motifs were removed.
  for(let y=0;y<54;y++) for(let x=0;x<160;x++)
    if(art.data[(y*160+x)*4+3]) throw new Error('Crystal exceeds the 26-pixel tip height: '+name);
  save(name+'-body.png',art);pool.push({name,art});
}
// Retain three low pre-existing blue motifs; legacy 2 and B-03 were the tallest.
// No recoloring or resizing.
for(const variant of [4,6,8]) {
  const name=`legacy-${variant}`, bytes=readFileSync(join(root,'blue-crystals',`blue-crystal-${variant}-body.png`));
  hashes[name]=createHash('sha256').update(bytes).digest('hex');
  const art=blank(160,128);stamp(art,PNG.sync.read(bytes),-16,-32);
  save(name+'-body.png',art);pool.push({name,art});
}
// These reviewed scene cutouts already have a visible rocky base, like the legacy motifs.
const existingEarth=new Set(['A-01','A-03','A-04','A-05','A-08','A-09','A-11','B-01']);
const earthBodies:Record<string,string>={};
const outlined=new Map(pool.map(m=>{
  let art=m.art;
  if(!m.name.startsWith('legacy-')&&!existingEarth.has(m.name)) {
    art=withEarthBase(art);
    earthBodies[m.name]=`grounded-${m.name}-body.png`;
    save(earthBodies[m.name],art);
  }
  return [m.name,withBlackOutline(art)];
}));
const outlineBodies:Record<string,string>={};
for(const [name,art] of outlined) {
  outlineBodies[name]=`outlined-${name}-body.png`;
  save(outlineBodies[name],art);
}
const positions:[number,number][]=[[0,-10],[-22,2],[22,2],[0,16]];
function fits(art:Image,dx:number,dy:number,outlineMargin=0) {
  for(let y=0;y<128;y++) for(let x=0;x<160;x++) if(art.data[(y*160+x)*4+3])
    if(x+dx<16-outlineMargin||x+dx>=144+outlineMargin||y+dy<0||y+dy>112+outlineMargin) return false;
  return true;
}
const used=new Set<string>(),layouts:string[][]=[],preview=blank(640,384);
for(let variant=0;variant<12;variant++) {
  const selected:typeof pool=[];
  for(let slot=0;slot<4;slot++) {
    const candidates=Array.from({length:pool.length},(_,i)=>pool[(variant*4+slot+i)%pool.length]);
    const item=candidates.find(m=>!selected.includes(m)&&fits(m.art,...positions[slot]));
    if(!item) throw new Error('No bounded cluster layout.');
    if(!fits(outlined.get(item.name)!,...positions[slot],1)) throw new Error('Outlined motif exceeds layout bounds.');
    selected.push(item);used.add(item.name);
  }
  layouts.push(selected.map(m=>m.name));
  const atlas=blank(1920,128);
  for(let frame=0;frame<12;frame++) {
    const art=blank(160,128);
    for(let slot=0;slot<=Math.floor(frame/3);slot++) stamp(art,outlined.get(selected[slot].name)!,...positions[slot]);
    stamp(atlas,art,frame*160,0);
    if(frame===11) stamp(preview,art,(variant%4)*160,Math.floor(variant/4)*128);
  }
  save(`cluster-${variant+1}-growth.png`,atlas,{FrameSize:'160,128',FrameAmount:'12',Offset:'0,-16'});
}
if(used.size!==pool.length) throw new Error('Not every small motif is represented.');
for(let p=0;p<preview.data.length;p+=4) {
  const a=preview.data[p+3]/255;
  for(let c=0;c<3;c++) preview.data[p+c]=Math.round(preview.data[p+c]*a+[101,126,75][c]*(1-a));
  preview.data[p+3]=255;
}
save('preview.png',preview);
writeFileSync(join(output,'layouts.json'),JSON.stringify({anchors,positions,layouts,sourceSHA256:hashes,earthBodies,outlineBodies,outline:{color:[0,0,0,255],radius:1,neighbors:4,preserveExistingDark:true}},null,2)+'\n');
writeFileSync(join(output,'provenance.txt'),'Generator: tools/prepare-blue-crystal-clusters.ts\nApproved scene cutouts: source/ (copied unchanged from extracted-blue-tiberium).\nRuntime: 27 small scene motifs plus legacy blue 4,6,8.\nNew small groups: C-01,02,03,04,07; D-01,02,03,04,05; E-01,04,06,07,09,11.\nExcluded scene groups: A-07,A-10,B-02,B-03,B-04,B-06,C-05,06,D-06,07,08,09,10,E-02,03,05,08,10. E-05 is cliff-occluded.\nExcluded legacy blue: 1,2,3,5,7.\nFour nested density states (1,2,3,4 motifs), each repeated for three frames.\nNative size, fixed ground anchors, original foreground RGBA, no generated glow or recoloring.\nSmall broken-earth bases added to the 19 previously bare motifs; eight soil-bearing scene motifs and all three legacy motifs unchanged.\nFinal outline: opaque black, one native pixel (4-neighbor mask), existing dark outlines not expanded; per motif before depth compositing; original source pixels unchanged.\n');
console.log(`PASS: ${pool.length} small blue motifs in 12 stable, bounded cluster arrangements.`);
