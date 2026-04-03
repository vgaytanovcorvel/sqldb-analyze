import { describe, it, expect } from 'vitest'
import {
  getPoolTiers,
  snapToPoolTier,
  getSingleDbMonthlyCost,
  getSingleDbTiers,
  snapToSingleDbTier,
} from './azure-pricing'

describe('getPoolTiers', () => {
  it('returns standard pool tiers', () => {
    const tiers = getPoolTiers('standard')

    expect(tiers.length).toBeGreaterThan(0)
    expect(tiers[0]!.eDtu).toBe(50)
  })

  it('returns premium pool tiers', () => {
    const tiers = getPoolTiers('premium')

    expect(tiers.length).toBeGreaterThan(0)
    expect(tiers[0]!.eDtu).toBe(125)
  })

  it('returns tiers sorted by eDtu in ascending order', () => {
    const tiers = getPoolTiers('standard')

    for (let i = 1; i < tiers.length; i++) {
      expect(tiers[i]!.eDtu).toBeGreaterThan(tiers[i - 1]!.eDtu)
    }
  })
})

describe('snapToPoolTier', () => {
  it('snaps up to the nearest pool tier for standard', () => {
    const result = snapToPoolTier(75, 'standard')

    expect(result.eDtu).toBe(100)
  })

  it('returns exact tier when DTU matches exactly', () => {
    const result = snapToPoolTier(200, 'standard')

    expect(result.eDtu).toBe(200)
  })

  it('returns smallest tier when DTU is below minimum', () => {
    const result = snapToPoolTier(10, 'standard')

    expect(result.eDtu).toBe(50)
  })

  it('returns largest tier when DTU exceeds maximum', () => {
    const result = snapToPoolTier(99999, 'standard')

    expect(result.eDtu).toBe(3000)
  })

  it('snaps to premium tiers correctly', () => {
    const result = snapToPoolTier(130, 'premium')

    expect(result.eDtu).toBe(250)
  })

  it('returns correct monthly price for the snapped tier', () => {
    const result = snapToPoolTier(50, 'standard')

    expect(result.monthlyPrice).toBe(110.27)
  })
})

describe('getSingleDbMonthlyCost', () => {
  it('returns cost for an exact DTU match', () => {
    const cost = getSingleDbMonthlyCost(100, 'standard')

    expect(cost).toBe(147.19)
  })

  it('snaps up to next tier for non-exact DTU', () => {
    const cost = getSingleDbMonthlyCost(60, 'standard')

    expect(cost).toBe(147.19)
  })

  it('returns smallest tier cost for very low DTU', () => {
    const cost = getSingleDbMonthlyCost(1, 'standard')

    expect(cost).toBe(4.90)
  })

  it('returns largest tier cost when DTU exceeds maximum', () => {
    const cost = getSingleDbMonthlyCost(99999, 'standard')

    expect(cost).toBe(4415.76)
  })

  it('returns premium pricing when tier is premium', () => {
    const cost = getSingleDbMonthlyCost(125, 'premium')

    expect(cost).toBe(456.25)
  })
})

describe('getSingleDbTiers', () => {
  it('returns standard single DB tiers', () => {
    const tiers = getSingleDbTiers('standard')

    expect(tiers.length).toBeGreaterThan(0)
    expect(tiers[0]!.dtu).toBe(5)
  })

  it('returns premium single DB tiers', () => {
    const tiers = getSingleDbTiers('premium')

    expect(tiers.length).toBeGreaterThan(0)
    expect(tiers[0]!.dtu).toBe(125)
  })
})

describe('snapToSingleDbTier', () => {
  it('snaps up to nearest single DB tier', () => {
    const result = snapToSingleDbTier(30, 'standard')

    expect(result.dtu).toBe(50)
  })

  it('returns exact tier when DTU matches', () => {
    const result = snapToSingleDbTier(100, 'standard')

    expect(result.dtu).toBe(100)
  })

  it('returns largest tier when DTU exceeds max', () => {
    const result = snapToSingleDbTier(99999, 'standard')

    expect(result.dtu).toBe(3000)
  })

  it('returns smallest tier when DTU is very low', () => {
    const result = snapToSingleDbTier(1, 'standard')

    expect(result.dtu).toBe(5)
  })

  it('snaps to premium tiers correctly', () => {
    const result = snapToSingleDbTier(200, 'premium')

    expect(result.dtu).toBe(250)
  })
})
