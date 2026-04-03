import { describe, it, expect, vi, beforeEach } from 'vitest'
import { printPoolReport } from './print-pool-report'
import type { PoolAssignment } from '../../domain/models'

const pool: PoolAssignment = {
  poolIndex: 0,
  databaseNames: ['db1', 'db2'],
  recommendedCapacity: 150,
  p95Load: 80,
  p99Load: 120,
  peakLoad: 200,
  diversificationRatio: 1.5,
  overloadFraction: 0.01,
}

const reportData = {
  serverName: 'test-server',
  poolTier: 'standard' as const,
  targetPercentile: 0.99,
  safetyFactor: 1.10,
  maxDatabasesPerPool: 50,
  pools: [pool],
  standaloneDatabaseNames: ['db3'],
  dtuLimits: { db1: 100, db2: 50, db3: 200 } as Record<string, number>,
  summary: {
    totalPoolCost: 500,
    totalStandaloneCost: 300,
    totalIndividualCost: 1000,
    savingsDollars: 200,
    savingsPercent: 20,
  },
}

describe('printPoolReport', () => {
  let mockWrite: ReturnType<typeof vi.fn>
  let mockClose: ReturnType<typeof vi.fn>

  beforeEach(() => {
    mockWrite = vi.fn()
    mockClose = vi.fn()
    vi.stubGlobal('open', vi.fn().mockReturnValue({
      document: { write: mockWrite, close: mockClose },
    }))
  })

  it('opens a new window and writes HTML', () => {
    printPoolReport(reportData)

    expect(window.open).toHaveBeenCalledWith('', '_blank')
    expect(mockWrite).toHaveBeenCalledOnce()
    expect(mockClose).toHaveBeenCalledOnce()
  })

  it('writes HTML containing the server name', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('test-server')
  })

  it('writes HTML containing pool information', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('Pool 1')
    expect(html).toContain('db1')
    expect(html).toContain('db2')
  })

  it('writes HTML containing standalone databases', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('Standalone Databases')
    expect(html).toContain('db3')
  })

  it('writes HTML containing the tier label', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('Standard')
  })

  it('writes HTML with premium tier label', () => {
    printPoolReport({ ...reportData, poolTier: 'premium' })

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('Premium')
  })

  it('does not write when window.open returns null', () => {
    vi.stubGlobal('open', vi.fn().mockReturnValue(null))

    printPoolReport(reportData)

    expect(mockWrite).not.toHaveBeenCalled()
  })

  it('omits standalone section when there are no standalone databases', () => {
    printPoolReport({ ...reportData, standaloneDatabaseNames: [] })

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).not.toContain('Standalone Databases')
  })

  it('shows P99 label when targetPercentile is 0.99', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('P99')
  })

  it('shows P95 label when targetPercentile is below 0.99', () => {
    printPoolReport({ ...reportData, targetPercentile: 0.95 })

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('P95')
  })

  it('includes savings summary in the HTML', () => {
    printPoolReport(reportData)

    const html = mockWrite.mock.calls[0]![0] as string
    expect(html).toContain('20.0%')
    expect(html).toContain('$200')
  })
})
