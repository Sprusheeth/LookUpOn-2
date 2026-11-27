const $ = id => document.getElementById(id);
const escape = value => String(value ?? '').replace(/[&<>"']/g, char => ({
  '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
}[char]));
const short = id => id ? id.slice(0, 8) : 'Unassigned';
const coord = value => Number(value).toFixed(1);
const key = region => `${region.x},${region.z}`;
const copyLabel = count => `${count} ${count === 1 ? 'copy' : 'copies'}`;
const rows = (headers, body) => `<table><thead><tr>${headers.map(h => `<th>${h}</th>`).join('')}</tr></thead><tbody>${body.join('')}</tbody></table>`;
const empty = message => `<p class="empty">${escape(message)}</p>`;
let selection = '', observer = '', targets = new Map(), request = '', awaiting = null, renderAgain = () => {};

export function initLiveWorld() {
  const map = $('live-world-map');
  function select(event) {
    const point = event.target.closest('[data-live-region]');
    if (!point) return;
    selection = point.dataset.liveRegion;
    renderAgain();
    if (event.type === 'keydown') map.querySelector(`[data-live-region="${selection}"]`)?.focus();
  }
  map.addEventListener('click', select);
  map.addEventListener('keydown', event => {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    select(event);
  });
  $('live-player-table').addEventListener('click', async event => {
    const button = event.target.closest('[data-peer-action]');
    if (!button || request) return;
    const {peerAction: action, peerId, enabled} = button.dataset;
    const target = targets.get(peerId);
    if (!target) return;
    request = peerId;
    $('live-control-result').textContent = '';
    renderAgain();
    try {
      const response = await fetch('/api/peer-control', {
        method: 'POST',
        headers: {'Content-Type': 'application/json', 'X-Lookup-Control': '1'},
        body: JSON.stringify({peerId, runId: target.runId, seed: target.seed, action, enabled: enabled === 'true'})
      });
      if (!response.ok) throw new Error(await response.text());
      awaiting = await response.json();
      $('live-control-result').textContent = `Waiting for ${short(peerId)} to acknowledge the command.`;
    } catch (error) {
      $('live-control-result').textContent = error.message;
    } finally {
      request = '';
      renderAgain();
    }
  });
}

export function renderLiveWorld(snapshot, sessions, recent, live, controls) {
  renderAgain = () => renderLiveWorld(snapshot, sessions, recent, live, controls);
  const map = $('live-world-map');
  if (!snapshot) {
    map.innerHTML = '<text x="385" y="260" text-anchor="middle">Waiting for a game in Unity</text>';
    $('live-world-count').textContent = 'Waiting for game';
    $('live-region-detail').textContent = 'Start Play mode to see the region seeds and players.';
    $('live-player-table').innerHTML = empty('No game sessions.');
    $('live-region-table').innerHTML = empty('No region telemetry.');
    targets.clear();
    return;
  }
  const regions = snapshot.regions;
  const peers = snapshot.peers;
  const locals = sessions.filter(s => s.seed === snapshot.seed && s.worldVersion === snapshot.worldVersion && s.protocol === snapshot.protocol &&
    s.snapshotAgeSeconds <= 5 && s.peerControlSupported);
  targets = new Map(locals.map(s => [s.peerId, s]));
  const visible = peers.filter(recent).filter(p => !p.local || !snapshot.networkPaused);
  const fresh = new Set(visible.map(p => p.id));
  const copies = region => [...new Set(region.holders ?? [])].filter(id => fresh.has(id));
  const label = id => short(id) + (id === snapshot.peerId ? ' (observer)' : '');
  const state = (region, count) => !live || snapshot.networkPaused ? 'unknown' :
    !region.available ? 'lost' : count >= 2 ? 'protected' : count ? 'single' : 'unknown';
  const description = (region, count) => ({
    unknown: live && !snapshot.networkPaused ? 'No fresh copy report' : 'Last reported',
    lost: 'Unavailable', protected: copyLabel(count), single: '1 copy'
  }[state(region, count)]);

  if (observer !== snapshot.peerId || !regions.some(region => key(region) === selection)) {
    observer = snapshot.peerId;
    const local = peers.find(p => p.local);
    selection = local ? `${local.chunkX},${local.chunkZ}` : key(regions[0] ?? {});
  }
  const selected = regions.find(region => key(region) === selection);
  const focusedRegion = map.contains(document.activeElement) ? document.activeElement.dataset.liveRegion : '';
  const mapWidth = 742, mapHeight = 512, left = 14, top = 14, cellWidth = mapWidth / 7, cellHeight = mapHeight / 6;
  map.innerHTML = regions.map(region => {
    const x = left + cellWidth * region.x, y = top + cellHeight * (5 - region.z);
    const count = copies(region).length, status = description(region, count), chosen = region === selected;
    return `<g class="live-region ${state(region, count)}" role="button" tabindex="0" data-live-region="${key(region)}"
      aria-label="Region ${key(region)}, seed ${escape(region.seed)}, ${status}" aria-pressed="${chosen}">
      <rect x="${x + 2}" y="${y + 2}" width="${cellWidth - 4}" height="${cellHeight - 4}" rx="4"/>
      <text x="${x + 10}" y="${y + 21}" class="live-cell-coords">${region.x}, ${region.z}</text>
      <text x="${x + 10}" y="${y + 37}" class="live-cell-seed">${escape(region.seed)}</text>
      <text x="${x + 10}" y="${y + cellHeight - 10}" class="live-cell-status">${status}</text>
    </g>`;
  }).join('') + visible.map((peer, index) => {
    if (![peer.x, peer.z].every(value => Number.isFinite(value) && Math.abs(value) <= 5000)) return '';
    const x = left + (peer.x + 5000) * mapWidth / 10000;
    const y = top + (5000 - peer.z) * mapHeight / 10000;
    return `<g class="live-player-marker${peer.local ? ' local-marker' : ''}" transform="translate(${x} ${y})">
      <title>${escape(label(peer.id))} · X ${coord(peer.x)} · Y ${coord(peer.y)} · Z ${coord(peer.z)}</title>
      <path d="M0 -6L6 0 0 6 -6 0Z"/><text x="9" y="${4 + (index % 4) * 12}">${index + 1}</text>
    </g>`;
  }).join('');
  if (focusedRegion) map.querySelector(`[data-live-region="${focusedRegion}"]`)?.focus({preventScroll: true});
  $('live-world-count').textContent = live && !snapshot.networkPaused ?
    `${regions.filter(r => r.available).length}/42 available · ${visible.length} live` : 'Last reported';
  $('live-region-detail').innerHTML = selected ? `<p class="region-title">${selected.x}, ${selected.z}</p>
    <p>${description(selected, copies(selected).length)}</p><dl>
    <dt>Regional seed</dt><dd class="mono">${escape(selected.seed)}</dd>
    <dt>Primary owner</dt><dd class="mono">${escape(label(selected.owners?.[0]))}</dd>
    <dt>Fresh holders</dt><dd>${copies(selected).map(id => `${escape(label(id))} · ${id === selected.owners?.[0] ? 'Primary' : 'Backup'}`).join('<br>') || 'None'}</dd>
    <dt>Players in region</dt><dd>${visible.filter(p => p.chunkX === selected.x && p.chunkZ === selected.z).map(p => escape(label(p.id))).join('<br>') || 'None'}</dd>
    <dt>Rendering tiles on observer</dt><dd>${escape(selected.loadedTiles)} / 64</dd>
    <dt>World bounds</dt><dd>X ${coord(selected.minX)} to ${coord(selected.maxX)} m<br>Z ${coord(selected.minZ)} to ${coord(selected.maxZ)} m</dd>
    </dl>` : empty('No regions reported.');

  const directory = new Map(peers.map(p => [p.id, p]));
  for (const session of locals) {
    if (!directory.has(session.peerId)) {
      const player = session.peers.find(p => p.local);
      if (player) directory.set(player.id, player);
    }
  }
  if (awaiting) {
    const session = targets.get(awaiting.peerId);
    if (session?.runId !== awaiting.runId || session.peerControlRevision >= awaiting.revision) {
      const result = session?.runId === awaiting.runId ? session.peerControlStatus : 'Session ended before acknowledgement';
      $('live-control-result').textContent = `${short(awaiting.peerId)} · ${result}`;
      awaiting = null;
    }
  }
  const pending = id => awaiting?.peerId === id || controls.some(p => p.peerId === id &&
    p.runId === targets.get(id)?.runId && p.revision > targets.get(id)?.peerControlRevision);
  const focusedAction = $('live-player-table').contains(document.activeElement) ? document.activeElement.dataset : {};
  $('live-player-table').innerHTML = rows(['PLAYER', 'WORLD X / Y / Z', 'REGION', 'PRIMARY / BACKUP', 'RETENTION', 'NETWORKING'], [...directory.values()].map(peer => {
    const target = targets.get(peer.id);
    const localPeer = target?.peers.find(p => p.local);
    const index = visible.findIndex(p => p.id === peer.id);
    const main = regions.filter(r => copies(r).includes(peer.id) && r.owners?.[0] === peer.id).length;
    const backup = regions.filter(r => copies(r).includes(peer.id) && r.owners?.[0] !== peer.id).length;
    const retention = !(localPeer ?? peer).retentionDisabled;
    const disabled = request || pending(peer.id) ? ' disabled' : '';
    const action = (id, verb, text, enabled) => `<button class="secondary" data-peer-id="${escape(id)}" data-peer-action="${verb}" data-enabled="${enabled}"${disabled}>${text}</button>`;
    const network = target?.networkPaused ? 'Disconnected' : index >= 0 ? peer.local && !snapshot.networking ? 'Solo' : 'Connected' :
      target?.networking ? 'Outside observer mesh' : target ? 'Solo' : 'No fresh heartbeat';
    return `<tr><td>${index >= 0 ? `<span class="live-player-number">${index + 1}</span>` : ''}<strong class="mono" title="${escape(peer.id)}">${escape(label(peer.id))}</strong>
      <div class="muted">${escape(target?.networking ? 'UDP ' + target.udpPort : peer.endpoint)}</div></td>
      <td class="mono">${coord(peer.x)} / ${coord(peer.y)} / ${coord(peer.z)}</td><td>${escape(peer.chunkX)}, ${escape(peer.chunkZ)}</td>
      <td>${fresh.has(peer.id) ? `${main} / ${backup}` : '—'}</td>
      <td>${target ? action(peer.id, 'retention', retention ? 'Turn retention OFF' : 'Turn retention ON', !retention) : retention ? 'Enabled' : 'Disabled'}
      <div class="muted">${(target?.backupsEnabled ?? peer.backupsEnabled) && retention ? 'Retaining backups' : 'Primary copies only'}</div></td>
      <td>${network}${target?.networkPaused ? action(peer.id, 'reconnect', 'Reconnect', '') : target?.networking ? action(peer.id, 'disconnect', 'Disconnect', '') : ''}
      <div class="muted">${pending(peer.id) ? 'Waiting for game…' : escape(target?.peerControlStatus)}</div></td></tr>`;
  }));
  if (focusedAction.peerId) {
    const button = [...$('live-player-table').querySelectorAll('[data-peer-action]')]
      .find(element => element.dataset.peerId === focusedAction.peerId && element.dataset.peerAction === focusedAction.peerAction);
    button?.focus({preventScroll: true});
  }
  $('live-region-table').innerHTML = rows(['REGION', 'REGIONAL SEED', 'PRIMARY OWNER', 'FRESH HOLDERS', 'AVAILABILITY'], regions.map(region => {
    const holders = copies(region);
    return `<tr><td>${escape(key(region))}</td><td class="mono">${escape(region.seed)}</td>
      <td>${escape(label(region.owners?.[0]))}</td>
      <td>${holders.map(id => `${escape(label(id))} · ${id === region.owners?.[0] ? 'Primary' : 'Backup'}`).join(', ') || 'None'}</td>
      <td>${description(region, holders.length)}</td></tr>`;
  }));
}
