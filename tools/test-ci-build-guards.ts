// Run with tsx. No npm dependencies; verifies the actual Makefile recipes.
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, writeFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';

const makefile = readFileSync(resolve(process.argv[2] ?? 'Makefile'), 'utf8');
const recipes = makefile.match(/^\t@set -e; for solution in \.\/\*\.sln;.*$/gm) ?? [];
assert.equal(recipes.length, 2, 'Both .NET and Mono must propagate solution build failures.');
const directory = mkdtempSync(join(tmpdir(), 'ymca-build-guards-'));
try {
  writeFileSync(join(directory, 'First.sln'), '');
  writeFileSync(join(directory, 'Second with spaces.sln'), '');
  for (const recipe of recipes) for (const exitCode of [0, 23]) {
    writeFileSync(join(directory, 'calls'), '');
    const shell = recipe.trim().slice(1)
      .replace(/\$\((DOTNET|MSBUILD)\)/g, 'fake_build')
      .replace(/\$\(TARGETPLATFORM\)/g, 'linux-x64')
      .replace(/\$\$/g, '$');
    const result = spawnSync('bash', ['-c',
      `fake_build() { printf '%s\\n' "$*" >> calls; return ${exitCode}; };\n${shell}`],
    { cwd: directory, encoding: 'utf8' });
    assert.ifError(result.error);
    assert.equal(result.status, exitCode, result.stderr);
    const calls = readFileSync(join(directory, 'calls'), 'utf8').trim().split('\n');
    assert.equal(calls.length, exitCode === 0 ? 2 : 1, 'Stop immediately on the first failed build.');
    if (exitCode === 0) assert.match(calls[1], /Second with spaces\.sln/);
  }
  console.log('PASS: .NET/Mono failures propagate, stop subsequent builds; successful builds and spaced paths work.');
} finally {
  rmSync(directory, { recursive: true, force: true });
}
