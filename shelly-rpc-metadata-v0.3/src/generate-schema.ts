import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';
import { isMainModule } from './is-main.js';
import {
  generatedTypesFile,
  rootDir,
  schemaFile,
  schemaTsconfigFile,
} from './paths.js';

type JsonSchema = boolean | { [key: string]: unknown };

type ExportedAlias = {
  name: string;
  declaration: ts.TypeAliasDeclaration;
  type: ts.Type;
};

function formatDiagnostics(diagnostics: readonly ts.Diagnostic[]): string {
  return ts.formatDiagnosticsWithColorAndContext(diagnostics, {
    getCanonicalFileName: (fileName) => fileName,
    getCurrentDirectory: () => rootDir,
    getNewLine: () => ts.sys.newLine,
  });
}

function loadSchemaProgram(): {
  program: ts.Program;
  checker: ts.TypeChecker;
  sourceFile: ts.SourceFile;
} {
  const config = ts.readConfigFile(schemaTsconfigFile, ts.sys.readFile);
  if (config.error) {
    throw new Error(formatDiagnostics([config.error]));
  }

  const parsed = ts.parseJsonConfigFileContent(
    config.config,
    ts.sys,
    path.dirname(schemaTsconfigFile),
    undefined,
    schemaTsconfigFile
  );

  if (parsed.errors.length > 0) {
    throw new Error(formatDiagnostics(parsed.errors));
  }

  const rootNames = [...new Set([...parsed.fileNames, generatedTypesFile])];
  const program = ts.createProgram({ rootNames, options: parsed.options });
  const diagnostics = ts.getPreEmitDiagnostics(program);
  if (diagnostics.length > 0) {
    throw new Error(formatDiagnostics(diagnostics));
  }

  const sourceFile = program.getSourceFile(generatedTypesFile);
  if (!sourceFile) {
    throw new Error(`Could not load generated type file: ${generatedTypesFile}`);
  }

  return { program, checker: program.getTypeChecker(), sourceFile };
}

function getExportedAliases(
  checker: ts.TypeChecker,
  sourceFile: ts.SourceFile
): ExportedAlias[] {
  const aliases: ExportedAlias[] = [];

  for (const statement of sourceFile.statements) {
    if (!ts.isTypeAliasDeclaration(statement)) continue;
    if (!statement.modifiers?.some((m) => m.kind === ts.SyntaxKind.ExportKeyword)) {
      continue;
    }

    aliases.push({
      name: statement.name.text,
      declaration: statement,
      type: checker.getTypeAtLocation(statement.name),
    });
  }

  return aliases;
}

function sanitizeDefinitionName(value: string): string {
  const sanitized = value.replace(/[^A-Za-z0-9_.-]/g, '_');
  return sanitized.length > 100 ? sanitized.slice(0, 100) : sanitized;
}

function isUndefinedType(type: ts.Type): boolean {
  return (type.flags & ts.TypeFlags.Undefined) !== 0;
}

function isNullType(type: ts.Type): boolean {
  return (type.flags & ts.TypeFlags.Null) !== 0;
}

function isBooleanLiteral(type: ts.Type): type is ts.LiteralType {
  return (type.flags & ts.TypeFlags.BooleanLiteral) !== 0;
}

class TypeScriptJsonSchemaBuilder {
  private readonly definitions: Record<string, JsonSchema> = {};
  private readonly definitionNameByType = new Map<ts.Type, string>();
  private readonly aliasesByType = new Map<ts.Type, string[]>();
  private internalCounter = 0;

  constructor(
    private readonly checker: ts.TypeChecker,
    private readonly aliases: readonly ExportedAlias[]
  ) {
    // Pre-register all exported aliases before expanding anything. This is the
    // key to handling recursive Shelly types such as Schedule.Create: when the
    // same TypeScript type is encountered again, it becomes a JSON Schema $ref.
    for (const alias of aliases) {
      const names = this.aliasesByType.get(alias.type) ?? [];
      names.push(alias.name);
      this.aliasesByType.set(alias.type, names);

      if (!this.definitionNameByType.has(alias.type)) {
        this.definitionNameByType.set(alias.type, alias.name);
      }
    }
  }

  build(): { definitions: Record<string, JsonSchema> } {
    const canonicalAliases = new Map<string, ExportedAlias>();

    for (const alias of this.aliases) {
      const canonicalName = this.definitionNameByType.get(alias.type)!;
      if (canonicalName === alias.name) {
        canonicalAliases.set(alias.name, alias);
      } else {
        this.definitions[alias.name] = this.ref(canonicalName);
      }
    }

    for (const [name, alias] of canonicalAliases) {
      // Reserve the slot before expansion so recursion can immediately refer
      // back to it.
      this.definitions[name] = {};
      this.definitions[name] = this.buildTypeBody(alias.type, alias.declaration);
    }

    return { definitions: this.definitions };
  }

  private ref(name: string): JsonSchema {
    return { $ref: `#/definitions/${encodeURIComponent(name)}` };
  }

  private typeLabel(type: ts.Type): string {
    return this.checker.typeToString(
      type,
      undefined,
      ts.TypeFormatFlags.NoTruncation |
        ts.TypeFormatFlags.UseAliasDefinedOutsideCurrentScope
    );
  }

  private ensureDefinition(type: ts.Type, location: ts.Node): JsonSchema {
    const existing = this.definitionNameByType.get(type);
    if (existing) return this.ref(existing);

    const aliasSymbolName = type.aliasSymbol?.getName();
    const symbolName = type.getSymbol()?.getName();
    const hint =
      aliasSymbolName && aliasSymbolName !== '__type'
        ? aliasSymbolName
        : symbolName && symbolName !== '__type'
          ? symbolName
          : 'Anonymous';

    let name: string;
    do {
      this.internalCounter += 1;
      name = `Internal_${String(this.internalCounter).padStart(4, '0')}_${sanitizeDefinitionName(hint)}`;
    } while (Object.prototype.hasOwnProperty.call(this.definitions, name));

    this.definitionNameByType.set(type, name);
    this.definitions[name] = {};
    this.definitions[name] = this.buildTypeBody(type, location);
    return this.ref(name);
  }

  private schemaFor(type: ts.Type, location: ts.Node): JsonSchema {
    // JSON has no undefined. Optionality is handled at the object-property
    // level. If it still reaches this point (for example as a top-level alias),
    // represent it as an unconstrained schema and retain a diagnostic hint.
    if (isUndefinedType(type) || (type.flags & ts.TypeFlags.Void) !== 0) {
      return { 'x-typescript-type': this.typeLabel(type) };
    }

    if ((type.flags & ts.TypeFlags.Never) !== 0) return false;
    if ((type.flags & (ts.TypeFlags.Any | ts.TypeFlags.Unknown)) !== 0) {
      return {};
    }
    if (isNullType(type)) return { type: 'null' };

    if ((type.flags & ts.TypeFlags.StringLiteral) !== 0) {
      return { type: 'string', const: (type as ts.StringLiteralType).value };
    }
    if ((type.flags & ts.TypeFlags.NumberLiteral) !== 0) {
      return { type: 'number', const: (type as ts.NumberLiteralType).value };
    }
    if (isBooleanLiteral(type)) {
      return {
        type: 'boolean',
        const: (type as { intrinsicName?: string }).intrinsicName === 'true',
      };
    }

    if ((type.flags & ts.TypeFlags.StringLike) !== 0) return { type: 'string' };
    if ((type.flags & ts.TypeFlags.NumberLike) !== 0) return { type: 'number' };
    if ((type.flags & ts.TypeFlags.BooleanLike) !== 0) return { type: 'boolean' };
    if ((type.flags & ts.TypeFlags.BigIntLike) !== 0) {
      return { type: 'integer', 'x-typescript-type': this.typeLabel(type) };
    }

    if (type.isUnion()) {
      return this.buildUnion(type, location);
    }

    // Promise<T> is not a JSON value. No Shelly wire shape should contain one,
    // but unwrapping here makes the extractor resilient to helper types that
    // temporarily introduce Promise/Awaited during type computation.
    const awaited = this.checker.getAwaitedType(type);
    if (awaited && awaited !== type) {
      return this.schemaFor(awaited, location);
    }

    if (this.checker.isTupleType(type)) {
      return this.ensureDefinition(type, location);
    }

    if (this.checker.isArrayType(type)) {
      return this.ensureDefinition(type, location);
    }

    if ((type.flags & ts.TypeFlags.Object) !== 0) {
      return this.ensureDefinition(type, location);
    }

    if ((type.flags & ts.TypeFlags.TemplateLiteral) !== 0) {
      return { type: 'string', 'x-typescript-type': this.typeLabel(type) };
    }

    return { 'x-typescript-type': this.typeLabel(type) };
  }

  private buildTypeBody(type: ts.Type, location: ts.Node): JsonSchema {
    if (type.isUnion()) return this.buildUnion(type, location);

    const awaited = this.checker.getAwaitedType(type);
    if (awaited && awaited !== type) return this.schemaFor(awaited, location);

    if (this.checker.isTupleType(type)) {
      return this.buildTuple(type, location);
    }

    if (this.checker.isArrayType(type)) {
      const element = this.checker.getIndexTypeOfType(type, ts.IndexKind.Number);
      return {
        type: 'array',
        items: element ? this.schemaFor(element, location) : {},
      };
    }

    if ((type.flags & ts.TypeFlags.Object) !== 0) {
      return this.buildObject(type, location);
    }

    // Primitive exported aliases arrive here because they were pre-registered
    // as named definitions. Render them inline rather than referring to self.
    if (isUndefinedType(type) || (type.flags & ts.TypeFlags.Void) !== 0) {
      return { 'x-typescript-type': this.typeLabel(type) };
    }
    if ((type.flags & ts.TypeFlags.Never) !== 0) return false;
    if ((type.flags & (ts.TypeFlags.Any | ts.TypeFlags.Unknown)) !== 0) return {};
    if (isNullType(type)) return { type: 'null' };
    if ((type.flags & ts.TypeFlags.StringLiteral) !== 0) {
      return { type: 'string', const: (type as ts.StringLiteralType).value };
    }
    if ((type.flags & ts.TypeFlags.NumberLiteral) !== 0) {
      return { type: 'number', const: (type as ts.NumberLiteralType).value };
    }
    if (isBooleanLiteral(type)) {
      return {
        type: 'boolean',
        const: (type as { intrinsicName?: string }).intrinsicName === 'true',
      };
    }
    if ((type.flags & ts.TypeFlags.StringLike) !== 0) return { type: 'string' };
    if ((type.flags & ts.TypeFlags.NumberLike) !== 0) return { type: 'number' };
    if ((type.flags & ts.TypeFlags.BooleanLike) !== 0) return { type: 'boolean' };
    if ((type.flags & ts.TypeFlags.TemplateLiteral) !== 0) {
      return { type: 'string', 'x-typescript-type': this.typeLabel(type) };
    }

    return { 'x-typescript-type': this.typeLabel(type) };
  }

  private buildUnion(type: ts.UnionType, location: ts.Node): JsonSchema {
    const members = type.types.filter((member) => !isUndefinedType(member));

    if (members.length === 0) {
      return { 'x-typescript-type': this.typeLabel(type) };
    }
    if (members.length === 1) return this.schemaFor(members[0], location);

    // TypeScript represents boolean as true | false in many contexts. Collapse
    // that pair to a simple JSON boolean schema.
    if (
      members.length === 2 &&
      members.every((member) => isBooleanLiteral(member))
    ) {
      return { type: 'boolean' };
    }

    return { anyOf: members.map((member) => this.schemaFor(member, location)) };
  }

  private buildTuple(type: ts.Type, location: ts.Node): JsonSchema {
    const args = this.checker.getTypeArguments(type as ts.TypeReference);
    return {
      type: 'array',
      items: args.map((arg) => this.schemaFor(arg, location)),
      minItems: args.length,
      maxItems: args.length,
      additionalItems: false,
    };
  }

  private buildObject(type: ts.Type, location: ts.Node): JsonSchema {
    const properties: Record<string, JsonSchema> = {};
    const required: string[] = [];

    for (const property of this.checker.getPropertiesOfType(type)) {
      const declaration = property.valueDeclaration ?? property.declarations?.[0];
      const propertyLocation = declaration ?? location;
      const propertyType = this.checker.getTypeOfSymbolAtLocation(
        property,
        propertyLocation
      );

      properties[property.getName()] = this.schemaFor(propertyType, propertyLocation);

      const optionalBySymbol = (property.flags & ts.SymbolFlags.Optional) !== 0;
      const optionalByUndefined =
        propertyType.isUnion() &&
        propertyType.types.some((member) => isUndefinedType(member));

      if (!optionalBySymbol && !optionalByUndefined) {
        required.push(property.getName());
      }
    }

    const stringIndex = this.checker.getIndexTypeOfType(type, ts.IndexKind.String);
    const numberIndex = this.checker.getIndexTypeOfType(type, ts.IndexKind.Number);

    const schema: { [key: string]: unknown } = { type: 'object' };

    if (Object.keys(properties).length > 0) {
      schema.properties = properties;
    }
    if (required.length > 0) {
      schema.required = required.sort((a, b) => a.localeCompare(b));
    }

    if (stringIndex) {
      schema.additionalProperties = this.schemaFor(stringIndex, location);
    } else {
      schema.additionalProperties = false;
    }

    if (numberIndex && !stringIndex) {
      // JSON object keys are strings, but preserve the TypeScript numeric index
      // information for downstream generators.
      schema.patternProperties = {
        '^(0|[1-9][0-9]*)$': this.schemaFor(numberIndex, location),
      };
    }

    const calls = type.getCallSignatures();
    if (calls.length > 0 && Object.keys(properties).length === 0 && !stringIndex) {
      return {
        'x-typescript-type': this.typeLabel(type),
        'x-typescript-callable': true,
      };
    }

    return schema;
  }
}

export function generateSchema(): void {
  if (!fs.existsSync(generatedTypesFile)) {
    throw new Error(
      `Generated aliases do not exist: ${generatedTypesFile}. Run npm run generate:aliases first.`
    );
  }

  const { checker, sourceFile } = loadSchemaProgram();
  const aliases = getExportedAliases(checker, sourceFile);
  const builder = new TypeScriptJsonSchemaBuilder(checker, aliases);
  const { definitions } = builder.build();

  const schema = {
    $schema: 'http://json-schema.org/draft-07/schema#',
    $id: 'https://github.com/taulfsime/shelly-rpc-ts/generated/shelly-rpc.schema.json',
    title: 'Shelly RPC metadata types',
    description:
      'Generated from @taulfsime/shelly-rpc-ts with the TypeScript Compiler API. Recursive TypeScript types are represented using JSON Schema $ref.',
    definitions,
  };

  fs.writeFileSync(schemaFile, `${JSON.stringify(schema, null, 2)}\n`, 'utf8');
  console.log(
    `Generated ${schemaFile} (${aliases.length} exported aliases, ${Object.keys(definitions).length} schema definitions).`
  );
}

if (isMainModule(import.meta.url)) {
  generateSchema();
}
