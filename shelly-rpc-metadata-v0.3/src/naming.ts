export function identifierPart(value: string): string {
  const sanitized = value.replace(/[^A-Za-z0-9_]/g, '_');
  return /^[0-9]/.test(sanitized) ? `_${sanitized}` : sanitized;
}

export function methodAliasBase(method: string): string {
  return `Rpc_${method.split('.').map(identifierPart).join('_')}`;
}

export function componentAliasBase(component: string): string {
  return `Component_${identifierPart(component)}`;
}

export function schemaRef(typeName: string): string {
  return `#/definitions/${encodeURIComponent(typeName)}`;
}
