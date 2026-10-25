const LEVELS = { quiet: 0, normal: 1, verbose: 2 };

let _level = LEVELS.normal;

function setLevel(level) {
  if (typeof level === 'number') { _level = level; return; }
  const key = String(level || '').toLowerCase();
  _level = key === 'verbose' || key === 'debug' || key === '2' ? LEVELS.verbose
    : key === 'quiet' || key === 'silent' || key === '0' ? LEVELS.quiet
    : LEVELS.normal;
}

function getLevel() { return _level; }

function error(...args) { console.error('[webworks:error]', ...args); }
function warn(...args) { console.warn('[webworks:warn]', ...args); }

function notice(...args) { if (_level >= LEVELS.normal) console.log('[webworks]', ...args); }

function info(...args) { if (_level >= LEVELS.verbose) console.log('[webworks]', ...args); }
function event(...args) { if (_level >= LEVELS.verbose) console.log('[webworks:event]', ...args); }

function banner(text) { if (_level >= LEVELS.normal) console.log(text); }

module.exports = { setLevel, getLevel, error, warn, notice, info, event, banner, LEVELS };
