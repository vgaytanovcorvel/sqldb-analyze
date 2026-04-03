import { describe, it, expect } from 'vitest'
import { computeRescaleRecommendations } from './rescale'
import type { DtuTimeSeries, DatabaseInfo } from './models'
import type { RescaleOptions } from './rescale'

function makeDatabases(entries: Array<{ name: string; dtuLimit: number }>): DatabaseInfo[] {
  return entries.map((e) => ({
    databaseName: e.name,
    dataSizeMB: 100,
    dtuLimit: e.dtuLimit,
    elasticPoolName: null,
  }))
}

function makeTimeSeries(data: Record<string, number[]>): DtuTimeSeries {
  const firstKey = Object.keys(data)[0]
  const length = firstKey ? data[firstKey]!.length : 0
  return {
    timestamps: Array.from({ length }, (_, i) => `2024-01-01T${i.toString().padStart(2, '0')}:00:00Z`),
    databaseValues: data,
  }
}

const defaultOptions: RescaleOptions = {
  targetPercentile: 0.99,
  safetyFactor: 1.10,
  tier: 'standard',
}

describe('computeRescaleRecommendations', () => {
  it('returns empty recommendations when no databases match', () => {
    const result = computeRescaleRecommendations(
      ['nonexistent'],
      [],
      makeTimeSeries({}),
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(0)
    expect(result.totalCurrentCost).toBe(0)
    expect(result.totalRecommendedCost).toBe(0)
  })

  it('filters out databases without time series data', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 100 }])
    const result = computeRescaleRecommendations(
      ['db1'],
      databases,
      makeTimeSeries({}),
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(0)
  })

  it('recommends downgrade for underutilized database', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 100 }])
    // All values are low percentages (5% of 100 DTU = 5 DTU actual usage)
    const timeSeries = makeTimeSeries({
      db1: Array(100).fill(5),
    })

    const result = computeRescaleRecommendations(
      ['db1'],
      databases,
      timeSeries,
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(1)
    expect(result.recommendations[0]!.direction).toBe('downgrade')
    expect(result.recommendations[0]!.recommendedTier.dtu).toBeLessThan(100)
    expect(result.downgradeCount).toBe(1)
    expect(result.upgradeCount).toBe(0)
  })

  it('recommends upgrade for heavily utilized database', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 5 }])
    // 100% usage on a 5 DTU DB means 5 DTU actual, * 1.10 safety = 5.5, snaps to 10
    const timeSeries = makeTimeSeries({
      db1: Array(100).fill(100),
    })

    const result = computeRescaleRecommendations(
      ['db1'],
      databases,
      timeSeries,
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(1)
    expect(result.recommendations[0]!.direction).toBe('upgrade')
    expect(result.upgradeCount).toBe(1)
  })

  it('recommends keep when usage matches current tier', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 50 }])
    // ~40% usage of 50 DTU = 20 actual. * 1.10 = 22, snaps to 50
    const timeSeries = makeTimeSeries({
      db1: Array(100).fill(40),
    })

    const result = computeRescaleRecommendations(
      ['db1'],
      databases,
      timeSeries,
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(1)
    expect(result.recommendations[0]!.direction).toBe('keep')
    expect(result.keepCount).toBe(1)
  })

  it('computes correct total savings', () => {
    const databases = makeDatabases([
      { name: 'db1', dtuLimit: 100 },
      { name: 'db2', dtuLimit: 100 },
    ])
    const timeSeries = makeTimeSeries({
      db1: Array(100).fill(5),
      db2: Array(100).fill(5),
    })

    const result = computeRescaleRecommendations(
      ['db1', 'db2'],
      databases,
      timeSeries,
      defaultOptions,
    )

    expect(result.totalSavingsDollars).toBe(result.totalCurrentCost - result.totalRecommendedCost)
    if (result.totalCurrentCost > 0) {
      const expectedPct = (result.totalSavingsDollars / result.totalCurrentCost) * 100
      expect(result.totalSavingsPercent).toBeCloseTo(expectedPct, 5)
    }
  })

  it('uses p95 when targetPercentile is below 0.99', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 100 }])
    // Create data with a spike at end so p99 and p95 differ
    const values = [...Array(95).fill(10), ...Array(5).fill(90)]
    const timeSeries = makeTimeSeries({ db1: values })

    const resultP99 = computeRescaleRecommendations(
      ['db1'], databases, timeSeries, { ...defaultOptions, targetPercentile: 0.99 },
    )
    const resultP95 = computeRescaleRecommendations(
      ['db1'], databases, timeSeries, { ...defaultOptions, targetPercentile: 0.95 },
    )

    expect(resultP95.recommendations[0]!.recommendedDtu)
      .toBeLessThanOrEqual(resultP99.recommendations[0]!.recommendedDtu)
  })

  it('handles empty values array for a database', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 100 }])
    const timeSeries = makeTimeSeries({ db1: [] })

    const result = computeRescaleRecommendations(
      ['db1'],
      databases,
      timeSeries,
      defaultOptions,
    )

    expect(result.recommendations).toHaveLength(1)
    expect(result.recommendations[0]!.p95Dtu).toBe(0)
    expect(result.recommendations[0]!.meanDtu).toBe(0)
  })

  it('applies safety factor correctly', () => {
    const databases = makeDatabases([{ name: 'db1', dtuLimit: 200 }])
    const timeSeries = makeTimeSeries({ db1: Array(100).fill(50) })

    const noSafety = computeRescaleRecommendations(
      ['db1'], databases, timeSeries, { ...defaultOptions, safetyFactor: 1.0 },
    )
    const withSafety = computeRescaleRecommendations(
      ['db1'], databases, timeSeries, { ...defaultOptions, safetyFactor: 1.50 },
    )

    expect(withSafety.recommendations[0]!.recommendedDtu)
      .toBeGreaterThanOrEqual(noSafety.recommendations[0]!.recommendedDtu)
  })
})
