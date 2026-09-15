# Shelly RPC metadata generator

One-off TypeScript tooling for materializing RPC metadata from
`@taulfsime/shelly-rpc-ts` and converting it to JSON Schema.

The package is intentionally treated as the source of truth for the method
catalog. A future .NET client can still call `Shelly.ListMethods` when it
connects to a device and use that list as the runtime capability check.

## Requirements

- Node.js 20 or newer (Node.js 22 is recommended)
- npm
- VS Code is optional

## First run

```bash
npm install
npm run generate
```

Generated files:

- `generated/shelly-types.generated.ts`
  - one concrete `Params` and `Result` alias per RPC method;
  - `Status`, `NotifyStatusPatch` and `Config` aliases per component type.
- `generated/shelly-rpc.schema.json`
  - JSON Schema generated from all exported aliases.
- `generated/shelly-rpc.metadata.json`
  - compact machine-readable index of methods, namespaces, components and
    `$ref` links into the schema.

## VS Code

Open this directory in VS Code and run **Terminal -> Run Build Task**.
The default build task is `Shelly: generate metadata`.

Other tasks are available under **Terminal -> Run Task**:

- `Shelly: install dependencies`
- `Shelly: generate aliases`
- `Shelly: generate JSON Schema`
- `Shelly: generate manifest`
- `Shelly: typecheck generator`
- `Shelly: typecheck generated aliases`
- `Shelly: clean generated files`

A launch configuration named **Debug Shelly metadata generator** is included,
so `src/generate.ts` can also be run under the VS Code debugger.

## Generation model

`src/probe.ts` imports the public Shelly types only to make TypeScript load the
package declaration graph. Discovery itself does **not** depend on the compiler
successfully reducing `shelly_rpc_method_t` or `shelly_component_type_t` to a
union of string literals.

Instead, `src/discover.ts` scans the installed package `.d.ts` files:

- RPC methods are read directly from top-level property names in
  `*_rpc_method_map_t` aliases (for example `shelly_switch_rpc_method_map_t`);
- component types are discovered from declaration files containing the quartet
  `*_type_t`, `*_key_t`, `*_status_t` and `*_config_t`; the literal value of
  `*_type_t` becomes the component name.

This avoids TypeScript Compiler API widening computed `keyof` / conditional
aliases to `string`, while still keeping the catalog fully automatic.

The generator then imports concrete declarations from the exact package
module where they were found and emits aliases such as:

```ts
import type {
  shelly_switch_config_t,
  shelly_switch_rpc_method_map_t,
  shelly_switch_status_t,
} from '../node_modules/@taulfsime/shelly-rpc-ts/dist/components/Switch.js';

type Rpc_Switch_Set_Entry =
  shelly_switch_rpc_method_map_t['Switch.Set'];

export type Rpc_Switch_Set_Params =
  Rpc_Switch_Set_Entry extends { params: infer P } ? P :
  Rpc_Switch_Set_Entry extends { params?: infer P } ? P | undefined :
  never;

export type Rpc_Switch_Set_Result =
  Rpc_Switch_Set_Entry extends { result: infer R } ? R : unknown;

export type Component_switch_Status = shelly_switch_status_t;
export type Component_switch_Config = shelly_switch_config_t;
```

The relative module path is generated from the actual resolved `.d.ts` file; it
is not hard-coded to the example path above.

No list of Shelly methods or component types is maintained manually in this
project.

## NotifyStatus vs NotifyFullStatus

For each component the generator emits a shallow patch type directly over the
concrete status alias, for example:

```ts
export type Component_switch_NotifyStatusPatch = {
  [K in keyof Component_switch_Status]?:
    Component_switch_Status[K] | null;
};
```

Its intended semantics are:

- property absent: unchanged;
- property present with `null`: removed/cleared;
- property present with a value: new value.

`NotifyFullStatus` uses the ordinary complete `Status` alias. The manifest
records which schema reference belongs to each case.

The generator deliberately does not materialize a giant notification DTO with
properties such as `switch:0`, `switch:1`, etc. Component keys are dynamic and
are better dispatched by component type/id by the eventual runtime parser.

## Updating shelly-rpc-ts

The Shelly package version is deliberately pinned in `package.json` for
repeatable output. To move to another version:

```bash
npm install -D @taulfsime/shelly-rpc-ts@<version>
npm run generate
```

Review the generated manifest/schema diff and commit the new dependency lock
file together with the generated metadata if desired.

## Troubleshooting: `Maximum call stack size exceeded`

Version 0.2.0 deliberately avoids materializing schemas through the global
`ShellyComponents` and `ShellyRpc` generic helper types.

Instead, generated aliases point directly to:

- the concrete component declaration, for example `shelly_switch_status_t`;
- the concrete RPC map entry, for example
  `shelly_switch_rpc_method_map_t["Switch.Set"]`.

This is important because `shelly_component_status_t<T>` is implemented as an
`Extract<>` over the complete component map. Some versions of
`ts-json-schema-generator` recursively expand that generic and can overflow the
JavaScript stack even when `T` is a string literal.

If schema generation still fails for a particular type, `generate-schema.ts`
now automatically probes exported aliases individually and prints the first
failing alias names. Run:

```bash
npm run generate
```

or validate just the generated TypeScript aliases first:

```bash
npm run generate:aliases
npm run typecheck:generated
npm run generate:schema
```
