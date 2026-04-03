import { AppError } from '../domain/errors'

export class ApiClient {
  constructor(private readonly baseUrl: string) {}

  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`)
    if (!res.ok) throw new AppError(`API error ${res.status}`, 'API_ERROR')
    return await res.json() as T
  }

  async post<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (!res.ok) throw new AppError(`API error ${res.status}`, 'API_ERROR')
    return await res.json() as T
  }
}
