import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ApiClient } from './api-client'
import { AppError } from '../domain/errors'

function mockFetch(response: { ok: boolean; status: number; json?: unknown }) {
  return vi.fn().mockResolvedValue({
    ok: response.ok,
    status: response.status,
    json: vi.fn().mockResolvedValue(response.json),
  })
}

describe('ApiClient', () => {
  let client: ApiClient

  beforeEach(() => {
    client = new ApiClient('http://localhost:5000/api')
    vi.restoreAllMocks()
  })

  describe('get', () => {
    it('sends GET request to correct URL and returns parsed JSON', async () => {
      const data = { id: 1, name: 'test' }
      globalThis.fetch = mockFetch({ ok: true, status: 200, json: data })

      const result = await client.get<typeof data>('/items/1')

      expect(globalThis.fetch).toHaveBeenCalledWith('http://localhost:5000/api/items/1')
      expect(result).toEqual(data)
    })

    it('throws AppError on non-OK response', async () => {
      globalThis.fetch = mockFetch({ ok: false, status: 404 })

      await expect(client.get('/items/999')).rejects.toThrow(AppError)
      await expect(client.get('/items/999')).rejects.toThrow('API error 404')
    })
  })

  describe('post', () => {
    it('sends POST request with JSON body and returns parsed JSON', async () => {
      const body = { name: 'new item' }
      const response = { id: 1, name: 'new item' }
      globalThis.fetch = mockFetch({ ok: true, status: 201, json: response })

      const result = await client.post<typeof response>('/items', body)

      expect(globalThis.fetch).toHaveBeenCalledWith('http://localhost:5000/api/items', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      expect(result).toEqual(response)
    })

    it('sends POST without body when body is undefined', async () => {
      const response = { status: 'ok' }
      globalThis.fetch = mockFetch({ ok: true, status: 200, json: response })

      await client.post('/trigger')

      expect(globalThis.fetch).toHaveBeenCalledWith('http://localhost:5000/api/trigger', {
        method: 'POST',
        headers: {},
        body: undefined,
      })
    })

    it('throws AppError on non-OK response', async () => {
      globalThis.fetch = mockFetch({ ok: false, status: 500 })

      await expect(client.post('/items', {})).rejects.toThrow(AppError)
    })
  })

  describe('delete', () => {
    it('sends DELETE request to correct URL', async () => {
      globalThis.fetch = mockFetch({ ok: true, status: 204 })

      await client.delete('/items/1')

      expect(globalThis.fetch).toHaveBeenCalledWith('http://localhost:5000/api/items/1', {
        method: 'DELETE',
      })
    })

    it('throws AppError on non-OK response', async () => {
      globalThis.fetch = mockFetch({ ok: false, status: 403 })

      await expect(client.delete('/items/1')).rejects.toThrow(AppError)
    })
  })
})
