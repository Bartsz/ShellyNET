function isPrimitive(value: unknown): boolean {
  return value === null || ['string', 'number', 'boolean'].includes(typeof value);
}

function canInlineArray(value: unknown[]): boolean {
  return value.every(isPrimitive);
}

function canInlineDescriptor(value: Record<string, unknown>): boolean {
  const keys = Object.keys(value);
  if (keys.length === 0) return true;

  if (keys.every((k) => k === '$enum' || k === '$nullable')) {
    return Array.isArray(value.$enum) && canInlineArray(value.$enum);
  }

  return false;
}

function render(value: unknown, level: number, indent: number): string {
  const pad = ' '.repeat(level * indent);
  const childPad = ' '.repeat((level + 1) * indent);

  if (value === null || typeof value !== 'object') {
    return JSON.stringify(value);
  }

  if (Array.isArray(value)) {
    if (value.length === 0) return '[]';
    if (canInlineArray(value)) return JSON.stringify(value);

    return `[` +
      `\n${childPad}${value.map((item) => render(item, level + 1, indent)).join(`,\n${childPad}`)}` +
      `\n${pad}]`;
  }

  const object = value as Record<string, unknown>;
  const entries = Object.entries(object);
  if (entries.length === 0) return '{}';

  if (canInlineDescriptor(object)) {
    return `{ ${entries
      .map(([key, item]) => `${JSON.stringify(key)}: ${render(item, level, indent)}`)
      .join(', ')} }`;
  }

  const body = entries
    .map(([key, item]) => `${childPad}${JSON.stringify(key)}: ${render(item, level + 1, indent)}`)
    .join(',\n');

  return `{\n${body}\n${pad}}`;
}

export function stringifyCompactJson(value: unknown, indent = 2): string {
  return `${render(value, 0, indent)}\n`;
}
