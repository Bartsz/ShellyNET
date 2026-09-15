import { discoverShellyTypes } from './discover.js';
import { generateAliases } from './generate-aliases.js';
import { generateMetadata } from './generate-metadata.js';

const discovery = discoverShellyTypes();

generateAliases(discovery);
generateMetadata(discovery);

console.log('Shelly RPC compact metadata generation completed.');
