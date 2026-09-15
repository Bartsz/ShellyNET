# Shelly RPC compact metadata generator

Generator metadanych z `@taulfsime/shelly-rpc-ts` przygotowanych pod późniejszy generator/parser .NET.

Nie generuje klasycznego JSON Schema i nie generuje kodu C#.

## Cel

Wynik ma opisywać dokładnie informacje potrzebne do biblioteki Shelly RPC:

- kształt `NotifyStatus` dla każdego komponentu,
- config każdego komponentu,
- wszystkie RPC poza komponentowymi `*.GetConfig`,
- parametry i wyniki RPC,
- string literal unions jako zwarte enumy,
- optional i nullable jako dwie różne cechy,
- brak `$ref` i brak wielkich sekcji `definitions`.

`Shelly.GetConfig` pozostaje na liście RPC. Komponentowe `Switch.GetConfig`, `Light.GetConfig`, itd. są pomijane, bo configi są już opisane w `components`.

## Start

```bash
npm install
npm run generate
```

W VS Code: `Terminal -> Run Build Task -> Shelly: generate compact metadata`.

## Wynik

```text
generated/
  shelly-types.generated.ts
  shelly-rpc.metadata.json
```

### Przykład

```json
{
  "components": {
    "switch": {
      "notifyStatus": {
        "id": "number",
        "source": { "$enum": ["button", "timer", "WS_in", "WS_out"] },
        "tag": "string?",
        "output": "bool",
        "aenergy": {
          "total": "number",
          "by_minute": "number[]",
          "minute_ts": "number"
        }
      },
      "config": {
        "id": "number",
        "name": "string?",
        "in_mode": { "$enum": ["momentary", "follow", "flip", "detached", "cycle", "activate"] }
      }
    }
  },
  "rpc": {
    "Switch": {
      "$component": "switch",
      "SetConfig": {
        "params": {
          "id": "number",
          "config": "component-config"
        },
        "result": {
          "restart_required": "bool"
        }
      },
      "Set": {
        "params": {
          "id": "number",
          "on": "bool",
          "toggle_after?": "number",
          "tag?": "string?"
        },
        "result": {
          "was_on": "bool"
        }
      }
    }
  }
}
```

## DSL typów

| Zapis | Znaczenie |
|---|---|
| `"number"` | JSON number |
| `"bool"` | JSON boolean |
| `"string"` | JSON string |
| `"number[]"` | tablica liczb |
| `"string?"` | string albo JSON null |
| `"foo?"` jako nazwa property | property opcjonalna / może zostać pominięta |
| `{ "$enum": [...] }` | zamknięty zbiór literal values |
| `{ "$array": T }` | tablica złożonego typu T |
| `{ "$tuple": [...] }` | heterogeniczny tuple; jednorodne tuple są upraszczane do `T[]` |
| `{ "$map": T }` | obiekt/dictionary o dynamicznych kluczach |
| `{ "$oneOf": [...] }` | union, którego nie można zredukować do enumu |
| `{ "$nullable": true, ... }` | złożony typ może mieć wartość JSON null |
| `"component-config"` | recursive partial config dla właściciela `SetConfig` |
| `"component-status"` | status właściciela `GetStatus` |
| `"rpc-method"` | dowolna metoda z sekcji `rpc` |
| `"rpc-params"` | params zależne od sąsiedniej wartości `rpc-method`, np. Schedule calls |

## NotifyStatus

`components.<component>.notifyStatus` zachowuje zagnieżdżoną strukturę statusu. Generator celowo jej nie spłaszcza.

Optionalność pól nie jest tam zapisywana jako `foo?`, ponieważ cały `NotifyStatus` jest patchem: brak property oznacza brak zmiany. Naturalna nullowalność wartości pochodząca z typu TypeScript nadal jest zachowywana.

Dodatkowo Shelly może wysłać `null` w `NotifyStatus` jako informację o usunięciu/wyczyszczeniu property. To jest reguła protokołu zapisana raz w `semantics.notifyStatus`, a nie dopisywana do każdego pola.

Pole `id` pozostaje częścią modelu statusu nawet jeżeli przyszły parser .NET wyciąga component id wcześniej z klucza `switch:3`.

## RPC

- wszystkie metody są brane z konkretnych `*_rpc_method_map_t`,
- `*.GetConfig` jest pomijane z wyjątkiem `Shelly.GetConfig`,
- namespace RPC powiązany z komponentem ma pole `"$component"`, np. `Switch -> switch` albo `Eth -> eth`,
- `SetConfig.config` jest skracane do `"component-config"`,
- komponentowy `GetStatus.result` jest skracany do `"component-status"`,
- metody bez params nie mają pola `params`,
- opcjonalne top-level params są zapisane jako `"params?"`.

## Rekurencja

Nie ma `$ref`.

Znany rekurencyjny przypadek `Schedule.* -> calls -> params -> wszystkie RPC params` jest reprezentowany domenowymi tokenami `rpc-method` i `rpc-params`. Dla innych nieoczekiwanych cykli generator używa `{ "$recursive": "TypeName" }`, zamiast rozwijać typ w nieskończoność.
