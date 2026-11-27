// Deterministic, browser-only failure laboratory. Never calls the game/control API.
export function hash(value){let h=2166136261;for(let i=0;i<value.length;i++)h=Math.imul(h^value.charCodeAt(i),16777619)>>>0;return h;}
export const regionSeed = (seed, x, z) => hash(`battlefield-v2:${seed}:${x}:${z}`) | 0;
function nodeKey(id,index){
  let key=hash(id+':'+index);
  key=Math.imul(key^(key>>>16),0x7feb352d);
  key=Math.imul(key^(key>>>15),0x846ca68b);
  return (key^(key>>>16))>>>0;
}
export function owners(ids){
  const nodes=ids.flatMap(id=>Array.from({length:64},(_,i)=>({key:nodeKey(id,i),id}))).sort((a,b)=>a.key-b.key||(a.id<b.id?-1:a.id>b.id?1:0));
  return Array.from({length:42},(_,r)=>{const key=hash(r%7+':'+Math.floor(r/7));return (nodes.find(n=>n.key>=key)||nodes[0])?.id||null;});
}
export class Simulation {
  constructor(){this.reset();}
  reset(enabled=true){
    this.time=0;this.events=[];
    this.peers=['A','B','C','D'].map((label,i)=>({id:String(i+1).repeat(32),label:'Peer '+label,online:true,backups:enabled}));
    const primary=owners(this.peers.map(p=>p.id));
    this.regions=primary.map((owner,id)=>({id,x:id%7,z:Math.floor(id/7),seed:regionSeed(2026,id%7,Math.floor(id/7)),owner,holders:new Set([owner])}));
    this.sync();this.log('Reset: four simulated peers, 42 regions, one example world-change record per region.');return this;
  }
  log(message){this.events.unshift({time:this.time,message});this.events=this.events.slice(0,40);}
  onlineIds(){return new Set(this.peers.filter(p=>p.online).map(p=>p.id));}
  activeHolders(r){const active=this.onlineIds();return [...r.holders].filter(id=>active.has(id));}
  sync(){
    for(const r of this.regions){
      // OFF peers retain their primary only. Offline storage is never counted as available.
      for(const p of this.peers)if(!p.backups&&p.id!==r.owner)r.holders.delete(p.id);
      if(this.activeHolders(r).length)for(const p of this.peers)if(p.online&&p.backups)r.holders.add(p.id);
    }
  }
  setBackup(id,enabled){const p=this.peers.find(p=>p.id===id);if(!p)return; p.backups=enabled;this.sync();this.log(p.label+' backup retention '+(enabled?'ON':'OFF')+'.');}
  setAll(enabled){for(const p of this.peers)p.backups=enabled;this.sync();this.log('All simulated backups '+(enabled?'ON':'OFF')+'.');}
  disconnect(id){
    const p=this.peers.find(p=>p.id===id);if(!p?.online)return;
    p.online=false;this.time+=8;
    const next=owners([...this.onlineIds()]);
    for(const r of this.regions)if(this.activeHolders(r).length&&next[r.id]){r.owner=next[r.id];r.holders.add(r.owner);}
    this.sync();this.log(p.label+' disconnected; advanced through the 8 s timeout. '+this.summary().unavailable+' regions unavailable.');
  }
  reconnect(id){
    const p=this.peers.find(p=>p.id===id);if(!p||p.online)return;p.online=true;
    const next=owners([...this.onlineIds()]);
    for(const r of this.regions)if(this.activeHolders(r).length){r.owner=next[r.id];r.holders.add(r.owner);}
    this.sync();this.log(p.label+' returned with its retained local copies; available copies synchronized.');
  }
  summary(){
    let unavailable=0,protectedRegions=0,copies=0;
    for(const r of this.regions){const n=this.activeHolders(r).length;if(!n)unavailable++;if(n>=2)protectedRegions++;copies+=n;}
    return {unavailable,available:42-unavailable,protectedRegions,copies,online:this.peers.filter(p=>p.online).length};
  }
  peerStats(id){
    const own=this.regions.filter(r=>r.owner===id),held=this.regions.filter(r=>r.holders.has(id));
    return {primary:own.length,backups:held.filter(r=>r.owner!==id).length,held:held.length};
  }
}
export function compareFailures(){
  return [true,false].map(enabled=>{
    const sim=new Simulation().reset(enabled),target=sim.peers[0],owned=sim.peerStats(target.id).primary;
    sim.disconnect(target.id);const after=sim.summary();sim.setAll(true);
    return {enabled,owned,...after,unavailableAfterEnabling:sim.summary().unavailable};
  });
}
