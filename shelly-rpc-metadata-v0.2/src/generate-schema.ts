import fs from 'node:fs';
import ts from 'typescript';
import { isMainModule } from './is-main.js';
import { createGenerator } from 'ts-json-schema-generator';
import {
  generatedTypesFile,
  schemaFile,
  schemaTsconfigFile,
} from './paths.js';

function exportedTypeAliases(): string[] {
  const source = fs.readFileSync(generatedTypesFile, 'utf8');
  const file = ts.createSourceFile(
    generatedTypesFile,
    source,
    ts.ScriptTarget.Latest,
    true,
    ts.ScriptKind.TS
  );

  const result: string[] = [];
  for (const statement of file.statements) {
    if (!ts.isTypeAliasDeclaration(statement)) continue;
    if (!statement.modifiers?.some((m) => m.kind === ts.SyntaxKind.ExportKeyword)) {
      continue;
    }
    result.push(statement.name.text);
  }
  return result;
}

function describeError(error: unknown): string {
  if (error instanceof Error) {
    const cause = (error as Error & { cause?: unknown }).cause;
    return cause instanceof Error
      ? `${error.message} Cause: ${cause.name}: ${cause.message}`
      : `${error.name}: ${error.message}`;
  }
  return String(error);
}

/**
 * When bulk generation fails, probe exported aliases individually. This is a
 * diagnostic fallback only; the normal path still creates one schema in one
 * pass. It turns an opaque ts-json-schema-generator stack overflow into a list
 * of the exact aliases/types that trigger it.
 */
function diagnoseFailingAliases(config: {
  path: string;
  tsconfig: string;
  expose: 'export';
  jsDoc: 'extended';
  skipTypeCheck: boolean;
  additionalProperties: boolean;
}): string[] {
  const aliases = exportedTypeAliases();
  const failures: string[] = [];

  console.error(
    `Bulk schema generation failed. Probing ${aliases.length} exported aliases individually...`
  );

  // Reuse a single parser/program. createSchema(type) starts a fresh schema
  // walk while avoiding the cost of rebuilding the TypeScript program each time.
  const generator = createGenerator({ ...config, type: aliases[0] ?? '*' });

  for (const alias of aliases) {
    try {
      generator.createSchema(alias);
    } catch (error) {
      failures.push(`${alias}: ${describeError(error)}`);
      console.error(`  FAILED ${alias}`);

      // A handful is enough to diagnose the source and avoids thousands of
      // repeated traces if one underlying helper breaks many aliases.
      if (failures.length >= 12) break;
    }
  }

  return failures;
}

export function generateSchema(): void {
  if (!fs.existsSync(generatedTypesFile)) {
    throw new Error(
      `Generated aliases do not exist: ${generatedTypesFile}. Run npm run generate:aliases first.`
    );
  }

  const baseConfig = {
    path: generatedTypesFile,
    tsconfig: schemaTsconfigFile,
    expose: 'export' as const,
    jsDoc: 'extended' as const,
    skipTypeCheck: false,
    additionalProperties: false,
  };

  try {
    const schema = createGenerator({ ...baseConfig, type: '*' }).createSchema('*');
    fs.writeFileSync(schemaFile, `${JSON.stringify(schema, null, 2)}\n`, 'utf8');
    console.log(`Generated ${schemaFile}.`);
  } catch (error) {
    const failures = diagnoseFailingAliases(baseConfig);
    const details = failures.length
      ? `\nFirst failing aliases:\n${failures.map((x) => `- ${x}`).join('\n')}`
      : '\nNo individual exported alias failed; the failure appears specific to bulk type="*" generation.';

    throw new Error(
      `ts-json-schema-generator failed during bulk generation: ${describeError(error)}${details}`,
      { cause: error }
    );
  }
}

if (isMainModule(import.meta.url)) {
  generateSchema();
}
