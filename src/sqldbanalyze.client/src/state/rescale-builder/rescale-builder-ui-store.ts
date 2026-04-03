import { create } from 'zustand'
import type { PoolTier } from '../../domain/azure-pricing'

interface RescaleBuilderUiState {
  selectedServerId: number | null
  selectedDatabaseNames: ReadonlySet<string>
  targetPercentile: number
  safetyFactor: number
  tier: PoolTier
  selectServer: (id: number | null) => void
  toggleDatabase: (name: string) => void
  selectAllDatabases: (names: readonly string[]) => void
  clearDatabaseSelection: () => void
  setTargetPercentile: (value: number) => void
  setSafetyFactor: (value: number) => void
  setTier: (value: PoolTier) => void
}

export const useRescaleBuilderUiStore = create<RescaleBuilderUiState>((set) => ({
  selectedServerId: null,
  selectedDatabaseNames: new Set<string>(),
  targetPercentile: 0.99,
  safetyFactor: 1.10,
  tier: 'standard',
  selectServer: (id) =>
    set({ selectedServerId: id, selectedDatabaseNames: new Set<string>() }),
  toggleDatabase: (name) =>
    set((state) => {
      const next = new Set(state.selectedDatabaseNames)
      if (next.has(name)) {
        next.delete(name)
      } else {
        next.add(name)
      }
      return { selectedDatabaseNames: next }
    }),
  selectAllDatabases: (names) => set({ selectedDatabaseNames: new Set(names) }),
  clearDatabaseSelection: () => set({ selectedDatabaseNames: new Set<string>() }),
  setTargetPercentile: (value) => set({ targetPercentile: value }),
  setSafetyFactor: (value) => set({ safetyFactor: value }),
  setTier: (value) => set({ tier: value }),
}))
