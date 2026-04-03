import { describe, it, expect } from 'vitest'
import { AppError, NotFoundException, ValidationError } from './errors'

describe('AppError', () => {
  it('sets message and code', () => {
    const error = new AppError('bad request', 'BAD_REQUEST')

    expect(error.message).toBe('bad request')
    expect(error.code).toBe('BAD_REQUEST')
    expect(error.name).toBe('AppError')
  })

  it('is an instance of Error', () => {
    const error = new AppError('test', 'TEST')

    expect(error).toBeInstanceOf(Error)
    expect(error).toBeInstanceOf(AppError)
  })
})

describe('NotFoundException', () => {
  it('sets code to NOT_FOUND', () => {
    const error = new NotFoundException('User not found')

    expect(error.message).toBe('User not found')
    expect(error.code).toBe('NOT_FOUND')
  })

  it('is an instance of AppError', () => {
    const error = new NotFoundException('missing')

    expect(error).toBeInstanceOf(AppError)
    expect(error).toBeInstanceOf(Error)
  })
})

describe('ValidationError', () => {
  it('sets code to VALIDATION_ERROR', () => {
    const error = new ValidationError('Invalid input')

    expect(error.message).toBe('Invalid input')
    expect(error.code).toBe('VALIDATION_ERROR')
  })

  it('is an instance of AppError', () => {
    const error = new ValidationError('invalid')

    expect(error).toBeInstanceOf(AppError)
    expect(error).toBeInstanceOf(Error)
  })
})
