import fs from 'node:fs';
import { isMainModule } from './is-main.js';
import { discoverShellyTypes, type ShellyDiscovery } from './discover.js';
import {
  componentAliasBase,
  methodAliasBase,
  schemaRef,
} from './naming.js';
import { getShellyPackageInfo } from './package-info.js';
import { manifestFile, schemaFile } from './paths.js';

export function generateManifest(
  discovery: ShellyDiscovery = discoverShellyTypes()
): void {
  if (!fs.existsSync(schemaFile)) {
    throw new Error(
      `Schema does not exist: ${schemaFile}. Run npm run generate:schema first.`
    );
  }

  const sourcePackage = getShellyPackageInfo();

  const methods = discovery.methods.map((method) => {
    const alias = methodAliasBase(method);
    const dot = method.indexOf('.');
    const namespace = dot >= 0 ? method.slice(0, dot) : method;
    const operation = dot >= 0 ? method.slice(dot + 1) : '';
    const paramsType = `${alias}_Params`;
    const resultType = `${alias}_Result`;

    return {
      method,
      namespace,
      operation,
      params: {
        type: paramsType,
        schemaRef: schemaRef(paramsType),
      },
      result: {
        type: resultType,
        schemaRef: schemaRef(resultType),
      },
    };
  });

  const namespaces = Object.fromEntries(
    [...new Set(methods.map((method) => method.namespace))]
      .sort((a, b) => a.localeCompare(b))
      .map((namespace) => [
        namespace,
        methods
          .filter((method) => method.namespace === namespace)
          .map((method) => method.method),
      ])
  );

  const components = discovery.componentTypes.map((component) => {
    const alias = componentAliasBase(component);
    const statusType = `${alias}_Status`;
    const patchType = `${alias}_NotifyStatusPatch`;
    const configType = `${alias}_Config`;

    return {
      component,
      status: {
        type: statusType,
        schemaRef: schemaRef(statusType),
        usedBy: ['NotifyFullStatus'],
      },
      notifyStatusPatch: {
        type: patchType,
        schemaRef: schemaRef(patchType),
        semantics: {
          missing: 'unchanged',
          null: 'property removed / cleared',
          value: 'new value',
        },
        usedBy: ['NotifyStatus'],
      },
      config: {
        type: configType,
        schemaRef: schemaRef(configType),
      },
    };
  });

  const manifest = {
    formatVersion: 1,
    source: {
      package: sourcePackage.name,
      version: sourcePackage.version,
      authority: 'shelly-rpc-ts',
      runtimeCapabilityCheck: 'Shelly.ListMethods',
    },
    schema: './shelly-rpc.schema.json',
    counts: {
      rpcMethods: methods.length,
      namespaces: Object.keys(namespaces).length,
      componentTypes: components.length,
    },
    namespaces,
    methods,
    components,
  };

  fs.writeFileSync(
    manifestFile,
    `${JSON.stringify(manifest, null, 2)}\n`,
    'utf8'
  );

  console.log(`Generated ${manifestFile}.`);
}

if (isMainModule(import.meta.url)) {
  generateManifest();
}
