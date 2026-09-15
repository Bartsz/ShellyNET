import path from 'node:path';
import { fileURLToPath } from 'node:url';

const srcDir = path.dirname(fileURLToPath(import.meta.url));

export const rootDir = path.resolve(srcDir, '..');
export const generatedDir = path.join(rootDir, 'generated');
export const probeFile = path.join(rootDir, 'src', 'probe.ts');
export const generatedTypesFile = path.join(
  generatedDir,
  'shelly-types.generated.ts'
);
export const schemaFile = path.join(generatedDir, 'shelly-rpc.schema.json');
export const manifestFile = path.join(generatedDir, 'shelly-rpc.metadata.json');
export const tsconfigFile = path.join(rootDir, 'tsconfig.json');
export const schemaTsconfigFile = path.join(rootDir, 'tsconfig.schema.json');
