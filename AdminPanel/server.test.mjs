import {test} from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {collectState,createServer} from './server.mjs';
test('collects multiple observers, labels stale data and ignores partial writes',async()=>{
  const root=await fs.mkdtemp(path.join(os.tmpdir(),'lookup-monitor-'));
  try{
    const dirs=[path.join(root,'editor'),path.join(root,'player')];
    for(let i=0;i<dirs.length;i++){
      await fs.mkdir(dirs[i]);const file=path.join(dirs[i],'peer.json');
      await fs.writeFile(file,JSON.stringify({schema:2,peerId:String(i),peers:[],regions:[]}));
      if(i)await fs.utimes(file,new Date(Date.now()-20000),new Date(Date.now()-20000));
    }
    await fs.writeFile(path.join(dirs[0],'bad.json'),'{');
    await fs.writeFile(path.join(dirs[0],'pending.json.tmp'),'{');
    const state=await collectState(dirs);
    assert.equal(state.sessions.length,2);assert.equal(state.unreadableSnapshots,1);
    assert.equal(state.sessions[0].source,'Editor');assert.ok(state.sessions[0].snapshotAgeSeconds<5);
    assert.ok(state.sessions[1].snapshotAgeSeconds>15);
  }finally{await fs.rm(root,{recursive:true,force:true});}
});
test('backup controls persist, increase revision and reject foreign commands',async()=>{
  const scratch=fileURLToPath(new URL('../../../work/',import.meta.url));await fs.mkdir(scratch,{recursive:true});
  const root=await fs.mkdtemp(path.join(scratch,'lookup-controls-')),file=path.join(root,'controls.json');
  const server=createServer([],file);await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  const command=enabled=>({method:'POST',headers:{'Content-Type':'application/json','X-Lookup-Control':'1',Origin:base},body:JSON.stringify({seed:2026,backupsEnabled:enabled})});
  try{
    let r=await fetch(base+'/api/control',command(false));assert.equal(r.status,200);const off=await r.json();assert.equal(off.backupsEnabled,false);
    r=await fetch(base+'/api/control',command(true));const on=await r.json();assert.equal(on.backupsEnabled,true);assert.ok(on.revision>off.revision);
    assert.equal((await (await fetch(base+'/api/state')).json()).controls.worlds[0].revision,on.revision);
    const bad=command(false);bad.headers.Origin='https://example.com';assert.equal((await fetch(base+'/api/control',bad)).status,403);
    assert.equal((await fetch(base+'/api/control',{method:'POST',body:'{}'})).status,403);
  }finally{server.closeAllConnections();await new Promise(resolve=>server.close(resolve));if(path.resolve(root).startsWith(path.resolve(scratch)+path.sep))await fs.rm(root,{recursive:true,force:true});}
});
test('serves real dashboard and read-only API with origin checks',async()=>{
  const server=createServer([]);await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  try{
    let r=await fetch(base+'/api/state');assert.equal(r.status,200);assert.equal(r.headers.get('cache-control'),'no-store');assert.deepEqual((await r.json()).sessions,[]);
    r=await fetch(base+'/');assert.match(await r.text(),/Network observatory/);
    for(const route of ['/app.js','/peer-lab.mjs','/peer-status.mjs','/simulation.mjs','/live-world.mjs']){r=await fetch(base+route);assert.equal(r.status,200);assert.match(r.headers.get('content-type'),/javascript/);}
    for(const [route,options,status] of [['/api/state',{method:'POST'},405],['/api/state',{headers:{Origin:'https://example.com'}},403],['/server.mjs',{},404]]){r=await fetch(base+route,options);assert.equal(r.status,status);}
  }finally{server.closeAllConnections();await new Promise(resolve=>server.close(resolve));}
});
test('peer commands require a fresh matching local run and acknowledge one revision at a time',async()=>{
  const root=await fs.mkdtemp(path.join(os.tmpdir(),'lookup-peer-control-'));
  const telemetry=path.join(root,'telemetry'),controls=path.join(root,'controls.json');
  const peerId='a'.repeat(32),runId='b'.repeat(32);
  const snapshot={schema:2,seed:2026,peerId,runId,peerControlSupported:true,peerControlRevision:0,
    networking:true,peers:[{id:peerId,local:true}],regions:[]};
  const server=createServer([telemetry],controls);
  await fs.mkdir(telemetry);
  const file=path.join(telemetry,'peer.json');
  const publish=()=>fs.writeFile(file,JSON.stringify(snapshot));
  await publish();
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const base='http://127.0.0.1:'+server.address().port;
  const send=(patch,origin=base)=>fetch(base+'/api/peer-control',{method:'POST',
    headers:{'Content-Type':'application/json','X-Lookup-Control':'1',Origin:origin},
    body:JSON.stringify({seed:2026,peerId,runId,action:'retention',enabled:false,...patch})});
  try{
    assert.equal((await send({},'https://example.com')).status,403);
    assert.equal((await send({peerId:'c'.repeat(32)})).status,409);
    assert.equal((await send({runId:'c'.repeat(32)})).status,409);
    const off=await send({});
    assert.equal(off.status,200);
    const retention=await off.json();
    assert.equal(retention.retentionDisabled,true);
    assert.equal((await send({action:'disconnect'})).status,409);
    snapshot.peerControlRevision=retention.revision;
    snapshot.peers[0].retentionDisabled=true;
    await publish();
    const pause=await send({action:'disconnect'});
    assert.equal(pause.status,200);
    const command=await pause.json();
    assert.equal(command.connection,'disconnect');
    assert.equal(command.retentionDisabled,true);
    assert.ok(command.revision>retention.revision);
    const stale=new Date(Date.now()-10000);
    await fs.utimes(file,stale,stale);
    assert.equal((await send({action:'reconnect'})).status,409);
  }finally{
    server.closeAllConnections();
    await new Promise(resolve=>server.close(resolve));
    await fs.unlink(file);
    await fs.unlink(controls);
    await fs.rmdir(telemetry);
    await fs.rmdir(root);
  }
});
