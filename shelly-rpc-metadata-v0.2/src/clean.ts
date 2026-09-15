import fs from 'node:fs';
import {
  generatedTypesFile,
  manifestFile,
  schemaFile,
} from './paths.js';

for (const file of [generatedTypesFile, schemaFile, manifestFile]) {
  if (fs.existsSync(file)) {
    fs.rmSync(file);
    console.log(`Removed ${file}.`);
  }
}
