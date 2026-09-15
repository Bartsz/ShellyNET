import type {
  shelly_component_type_t,
  shelly_rpc_method_t,
} from '@taulfsime/shelly-rpc-ts';

/**
 * This file exists only to make TypeScript resolve and load the declaration
 * graph of @taulfsime/shelly-rpc-ts.
 *
 * Discovery does not inspect these aliases as literal unions. In particular,
 * it is valid for the Compiler API to represent one of them as broad `string`.
 */
export type __ShellyRpcMethods = shelly_rpc_method_t;
export type __ShellyComponentTypes = shelly_component_type_t;
