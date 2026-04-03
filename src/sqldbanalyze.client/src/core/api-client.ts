import { AppError } from '../domain/errors'

export class ApiClient {
  constructor(private readonly baseUrl: string) {}

  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`)
    if (!res.ok) throw new AppError(`API error ${res.status}`, 'API_ERROR')
    return await res.json() as T
  }

  async post<T>(path: string, body?: unknown): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: body !== undefined ? { 'Content-Type': 'application/json' } : {},
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
    if (!res.ok) throw new AppError(`API error ${res.status}`, 'API_ERROR')
    return await res.json() as T
  }

  async delete(path: string): Promise<void> {
    const res = await fetch(`${this.baseUrl}${path}`, { method: 'DELETE' })
    if (!res.ok) throw new AppError(`API error ${res.status}`, 'API_ERROR')
  }
}
