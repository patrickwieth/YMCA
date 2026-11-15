// Review-only extraction from opaque scenes. Run from the YMCA top folder with external tsx/pngjs.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { join, resolve } from 'node:path';
const { PNG } = require('pngjs');
type Point = [number, number];
const output = resolve('extracted-blue-tiberium');
const sources = [
  { id: 'C', file: 'isometric_scattered_...-2112784646-0.png', regions: [
    [[4,93],[26,93],[26,112],[4,112]],
    [[105,78],[127,78],[127,97],[105,97]],
    [[153,89],[179,89],[179,112],[153,112]],
    [[213,108],[237,108],[237,130],[213,130]],
    [[105,136],[155,136],[155,174],[105,174]],
    [[63,166],[97,166],[97,198],[63,198]],
    [[207,201],[231,201],[231,225],[207,225]],
  ] },
  { id: 'D', file: 'isometric_scattered_...-1492263921-0.png', regions: [
    [[122,29],[128,29],[128,37],[122,37]],
    [[108,36],[117,36],[117,46],[108,46]],
    [[85,42],[100,42],[100,59],[85,59]],
    [[74,61],[84,61],[84,72],[74,72]],
    [[8,86],[18,86],[18,99],[8,99]],
    [[147,88],[151,78],[160,79],[160,61],[164,58],[169,61],[170,72],[174,66],[184,55],[188,55],[194,70],[194,85],[199,84],[205,85],[205,94],[199,105],[190,114],[177,114],[168,112],[159,108],[153,103],[152,94]],
    [[34,127],[42,130],[46,108],[50,105],[55,108],[60,121],[64,118],[70,116],[76,120],[77,128],[74,135],[78,142],[71,150],[52,153],[40,146]],
    [[80,101],[104,101],[104,142],[80,142]],
    [[18,186],[61,186],[61,227],[18,227]],
    [[177,161],[221,161],[221,207],[177,207]],
  ] },
  { id: 'E', file: 'isometric_scattered_...-1516823658-0.png', regions: [
    [[145,27],[170,27],[170,49],[145,49]],
    [[198,25],[239,25],[239,69],[198,69]],
    [[77,80],[80,64],[84,63],[91,73],[95,77],[98,72],[106,72],[108,79],[108,88],[112,92],[108,98],[93,100],[83,94],[78,90]],
    [[145,81],[175,81],[175,112],[145,112]],
    [[42,102],[57,102],[57,130],[42,130]],
    [[65,131],[69,131],[70,125],[73,124],[77,132],[80,134],[81,140],[75,143],[66,141]],
    [[209,123],[237,123],[237,152],[209,152]],
    [[107,147],[173,147],[173,195],[107,195]],
    [[8,190],[30,190],[30,218],[8,218]],
    [[36,208],[80,208],[80,249],[36,249]],
    [[184,221],[225,221],[225,254],[184,254]],
  ] },
  { id: 'A', file: 'isometric_scattered_...-1559604645-0.png', regions: [
    [[18,94],[26,88],[30,84],[37,94],[43,93],[46,99],[42,108],[31,113],[18,109]],
    [[91,98],[96,94],[100,94],[104,103],[91,105]],
    [[177,85],[184,81],[187,83],[188,77],[194,77],[196,84],[200,81],[204,82],[207,96],[191,99],[177,96]],
    [[214,91],[220,82],[224,84],[224,76],[230,77],[233,85],[236,82],[239,86],[237,96],[226,100],[214,96]],
    [[160,109],[164,109],[164,104],[168,99],[171,100],[174,108],[180,108],[184,114],[183,120],[173,123],[161,121]],
    [[220,135],[223,129],[226,131],[227,135],[231,136],[231,141],[223,143],[220,139]],
    [[88,159],[93,145],[103,147],[103,133],[108,123],[115,126],[118,143],[124,135],[130,137],[136,127],[143,122],[150,127],[151,144],[148,151],[156,155],[157,161],[151,168],[137,168],[120,174],[108,172],[100,165],[89,165]],
    [[20,175],[25,172],[25,166],[31,158],[36,162],[40,175],[43,173],[47,175],[50,170],[54,169],[57,175],[53,181],[45,181],[43,190],[34,191],[24,188],[21,182]],
    [[178,178],[207,178],[207,194],[204,194],[204,201],[178,201]],
    [[205,195],[213,195],[216,185],[221,179],[227,173],[231,176],[231,197],[236,195],[240,197],[239,209],[234,216],[225,223],[205,223],[204,214],[208,207]],
    [[49,240],[55,232],[58,218],[65,219],[70,228],[72,221],[77,222],[79,231],[83,228],[87,231],[87,238],[96,237],[96,242],[86,248],[73,254],[59,248],[49,247]],
  ] },
  { id: 'B', file: 'isometric_scattered_...-46821529-0.png', regions: [
    [[42,59],[66,59],[66,82],[42,82]],
    [[215,57],[253,57],[253,93],[215,93]],
    [[134,106],[138,105],[142,111],[145,110],[148,100],[153,101],[155,108],[158,99],[165,90],[169,90],[169,108],[175,108],[176,115],[171,122],[157,125],[147,122],[145,118],[140,116],[134,111]],
    [[43,142],[48,143],[48,129],[51,128],[60,138],[61,124],[67,114],[71,117],[74,129],[74,137],[78,133],[82,137],[89,133],[94,130],[96,133],[93,142],[86,149],[96,147],[98,151],[93,158],[80,160],[74,162],[60,161],[56,155],[52,154],[49,148],[44,147]],
    [[204,129],[208,129],[209,119],[211,115],[214,118],[216,129],[219,128],[222,126],[225,129],[227,136],[223,140],[209,141],[204,137]],
    [[193,205],[198,205],[198,196],[201,195],[207,202],[209,193],[213,187],[219,186],[220,190],[218,208],[227,201],[231,201],[233,206],[226,216],[224,222],[210,223],[198,219],[193,213]],
  ] },
];
function inside(x: number, y: number, polygon: Point[]) {
  let result = false;
  for (let i = 0, j = polygon.length-1; i < polygon.length; j = i++) {
    const [xi,yi] = polygon[i], [xj,yj] = polygon[j];
    if ((yi > y) !== (yj > y) && x < (xj-xi)*(y-yi)/(yj-yi)+xi) result = !result;
  }
  return result;
}
mkdirSync(join(output,'crystals'),{recursive:true});
mkdirSync(join(output,'original-crops'),{recursive:true});
const manifest: object[] = [], cards: string[] = [];
for (const source of sources) {
  const original = readFileSync(source.file), hash = createHash('sha256').update(original).digest('hex');
  const image = PNG.sync.read(original);
  if (image.width !== 256 || image.height !== 256) throw new Error('Unexpected image dimensions.');
  source.regions.forEach((region,index) => {
    const polygon = region as Point[], mask = new Uint8Array(256*256), seeds = new Uint8Array(mask.length);
    for (let y = 0; y < 256; y++) for (let x = 0; x < 256; x++) {
      if (!inside(x+0.5,y+0.5,polygon)) continue;
      const p = (y*256+x)*4, r=image.data[p], g=image.data[p+1], b=image.data[p+2];
      // The new scenes have cyan-lit ground: reject its muted colors while
      // retaining saturated blue faces and bright pale crystal highlights.
      const foreground = 'CDE'.includes(source.id)
        ? b >= 85 && g-r >= 20 && b >= g-12 && ((b-r >= 50 && (b-r)/b >= 0.58) || (b >= 170 && g-r >= 45))
        : b-r >= 35 && g-r >= 20 && b >= g-12 && b >= 65;
      if (foreground) seeds[y*256+x] = mask[y*256+x] = 1;
    }
    // Keep a one-pixel authored dark edge next to the colored body. No new outline is painted.
    for (let y = 1; y < 255; y++) for (let x = 1; x < 255; x++) {
      if (!inside(x+0.5,y+0.5,polygon) || mask[y*256+x]) continue;
      const p=(y*256+x)*4, r=image.data[p],g=image.data[p+1],b=image.data[p+2];
      if (Math.max(r,g,b)>60 || b<r) continue;
      let neighbors=0;
      for (let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++) neighbors+=seeds[(y+dy)*256+x+dx];
      if (neighbors>=2) mask[y*256+x]=1;
    }
    // Restore enclosed shaded pixels rather than leaving holes inside the crystals.
    const outside = new Uint8Array(mask.length), queue = [0]; outside[0]=1;
    for(let i=0;i<queue.length;i++) {
      const p=queue[i],x=p%256,y=Math.floor(p/256);
      for(const [dx,dy] of [[1,0],[-1,0],[0,1],[0,-1]]) {
        const nx=x+dx,ny=y+dy,q=ny*256+nx;
        if(nx<0||nx>=256||ny<0||ny>=256||outside[q]||mask[q]) continue;
        outside[q]=1;queue.push(q);
      }
    }
    for(let p=0;p<mask.length;p++) if(!outside[p]) mask[p]=1;
    const pixels=Array.from(mask.keys()).filter(p=>mask[p]);
    if(!pixels.length) throw new Error('Empty crystal.');
    const x0=Math.max(0,Math.min(...pixels.map(p=>p%256))-3), y0=Math.max(0,Math.min(...pixels.map(p=>Math.floor(p/256)))-3);
    const x1=Math.min(256,Math.max(...pixels.map(p=>p%256))+4), y1=Math.min(256,Math.max(...pixels.map(p=>Math.floor(p/256)))+4);
    const width=x1-x0,height=y1-y0, cut={width,height,data:Buffer.alloc(width*height*4)}, crop={width,height,data:Buffer.alloc(width*height*4)};
    for(let y=y0;y<y1;y++) for(let x=x0;x<x1;x++) {
      const p=(y*256+x)*4,q=((y-y0)*width+x-x0)*4;
      image.data.copy(crop.data,q,p,p+4);
      if(mask[y*256+x]) image.data.copy(cut.data,q,p,p+4);
    }
    const name=`${source.id}-${String(index+1).padStart(2,'0')}.png`;
    writeFileSync(join(output,'crystals',name),PNG.sync.write(cut));
    writeFileSync(join(output,'original-crops',name),PNG.sync.write(crop));
    manifest.push({name,source:source.file,sourceSHA256:hash,sourceRectangle:[x0,y0,width,height],retainedPixels:pixels.length,polygon});
    cards.push(`<article><h2>${name}</h2><div class="pair"><div>Originalausschnitt<br><img src="original-crops/${name}" width="${width*4}" height="${height*4}"></div><div>Freigestellt (4×)<br><a href="crystals/${name}"><img class="checker" src="crystals/${name}" width="${width*4}" height="${height*4}"></a></div></div><p>Originalgröße: ${width} × ${height} Pixel. <a href="crystals/${name}">PNG öffnen</a></p></article>`);
  });
  if(!readFileSync(source.file).equals(original)) throw new Error('Source was modified.');
}
writeFileSync(join(output,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
writeFileSync(join(output,'START-HIER.html'),`<!doctype html><html lang="de"><meta charset="utf-8"><title>Blaue Tiberiumkristalle – Freistellprüfung</title><style>body{background:#252831;color:#eee;font:16px sans-serif;margin:30px}article{padding:16px;border:1px solid #666;margin:20px 0;break-inside:avoid}.pair{display:flex;gap:32px;align-items:start;flex-wrap:wrap}img{image-rendering:pixelated;max-width:100%;object-fit:contain}a{color:#8eeaff}.checker{background-color:#aaa;background-image:conic-gradient(#666 25%,transparent 0 50%,#666 0 75%,transparent 0);background-size:16px 16px}</style><h1>${manifest.length} blaue Kristallgruppen – Prüfversion</h1><p>A/B: bisherige Szenen. C/D/E: die drei neuen Szenen. Die Zuordnung zu Originaldateien steht in manifest.json. Die PNGs behalten Originalpixel und Originalgröße; hier nur zur Kontrolle 4× vergrößert.</p><p>Die Vorlagen enthalten Landschaft statt Transparenz. Die Freistellung benutzt manuelle Regionen und Farbmasken. Bitte insbesondere Füße und dunkle Ränder kontrollieren. Verdeckte Stellen (vor allem B-04) wurden nicht erfunden oder ergänzt. Noch nicht ins Spiel eingebunden.</p>${cards.join('\n')}</html>`);
console.log(`Extracted ${manifest.length} review PNGs to ${join(output,'crystals')}`);
