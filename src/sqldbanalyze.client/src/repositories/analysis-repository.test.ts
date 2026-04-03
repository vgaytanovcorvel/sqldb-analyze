import { describe, it, expect, vi } from 'vitest'
import { AnalysisRepository } from './analysis-repository'
import type { ApiClient } from '../core/api-client'

function createMockApiClient(): ApiClient {
  return {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  } as unknown as ApiClient
}

describe('AnalysisRepository', () => {
  describe('analysisFetchDatabases', () => {
    it('calls GET /analysis/:serverId/databases', async () => {
      const http = createMockApiClient()
      const dbs = [{ databaseName: 'db1', dataSizeMB: 100, dtuLimit: 50, elasticPoolName: null }]
      vi.mocked(http.get).mockResolvedValue(dbs)
      const repo = new AnalysisRepository(http)

      const result = await repo.analysisFetchDatabases(3)

      expect(http.get).toHaveBeenCalledWith('/analysis/3/databases')
      expect(result).toEqual(dbs)
    })
  })

  describe('analysisFetchIntervals', () => {
    it('calls GET /analysis/:serverId/intervals', async () => {
      const http = createMockApiClient()
      vi.mocked(http.get).mockResolvedValue([])
      const repo = new AnalysisRepository(http)

      await repo.analysisFetchIntervals(7)

      expect(http.get).toHaveBeenCalledWith('/analysis/7/intervals')
    })
  })

  describe('analysisRefreshMetrics', () => {
    it('calls POST /analysis/:serverId/refresh with hours query param', async () => {
      const http = createMockApiClient()
      const timeSeries = { timestamps: [], databaseValues: {} }
      vi.mocked(http.post).mockResolvedValue(timeSeries)
      const repo = new AnalysisRepository(http)

      const result = await repo.analysisRefreshMetrics(2, 24)

      expect(http.post).toHaveBeenCalledWith('/analysis/2/refresh?hours=24')
      expect(result).toEqual(timeSeries)
    })
  })

  describe('analysisFetchTimeSeries', () => {
    it('calls GET /analysis/:serverId/time-series', async () => {
      const http = createMockApiClient()
      vi.mocked(http.get).mockResolvedValue({ timestamps: [], databaseValues: {} })
      const repo = new AnalysisRepository(http)

      await repo.analysisFetchTimeSeries(1)

      expect(http.get).toHaveBeenCalledWith('/analysis/1/time-series')
    })
  })

  describe('analysisFetchCorrelationMatrix', () => {
    it('calls GET /analysis/:serverId/correlation-matrix', async () => {
      const http = createMockApiClient()
      vi.mocked(http.get).mockResolvedValue([])
      const repo = new AnalysisRepository(http)

      await repo.analysisFetchCorrelationMatrix(4)

      expect(http.get).toHaveBeenCalledWith('/analysis/4/correlation-matrix')
    })
  })

  describe('analysisSimulatePool', () => {
    it('calls POST /analysis/:serverId/simulate-pool with request body', async () => {
      const http = createMockApiClient()
      const request = { databaseNames: ['db1'], dtuLimits: { db1: 100 } }
      const response = { databaseNames: ['db1'], p95Dtu: 50, p99Dtu: 60 }
      vi.mocked(http.post).mockResolvedValue(response)
      const repo = new AnalysisRepository(http)

      const result = await repo.analysisSimulatePool(1, request)

      expect(http.post).toHaveBeenCalledWith('/analysis/1/simulate-pool', request)
      expect(result).toEqual(response)
    })
  })

  describe('analysisBuildPools', () => {
    it('calls POST /analysis/:serverId/build-pools with request body', async () => {
      const http = createMockApiClient()
      const request = { databaseNames: ['db1'], dtuLimits: { db1: 100 } }
      const response = { pools: [], totalRequiredCapacity: 0, isolatedDatabases: [] }
      vi.mocked(http.post).mockResolvedValue(response)
      const repo = new AnalysisRepository(http)

      const result = await repo.analysisBuildPools(2, request)

      expect(http.post).toHaveBeenCalledWith('/analysis/2/build-pools', request)
      expect(result).toEqual(response)
    })
  })
})
