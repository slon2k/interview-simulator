// The .NET OpenAPI generator emits Int32 as `number | string`, so this conversion
// is a real case, not dead defensive code.
export function toCount(value: number | string): number {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) ? parsed : 0
}
