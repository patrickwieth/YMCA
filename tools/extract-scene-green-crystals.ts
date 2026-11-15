// Small motifs only. Run from Game with external tsx/pngjs; sources are kept unchanged.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
const { PNG } = require('pngjs');
const root=resolve(process.argv[2]??'mods/ca/bits/resources/green-scene-crystals');
const sources=[
  {file:'isometric_scattered_...-193181616-0.png',id:'A',regions:[[38,104,52,123,45,117],[71,113,97,146,83,140],[116,103,145,137,130,132],[133,139,159,167,146,163],[207,94,229,119,217,115]]},
  {file:'isometric_scattered_...-581459725-0.png',id:'B',regions:[[96,65,117,94,106,89],[110,109,141,139,126,134],[92,142,115,165,105,161],[168,122,200,153,184,149]]},
  {file:'isometric_scattered_...-180869324-0.png',id:'C',regions:[[104,46,125,68,115,64],[48,92,71,119,60,115],[104,101,126,130,116,126],[210,94,225,115,218,111],[148,141,178,168,162,165]]},
];
mkdirSync(root,{recursive:true});
const manifest:object[]=[];
const preview={width:800,height:384,data:Buffer.alloc(800*384*4)};
for(let p=0;p<preview.data.length;p+=4) {preview.data[p]=101;preview.data[p+1]=126;preview.data[p+2]=75;preview.data[p+3]=255;}
for(const source of sources) {
  const bytes=readFileSync(join(root,'source',source.file)),hash=createHash('sha256').update(bytes).digest('hex'),image=PNG.sync.read(bytes);
  if(image.width!==256||image.height!==256) throw new Error('Expected 256x256 source.');
  source.regions.forEach(([x0,y0,x1,y1,ax,ay],index)=>{
    const mask=new Uint8Array(65536),seeds=new Uint8Array(65536);
    for(let y=y0;y<y1;y++) for(let x=x0;x<x1;x++) {
      const p=(y*256+x)*4,r=image.data[p],g=image.data[p+1],b=image.data[p+2];
      if(image.data[p+3]&&g>=65&&g-r>=15&&g-b>=30) mask[y*256+x]=seeds[y*256+x]=1;
    }
    for(let y=y0;y<y1;y++) for(let x=x0;x<x1;x++) {
      const p=(y*256+x)*4;
      if(!image.data[p+3]||Math.max(image.data[p],image.data[p+1],image.data[p+2])>60) continue;
      let neighbors=0;
      for(let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++) neighbors+=seeds[(y+dy)*256+x+dx]??0;
      if(neighbors>=2) mask[y*256+x]=1;
    }
    const outside=new Uint8Array(65536),queue=[0];outside[0]=1;
    for(let i=0;i<queue.length;i++) {
      const p=queue[i],x=p%256,y=Math.floor(p/256);
      for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]) {
        const nx=x+dx,ny=y+dy,q=ny*256+nx;
        if(nx<0||nx>=256||ny<0||ny>=256||outside[q]||mask[q]) continue;
        outside[q]=1;queue.push(q);
      }
    }
    for(let p=0;p<mask.length;p++) if(!outside[p]) mask[p]=1;
    const body={width:160,height:128,data:Buffer.alloc(160*128*4)};let pixels=0;
    for(let y=0;y<256;y++) for(let x=0;x<256;x++) if(mask[y*256+x]) {
      const bx=x-ax+80,by=y-ay+80;
      if(bx<0||bx>=160||by<52||by>=128) throw new Error('Large or clipped motif.');
      image.data.copy(body.data,(by*160+bx)*4,(y*256+x)*4,(y*256+x)*4+4);pixels++;
    }
    if(pixels<20) throw new Error('Empty/insufficient motif.');
    const name=`scene-${source.id}-${index+1}`;
    writeFileSync(join(root,name+'-raw.png'),PNG.sync.write(body,{colorType:6}));
    // Match the established yellow-green family. Preserve HSL lightness and
    // geometry, but cap saturation at 0.62 to avoid neon patches among the
    // older pastel-green crystals. Raw extracted pixels remain unchanged.
    for(let p=0;p<body.data.length;p+=4) {
      const r=body.data[p],g=body.data[p+1],b=body.data[p+2],low=Math.min(r,g,b),chroma=g-low;
      if(!body.data[p+3]||g<40||g<=r||g<=b||chroma<15) continue;
      const hue=60*((b-r)/chroma+2);
      if(hue<60||hue>170) continue;
      const h=(95+(hue-100)*0.35)/60,lightness=(g+low)/2;
      const matchedChroma=Math.min(chroma,0.62*(255-Math.abs(g+low-255)));
      const base=lightness-matchedChroma/2;
      body.data[p]=Math.round(matchedChroma*(2-h)+base);
      body.data[p+1]=Math.round(lightness+matchedChroma/2);
      body.data[p+2]=Math.round(base);
    }
    writeFileSync(join(root,name+'-body.png'),PNG.sync.write(body,{colorType:6}));
    const v=manifest.length;
    for(let y=0;y<128;y++) for(let x=0;x<160;x++) {
      const p=(y*160+x)*4,a=body.data[p+3]/255,q=((Math.floor(v/5)*128+y)*800+(v%5)*160+x)*4;
      for(let c=0;c<3;c++) preview.data[q+c]=Math.round(body.data[p+c]*a+preview.data[q+c]*(1-a));
    }
    manifest.push({name,source:source.file,sourceSHA256:hash,region:[x0,y0,x1,y1],anchor:[ax,ay],pixels});
  });
  if(!readFileSync(join(root,'source',source.file)).equals(bytes)) throw new Error('Source modified.');
}
writeFileSync(join(root,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
writeFileSync(join(root,'preview.png'),PNG.sync.write(preview,{colorType:6}));
console.log(`PASS: ${manifest.length} small green scene motifs extracted and harmonized; large groups excluded.`);
