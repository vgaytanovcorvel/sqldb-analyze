import { describe, it, expect, vi } from 'vitest'
import { RegisteredServerRepository } from './registered-server-repository'
import type { ApiClient } from '../core/api-client'

function createMockApiClient(): ApiClient {
  return {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  } as unknown as ApiClient
}

describe('RegisteredServerRepository', () => {
  describe('registeredServerFindAll', () => {
    it('calls GET /registered-servers', async () => {
      const http = createMockApiClient()
      const servers = [{ registeredServerId: 1, name: 'test' }]
      vi.mocked(http.get).mockResolvedValue(servers)
      const repo = new RegisteredServerRepository(http)

      const result = await repo.registeredServerFindAll()

      expect(http.get).toHaveBeenCalledWith('/registered-servers')
      expect(result).toEqual(servers)
    })
  })

  describe('registeredServerCreate', () => {
    it('calls POST /registered-servers with request body', async () => {
      const http = createMockApiClient()
      const request = { name: 'test', subscriptionId: 's1', resourceGroupName: 'rg1', serverName: 'srv1' }
      const response = { registeredServerId: 1, ...request, createdAt: '2024-01-01' }
      vi.mocked(http.post).mockResolvedValue(response)
      const repo = new RegisteredServerRepository(http)

      const result = await repo.registeredServerCreate(request)

      expect(http.post).toHaveBeenCalledWith('/registered-servers', request)
      expect(result).toEqual(response)
    })
  })

  describe('registeredServerDelete', () => {
    it('calls DELETE /registered-servers/:id', async () => {
      const http = createMockApiClient()
      vi.mocked(http.delete).mockResolvedValue(undefined)
      const repo = new RegisteredServerRepository(http)

      await repo.registeredServerDelete(5)

      expect(http.delete).toHaveBeenCalledWith('/registered-servers/5')
    })
  })
})
