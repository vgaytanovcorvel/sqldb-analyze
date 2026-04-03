import { describe, it, expect, beforeEach } from 'vitest'
import { usePoolBuilderUiStore } from './pool-builder-ui-store'

describe('poolBuilderUiStore', () => {
  beforeEach(() => {
    usePoolBuilderUiStore.setState({
      selectedServerId: null,
      selectedDatabaseNames: new Set<string>(),
      targetPercentile: 0.99,
      safetyFactor: 1.10,
      maxDatabasesPerPool: 50,
      minDiversificationRatio: 1.25,
      poolTier: 'standard',
    })
  })

  it('has correct initial state', () => {
    const state = usePoolBuilderUiStore.getState()

    expect(state.selectedServerId).toBeNull()
    expect(state.selectedDatabaseNames.size).toBe(0)
    expect(state.targetPercentile).toBe(0.99)
    expect(state.safetyFactor).toBe(1.10)
    expect(state.maxDatabasesPerPool).toBe(50)
    expect(state.minDiversificationRatio).toBe(1.25)
    expect(state.poolTier).toBe('standard')
  })

  describe('selectServer', () => {
    it('sets the server and clears database selection', () => {
      const store = usePoolBuilderUiStore.getState()
      store.selectAllDatabases(['db1'])
      store.selectServer(7)

      const state = usePoolBuilderUiStore.getState()
      expect(state.selectedServerId).toBe(7)
      expect(state.selectedDatabaseNames.size).toBe(0)
    })
  })

  describe('toggleDatabase', () => {
    it('adds and removes databases', () => {
      usePoolBuilderUiStore.getState().toggleDatabase('db1')
      expect(usePoolBuilderUiStore.getState().selectedDatabaseNames.has('db1')).toBe(true)

      usePoolBuilderUiStore.getState().toggleDatabase('db1')
      expect(usePoolBuilderUiStore.getState().selectedDatabaseNames.has('db1')).toBe(false)
    })
  })

  describe('parameter setters', () => {
    it('sets target percentile', () => {
      usePoolBuilderUiStore.getState().setTargetPercentile(0.95)
      expect(usePoolBuilderUiStore.getState().targetPercentile).toBe(0.95)
    })

    it('sets safety factor', () => {
      usePoolBuilderUiStore.getState().setSafetyFactor(1.25)
      expect(usePoolBuilderUiStore.getState().safetyFactor).toBe(1.25)
    })

    it('sets max databases per pool', () => {
      usePoolBuilderUiStore.getState().setMaxDatabasesPerPool(25)
      expect(usePoolBuilderUiStore.getState().maxDatabasesPerPool).toBe(25)
    })

    it('sets min diversification ratio', () => {
      usePoolBuilderUiStore.getState().setMinDiversificationRatio(2.0)
      expect(usePoolBuilderUiStore.getState().minDiversificationRatio).toBe(2.0)
    })

    it('sets pool tier', () => {
      usePoolBuilderUiStore.getState().setPoolTier('premium')
      expect(usePoolBuilderUiStore.getState().poolTier).toBe('premium')
    })
  })

  describe('selectAllDatabases', () => {
    it('replaces entire selection', () => {
      usePoolBuilderUiStore.getState().selectAllDatabases(['a', 'b'])
      expect(usePoolBuilderUiStore.getState().selectedDatabaseNames.size).toBe(2)

      usePoolBuilderUiStore.getState().selectAllDatabases(['c'])
      const names = usePoolBuilderUiStore.getState().selectedDatabaseNames
      expect(names.size).toBe(1)
      expect(names.has('c')).toBe(true)
    })
  })

  describe('clearDatabaseSelection', () => {
    it('empties the set', () => {
      usePoolBuilderUiStore.getState().selectAllDatabases(['a', 'b'])
      usePoolBuilderUiStore.getState().clearDatabaseSelection()

      expect(usePoolBuilderUiStore.getState().selectedDatabaseNames.size).toBe(0)
    })
  })
})
