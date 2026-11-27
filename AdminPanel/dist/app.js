'use strict';
import {initSimulation,renderConnections} from './peer-lab.mjs';
import {initLiveWorld,renderLiveWorld} from './live-world.mjs';
initSimulation();
const $ = id => document.getElementById(id);
const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const number = value => Number.isFinite(Number(value)) ? Number(value) : 0;
const short = value => value ? String(value).slice(0,8) : 'Unassigned';
const fmt = value => number(value).toLocaleString(undefined,{maximumFractionDigits:1});
let snapshot = null, knownSessions = [], selectedSession = '', selectedRegion = '', currentTab = 'overview', fetchedAt = 0, apiOk = false;
const headings = {overview:['Network overview','Live peers, region responsibility, and the state they carry.'],peers:['Peer directory','Every visible peer, its coordinates, and its latest heartbeat.'],regions:['World regions','Inspect primary and backup assignments across loaded chunks.'],replication:['Replication & saves','See which peers report matching memory and saved state.'],activity:['Network activity','Membership changes and traffic from the selected observer.']};
headings.connections=['Connections & backups','Inspect direct links, retained copies, and regions at risk when a peer leaves.'];
headings['live-world']=['Live world','Follow players and run backup and disconnect trials on active game sessions.'];
headings.simulation=['Failure simulation','Experiment with peer backups and owner loss without changing the live world.'];
let observerPinned=false;
function setTab(tab) {
  currentTab=tab;
  document.body.classList.toggle('simulating',tab==='simulation');
  document.querySelectorAll('[data-tab]').forEach(b=>b.classList.toggle('active',b.dataset.tab===tab));
  document.querySelectorAll('.tab-content').forEach(s=>s.hidden=s.id!=='tab-'+tab);
  $('page-heading').textContent=headings[tab][0]; $('subtitle').textContent=headings[tab][1];
  $('crumb').textContent=tab==='overview'?'Overview':headings[tab][0]; render();window.scrollTo({top:0,behavior:'auto'});
  if(location.hash.slice(1)!==tab)history.replaceState(null,'','#'+tab);
}
document.querySelectorAll('[data-tab]').forEach(b=>b.addEventListener('click',()=>setTab(b.dataset.tab)));
window.addEventListener('hashchange',()=>{const tab=location.hash.slice(1);if(Object.hasOwn(headings,tab))setTab(tab);});
initLiveWorld();
$('view-peers').addEventListener('click',()=>setTab('peers'));
$('zoom').addEventListener('change',()=>render());
$('peer-search').addEventListener('input',()=>render());
$('region-search').addEventListener('input',()=>render());
$('session').addEventListener('change',()=>{
  observerPinned=true;
  selectedSession=$('session').value;selectedRegion='';
  snapshot=knownSessions.find(s=>s.peerId===selectedSession)||null;
  render();pollSoon=true;
});
let pollSoon=false;
let controls={worlds:[],peers:[]}, controlBusy=false, controlError='';
$('backup-toggle').addEventListener('click',async()=>{
  if(!snapshot||controlBusy||age()>5)return;controlBusy=true;controlError='';render();
  try{
    const response=await fetch('/api/control',{method:'POST',headers:{'Content-Type':'application/json','X-Lookup-Control':'1'},body:JSON.stringify({seed:snapshot.seed,backupsEnabled:!snapshot.backupsEnabled})});
    if(!response.ok)throw new Error(await response.text());
    const control=await response.json();controls.worlds=controls.worlds.filter(w=>w.seed!==control.seed);controls.worlds.push(control);
  }catch(e){controlError=e.message;}finally{controlBusy=false;render();}
});
function age() {return snapshot?number(snapshot.snapshotAgeSeconds)+(performance.now()-fetchedAt)/1000:Infinity;}
function recent(p) {return apiOk&&age()<=5&&(p.local||number(p.ageSeconds)+age()<=2);}
function memoryMatch(p) {return !!snapshot&&!!p.stateDigest&&p.stateDigest===snapshot.stateDigest;}
function diskMatch(p) {return !!snapshot&&!!p.savedDigest&&p.savedDigest===snapshot.stateDigest;}
function chip(text,type=''){return `<span class="chip ${type}">${esc(text)}</span>`;}
function identity(p){return `<span class="mono" title="${esc(p.id)}">${esc(short(p.id))}</span>${p.local?'<span class="local-tag">YOU</span>':''}`;}
function table(headers,rows,empty){return rows.length?`<table><thead><tr>${headers.map(h=>`<th>${esc(h)}</th>`).join('')}</tr></thead><tbody>${rows.join('')}</tbody></table>`:`<div class="empty">${esc(empty)}</div>`;}
function peerTable(peers){return table(['PEER / ENDPOINT','WORLD X / Y / Z','CHUNK X / Z','HEARTBEAT AGE','BEACONS','MEMORY'],peers.map(p=>`<tr><td>${identity(p)}<div class="muted mono">${esc(p.endpoint)}</div></td><td class="mono">${fmt(p.x)} / ${fmt(p.y)} / ${fmt(p.z)}</td><td class="mono">${fmt(p.chunkX)} / ${fmt(p.chunkZ)}</td><td>${p.local?(recent(p)?chip('Local'):chip('Stale','warn')):chip(fmt(number(p.ageSeconds)+age())+' s',recent(p)?'':'warn')}</td><td>${fmt(p.beacons)}</td><td>${!recent(p)?chip('Stale report','gray'):memoryMatch(p)?chip('Matches'):chip('Different','warn')}</td></tr>`),'No connected peers match this filter.');}
function render(){
  const live=apiOk&&snapshot&&age()<=5;
  document.body.classList.toggle('disconnected',!live);
  $('connection').textContent=live?'● Receiving telemetry':snapshot?'○ Last known state':'○ Waiting for game';
  if(currentTab==='simulation')$('connection').textContent='● Local simulation · isolated';
  $('connection').classList.toggle('stale',!live);
  if(currentTab==='simulation')$('connection').classList.remove('stale');
  $('export').disabled=!snapshot;
  const alert=$('alert'); alert.hidden=!!live;
  alert.textContent=!apiOk?'Dashboard connection unavailable. Values below are last known, not live.':snapshot?`Observer telemetry is ${fmt(age())} seconds old. The game may be closed, paused, or stalled.`:'Start the updated LookUpOn Windows build or enter Play mode in Unity. This dashboard will connect automatically.';
  renderConnections(snapshot,recent,!!live);
  if(currentTab==='live-world')renderLiveWorld(snapshot,knownSessions,recent,!!live,controls.peers??[]);
  $('recipe-tag').textContent=snapshot?.worldVersion? `RECIPE ${String(snapshot.worldVersion).padStart(2,'0')}` : 'RECIPE';
  $('protocol-label').textContent=snapshot?.protocol? `Backup control · Protocol ${String(snapshot.protocol).padStart(2,'0')}` : 'Backup control';
  if(!snapshot){$('backup-toggle').disabled=true;return;}
  const peers=snapshot.peers||[], remote=peers.filter(p=>!p.local), local=peers.find(p=>p.local);
  const desired=controls.worlds.find(w=>w.seed===snapshot.seed),pending=desired&&number(desired.revision)>number(snapshot.backupRevision);
  $('backup-toggle').textContent=controlBusy?'Applying…':pending?'Waiting for game…':snapshot.backupsEnabled?'Turn backups OFF':'Turn backups ON';
  $('backup-toggle').disabled=!live||controlBusy||pending;$('backup-toggle').classList.toggle('off',!snapshot.backupsEnabled);
  $('backup-toggle').setAttribute('aria-pressed',String(snapshot.backupsEnabled));
  $('policy-status').textContent=controlError||`Game reports ${snapshot.backupsEnabled?'ON':'OFF'} · ${pending?'request awaiting acknowledgement':('revision '+(snapshot.backupRevision||0))} · ${snapshot.adminControlStatus||''}`;
  const visible=remote.filter(recent).length;
  $('metric-peers').textContent=live?visible:'—'; $('nav-peers').textContent=live?visible:0;
  $('peer-note').textContent=live?(snapshot.networking?`${remote.length} known remote · 1 local observer`:'Solo mode · networking is off'):'Observer offline / stale';
  $('metric-chunks').textContent=fmt(snapshot.loadedChunks); $('chunk-note').textContent=`${fmt(snapshot.pendingChunks)} queued · 178.6 × 208.3 m per tile`;
  $('metric-beacons').textContent=fmt(snapshot.beaconCount); $('metric-backups').textContent=!snapshot.backupsEnabled?'OFF':live?remote.filter(p=>recent(p)&&diskMatch(p)).length:'—';
  $('world-tag').textContent=`10 × 10 KM / 42 REGIONS / SEED ${snapshot.seed} / ${snapshot.networking?'UDP '+snapshot.udpPort:'SOLO'}`;
  $('freshness').textContent=`Snapshot ${fmt(age())} s ago · ${snapshot.source}`;
  $('overview-peers').innerHTML=peerTable(peers.slice(0,4));
  const query=$('peer-search').value.trim().toLowerCase();
  $('all-peers').innerHTML=peerTable(peers.filter(p=>(p.id+' '+p.endpoint).toLowerCase().includes(query)));
  const regions=snapshot.regions||[];
  const rq=$('region-search').value.replace(/\s/g,'');
  $('all-regions').innerHTML=table(['REGION X, Z','REGIONAL SEED','PRIMARY','REPORTED SEED HOLDERS','LOADED / 64 TILES','AVAILABILITY'],regions.filter(r=>(r.x+','+r.z).includes(rq)).map(r=>`<tr><td class="mono">${fmt(r.x)}, ${fmt(r.z)}</td><td class="mono">${r.seed}</td><td class="mono">${esc(short(r.owners?.[0]))}</td><td>${(r.holders||[]).map(short).map(esc).join(', ')||'None'}</td><td>${fmt(r.loadedTiles)} / 64</td><td>${chip(r.available?'Available':'Owner missing',r.available?'':'warn')}</td></tr>`),'No regions match these coordinates.');
  if(!selectedRegion||!regions.some(r=>r.x+','+r.z===selectedRegion)){selectedRegion=local?local.chunkX+','+local.chunkZ:'';}
  const region=regions.find(r=>r.x+','+r.z===selectedRegion);
  $('region-detail').innerHTML=region?`<p class="region-title">${fmt(region.x)} <span class="muted">/</span> ${fmt(region.z)}</p><p>${chip(region.available?'Available':'Unavailable · owner missing',region.available?'':'warn')}</p><p class="muted">X ${fmt(region.minX)}–${fmt(region.maxX)} m<br>Z ${fmt(region.minZ)}–${fmt(region.maxZ)} m</p><dl><dt>Independent regional seed</dt><dd class="mono">${region.seed}</dd><dt>Primary owner</dt><dd class="mono">${esc(region.owners?.[0]||'None')}</dd><dt>Reported seed holders</dt><dd class="mono">${(region.holders||[]).map(esc).join('<br>')||'No retained copy'}</dd><dt>Rendering tiles</dt><dd>${fmt(region.loadedTiles)} loaded / 64 total</dd><dt>Local beacons</dt><dd>${fmt(region.localBeacons)}</dd></dl>`:'<p class="muted">No region selected.</p>';
  drawMap(local,regions,peers);
  $('replicas').innerHTML=table(['PEER','BACKUP POLICY','SEED COPIES','IN MEMORY','SAVED REPLICA'],peers.map(p=>`<tr><td>${identity(p)}</td><td>${chip(p.backupsEnabled?'ON':'OFF',p.backupsEnabled?'':'warn')}</td><td>${p.seedCopies} / 42</td><td>${!recent(p)?chip('Stale','gray'):memoryMatch(p)?chip('Matches'):chip('Different','warn')}</td><td>${!snapshot.backupsEnabled?chip('Disabled','gray'):!recent(p)?chip('Stale','gray'):diskMatch(p)?chip(p.local?'Saved locally':'Reports match'):chip('Not matching yet','warn')}</td></tr>`),'Waiting for replica reports.');
  $('persistence').innerHTML=`<dl><dt>Local save status</dt><dd>${esc(snapshot.saveStatus)}</dd><dt>Last completed save</dt><dd>${snapshot.lastSavedUtc?esc(new Date(snapshot.lastSavedUtc).toLocaleString()):'Not reported'}</dd><dt>Previous local save (.bak)</dt><dd>${snapshot.backupFileExists?'Present · previous version':'Not created yet'}</dd><dt>Save location on observer</dt><dd class="mono">${esc(snapshot.savePath)}</dd><dt>Current memory SHA-256</dt><dd class="mono">${esc(snapshot.stateDigest)}</dd><dt>Last saved SHA-256</dt><dd class="mono">${esc(snapshot.savedDigest||'Not reported')}</dd></dl>`;
  $('traffic').textContent=`RX ${fmt(number(snapshot.bytesIn)/1024)} KiB / TX ${fmt(number(snapshot.bytesOut)/1024)} KiB`;
  $('events').innerHTML=snapshot.events?.length?snapshot.events.map(e=>`<li>${esc(e)}</li>`).join(''):'<li>No membership events yet. Start peer networking in the game.</li>';
}
function drawMap(local,regions,peers){
  $('map-empty').hidden=!!local;
  if(!local){$('map').innerHTML='';return;}
  const selected=regions.find(r=>r.x+','+r.z===selectedRegion),detail=$('zoom').value==='region'&&selected;
  const bounds=detail?selected:{minX:-5000,maxX:5000,minZ:-5000,maxZ:5000};
  const scale=Math.min(700/(bounds.maxX-bounds.minX),360/(bounds.maxZ-bounds.minZ));
  const ox=450-(bounds.maxX-bounds.minX)*scale/2,oy=210-(bounds.maxZ-bounds.minZ)*scale/2;
  const sx=x=>ox+(x-bounds.minX)*scale,sy=z=>oy+(bounds.maxZ-z)*scale;
  let svg='<rect width="900" height="420" fill="#f8faf7"/>';
  for(const r of regions){
    if(detail&&r!==selected)continue;
    const x=sx(r.minX),y=sy(r.maxZ),w=(r.maxX-r.minX)*scale,h=(r.maxZ-r.minZ)*scale,chosen=selectedRegion===r.x+','+r.z;
    const fill=!r.available?'#e8b8ab':r.owners?.[0]===snapshot.peerId?'#dcebd9':'#dbe7ed';
    svg+=`<rect class="region" tabindex="0" role="button" aria-label="Region ${r.x}, ${r.z}" data-region="${r.x},${r.z}" x="${x+1}" y="${y+1}" width="${w-2}" height="${h-2}" rx="2" fill="${fill}" stroke="${chosen?'#2c6953':'#a6b9a6'}" stroke-width="${chosen?2:1}"><title>Region ${r.x},${r.z} · seed ${r.seed} · ${r.available?'available':'unavailable'}</title></rect>`;
    if(detail||chosen)for(let i=1;i<8;i++)svg+=`<path d="M${x+w*i/8},${y}v${h} M${x},${y+h*i/8}h${w}" stroke="#9aaa99" stroke-width=".5" opacity=".5" pointer-events="none"/>`;
    svg+=`<text x="${x+w/2}" y="${y+h/2+3}" text-anchor="middle" font-size="${detail?16:10}" fill="#405d47">${r.x},${r.z}${!r.available?' ×':''}</text>`;
    if(r.localBeacons)svg+=`<text x="${x+w-13}" y="${y+15}" fill="#a97928" font-size="14">+</text>`;
  }
  for(const p of peers){
    if(p.x<bounds.minX||p.x>bounds.maxX||p.z<bounds.minZ||p.z>bounds.maxZ)continue;
    const color=!recent(p)?'#9ba49b':p.local?'#245e43':'#4e8099';
    svg+=`<circle cx="${sx(p.x)}" cy="${sy(p.z)}" r="6" fill="${color}" stroke="white" stroke-width="2"><title>${esc(short(p.id))} · X ${fmt(p.x)}, Y ${fmt(p.y)}, Z ${fmt(p.z)}</title></circle>`;
  }
  svg+='<text x="58" y="44" fill="#78927b" font-size="10">+ Z ↑</text><text x="58" y="62" fill="#78927b" font-size="10">+ X →</text>';
  $('map').innerHTML=svg; $('map-center').textContent=detail?'8 × 8 RENDER TILES':'42 REGIONS / 100 KM²';
}
function selectMapRegion(e){const r=e.target.closest('[data-region]');if(r){selectedRegion=r.dataset.region;render();}}
$('map').addEventListener('click',selectMapRegion);
$('map').addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();selectMapRegion(e);}});
$('export').addEventListener('click',()=>{
  if(!snapshot)return;
  const exported={...snapshot,exportedAt:new Date().toISOString(),snapshotAgeSeconds:age(),live:apiOk&&age()<=5};
  const url=URL.createObjectURL(new Blob([JSON.stringify(exported,null,2)],{type:'application/json'}));
  const a=document.createElement('a');a.href=url;a.download=`lookupOn-${short(snapshot.peerId)}-${Date.now()}.json`;a.click();URL.revokeObjectURL(url);
});
async function poll(){
  try{
    const response=await fetch('/api/state',{cache:'no-store',signal:AbortSignal.timeout(3000)});
    if(!response.ok)throw new Error('HTTP '+response.status);
    const data=await response.json();const sessions=data.sessions||[];knownSessions=sessions;controls=data.controls||{worlds:[]};apiOk=true;
    const current=sessions.find(s=>s.peerId===selectedSession);
    if(!selectedSession||!observerPinned&&(!current||current.snapshotAgeSeconds>5||current.networkPaused)){
      const active=sessions.find(s=>s.snapshotAgeSeconds<=5&&s.networking)||sessions.find(s=>s.snapshotAgeSeconds<=5)||sessions[0];
      if(active?.peerId!==selectedSession)selectedRegion='';
      selectedSession=active?.peerId||'';
    }
    const desired=sessions.map(s=>({id:s.peerId,label:`${short(s.peerId)} · ${s.source} · ${s.snapshotAgeSeconds>5?'offline':s.networkPaused?'disconnected':s.networking?'UDP '+s.udpPort:'solo'}`}));
    if(selectedSession&&!desired.some(s=>s.id===selectedSession))desired.push({id:selectedSession,label:`${short(selectedSession)} · telemetry pending`});
    $('session').innerHTML=desired.length?desired.map(s=>`<option value="${esc(s.id)}"${s.id===selectedSession?' selected':''}>${esc(s.label)}</option>`).join(''):'<option>Waiting for a game session</option>';
    const nextSnapshot=sessions.find(s=>s.peerId===selectedSession);
    if(nextSnapshot){snapshot=nextSnapshot;fetchedAt=performance.now();}
    else if(snapshot?.peerId!==selectedSession)snapshot=null;
    render();
    if(data.unreadableSnapshots&&$('alert').hidden){$('alert').hidden=false;$('alert').textContent=`${data.unreadableSnapshots} telemetry file(s) could not be read. Other sessions remain available.`;}
  }catch(error){apiOk=false;render();}
  setTimeout(poll,pollSoon?0:1000);pollSoon=false;
}
if(Object.hasOwn(headings,location.hash.slice(1)))setTab(location.hash.slice(1));
poll();
