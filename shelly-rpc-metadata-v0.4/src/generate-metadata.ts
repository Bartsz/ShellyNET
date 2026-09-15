import fs from 'node:fs';
import { CompactTypeEncoder, type CompactType } from './compact-type.js';
import { discoverShellyTypes, type ShellyDiscovery, type ShellyRpcMethodDefinition } from './discover.js';
import { isMainModule } from './is-main.js';
import { componentAliasBase, methodAliasBase } from './naming.js';
import { getShellyPackageInfo } from './package-info.js';
import { manifestFile } from './paths.js';
import { stringifyCompactJson } from './pretty-json.js';
import { loadGeneratedTypeProgram, type ExportedAlias } from './type-program.js';

type JsonObject = Record<string, unknown>;

function methodParts(method: string): { namespace: string; operation: string } {
  const dot = method.indexOf('.');
  return dot < 0
    ? { namespace: method, operation: '' }
    : { namespace: method.slice(0, dot), operation: method.slice(dot + 1) };
}

function mapStem(mapTypeName: string): string | undefined {
  const match = /^shelly_(.+)_rpc_method_map_t$/.exec(mapTypeName);
  return match?.[1];
}

function requireAlias(
  aliases: Map<string, ExportedAlias>,
  name: string
): ExportedAlias {
  const alias = aliases.get(name);
  if (!alias) throw new Error(`Generated alias was not found: ${name}`);
  return alias;
}

function replaceConfigWithComponentConfig(value: CompactType): CompactType {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return value;
  const object = value as Record<string, unknown>;
  if ('$oneOf' in object || '$array' in object || '$tuple' in object || '$enum' in object) {
    return value;
  }

  for (const key of ['config', 'config?']) {
    if (Object.prototype.hasOwnProperty.call(object, key)) {
      object[key] = 'component-config';
      return value;
    }
  }

  return value;
}

function shouldOmitGetConfig(method: string): boolean {
  const { namespace, operation } = methodParts(method);
  return operation === 'GetConfig' && namespace !== 'Shelly';
}

function componentForMethod(method: ShellyRpcMethodDefinition, discovery: ShellyDiscovery): string | undefined {
  const stem = mapStem(method.mapTypeName);
  if (stem === undefined) return undefined;
  return discovery.components.find((c) => c.stem === stem)?.component;
}


function generateComponentStatuses(discovery: ShellyDiscovery, encoder: CompactTypeEncoder, aliases: Map<string, ExportedAlias>): void {
  const statuses: JsonObject = {};

  for (const component of discovery.components) {
    const aliasBase = componentAliasBase(component.component);
    const statusAlias = requireAlias(aliases, `${aliasBase}_Status`);

    const encodedStatus = encoder.encode(statusAlias.type, statusAlias.declaration, {
      includeOptionalPropertyMarker: false,
    });

    const componentNameParts = component.component
      .split(/[^A-Za-z0-9]+/)
      .filter(Boolean);

    const statusTypeName = componentNameParts
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join('');

    const statusName = `${statusTypeName}Status`;

    const isObjectStatus = encodedStatus !== null && typeof encodedStatus === 'object' && !Array.isArray(encodedStatus);
    if (!isObjectStatus) {
      statuses[statusName] = {
        $type: component.component,
        $value: encodedStatus,
      };

      continue;
    }

    statuses[statusName] = {
      $type: component.component,
      ...encodedStatus,
    };
  }

  const outputFile = manifestFile.replace('shelly-rpc.metadata.json', 'ComponentStats.json');

  fs.writeFileSync(outputFile, stringifyCompactJson(statuses), 'utf8');

  console.log(`Generated ${outputFile} (${discovery.components.length} component statuses).`);
}


function generateComponentConfigs(discovery: ShellyDiscovery, encoder: CompactTypeEncoder, aliases: Map<string, ExportedAlias>): void {
  const configs: JsonObject = {};

  const toTypeName = (component: string): string =>
    component
      .split(/[^A-Za-z0-9]+/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join('');

  for (const component of discovery.components) {
    const base = componentAliasBase(component.component);
    const config = requireAlias(aliases, `${base}_Config`);

    const typeName = `${toTypeName(component.component)}Config`;

    const encoded = encoder.encode(config.type, config.declaration, { includeOptionalPropertyMarker: true, });

    configs[typeName] = encoded && typeof encoded === 'object' && !Array.isArray(encoded)
      ? { $type: component.component, ...encoded, }
      : { $type: component.component, $value: encoded, };
  }

  const outputFile = manifestFile.replace('shelly-rpc.metadata.json', 'ComponentConfigs.json');

  fs.writeFileSync(outputFile, stringifyCompactJson(configs), 'utf8');

  console.log(`Generated ${outputFile} (${discovery.components.length} component configs).`);
}


export function generateMetadata(discovery: ShellyDiscovery = discoverShellyTypes()): void {
  const { checker, aliases } = loadGeneratedTypeProgram();
  const encoder = new CompactTypeEncoder(checker, discovery.methods);
  const sourcePackage = getShellyPackageInfo();

  generateComponentConfigs(discovery, encoder, aliases);
  generateComponentStatuses(discovery, encoder, aliases);
  const components: JsonObject = {};
  for (const component of discovery.components) {
    const base = componentAliasBase(component.component);
    const status = requireAlias(aliases, `${base}_Status`);
    const config = requireAlias(aliases, `${base}_Config`);

    components[component.component] = {
      notifyStatus: encoder.encode(status.type, status.declaration, {
        // In NotifyStatus every property is optional by protocol definition,
        // so repeating '?' on every optional status member only adds noise.
        includeOptionalPropertyMarker: false,
      }),
      config: encoder.encode(config.type, config.declaration, {
        includeOptionalPropertyMarker: true,
      }),
    };
  }

  const rpc: JsonObject = {};
  let includedRpcMethods = 0;
  let omittedGetConfigMethods = 0;

  for (const methodDefinition of discovery.rpcMethods) {
    const method = methodDefinition.method;
    if (shouldOmitGetConfig(method)) {
      omittedGetConfigMethods += 1;
      continue;
    }

    const { namespace, operation } = methodParts(method);
    const namespaceObject = (rpc[namespace] ??= {}) as JsonObject;
    const ownerComponent = componentForMethod(methodDefinition, discovery);
    if (ownerComponent) {
      const existingOwner = namespaceObject.$component;
      if (existingOwner !== undefined && existingOwner !== ownerComponent) {
        throw new Error(
          `RPC namespace ${namespace} maps to multiple components: ${existingOwner} and ${ownerComponent}.`
        );
      }
      namespaceObject.$component = ownerComponent;
    }
    const aliasBase = methodAliasBase(method);
    const paramsAlias = requireAlias(aliases, `${aliasBase}_Params`);
    const resultAlias = requireAlias(aliases, `${aliasBase}_Result`);
    const methodObject: JsonObject = {};

    if (!encoder.isNever(paramsAlias.type)) {
      const paramsKey = encoder.isOptionalTopLevel(paramsAlias.type)
        ? 'params?'
        : 'params';
      let params = encoder.encode(paramsAlias.type, paramsAlias.declaration, {
        includeOptionalPropertyMarker: true,
      });

      if (operation === 'SetConfig' && ownerComponent) {
        params = replaceConfigWithComponentConfig(params);
      }

      methodObject[paramsKey] = params;
    }

    // GetStatus is already described once under components.<type>.notifyStatus.
    // Keep RPC metadata compact by using a domain marker for component methods.
    if (operation === 'GetStatus' && ownerComponent) {
      methodObject.result = 'component-status';
    } else {
      methodObject.result = encoder.encode(resultAlias.type, resultAlias.declaration, {
        includeOptionalPropertyMarker: true,
      });
    }

    namespaceObject[operation] = methodObject;
    includedRpcMethods += 1;
  }

  const metadata = {
    formatVersion: 2,
    source: {
      package: sourcePackage.name,
      version: sourcePackage.version,
      authority: 'shelly-rpc-ts',
      runtimeCapabilityCheck: 'Shelly.ListMethods',
    },
    semantics: {
      optionalProperty: 'A ? suffix on a property name means the JSON property may be omitted.',
      nullableValue: 'A ? suffix on a scalar/array type, or $nullable on a compound descriptor, means JSON null is part of the TypeScript value type.',
      notifyStatus: {
        partial: true,
        propertyOmission: 'unchanged',
        nullOnWire: 'emit NullPayload / property removed or cleared according to Shelly notification semantics',
        shape: 'components.<component>.notifyStatus',
      },
      notifyFullStatus: {
        partial: false,
        shape: 'same component status model as notifyStatus',
      },
      specialTypes: {
        'component-config': 'Recursive partial config of the component owning SetConfig.',
        'component-status': 'Status type of the component owning GetStatus.',
        'rpc-method': 'Any RPC method present in this metadata file.',
        'rpc-params': 'RPC params selected by the sibling rpc-method value (used by Schedule calls).',
      },
    },
    counts: {
      discoveredRpcMethods: discovery.methods.length,
      includedRpcMethods,
      omittedComponentGetConfigMethods: omittedGetConfigMethods,
      componentTypes: discovery.components.length,
    },
    components,
    rpc,
  };

  fs.writeFileSync(manifestFile, stringifyCompactJson(metadata), 'utf8');
  console.log(
    `Generated ${manifestFile} (${includedRpcMethods} RPC methods, ${discovery.components.length} components; omitted ${omittedGetConfigMethods} component GetConfig methods).`
  );
}

if (isMainModule(import.meta.url)) {
  generateMetadata();
}
