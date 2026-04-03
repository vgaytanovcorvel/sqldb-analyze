export class AppError extends Error {
  constructor(message: string, public readonly code: string) {
    super(message)
    this.name = 'AppError'
  }
}

export class NotFoundException extends AppError {
  constructor(message: string) { super(message, 'NOT_FOUND') }
}

export class ValidationError extends AppError {
  constructor(message: string) { super(message, 'VALIDATION_ERROR') }
}
