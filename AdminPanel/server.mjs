// Local dashboard and world backup control. Uses only Node's built-in libraries.
import http from 'node:http';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {randomUUID} from 'node:crypto';
const root=path.dirname(fileURLToPath(import.meta.url));
const project=path.dirname(root);
export const sources=[path.join(project,'Telemetry'),path.join(project,'Builds','Windows','Telemetry')];
export const controlsPath=path.join(root,'controls.json');
async function readControls(filename){
  try{
    const data=JSON.parse(await fs.readFile(filename,'utf8'));
    if(!Array.isArray(data.worlds)||data.peers!==undefined&&!Array.isArray(data.peers))throw new Error('Invalid controls');
    return {...data,peers:data.peers??[]};
  }catch(e){if(e.code==='ENOENT')return {worlds:[],peers:[]};throw e;}
}
export async function collectState(directories=sources){
  const files=[];let unreadable=0;
  for(let i=0;i<directories.length;i++){
    let names=[];try{names=await fs.readdir(directories[i]);}catch(e){if(e.code!=='ENOENT')unreadable++;}
    for(const name of names.filter(n=>n.endsWith('.json'))){
      const filename=path.join(directories[i],name);
      try{const stat=await fs.stat(filename);if(stat.isFile())files.push({filename,stat,source:i===0?'Editor':'Windows player'});}catch{unreadable++;}
    }
  }
  files.sort((a,b)=>b.stat.mtimeMs-a.stat.mtimeMs);
  const sessions=[];
  for(const file of files.slice(0,40)){
    try{
      if(file.stat.size>2_000_000)throw new Error('Oversized');
      const data=JSON.parse((await fs.readFile(file.filename,'utf8')).replace(/^\uFEFF/,''));
      if(data?.schema===1)continue; // Old world-recipe snapshots are historical, not failed telemetry.
      if(!data||data.schema!==2||!Array.isArray(data.peers)||!Array.isArray(data.regions))throw new Error('Invalid schema');
      sessions.push({...data,snapshotAgeSeconds:Math.max(0,(Date.now()-file.stat.mtimeMs)/1000),source:file.source});
    }catch{unreadable++;}
  }
  return {serverUtc:new Date().toISOString(),sessions,unreadableSnapshots:unreadable};
}
export function createServer(directories=sources,controlFile=controlsPath){
  let writes=Promise.resolve();
  async function saveControls(doc){
    const data=JSON.stringify(doc);
    if(Buffer.byteLength(data)>65536)throw new Error('Control document is full');
    await fs.mkdir(path.dirname(controlFile),{recursive:true});
    await fs.writeFile(controlFile+'.tmp',data);
    await fs.rename(controlFile+'.tmp',controlFile);
  }
  return http.createServer(async(req,res)=>{
    const port=res.socket.localPort,hosts=[`127.0.0.1:${port}`,`localhost:${port}`];
    function send(status,body,type='text/plain; charset=utf-8'){
      res.writeHead(status,{'Content-Type':type,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff','Content-Security-Policy':"default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'"});res.end(body);
    }
    if(!hosts.includes(req.headers.host)||req.headers.origin&&!hosts.some(h=>req.headers.origin==='http://'+h)){send(403,'Local same-origin monitor only');return;}
    try{
      const route=new URL(req.url,'http://'+req.headers.host).pathname;
      if(req.method==='POST'&&(route==='/api/control'||route==='/api/peer-control')){
        if(!req.headers.origin||req.headers['x-lookup-control']!=='1'||req.headers['content-type']!=='application/json'){send(403,'Same-origin control request required');return;}
        let body='';for await(const part of req){body+=part;if(body.length>4096){send(413,'Request too large');return;}}
        let command;try{command=JSON.parse(body);}catch{send(400,'Invalid JSON');return;}
        if(!command||!Number.isInteger(command.seed)||command.seed< -2147483648||command.seed>2147483647){send(400,'Expected an integer world seed');return;}
        if(route==='/api/peer-control'){
          const {peerId,runId,seed,action,enabled}=command;
          const id=/^[a-f0-9]{32}$/;
          if(typeof peerId!=='string'||!id.test(peerId)||typeof runId!=='string'||!id.test(runId)||
            !['retention','disconnect','reconnect'].includes(action)||action==='retention'&&typeof enabled!=='boolean'){
            send(400,'Expected peer ID, run ID and a supported action');return;
          }
          const result=writes.catch(()=>{}).then(async()=>{
            const state=await collectState(directories);
            const target=state.sessions.find(s=>s.peerId===peerId&&s.seed===seed&&s.runId===runId&&s.peerControlSupported&&s.snapshotAgeSeconds<=5);
            if(!target||action==='disconnect'&&!target.networking||action==='reconnect'&&!target.networkPaused)return null;
            const doc=await readControls(controlFile);
            const old=doc.peers.find(p=>p.peerId===peerId&&p.seed===seed);
            if(old?.revision>target.peerControlRevision&&old.runId===runId)return null;
            const control={peerId,runId,seed,retentionDisabled:action==='retention'?!enabled:!!target.peers.find(p=>p.local)?.retentionDisabled,
              connection:action==='retention'?'':action,revision:Math.max(Date.now(),(old?.revision||0)+1,(target.peerControlRevision||0)+1)};
            doc.peers=doc.peers.filter(p=>p.peerId!==peerId||p.seed!==seed);
            // Keep only outstanding connection actions and persisted retention overrides.
            doc.peers=doc.peers.filter(p=>p.retentionDisabled||p.connection&&state.sessions.some(s=>s.runId===p.runId&&s.snapshotAgeSeconds<=5));
            doc.peers.push(control);
            await saveControls(doc);
            return control;
          });writes=result;
          const control=await result;
          if(!control){send(409,'Peer is offline, changed state, or is applying a previous command');return;}
          send(200,JSON.stringify(control),'application/json; charset=utf-8');return;
        }
        if(!Number.isInteger(command.seed)||command.seed< -2147483648||command.seed>2147483647||typeof command.backupsEnabled!=='boolean'){send(400,'Expected seed and backupsEnabled boolean');return;}
        const result=writes.catch(()=>{}).then(async()=>{
          const doc=await readControls(controlFile),state=await collectState(directories),old=doc.worlds.find(w=>w.seed===command.seed);
          const latest=Math.max(0,...state.sessions.filter(s=>s.seed===command.seed).map(s=>Number(s.backupRevision)||0),old?.revision||0);
          const control={seed:command.seed,backupsEnabled:command.backupsEnabled,revision:Math.max(Date.now(),latest+1),authority:old?.authority||randomUUID().replaceAll('-','')};
          doc.worlds=doc.worlds.filter(w=>w.seed!==command.seed);doc.worlds.push(control);
          await saveControls(doc);
          return control;
        });writes=result;
        send(200,JSON.stringify(await result),'application/json; charset=utf-8');return;
      }
      if(req.method!=='GET'){send(405,'Method not allowed');return;}
      if(route==='/api/state'){send(200,JSON.stringify({...await collectState(directories),controls:await readControls(controlFile)}),'application/json; charset=utf-8');return;}
      const assets={'/':['index.html','text/html'],'/index.html':['index.html','text/html'],'/app.js':['app.js','text/javascript'],'/style.css':['style.css','text/css'],'/peer-lab.mjs':['peer-lab.mjs','text/javascript'],'/peer-status.mjs':['peer-status.mjs','text/javascript'],'/simulation.mjs':['simulation.mjs','text/javascript'],'/live-world.mjs':['live-world.mjs','text/javascript']};
      if(!Object.hasOwn(assets,route)){send(404,'Not found');return;}
      const [filename,type]=assets[route];send(200,await fs.readFile(path.join(root,'dist',filename)),type+'; charset=utf-8');
    }catch(error){console.error('Dashboard operation failed:',error.message);send(500,'Could not read local telemetry, controls or dashboard assets');}
  });
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url)){
  const arg=name=>{const index=process.argv.indexOf(name);return index>=0?process.argv[index+1]:undefined;};
  const port=Number(arg('--port')??8765);
  if(!Number.isInteger(port)||port<1024||port>65535)throw new Error('Use a port between 1024 and 65535');
  const telemetry=arg('--telemetry-dir'),control=arg('--control-file');
  const server=createServer(telemetry?[path.resolve(telemetry)]:sources,control?path.resolve(control):controlsPath);
  server.on('error',error=>{console.error(`Cannot start dashboard: ${error.message}`);process.exitCode=1;});
  server.listen(port,'127.0.0.1',()=>console.log(`LookUpOn admin: http://127.0.0.1:${port} — Ctrl+C to stop`));
}
