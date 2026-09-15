# Shelly RPC metadata generator

Jednorazowy ekstraktor metadanych z `@taulfsime/shelly-rpc-ts`.

Celem projektu jest uzyskanie stabilnego, machine-readable opisu:

- wszystkich RPC methods z biblioteki,
- `params` i `result` każdej metody,
- `status`, `NotifyStatusPatch` i `config` każdego komponentu,
- JSON Schema z poprawną obsługą typów rekurencyjnych.

Nie generuje kodu C#.

## Wymagania

- Node.js 20+
- VS Code opcjonalnie

## Start

```bash
npm install
npm run generate
```

W VS Code można również użyć:

`Terminal -> Run Build Task -> Shelly: generate metadata`

## Pliki wynikowe

Generator tworzy:

```text
generated/
  shelly-types.generated.ts
  shelly-rpc.schema.json
  shelly-rpc.metadata.json
```

### `shelly-types.generated.ts`

Materializuje konkretne aliasy dla każdej metody i komponentu. Generator celowo indeksuje konkretne `*_rpc_method_map_t` i korzysta z konkretnych `*_status_t` / `*_config_t`, zamiast globalnych helperów generycznych.

### `shelly-rpc.schema.json`

JSON Schema Draft-07 tworzony bezpośrednio przez TypeScript Compiler API.

Od wersji `0.3.0` projekt **nie używa `ts-json-schema-generator`**. Powód: `Schedule.Create` jest typem rekurencyjnym. `Schedule.Create.params.calls[].params` jest wyliczane z `shelly_rpc_method_params_t<shelly_rpc_method_t>`, a w `shelly_rpc_method_t` znajduje się ponownie `Schedule.Create`. Generyczny generator wpadał na tej konstrukcji w rekursję `PromiseNodeParser/Awaited<T>` i kończył się `Maximum call stack size exceeded`.

Własny ekstraktor rejestruje typ przed rozwinięciem i przy ponownym napotkaniu tego samego typu emituje `$ref`. Rekurencja jest więc częścią schematu, a nie błędem generatora.

Przykładowo schemat może zawierać odwołania w stylu:

```json
{
  "$ref": "#/definitions/Rpc_Schedule_Create_Params"
}
```

### `shelly-rpc.metadata.json`

Manifest wysokiego poziomu, m.in.:

```json
{
  "method": "Switch.Set",
  "namespace": "Switch",
  "operation": "Set",
  "params": {
    "type": "Rpc_Switch_Set_Params",
    "schemaRef": "#/definitions/Rpc_Switch_Set_Params"
  },
  "result": {
    "type": "Rpc_Switch_Set_Result",
    "schemaRef": "#/definitions/Rpc_Switch_Set_Result"
  }
}
```

Dla komponentów manifest wskazuje również pełny status oraz patch statusu.

`NotifyStatusPatch<T>` ma semantykę:

```text
property missing -> brak zmiany
property: null   -> wartość usunięta / wyczyszczona
property: value  -> nowa wartość
```

`NotifyFullStatus` wskazuje zwykły pełny typ `Status`.

## Źródło metod

Źródłem nadrzędnym jest zainstalowane `@taulfsime/shelly-rpc-ts`.

Generator skanuje deklaracje `.d.ts` i znajduje literalne klucze `*_rpc_method_map_t`, więc zachowuje również metody obecne w bibliotece, a niekoniecznie w aktualnej oficjalnej dokumentacji Shelly.

Przyszła biblioteka .NET może niezależnie zweryfikować możliwości konkretnego urządzenia przez `Shelly.ListMethods`.

## Komendy

```bash
npm run generate
npm run generate:aliases
npm run typecheck:generated
npm run generate:schema
npm run generate:manifest
npm run typecheck
npm run clean
```

Do diagnostyki wygodna kolejność to:

```bash
npm run generate:aliases
npm run typecheck:generated
npm run generate:schema
npm run generate:manifest
```

## Uwagi do schematu

Ekstraktor obsługuje m.in.:

- obiekty i właściwości opcjonalne,
- `null`, `undefined`, `never`, `unknown`,
- string/number/boolean literals,
- union types,
- arrays i tuples,
- `Record<string, T>` / index signatures,
- conditional/mapped utility types po ich rozwiązaniu przez TypeScript checker,
- rekurencję przez `$ref`,
- `Promise<T>` przez rozpakowanie do `T` jako zabezpieczenie.

Dla konstrukcji TypeScript, które nie mają jednoznacznego odpowiednika JSON, schemat zachowuje pole diagnostyczne `x-typescript-type` zamiast przerywać całe generowanie.
