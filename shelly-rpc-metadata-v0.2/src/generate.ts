import { discoverShellyTypes } from './discover.js';
import { generateAliases } from './generate-aliases.js';
import { generateManifest } from './generate-manifest.js';
import { generateSchema } from './generate-schema.js';

const discovery = discoverShellyTypes();

generateAliases(discovery);
generateSchema();
generateManifest(discovery);

console.log('Shelly RPC metadata generation completed.');
