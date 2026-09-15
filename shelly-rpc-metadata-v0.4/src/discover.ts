import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';
import { probeFile, rootDir, tsconfigFile } from './paths.js';

export type ShellyRpcMethodDefinition = {
  method: string;
  mapTypeName: string;
  declarationFile: string;
};

export type ShellyComponentDefinition = {
  component: string;
  stem: string;
  statusTypeName: string;
  configTypeName: string;
  declarationFile: string;
};

export type ShellyDiscovery = {
  /** Convenience list retained for manifest generation / logging. */
  methods: string[];
  /** Convenience list retained for manifest generation / logging. */
  componentTypes: string[];
  /** Concrete source of every method; used to avoid the global RPC generic. */
  rpcMethods: ShellyRpcMethodDefinition[];
  /** Concrete source of every component; used to avoid the global component generic. */
  components: ShellyComponentDefinition[];
};

function readCompilerOptions(): ts.CompilerOptions {
  const config = ts.readConfigFile(tsconfigFile, ts.sys.readFile);
  if (config.error) {
    throw new Error(formatDiagnostics([config.error]));
  }

  const parsed = ts.parseJsonConfigFileContent(
    config.config,
    ts.sys,
    rootDir,
    undefined,
    tsconfigFile
  );

  if (parsed.errors.length > 0) {
    throw new Error(formatDiagnostics(parsed.errors));
  }

  return parsed.options;
}

function formatDiagnostics(diagnostics: readonly ts.Diagnostic[]): string {
  return ts.formatDiagnosticsWithColorAndContext(diagnostics, {
    getCanonicalFileName: (fileName) => fileName,
    getCurrentDirectory: () => rootDir,
    getNewLine: () => ts.sys.newLine,
  });
}

function normalizePath(fileName: string): string {
  return path.resolve(fileName).replaceAll('\\', '/').toLowerCase();
}

/**
 * The package may be installed by npm, pnpm, yarn, or linked locally.
 * First prefer declaration files whose path clearly belongs to the package.
 * If TypeScript resolved a linked package outside node_modules, fall back to
 * declaration files that contain Shelly's characteristic exported aliases.
 */
function findShellyDeclarationFiles(program: ts.Program): ts.SourceFile[] {
  const packageMarker = '/@taulfsime/shelly-rpc-ts/';
  const declarations = program
    .getSourceFiles()
    .filter((file) => file.isDeclarationFile);

  const byPath = declarations.filter((file) =>
    normalizePath(file.fileName).includes(packageMarker)
  );

  if (byPath.length > 0) {
    return byPath;
  }

  const byContents = declarations.filter((file) =>
    file.statements.some(
      (statement) =>
        ts.isTypeAliasDeclaration(statement) &&
        (statement.name.text === 'shelly_rpc_method_t' ||
          statement.name.text === 'shelly_component_type_t' ||
          statement.name.text.endsWith('_rpc_method_map_t'))
    )
  );

  if (byContents.length > 0) {
    return byContents;
  }

  throw new Error(
    'Could not locate declaration files for @taulfsime/shelly-rpc-ts. ' +
      'Make sure npm install completed successfully.'
  );
}

function propertyNameText(name: ts.PropertyName): string | undefined {
  if (
    ts.isStringLiteral(name) ||
    ts.isNoSubstitutionTemplateLiteral(name) ||
    ts.isIdentifier(name) ||
    ts.isNumericLiteral(name)
  ) {
    return name.text;
  }

  return undefined;
}

function collectRpcMethodsFromTypeNode(
  node: ts.TypeNode,
  mapTypeName: string,
  declarationFile: string,
  methods: Map<string, ShellyRpcMethodDefinition>
): void {
  if (ts.isParenthesizedTypeNode(node)) {
    collectRpcMethodsFromTypeNode(
      node.type,
      mapTypeName,
      declarationFile,
      methods
    );
    return;
  }

  if (ts.isUnionTypeNode(node) || ts.isIntersectionTypeNode(node)) {
    for (const type of node.types) {
      collectRpcMethodsFromTypeNode(
        type,
        mapTypeName,
        declarationFile,
        methods
      );
    }
    return;
  }

  if (!ts.isTypeLiteralNode(node)) {
    // The aggregate shelly_rpc_method_map_t is an intersection of references
    // to per-component maps. Those referenced maps are scanned separately.
    return;
  }

  for (const member of node.members) {
    if (!ts.isPropertySignature(member) && !ts.isMethodSignature(member)) {
      continue;
    }

    const method = propertyNameText(member.name);
    if (!method?.includes('.')) {
      continue;
    }

    const definition: ShellyRpcMethodDefinition = {
      method,
      mapTypeName,
      declarationFile: path.resolve(declarationFile),
    };

    const existing = methods.get(method);
    if (existing) {
      const sameSource =
        normalizePath(existing.declarationFile) ===
          normalizePath(definition.declarationFile) &&
        existing.mapTypeName === definition.mapTypeName;

      if (!sameSource) {
        throw new Error(
          `RPC method ${JSON.stringify(method)} is declared by more than one map: ` +
            `${existing.mapTypeName} (${existing.declarationFile}) and ` +
            `${definition.mapTypeName} (${definition.declarationFile}).`
        );
      }

      continue;
    }

    methods.set(method, definition);
  }
}

function getSingleStringLiteral(node: ts.TypeNode): string | undefined {
  if (ts.isParenthesizedTypeNode(node)) {
    return getSingleStringLiteral(node.type);
  }

  if (ts.isLiteralTypeNode(node) && ts.isStringLiteral(node.literal)) {
    return node.literal.text;
  }

  return undefined;
}

function discoverFromDeclarations(
  files: readonly ts.SourceFile[]
): ShellyDiscovery {
  const rpcMethods = new Map<string, ShellyRpcMethodDefinition>();
  const components = new Map<string, ShellyComponentDefinition>();

  for (const sourceFile of files) {
    const aliases = new Map<string, ts.TypeAliasDeclaration>();

    for (const statement of sourceFile.statements) {
      if (ts.isTypeAliasDeclaration(statement)) {
        aliases.set(statement.name.text, statement);
      }
    }

    // RPC methods come from concrete *_rpc_method_map_t aliases. We also keep
    // the exact map alias and declaration file so generated schema aliases can
    // index the concrete map instead of using shelly_rpc_method_*<K>.
    for (const [name, declaration] of aliases) {
      if (name.endsWith('_rpc_method_map_t')) {
        collectRpcMethodsFromTypeNode(
          declaration.type,
          name,
          sourceFile.fileName,
          rpcMethods
        );
      }
    }

    // A component is identified by the quartet used consistently by the
    // library: *_type_t, *_key_t, *_status_t and *_config_t. Keep the concrete
    // status/config aliases; this avoids shelly_component_status_t<T>, whose
    // Extract<> over the global component map can overflow schema generators.
    for (const [name, declaration] of aliases) {
      if (!name.startsWith('shelly_') || !name.endsWith('_type_t')) {
        continue;
      }

      const stem = name.slice('shelly_'.length, -'_type_t'.length);
      const keyTypeName = `shelly_${stem}_key_t`;
      const statusTypeName = `shelly_${stem}_status_t`;
      const configTypeName = `shelly_${stem}_config_t`;

      if (
        !aliases.has(keyTypeName) ||
        !aliases.has(statusTypeName) ||
        !aliases.has(configTypeName)
      ) {
        continue;
      }

      const component = getSingleStringLiteral(declaration.type);
      if (!component) {
        continue;
      }

      const definition: ShellyComponentDefinition = {
        component,
        stem,
        statusTypeName,
        configTypeName,
        declarationFile: path.resolve(sourceFile.fileName),
      };

      const existing = components.get(component);
      if (existing) {
        const sameSource =
          normalizePath(existing.declarationFile) ===
            normalizePath(definition.declarationFile) &&
          existing.statusTypeName === definition.statusTypeName &&
          existing.configTypeName === definition.configTypeName;

        if (!sameSource) {
          throw new Error(
            `Component ${JSON.stringify(component)} is declared more than once: ` +
              `${existing.declarationFile} and ${definition.declarationFile}.`
          );
        }

        continue;
      }

      components.set(component, definition);
    }
  }

  if (rpcMethods.size === 0) {
    throw new Error(
      'No RPC methods were found in @taulfsime/shelly-rpc-ts declaration files.'
    );
  }

  if (components.size === 0) {
    throw new Error(
      'No component types were found in @taulfsime/shelly-rpc-ts declaration files.'
    );
  }

  const sortedRpcMethods = [...rpcMethods.values()].sort((a, b) =>
    a.method.localeCompare(b.method)
  );
  const sortedComponents = [...components.values()].sort((a, b) =>
    a.component.localeCompare(b.component)
  );

  return {
    methods: sortedRpcMethods.map((entry) => entry.method),
    componentTypes: sortedComponents.map((entry) => entry.component),
    rpcMethods: sortedRpcMethods,
    components: sortedComponents,
  };
}

export function discoverShellyTypes(): ShellyDiscovery {
  if (!fs.existsSync(probeFile)) {
    throw new Error(`Probe file does not exist: ${probeFile}`);
  }

  const program = ts.createProgram({
    rootNames: [probeFile],
    options: readCompilerOptions(),
  });

  const diagnostics = ts.getPreEmitDiagnostics(program);
  if (diagnostics.length > 0) {
    throw new Error(formatDiagnostics(diagnostics));
  }

  const declarationFiles = findShellyDeclarationFiles(program);
  const discovery = discoverFromDeclarations(declarationFiles);

  console.log(
    `Discovered ${discovery.methods.length} RPC methods and ` +
      `${discovery.componentTypes.length} component types from ` +
      `${declarationFiles.length} Shelly declaration files.`
  );

  return discovery;
}
