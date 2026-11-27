// Holder evidence is read from reported manifests, never inferred from assignments.
export function peerBackupStatus(snapshot,id,isRecent){
  const peers=snapshot.peers||[],regions=snapshot.regions||[];
  const fresh=new Set(peers.filter(isRecent).map(p=>p.id));
  const held=regions.filter(r=>(r.holders||[]).includes(id));
  const primary=regions.filter(r=>r.owners?.[0]===id);
  const backups=held.filter(r=>r.owners?.[0]!==id);
  const atRisk=regions.filter(r=>r.available&&(r.owners?.[0]===id||(r.holders||[]).includes(id))&&!(r.holders||[]).some(other=>other!==id&&fresh.has(other)));
  return {held,primary,backups,atRisk};
}
