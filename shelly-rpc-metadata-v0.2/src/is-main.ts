import path from 'node:path';
import { fileURLToPath } from 'node:url';

export function isMainModule(importMetaUrl: string): boolean {
  const entry = process.argv[1];
  if (!entry) return false;

  return path.resolve(fileURLToPath(importMetaUrl)) === path.resolve(entry);
}
