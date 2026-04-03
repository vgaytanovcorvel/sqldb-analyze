import { describe, it, expect, beforeEach } from 'vitest'
import { useRescaleBuilderUiStore } from './rescale-builder-ui-store'

describe('rescaleBuilderUiStore', () => {
  beforeEach(() => {
    useRescaleBuilderUiStore.setState({
      selectedServerId: null,
      selectedDatabaseNames: new Set<string>(),
      targetPercentile: 0.99,
      safetyFactor: 1.10,
      tier: 'standard',
    })
  })

  it('has correct initial state', () => {
    const state = useRescaleBuilderUiStore.getState()

    expect(state.selectedServerId).toBeNull()
    expect(state.selectedDatabaseNames.size).toBe(0)
    expect(state.targetPercentile).toBe(0.99)
    expect(state.safetyFactor).toBe(1.10)
    expect(state.tier).toBe('standard')
  })

  describe('selectServer', () => {
    it('sets server and clears databases', () => {
      const store = useRescaleBuilderUiStore.getState()
      store.selectAllDatabases(['db1'])
      store.selectServer(3)

      const state = useRescaleBuilderUiStore.getState()
      expect(state.selectedServerId).toBe(3)
      expect(state.selectedDatabaseNames.size).toBe(0)
    })
  })

  describe('toggleDatabase', () => {
    it('toggles databases on and off', () => {
      useRescaleBuilderUiStore.getState().toggleDatabase('x')
      expect(useRescaleBuilderUiStore.getState().selectedDatabaseNames.has('x')).toBe(true)

      useRescaleBuilderUiStore.getState().toggleDatabase('x')
      expect(useRescaleBuilderUiStore.getState().selectedDatabaseNames.has('x')).toBe(false)
    })
  })

  describe('parameter setters', () => {
    it('sets target percentile', () => {
      useRescaleBuilderUiStore.getState().setTargetPercentile(0.95)
      expect(useRescaleBuilderUiStore.getState().targetPercentile).toBe(0.95)
    })

    it('sets safety factor', () => {
      useRescaleBuilderUiStore.getState().setSafetyFactor(1.50)
      expect(useRescaleBuilderUiStore.getState().safetyFactor).toBe(1.50)
    })

    it('sets tier', () => {
      useRescaleBuilderUiStore.getState().setTier('premium')
      expect(useRescaleBuilderUiStore.getState().tier).toBe('premium')
    })
  })
})
