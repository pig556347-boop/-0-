mergeInto(LibraryManager.library, {
  $KickBackAudioState: {
    context: null, buffers: {}, source: null, generation: 0,
    init: function() {
      if (this.context) return;
      this.context = new (window.AudioContext || window.webkitAudioContext)();
      this.music = this.context.createGain(); this.effects = this.context.createGain();
      this.music.connect(this.context.destination); this.effects.connect(this.context.destination);
      var self = this;
      this.unlock = function() { if (self.context && self.context.state === 'suspended') self.context.resume().catch(function(){}); };
      document.addEventListener('pointerdown', this.unlock, true);
      document.addEventListener('keydown', this.unlock, true);
    },
    load: function(name) {
      this.init(); var self = this;
      if (!this.buffers[name]) this.buffers[name] = fetch(new URL('Audio/' + name + '.wav', document.baseURI), {cache:'no-cache'})
        .then(function(r) { if (!r.ok) throw new Error(name + ' HTTP ' + r.status); return r.arrayBuffer(); })
        .then(function(data) { return self.context.decodeAudioData(data); });
      return this.buffers[name];
    },
    fail: function(error) { console.error('KICK_BACK_AUDIO_ERROR ' + error); }
  },
  KickBackAudio_Music__deps: ['$KickBackAudioState'],
  KickBackAudio_Music: function(battle) {
    var s = KickBackAudioState; s.init(); var generation = ++s.generation;
    ['Hit','Whoosh','Block','Parry','Down','Menu','Success'].forEach(function(name){s.load(name).catch(s.fail);});
    if (s.source) { s.source.stop(); s.source = null; }
    s.load(battle ? 'BattleMusic' : 'LobbyMusic').then(function(buffer) {
      if (!s.context || generation !== s.generation) return;
      s.source = s.context.createBufferSource(); s.source.buffer = buffer;
      s.source.loop = true; s.source.connect(s.music); s.source.start(); s.unlock();
    }).catch(s.fail);
  },
  KickBackAudio_Play__deps: ['$KickBackAudioState'],
  KickBackAudio_Play: function(id) {
    var names = ['Hit','Whoosh','Block','Parry','Down','Menu','Success'];
    var s = KickBackAudioState; s.init(); var generation = s.generation;
    s.load(names[id]).then(function(buffer) {
      if (!s.context || generation !== s.generation) return;
      var source = s.context.createBufferSource(); source.buffer = buffer;
      source.connect(s.effects); source.onended = function(){ source.disconnect(); };
      source.start(); s.unlock();
    }).catch(s.fail);
  },
  KickBackAudio_Volumes__deps: ['$KickBackAudioState'],
  KickBackAudio_Volumes: function(master, music, effects) {
    var s = KickBackAudioState; s.init();
    s.music.gain.value = master * music; s.effects.gain.value = master * effects;
  },
  KickBackAudio_Close__deps: ['$KickBackAudioState'],
  KickBackAudio_Close: function() {
    var s = KickBackAudioState; ++s.generation;
    if (!s.context) return;
    document.removeEventListener('pointerdown', s.unlock, true);
    document.removeEventListener('keydown', s.unlock, true);
    if (s.source) s.source.stop();
    s.context.close().catch(function(){}); s.context = null; s.source = null; s.buffers = {};
  }
});
