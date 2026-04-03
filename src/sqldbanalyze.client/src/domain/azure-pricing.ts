export type PoolTier = 'standard' | 'premium'

export interface PoolTierOption {
  readonly eDtu: number
  readonly monthlyPrice: number
}

export interface SingleDbTierOption {
  readonly dtu: number
  readonly monthlyPrice: number
}

// Standard elastic pool eDTU sizes and monthly costs (US East, as of 2025)
const STANDARD_POOL_TIERS: readonly PoolTierOption[] = [
  { eDtu: 50, monthlyPrice: 110.27 },
  { eDtu: 100, monthlyPrice: 220.53 },
  { eDtu: 200, monthlyPrice: 441.05 },
  { eDtu: 300, monthlyPrice: 661.57 },
  { eDtu: 400, monthlyPrice: 882.09 },
  { eDtu: 800, monthlyPrice: 1764.17 },
  { eDtu: 1200, monthlyPrice: 2646.25 },
  { eDtu: 1600, monthlyPrice: 3528.34 },
  { eDtu: 2000, monthlyPrice: 4410.42 },
  { eDtu: 2500, monthlyPrice: 5513.03 },
  { eDtu: 3000, monthlyPrice: 6615.63 },
]

// Premium elastic pool eDTU sizes and monthly costs (US East, as of 2025)
const PREMIUM_POOL_TIERS: readonly PoolTierOption[] = [
  { eDtu: 125, monthlyPrice: 684.38 },
  { eDtu: 250, monthlyPrice: 1368.75 },
  { eDtu: 500, monthlyPrice: 2737.50 },
  { eDtu: 1000, monthlyPrice: 5475.00 },
  { eDtu: 1500, monthlyPrice: 8212.51 },
  { eDtu: 2000, monthlyPrice: 10950.00 },
  { eDtu: 2500, monthlyPrice: 13687.50 },
  { eDtu: 3000, monthlyPrice: 16425.00 },
  { eDtu: 3500, monthlyPrice: 19162.50 },
  { eDtu: 4000, monthlyPrice: 21900.00 },
]

// Standard single database DTU tiers and monthly costs (US East, approximate)
const STANDARD_SINGLE_DB_TIERS: readonly SingleDbTierOption[] = [
  { dtu: 5, monthlyPrice: 4.90 },
  { dtu: 10, monthlyPrice: 14.72 },
  { dtu: 20, monthlyPrice: 29.44 },
  { dtu: 50, monthlyPrice: 73.60 },
  { dtu: 100, monthlyPrice: 147.19 },
  { dtu: 200, monthlyPrice: 294.38 },
  { dtu: 400, monthlyPrice: 588.77 },
  { dtu: 800, monthlyPrice: 1177.54 },
  { dtu: 1600, monthlyPrice: 2355.07 },
  { dtu: 3000, monthlyPrice: 4415.76 },
]

// Premium single database DTU tiers and monthly costs (US East, approximate)
const PREMIUM_SINGLE_DB_TIERS: readonly SingleDbTierOption[] = [
  { dtu: 125, monthlyPrice: 456.25 },
  { dtu: 250, monthlyPrice: 912.50 },
  { dtu: 500, monthlyPrice: 1825.00 },
  { dtu: 1000, monthlyPrice: 3650.00 },
  { dtu: 1750, monthlyPrice: 6387.50 },
  { dtu: 4000, monthlyPrice: 14600.00 },
]

const POOL_TIERS_BY_TIER: Record<PoolTier, readonly PoolTierOption[]> = {
  standard: STANDARD_POOL_TIERS,
  premium: PREMIUM_POOL_TIERS,
}

const SINGLE_DB_TIERS_BY_TIER: Record<PoolTier, readonly SingleDbTierOption[]> = {
  standard: STANDARD_SINGLE_DB_TIERS,
  premium: PREMIUM_SINGLE_DB_TIERS,
}

export function getPoolTiers(tier: PoolTier): readonly PoolTierOption[] {
  return POOL_TIERS_BY_TIER[tier]
}

export function snapToPoolTier(rawDtu: number, tier: PoolTier): PoolTierOption {
  const tiers = POOL_TIERS_BY_TIER[tier]
  for (const t of tiers) {
    if (t.eDtu >= rawDtu) return t
  }
  return tiers[tiers.length - 1]!
}

export function getSingleDbMonthlyCost(dtuLimit: number, tier: PoolTier): number {
  const tiers = SINGLE_DB_TIERS_BY_TIER[tier]
  for (const t of tiers) {
    if (t.dtu >= dtuLimit) return t.monthlyPrice
  }
  return tiers[tiers.length - 1]!.monthlyPrice
}

export function getSingleDbTiers(tier: PoolTier): readonly SingleDbTierOption[] {
  return SINGLE_DB_TIERS_BY_TIER[tier]
}

export function snapToSingleDbTier(rawDtu: number, tier: PoolTier): SingleDbTierOption {
  const tiers = SINGLE_DB_TIERS_BY_TIER[tier]
  for (const t of tiers) {
    if (t.dtu >= rawDtu) return t
  }
  return tiers[tiers.length - 1]!
}
