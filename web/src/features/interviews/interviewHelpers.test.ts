import { describe, it, expect } from 'vitest'
import { toCount } from './interviewHelpers'

describe('toCount', () => {
  it('returns a number as-is', () => {
    expect(toCount(7)).toBe(7)
  })

  it('parses a numeric string', () => {
    expect(toCount('3')).toBe(3)
  })

  it('returns 0 for a non-numeric string', () => {
    expect(toCount('abc')).toBe(0)
  })
})
