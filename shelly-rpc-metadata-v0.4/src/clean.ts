import fs from 'node:fs';
import { generatedTypesFile, manifestFile } from './paths.js';

for (const file of [generatedTypesFile, manifestFile]) {
  if (fs.existsSync(file)) {
    fs.rmSync(file);
    console.log(`Removed ${file}.`);
  }
}
