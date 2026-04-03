import { describe, it, expect } from 'vitest'
import { Result } from './result'

describe('Result', () => {
  describe('ok', () => {
    it('creates a success result with the given value', () => {
      const result = Result.ok(42)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.value).toBe(42)
      }
    })

    it('creates a success result with a complex value', () => {
      const value = { name: 'test', count: 5 }
      const result = Result.ok(value)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.value).toEqual(value)
      }
    })

    it('creates a success result with null', () => {
      const result = Result.ok(null)

      expect(result.success).toBe(true)
      if (result.success) {
        expect(result.value).toBeNull()
      }
    })
  })

  describe('fail', () => {
    it('creates a failure result with the given error message', () => {
      const result = Result.fail('something went wrong')

      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.error).toBe('something went wrong')
      }
    })

    it('creates a failure result with an empty string', () => {
      const result = Result.fail('')

      expect(result.success).toBe(false)
      if (!result.success) {
        expect(result.error).toBe('')
      }
    })
  })
})
