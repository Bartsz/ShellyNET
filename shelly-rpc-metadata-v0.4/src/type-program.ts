import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';
import {
  generatedTypesFile,
  rootDir,
  generatedTsconfigFile,
} from './paths.js';

export type ExportedAlias = {
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

export function loadGeneratedTypeProgram(): {
  program: ts.Program;
  checker: ts.TypeChecker;
  sourceFile: ts.SourceFile;
  aliases: Map<string, ExportedAlias>;
} {
  if (!fs.existsSync(generatedTypesFile)) {
    throw new Error(
      `Generated aliases do not exist: ${generatedTypesFile}. Run npm run generate:aliases first.`
    );
  }

  const config = ts.readConfigFile(generatedTsconfigFile, ts.sys.readFile);
  if (config.error) throw new Error(formatDiagnostics([config.error]));

  const parsed = ts.parseJsonConfigFileContent(
    config.config,
    ts.sys,
    path.dirname(generatedTsconfigFile),
    undefined,
    generatedTsconfigFile
  );
  if (parsed.errors.length > 0) throw new Error(formatDiagnostics(parsed.errors));

  const rootNames = [...new Set([...parsed.fileNames, generatedTypesFile])];
  const program = ts.createProgram({ rootNames, options: parsed.options });
  const diagnostics = ts.getPreEmitDiagnostics(program);
  if (diagnostics.length > 0) throw new Error(formatDiagnostics(diagnostics));

  const sourceFile = program.getSourceFile(generatedTypesFile);
  if (!sourceFile) {
    throw new Error(`Could not load generated type file: ${generatedTypesFile}`);
  }

  const checker = program.getTypeChecker();
  const aliases = new Map<string, ExportedAlias>();

  for (const statement of sourceFile.statements) {
    if (!ts.isTypeAliasDeclaration(statement)) continue;
    if (!statement.modifiers?.some((m) => m.kind === ts.SyntaxKind.ExportKeyword)) {
      continue;
    }

    aliases.set(statement.name.text, {
      name: statement.name.text,
      declaration: statement,
      type: checker.getTypeAtLocation(statement.name),
    });
  }

  return { program, checker, sourceFile, aliases };
}
