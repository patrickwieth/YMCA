namespace Ymca.TournamentBot;

internal static class TournamentBracketScript
{
    public const string Value = @"
'use strict';
let highlightedPlayers=[];
function highlight(players){
 highlightedPlayers=players;
 document.querySelectorAll('.tree').forEach(tree=>{
  tree.classList.toggle('highlighting',players.length>0);
  tree.querySelectorAll('.series').forEach(card=>card.classList.toggle('active',card.dataset.players.split(',').some(p=>players.includes(p))));
  tree.querySelectorAll('path').forEach(path=>path.classList.toggle('active',players.includes(path.dataset.player)));
 });
}
function draw(){
 document.querySelectorAll('.tree').forEach(tree=>{
  const svg=tree.querySelector('svg');svg.replaceChildren();
  const rounds=[...tree.querySelectorAll('.round')];
  tree.querySelectorAll('.series').forEach(card=>card.style.marginTop='0px');
  // Align later cards with their feeders without moving historical match IDs or pairings.
  rounds.slice(1).forEach((round,index)=>{
   round.querySelectorAll('.series').forEach(card=>{
    const feeders=[...card.querySelectorAll('.source-link')].map(link=>document.getElementById('series-'+link.dataset.source))
     .filter(from=>from?.closest('.round')===rounds[index]);
    if(!feeders.length)return;
    const center=feeders.reduce((sum,from)=>{const box=from.getBoundingClientRect();return sum+box.top+box.height/2;},0)/feeders.length;
    const box=card.getBoundingClientRect();card.style.marginTop=Math.max(0,center-box.top-box.height/2)+'px';
   });
  });
  const base=tree.getBoundingClientRect();
  tree.querySelectorAll('.source-link').forEach(link=>{
   const card=link.closest('.series'),from=document.getElementById('series-'+link.dataset.source);
   // Cross-bracket and skipped-round transitions use explicit navigable source links.
   // Never route a long line through unrelated cards or across another bracket.
   if(!from||from.closest('.tree')!==tree||rounds.indexOf(card.closest('.round'))!==rounds.indexOf(from.closest('.round'))+1)return;
   const player=link.dataset.player;
   const a=from.querySelector(`.side[data-player='${player}']`).getBoundingClientRect();
   const b=card.querySelector(`.side[data-player='${player}']`).getBoundingClientRect();
   const x=from.getBoundingClientRect().right-base.left,y=a.top+a.height/2-base.top;
   const u=card.getBoundingClientRect().left-base.left,v=b.top+b.height/2-base.top;
   const middle=(x+u)/2+(card.dataset.players.split(',')[0]===player?-7:7);
   const path=document.createElementNS('http://www.w3.org/2000/svg','path');
   path.dataset.player=player;path.dataset.source=from.id;path.dataset.target=card.id;
   path.setAttribute('d',`M ${x} ${y} H ${middle} V ${v} H ${u}`);
   svg.append(path);
  });
 });
 highlight(highlightedPlayers);
}
function playersAt(target){
 if(!(target instanceof Element))return [];
 const side=target.closest('[data-player]');
 if(side)return [side.dataset.player];
 return target.closest('.series')?.dataset.players.split(',')||[];
}
document.addEventListener('pointerover',event=>highlight(playersAt(event.target)));
document.addEventListener('pointerout',event=>{if(!event.relatedTarget?.closest?.('.series'))highlight(playersAt(document.activeElement));});
document.addEventListener('focusin',event=>highlight(playersAt(event.target)));
document.addEventListener('focusout',event=>highlight(playersAt(event.relatedTarget)));
document.addEventListener('click',event=>{
 const link=event.target.closest?.('.source-link');if(!link)return;
 const target=document.getElementById('series-'+link.dataset.source);if(!target)return;
 event.preventDefault();history.replaceState(null,'',link.getAttribute('href'));
 target.scrollIntoView({block:'center',inline:'center'});
 target.querySelector('summary').focus({preventScroll:true});
 highlight([link.dataset.player]);
});
document.addEventListener('toggle',()=>requestAnimationFrame(draw),true);
let observer;
function observe(){
 observer?.disconnect();
 if(typeof ResizeObserver!=='undefined'){
  observer=new ResizeObserver(()=>requestAnimationFrame(draw));
  document.querySelectorAll('.rounds').forEach(rounds=>observer.observe(rounds));
 }
 draw();
}
let loading=false;
async function refresh(){
 if(loading||document.hidden)return;loading=true;
 try{
  const response=await fetch(location.pathname,{cache:'no-store'});if(!response.ok)throw Error();
  const page=new DOMParser().parseFromString(await response.text(),'text/html'),next=page.getElementById('content');
  if(!next)throw Error();
  const opened=[...document.querySelectorAll('details[id][open]')].map(d=>d.id);
  const scroll=[...document.querySelectorAll('.tree-scroll')].map(el=>[el.closest('.bracket-lane').id,el.scrollLeft,el.scrollTop]);
  const focused=document.activeElement,focusDetails=focused.closest?.('details')?.id;
  const focusHref=focused.matches?.('a')?focused.getAttribute('href'):null;
  const focusLane=focused.matches?.('.tree-scroll')?focused.closest('.bracket-lane').id:null;
  const x=window.scrollX,y=window.scrollY;
  document.getElementById('content').replaceWith(next);
  opened.forEach(id=>{const details=document.getElementById(id);if(details)details.open=true;});
  scroll.forEach(([id,left,top])=>{const el=document.getElementById(id)?.querySelector('.tree-scroll');if(el){el.scrollLeft=left;el.scrollTop=top;}});
  let replacement=focusDetails?document.getElementById(focusDetails)?.querySelector('summary'):null;
  if(focusHref)replacement=[...next.querySelectorAll('a')].find(a=>a.getAttribute('href')===focusHref)||replacement;
  if(focusLane)replacement=document.getElementById(focusLane)?.querySelector('.tree-scroll');
  replacement?.focus({preventScroll:true});window.scrollTo(x,y);
  observe();document.getElementById('refresh-status').textContent='Updated '+new Date().toLocaleTimeString();
 }catch{document.getElementById('refresh-status').textContent='Update failed — retrying shortly';}
 finally{loading=false;}
}
observe();window.addEventListener('resize',draw);setInterval(refresh,10000);
document.addEventListener('visibilitychange',()=>{if(!document.hidden)refresh();});
";
}
