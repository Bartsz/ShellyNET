import ts from 'typescript';

export type CompactPrimitive =
  | 'string'
  | 'number'
  | 'integer'
  | 'bool'
  | 'null'
  | 'unknown'
  | 'never'
  | 'undefined'
  | 'rpc-method'
  | 'rpc-params'
  | 'component-config'
  | 'component-status';

export type CompactType =
  | CompactPrimitive
  | string
  | CompactObject
  | { $enum: Array<string | number | boolean>; $nullable?: true }
  | { $array: CompactType; $nullable?: true }
  | { $tuple: CompactType[]; $nullable?: true }
  | { $map: CompactType; $nullable?: true }
  | { $oneOf: CompactType[]; $nullable?: true }
  | { $recursive: string; $nullable?: true };

export type CompactObject = {
  [property: string]: CompactType | true | undefined;
  $nullable?: true;
};

export type EncodeOptions = {
  /**
   * NotifyStatus is already a partial message by protocol definition, so
   * source-level optional properties do not add useful information there.
   */
  includeOptionalPropertyMarker?: boolean;
};

function isUndefined(type: ts.Type): boolean {
  return (type.flags & ts.TypeFlags.Undefined) !== 0;
}

function isNull(type: ts.Type): boolean {
  return (type.flags & ts.TypeFlags.Null) !== 0;
}

function isBooleanLiteral(type: ts.Type): boolean {
  return (type.flags & ts.TypeFlags.BooleanLiteral) !== 0;
}

function booleanLiteralValue(type: ts.Type): boolean {
  return (type as { intrinsicName?: string }).intrinsicName === 'true';
}

function withNullable(value: CompactType, nullable: boolean): CompactType {
  if (!nullable) return value;

  if (typeof value === 'string') {
    if (value === 'null') return value;
    return value.endsWith('?') ? value : `${value}?`;
  }

  return { ...value, $nullable: true } as CompactType;
}

function withoutUndefined(type: ts.Type): ts.Type[] {
  return type.isUnion() ? type.types.filter((t) => !isUndefined(t)) : [type];
}

function typeIdentity(type: ts.Type): ts.Type {
  return type;
}

export class CompactTypeEncoder {
  private readonly rpcMethodSet: Set<string>;

  constructor(
    private readonly checker: ts.TypeChecker,
    rpcMethods: readonly string[]
  ) {
    this.rpcMethodSet = new Set(rpcMethods);
  }

  encode(
    type: ts.Type,
    location: ts.Node,
    options: EncodeOptions = {}
  ): CompactType {
    return this.encodeInternal(type, location, options, new Set<ts.Type>());
  }

  isOptionalTopLevel(type: ts.Type): boolean {
    return type.isUnion() && type.types.some(isUndefined);
  }

  isNever(type: ts.Type): boolean {
    return (type.flags & ts.TypeFlags.Never) !== 0;
  }

  private typeLabel(type: ts.Type): string {
    return this.checker.typeToString(
      type,
      undefined,
      ts.TypeFormatFlags.NoTruncation |
        ts.TypeFormatFlags.UseAliasDefinedOutsideCurrentScope
    );
  }

  private aliasName(type: ts.Type): string | undefined {
    return type.aliasSymbol?.getName();
  }

  private isRpcMethodUnion(type: ts.Type): boolean {
    if (!type.isUnion() || type.types.length !== this.rpcMethodSet.size) {
      return false;
    }

    for (const member of type.types) {
      if ((member.flags & ts.TypeFlags.StringLiteral) === 0) return false;
      if (!this.rpcMethodSet.has((member as ts.StringLiteralType).value)) {
        return false;
      }
    }

    return true;
  }

  private isRpcParamsType(type: ts.Type, location: ts.Node): boolean {
    const alias = this.aliasName(type);
    if (alias === 'shelly_rpc_method_params_t') return true;

    const label = this.typeLabel(type);
    if (label.includes('shelly_rpc_method_params_t<')) return true;

    // Schedule calls are the known intentionally recursive use of the global
    // RPC params map. Depending on the TS version, the alias can disappear and
    // become a very large union. Recognise that shape without expanding it.
    const locationName = (location as ts.NamedDeclaration).name;
    if (
      locationName &&
      ts.isIdentifier(locationName) &&
      locationName.text === 'params' &&
      type.isUnion() &&
      type.types.length > Math.max(50, this.rpcMethodSet.size / 4)
    ) {
      return true;
    }

    return false;
  }

  private encodeInternal(
    originalType: ts.Type,
    location: ts.Node,
    options: EncodeOptions,
    stack: Set<ts.Type>
  ): CompactType {
    if (this.isRpcParamsType(originalType, location)) return 'rpc-params';
    if (this.isRpcMethodUnion(originalType)) return 'rpc-method';

    // Optionality belongs to the property name. Remove undefined here so that
    // `foo?: number` becomes "foo?": "number" rather than a union.
    const nonUndefined = withoutUndefined(originalType);
    if (nonUndefined.length === 0) return 'undefined';
    let type: ts.Type =
      nonUndefined.length === 1 ? nonUndefined[0] : originalType;

    // Nullability belongs to the value type. Remove null, encode the actual
    // value, then append '?' for simple types or $nullable for compound types.
    const unionMembers = type.isUnion() ? type.types : [type];
    const nullable = unionMembers.some(isNull);
    const nonNull = unionMembers.filter((t) => !isNull(t) && !isUndefined(t));

    if (nullable && nonNull.length === 0) return 'null';
    if (nullable && nonNull.length === 1) {
      const encoded = this.encodeInternal(nonNull[0], location, options, stack);
      return withNullable(encoded, true);
    }

    if ((type.flags & ts.TypeFlags.Never) !== 0) return 'never';
    if (
      (type.flags & ts.TypeFlags.Any) !== 0 ||
      (type.flags & ts.TypeFlags.Unknown) !== 0
    ) {
      return 'unknown';
    }
    if (isNull(type)) return 'null';
    if (isUndefined(type) || (type.flags & ts.TypeFlags.Void) !== 0) {
      return 'undefined';
    }

    if ((type.flags & ts.TypeFlags.StringLiteral) !== 0) {
      return { $enum: [(type as ts.StringLiteralType).value] };
    }
    if ((type.flags & ts.TypeFlags.NumberLiteral) !== 0) {
      return { $enum: [(type as ts.NumberLiteralType).value] };
    }
    if (isBooleanLiteral(type)) {
      return { $enum: [booleanLiteralValue(type)] };
    }

    if ((type.flags & ts.TypeFlags.StringLike) !== 0) return 'string';
    if ((type.flags & ts.TypeFlags.NumberLike) !== 0) return 'number';
    if ((type.flags & ts.TypeFlags.BooleanLike) !== 0) return 'bool';
    if ((type.flags & ts.TypeFlags.BigIntLike) !== 0) return 'integer';
    if ((type.flags & ts.TypeFlags.TemplateLiteral) !== 0) return 'string';

    if (type.isUnion()) {
      const members = type.types.filter((t) => !isUndefined(t) && !isNull(t));

      // true | false is just bool.
      if (members.length === 2 && members.every(isBooleanLiteral)) {
        return withNullable('bool', nullable);
      }

      const allStringLiterals = members.every(
        (m) => (m.flags & ts.TypeFlags.StringLiteral) !== 0
      );
      const allNumberLiterals = members.every(
        (m) => (m.flags & ts.TypeFlags.NumberLiteral) !== 0
      );
      const allBooleanLiterals = members.every(isBooleanLiteral);

      if (allStringLiterals || allNumberLiterals || allBooleanLiterals) {
        const values = members.map((m) => {
          if ((m.flags & ts.TypeFlags.StringLiteral) !== 0) {
            return (m as ts.StringLiteralType).value;
          }
          if ((m.flags & ts.TypeFlags.NumberLiteral) !== 0) {
            return (m as ts.NumberLiteralType).value;
          }
          return booleanLiteralValue(m);
        });
        return withNullable({ $enum: values }, nullable);
      }

      const encoded = members.map((member) =>
        this.encodeInternal(member, location, options, stack)
      );
      return withNullable({ $oneOf: this.deduplicate(encoded) }, nullable);
    }

    // Promise/Awaited is not meaningful on the wire. Unwrap it defensively.
    const awaited = this.checker.getAwaitedType(type);
    if (awaited && awaited !== type) {
      return this.encodeInternal(awaited, location, options, stack);
    }

    if (stack.has(typeIdentity(type))) {
      return { $recursive: this.recursiveName(type) };
    }

    const nextStack = new Set(stack);
    nextStack.add(typeIdentity(type));

    if (this.checker.isTupleType(type)) {
      const args = this.checker.getTypeArguments(type as ts.TypeReference);
      const encoded = args.map((arg) =>
        this.encodeInternal(arg, location, options, nextStack)
      );

      // Homogeneous tuples such as [number, number, number] do not need a
      // dedicated parser shape for the intended C# generator. Collapse them
      // to the compact array notation; keep genuinely heterogeneous tuples.
      if (encoded.length > 0) {
        const first = JSON.stringify(encoded[0]);
        if (encoded.every((item) => JSON.stringify(item) === first)) {
          return this.compactArray(encoded[0]);
        }
      }

      return { $tuple: encoded };
    }

    if (this.checker.isArrayType(type)) {
      const element = this.checker.getIndexTypeOfType(type, ts.IndexKind.Number);
      if (!element) return { $array: 'unknown' };

      const encodedElement = this.encodeInternal(
        element,
        location,
        options,
        nextStack
      );
      return this.compactArray(encodedElement);
    }

    if ((type.flags & ts.TypeFlags.Object) !== 0) {
      return this.encodeObject(type, location, options, nextStack);
    }

    return 'unknown';
  }

  private compactArray(element: CompactType): CompactType {
    if (typeof element === 'string') {
      // Preserve nullable element semantics: number?[] = array of nullable
      // numbers. A nullable array itself would be number[]?.
      return `${element}[]`;
    }
    return { $array: element };
  }

  private encodeObject(
    type: ts.Type,
    location: ts.Node,
    options: EncodeOptions,
    stack: Set<ts.Type>
  ): CompactType {
    const properties = this.checker.getPropertiesOfType(type);
    const stringIndex = this.checker.getIndexTypeOfType(type, ts.IndexKind.String);
    const numberIndex = this.checker.getIndexTypeOfType(type, ts.IndexKind.Number);

    if (properties.length === 0 && stringIndex) {
      return {
        $map: this.encodeInternal(stringIndex, location, options, stack),
      };
    }

    if (properties.length === 0 && numberIndex) {
      return {
        $map: this.encodeInternal(numberIndex, location, options, stack),
      };
    }

    if (properties.length === 0 && type.getCallSignatures().length > 0) {
      return 'unknown';
    }

    const result: CompactObject = {};

    // Schedule call objects are typed as { method: K; params:
    // shelly_rpc_method_params_t<K> }. Once K is the global method union,
    // expanding params would inline hundreds of RPC parameter shapes and
    // recreate the Schedule recursion. Model that relationship explicitly.
    const methodProperty = properties.find((p) => p.getName() === 'method');
    let isRpcCallObject = false;
    if (methodProperty) {
      const methodDeclaration =
        methodProperty.valueDeclaration ?? methodProperty.declarations?.[0];
      const methodType = this.checker.getTypeOfSymbolAtLocation(
        methodProperty,
        methodDeclaration ?? location
      );
      isRpcCallObject = this.isRpcMethodUnion(methodType);
    }

    for (const property of properties) {
      const declaration = property.valueDeclaration ?? property.declarations?.[0];
      const propertyLocation = declaration ?? location;
      const propertyType = this.checker.getTypeOfSymbolAtLocation(
        property,
        propertyLocation
      );

      if (isRpcCallObject && property.getName() === 'params') {
        result.params = 'rpc-params';
        continue;
      }

      const optionalBySymbol = (property.flags & ts.SymbolFlags.Optional) !== 0;
      const optionalByUndefined =
        propertyType.isUnion() && propertyType.types.some(isUndefined);
      const isOptional = optionalBySymbol || optionalByUndefined;

      const key =
        options.includeOptionalPropertyMarker !== false && isOptional
          ? `${property.getName()}?`
          : property.getName();

      result[key] = this.encodeInternal(
        propertyType,
        propertyLocation,
        options,
        stack
      );
    }

    if (stringIndex) {
      result.$map = this.encodeInternal(stringIndex, location, options, stack);
    } else if (numberIndex) {
      result.$map = this.encodeInternal(numberIndex, location, options, stack);
    }

    return result;
  }

  private recursiveName(type: ts.Type): string {
    const alias = this.aliasName(type);
    if (alias && alias !== '__type') return alias;
    const symbol = type.getSymbol()?.getName();
    if (symbol && symbol !== '__type') return symbol;
    return this.typeLabel(type);
  }

  private deduplicate(values: CompactType[]): CompactType[] {
    const seen = new Set<string>();
    const result: CompactType[] = [];

    for (const value of values) {
      const key = JSON.stringify(value);
      if (seen.has(key)) continue;
      seen.add(key);
      result.push(value);
    }

    return result;
  }
}
