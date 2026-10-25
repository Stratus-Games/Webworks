const test = require('node:test');
const assert = require('node:assert');
const readline = require('node:readline');
const { PassThrough } = require('node:stream');
const os = require('node:os');
const path = require('node:path');

const { ask, normalize, resolveSync } = require('../src/config');

// A prompt must end even when nobody answers: a piped start, a closed terminal or a
// CI runner would otherwise sit at the question and never reach listen().
test('a prompt resolves with an empty answer when stdin closes', async () => {
  const input = new PassThrough();
  const rl = readline.createInterface({ input, terminal: false });

  const answered = ask(rl, 'pick something: ');
  input.end();

  const answer = await Promise.race([
    answered,
    new Promise((resolve) => setTimeout(() => resolve('TIMEOUT'), 2000)),
  ]);
  rl.close();
  assert.strictEqual(answer, '');
});

test('an unusable port falls back to 3000 instead of binding a random one', () => {
  assert.strictEqual(normalize({ port: 0 }).port, 3000);
  assert.strictEqual(normalize({ port: -1 }).port, 3000);
  assert.strictEqual(normalize({ port: 70000 }).port, 3000);
  assert.strictEqual(normalize({ port: 'nope' }).port, 3000);
  assert.strictEqual(normalize({ port: '8080' }).port, 8080);
});

// Some launchers and hosting environments export PORT=0, meaning "any free port". Binding
// that gives an address nobody can guess, which reads as "localhost:3000 does not work".
test('PORT=0 in the environment is ignored', () => {
  const previous = { port: process.env.PORT, cfg: process.env.WEBWORKS_CONFIG };
  process.env.PORT = '0';
  process.env.WEBWORKS_CONFIG = path.join(os.tmpdir(), 'webworks-missing-config.json');
  try {
    assert.strictEqual(resolveSync().port, 3000);
    process.env.PORT = '4100';
    assert.strictEqual(resolveSync().port, 4100);
  } finally {
    if (previous.port === undefined) delete process.env.PORT; else process.env.PORT = previous.port;
    if (previous.cfg === undefined) delete process.env.WEBWORKS_CONFIG; else process.env.WEBWORKS_CONFIG = previous.cfg;
  }
});

test('a prompt still returns what was typed', async () => {
  const input = new PassThrough();
  const rl = readline.createInterface({ input, terminal: false });

  const answerPromise = ask(rl, 'pick something: ');
  input.write('2\n');

  const answer = await Promise.race([
    answerPromise,
    new Promise((resolve) => setTimeout(() => resolve('TIMEOUT'), 2000)),
  ]);
  input.end();
  rl.close();
  assert.strictEqual(answer, '2');
});
