import {Simulation,compareFailures} from './simulation.mjs';
import {peerBackupStatus} from './peer-status.mjs';
const $=id=>document.getElementById(id);
const esc=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const short=id=>id?.slice(0,8)||'None';
const table=(headers,rows)=>`<table><thead><tr>${headers.map(h=>`<th>${esc(h)}</th>`).join('')}</tr></thead><tbody>${rows.join('')}</tbody></table>`;
const regions=list=>list.map(r=>`(${r.x}, ${r.z})`).join(', ')||'None';
let selectedPeer='',lastSnapshot=null,lastRecent=()=>false,lastLive=false;
export function renderConnections(snapshot,isRecent,live){
  lastSnapshot=snapshot;lastRecent=isRecent;lastLive=live;
  if(!snapshot){$('connection-map').innerHTML='';$('connection-note').textContent='Start a game observer to inspect real connections. The simulation works without a game.';$('peer-storage').innerHTML='<div class="empty">No telemetry yet.</div>';$('peer-storage-detail').textContent='';$('backup-coverage').textContent='Waiting for manifests.';return;}
  const peers=snapshot.peers||[],local=peers.find(p=>p.local),remote=peers.filter(p=>!p.local),fresh=new Set(peers.filter(isRecent).map(p=>p.id));
  let svg='';
  const cx=380,cy=170,radius=120;
  remote.forEach((p,i)=>{const angle=i/Math.max(1,remote.length)*Math.PI*2-Math.PI/2,x=cx+Math.cos(angle)*240,y=cy+Math.sin(angle)*radius;
    svg+=`<line x1="${cx}" y1="${cy}" x2="${x}" y2="${y}" stroke="${isRecent(p)?'#82b89c':'#b7bdb9'}" stroke-width="2" ${isRecent(p)?'':'stroke-dasharray="5 5"'}/><circle cx="${x}" cy="${y}" r="21" fill="${isRecent(p)?'#dcebd9':'#eef0ed'}" stroke="#96ad9f"/><text x="${x}" y="${y+4}" text-anchor="middle" font-size="11">${i+1}</text><text x="${x}" y="${y+38}" text-anchor="middle" font-size="11">${esc(short(p.id))}</text><text x="${x}" y="${y+53}" text-anchor="middle" font-size="10">${isRecent(p)?'fresh heartbeat':'aging / stale'}</text>`;
  });
  if(local)svg+=`<circle cx="${cx}" cy="${cy}" r="36" fill="#204f42"/><text x="${cx}" y="${cy+4}" text-anchor="middle" fill="white" font-size="11">OBSERVER</text><text x="${cx}" y="${cy+56}" text-anchor="middle" font-size="11">${esc(short(local.id))}</text>`;
  $('connection-map').innerHTML=svg;
  $('connection-note').textContent=live?`${remote.filter(isRecent).length} fresh direct links / ${remote.length} known. ${snapshot.networking?'UDP active.':'Networking is OFF; start LAN in the game to connect peers.'} No links between remote peers are inferred.`:'Last known topology; observer telemetry is stale. Coverage below is unverified until fresh reports arrive.';
  const all=snapshot.regions||[];
  const counts=all.map(r=>(r.holders||[]).filter(id=>fresh.has(id)).length);
  $('backup-coverage').innerHTML=`<dl><dt>Regions with 2+ fresh reported copies</dt><dd class="coverage-value">${live?counts.filter(n=>n>=2).length:'—'} / 42</dd><dt>Only one fresh reported copy</dt><dd>${live?counts.filter(n=>n===1).length:'—'}</dd><dt>No fresh holder evidence</dt><dd>${live?counts.filter(n=>!n).length:'—'}</dd><dt>Game reports unavailable</dt><dd>${all.filter(r=>!r.available).length} / 42 ${live?'':'(last known)'}</dd></dl><p class="muted">Seed coverage does not prove that all world changes or remote saves match.</p>`;
  if(!peers.some(p=>p.id===selectedPeer))selectedPeer=local?.id||peers[0]?.id||'';
  $('peer-storage').innerHTML=table(['PEER / LINK','PRIMARY REGIONS','REPORTED PRIMARY COPIES','BACKUP COPIES','MEMORY / SAVE','LOSS EXPOSURE'],peers.map(p=>{
    const s=peerBackupStatus(snapshot,p.id,isRecent),matching=p.stateDigest&&p.stateDigest===snapshot.stateDigest,saved=p.savedDigest&&p.savedDigest===snapshot.stateDigest;
    return `<tr><td><button class="text-button mono" data-storage-peer="${esc(p.id)}" aria-pressed="${p.id===selectedPeer}">${esc(short(p.id))}${p.local?' · YOU':''}</button><div class="muted">${esc(p.endpoint)} · ${isRecent(p)?'fresh':'stale'}</div></td><td>${s.primary.length}</td><td>${s.held.length-s.backups.length}</td><td>${s.backups.length} / ${s.held.length} total</td><td>${!isRecent(p)?'Stale report':`${matching?'Matches':'Different'} / ${snapshot.backupsEnabled?(saved?'reports match':'not matching'):'backups OFF'}`}</td><td>${isRecent(p)?s.atRisk.length+' regions lack another fresh holder':'Unknown'}</td></tr>`;
  }));
  const p=peers.find(p=>p.id===selectedPeer);
  if(p){const s=peerBackupStatus(snapshot,p.id,isRecent);$('peer-storage-detail').innerHTML=`<h2>${esc(short(p.id))} · reported seed inventory</h2><p class="inventory-line"><strong>Primary:</strong> ${esc(regions(s.primary))}</p><p class="inventory-line"><strong>Retained backups:</strong> ${esc(regions(s.backups))}</p><p class="inventory-line"><strong>Loss exposure:</strong> ${isRecent(p)?esc(regions(s.atRisk)):'Unknown — stale telemetry'}</p><p class="muted">Exposure means no other fresh holder is reported; it is a conservative warning, not a guaranteed failure prediction.</p>`;}
}
export function initSimulation(){
  const sim=new Simulation();
  $('peer-storage').addEventListener('click',e=>{const b=e.target.closest('[data-storage-peer]');if(b){selectedPeer=b.dataset.storagePeer;renderConnections(lastSnapshot,lastRecent,lastLive);}});
  function render(){
    const s=sim.summary();
    $('sim-clock').textContent='T + '+sim.time+' s';
    $('sim-metrics').innerHTML=[['ONLINE PEERS',s.online+'/4'],['AVAILABLE REGIONS',s.available+'/42'],['PROTECTED BY 2+ COPIES',s.protectedRegions+'/42'],['UNAVAILABLE',s.unavailable]].map(([label,value])=>`<div><span>${label}</span><strong>${value}</strong></div>`).join('');
    $('sim-peers').innerHTML=sim.peers.map(p=>{const stats=sim.peerStats(p.id);return `<article class="sim-peer ${p.online?'':'offline'}"><div><h2>${p.label}</h2><span class="chip ${p.online?'':'gray'}">${p.online?'Online':'Disconnected'}</span></div><p>${stats.primary} primary · ${stats.backups} backup copies</p><small>${p.online?'Available online':'Retained locally, unavailable to others'}: ${stats.held} copies</small><label><input type="checkbox" data-sim-backup="${p.id}" ${p.backups?'checked':''}> Retain backups</label><button class="secondary" data-sim-peer="${p.id}">${p.online?'Disconnect (expire 8 s)':'Reconnect'}</button></article>`;}).join('');
    $('sim-regions').innerHTML=sim.regions.map(r=>{const copies=sim.activeHolders(r).length;return `<div class="sim-region ${copies>=2?'protected':copies?'single':'lost'}" title="Region ${r.x},${r.z} · seed ${r.seed}"><strong>${r.x},${r.z}</strong><small>${r.seed}</small><small>${copies} ${copies===1?'copy':'copies'}</small></div>`;}).join('');
    $('sim-matrix').innerHTML=table(['REGION','REGIONAL SEED','PRIMARY',...sim.peers.map(p=>p.label),'STATUS'],sim.regions.map(r=>{
      const n=sim.activeHolders(r).length;return `<tr><td class="mono">${r.x}, ${r.z}</td><td class="mono">${r.seed}</td><td>${sim.peers.find(p=>p.id===r.owner)?.label||'None'}</td>${sim.peers.map(p=>`<td>${r.holders.has(p.id)?!p.online?'Offline copy':r.owner===p.id?'Primary':'Backup':'—'}</td>`).join('')}<td><span class="chip ${n>=2?'':n?'warn':'danger'}">${n>=2?'Protected':n?'Single copy':'Unavailable'}</span></td></tr>`;
    }));
    $('sim-events').innerHTML=sim.events.map(e=>`<li>T + ${e.time} s · ${esc(e.message)}</li>`).join('');
  }
  $('sim-reset').addEventListener('click',()=>{sim.reset();$('sim-comparison').hidden=true;render();});
  $('sim-all-on').addEventListener('click',()=>{sim.setAll(true);render();});
  $('sim-all-off').addEventListener('click',()=>{sim.setAll(false);render();});
  $('sim-peers').addEventListener('change',e=>{const input=e.target.closest('[data-sim-backup]');if(input){sim.setBackup(input.dataset.simBackup,input.checked);render();}});
  $('sim-peers').addEventListener('click',e=>{const b=e.target.closest('[data-sim-peer]');if(!b)return;const p=sim.peers.find(p=>p.id===b.dataset.simPeer);if(p.online)sim.disconnect(p.id);else sim.reconnect(p.id);render();});
  $('sim-compare').addEventListener('click',()=>{
    $('sim-comparison').hidden=false;
    $('sim-comparison-table').innerHTML=table(['INITIAL POLICY','PEER A OWNED','AFTER A EXPIRES','UNAVAILABLE','THEN TURN ON'],compareFailures().map(r=>`<tr><td>${r.enabled?'ON':'OFF'}</td><td>${r.owned} regions</td><td>${r.available} / 42 available</td><td>${r.unavailable}</td><td>${r.unavailableAfterEnabling} still unavailable</td></tr>`));
    $('sim-comparison').scrollIntoView({behavior:'smooth',block:'nearest'});
  });render();
}
