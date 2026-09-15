import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);

export type PackageInfo = {
  name: string;
  version: string;
};

export function getShellyPackageInfo(): PackageInfo {
  const entry = require.resolve('@taulfsime/shelly-rpc-ts');
  let current = path.dirname(entry);

  while (true) {
    const candidate = path.join(current, 'package.json');
    if (fs.existsSync(candidate)) {
      const json = JSON.parse(fs.readFileSync(candidate, 'utf8')) as {
        name?: string;
        version?: string;
      };

      if (json.name === '@taulfsime/shelly-rpc-ts' && json.version) {
        return { name: json.name, version: json.version };
      }
    }

    const parent = path.dirname(current);
    if (parent === current) break;
    current = parent;
  }

  throw new Error(
    `Could not locate package.json for @taulfsime/shelly-rpc-ts from ${entry}`
  );
}
