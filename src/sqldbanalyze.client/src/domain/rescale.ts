import type { DtuTimeSeries, DatabaseInfo } from './models'
import type { PoolTier, SingleDbTierOption } from './azure-pricing'
import { snapToSingleDbTier, getSingleDbMonthlyCost } from './azure-pricing'

export type RescaleDirection = 'downgrade' | 'keep' | 'upgrade'

export interface RescaleRecommendation {
  readonly databaseName: string
  readonly currentDtuLimit: number
  readonly currentMonthlyCost: number
  readonly p95Dtu: number
  readonly p99Dtu: number
  readonly peakDtu: number
  readonly meanDtu: number
  readonly recommendedDtu: number
  readonly recommendedTier: SingleDbTierOption
  readonly recommendedMonthlyCost: number
  readonly savingsDollars: number
  readonly savingsPercent: number
  readonly direction: RescaleDirection
}

export interface RescaleResult {
  readonly recommendations: readonly RescaleRecommendation[]
  readonly totalCurrentCost: number
  readonly totalRecommendedCost: number
  readonly totalSavingsDollars: number
  readonly totalSavingsPercent: number
  readonly upgradeCount: number
  readonly downgradeCount: number
  readonly keepCount: number
}

export interface RescaleOptions {
  readonly targetPercentile: number
  readonly safetyFactor: number
  readonly tier: PoolTier
}

function computePercentile(sorted: readonly number[], p: number): number {
  if (sorted.length === 0) return 0
  const idx = p * (sorted.length - 1)
  const lo = Math.floor(idx)
  const hi = Math.ceil(idx)
  if (lo === hi) return sorted[lo]!
  const frac = idx - lo
  return sorted[lo]! * (1 - frac) + sorted[hi]! * frac
}

function computeDbStats(
  values: readonly number[],
  dtuLimit: number,
): { p95: number; p99: number; peak: number; mean: number } {
  if (values.length === 0) return { p95: 0, p99: 0, peak: 0, mean: 0 }

  const absolute = values.map((pct) => (pct / 100) * dtuLimit)
  const sorted = [...absolute].sort((a, b) => a - b)

  return {
    p95: computePercentile(sorted, 0.95),
    p99: computePercentile(sorted, 0.99),
    peak: sorted[sorted.length - 1]!,
    mean: absolute.reduce((a, b) => a + b, 0) / absolute.length,
  }
}

function classifyDirection(currentDtu: number, recommendedDtu: number): RescaleDirection {
  if (recommendedDtu < currentDtu) return 'downgrade'
  if (recommendedDtu > currentDtu) return 'upgrade'
  return 'keep'
}

export function computeRescaleRecommendations(
  databaseNames: readonly string[],
  databases: readonly DatabaseInfo[],
  timeSeries: DtuTimeSeries,
  options: RescaleOptions,
): RescaleResult {
  const dbMap = new Map(databases.map((db) => [db.databaseName, db]))

  const recommendations: RescaleRecommendation[] = databaseNames
    .filter((name) => dbMap.has(name) && timeSeries.databaseValues[name])
    .map((name) => {
      const db = dbMap.get(name)!
      const values = timeSeries.databaseValues[name]!
      const stats = computeDbStats(values, db.dtuLimit)

      const targetLoad = options.targetPercentile >= 0.99 ? stats.p99 : stats.p95
      const recommendedDtu = targetLoad * options.safetyFactor
      const recommendedTier = snapToSingleDbTier(recommendedDtu, options.tier)

      const currentCost = getSingleDbMonthlyCost(db.dtuLimit, options.tier)
      const recommendedCost = recommendedTier.monthlyPrice
      const savingsDollars = currentCost - recommendedCost
      const savingsPercent = currentCost > 0 ? (savingsDollars / currentCost) * 100 : 0

      return {
        databaseName: name,
        currentDtuLimit: db.dtuLimit,
        currentMonthlyCost: currentCost,
        p95Dtu: stats.p95,
        p99Dtu: stats.p99,
        peakDtu: stats.peak,
        meanDtu: stats.mean,
        recommendedDtu,
        recommendedTier,
        recommendedMonthlyCost: recommendedCost,
        savingsDollars,
        savingsPercent,
        direction: classifyDirection(db.dtuLimit, recommendedTier.dtu),
      }
    })

  const totalCurrentCost = recommendations.reduce((s, r) => s + r.currentMonthlyCost, 0)
  const totalRecommendedCost = recommendations.reduce((s, r) => s + r.recommendedMonthlyCost, 0)
  const totalSavingsDollars = totalCurrentCost - totalRecommendedCost
  const totalSavingsPercent = totalCurrentCost > 0
    ? (totalSavingsDollars / totalCurrentCost) * 100
    : 0

  return {
    recommendations,
    totalCurrentCost,
    totalRecommendedCost,
    totalSavingsDollars,
    totalSavingsPercent,
    upgradeCount: recommendations.filter((r) => r.direction === 'upgrade').length,
    downgradeCount: recommendations.filter((r) => r.direction === 'downgrade').length,
    keepCount: recommendations.filter((r) => r.direction === 'keep').length,
  }
}
