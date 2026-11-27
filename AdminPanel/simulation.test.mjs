import {test} from 'node:test';
import assert from 'node:assert/strict';
import {Simulation,compareFailures,owners,hash} from './dist/simulation.mjs';
import {peerBackupStatus} from './dist/peer-status.mjs';
test('all replicated regions survive an owner timeout and reassignment',()=>{
  const sim=new Simulation();assert.equal(sim.summary().copies,168);
  assert.ok(sim.peers.every(p=>sim.peerStats(p.id).primary>0));
  sim.disconnect(sim.peers[0].id);assert.equal(sim.time,8);
  assert.deepEqual(sim.summary(),{available:42,unavailable:0,protectedRegions:42,copies:126,online:3});
  assert.ok(sim.regions.every(r=>r.owner!==sim.peers[0].id));
});
test('OFF prunes replicas, exposes departed regions, and ON cannot invent lost state',()=>{
  const sim=new Simulation(),a=sim.peers[0],owned=sim.peerStats(a.id).primary;
  sim.setAll(false);assert.equal(sim.summary().copies,42);
  assert.ok(sim.peers.every(p=>sim.peerStats(p.id).backups===0));
  sim.disconnect(a.id);assert.equal(sim.summary().unavailable,owned);
  sim.setAll(true);assert.equal(sim.summary().unavailable,owned);
  sim.reconnect(a.id);assert.equal(sim.summary().unavailable,0);assert.equal(sim.summary().copies,168);
});
test('a single remaining backup holder protects regions until it also fails',()=>{
  const sim=new Simulation(),[a,b,c,d]=sim.peers;
  for(const p of [a,c,d])sim.setBackup(p.id,false);
  assert.equal(sim.peerStats(c.id).backups,0);assert.ok(sim.peerStats(b.id).backups>0);
  sim.disconnect(a.id);assert.equal(sim.summary().unavailable,0);
  const bPrimary=sim.peerStats(b.id).primary;sim.disconnect(b.id);
  assert.equal(sim.summary().unavailable,bPrimary);assert.ok(bPrimary>0);
});
test('all peers offline means no available online copies; reset restores independent state',()=>{
  const sim=new Simulation();for(const p of sim.peers)sim.disconnect(p.id);
  assert.equal(sim.summary().unavailable,42);sim.setAll(true);assert.equal(sim.summary().unavailable,42);
  sim.reset();assert.equal(sim.time,0);assert.equal(sim.summary().available,42);
  const a=new Simulation(),b=new Simulation();a.setAll(false);assert.equal(b.summary().copies,168);
});
test('comparison is reproducible and hash-ring ordering is independent of input order',()=>{
  assert.equal(hash('hello'),1335831723);
  const ids=new Simulation().peers.map(p=>p.id);assert.deepEqual(owners(ids),owners([...ids].reverse()));
  const [on,off]=compareFailures();assert.equal(on.unavailable,0);assert.equal(off.unavailable,off.owned);assert.ok(off.owned>0);assert.equal(off.unavailableAfterEnabling,off.owned);
});
test('live inventories use holder evidence; stale peers cannot establish redundant coverage',()=>{
  const snapshot={peers:[{id:'a',fresh:true},{id:'b',fresh:true},{id:'c',fresh:false}],regions:[
    {x:0,z:0,available:true,owners:['a','b'],holders:['a']},
    {x:1,z:0,available:true,owners:['b'],holders:['a','b']},
    {x:2,z:0,available:true,owners:['a'],holders:['a','c']}
  ]};
  const a=peerBackupStatus(snapshot,'a',p=>p.fresh);assert.equal(a.primary.length,2);assert.equal(a.held.length,3);assert.equal(a.backups.length,1);assert.equal(a.atRisk.length,2);
  const b=peerBackupStatus(snapshot,'b',p=>p.fresh);assert.equal(b.backups.length,0);assert.equal(b.atRisk.length,0);
});
