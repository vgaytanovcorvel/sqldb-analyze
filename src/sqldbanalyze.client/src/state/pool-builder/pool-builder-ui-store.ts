import { create } from 'zustand'
import type { PoolTier } from '../../domain/azure-pricing'

interface PoolBuilderUiState {
  selectedServerId: number | null
  selectedDatabaseNames: ReadonlySet<string>
  targetPercentile: number
  safetyFactor: number
  maxDatabasesPerPool: number
  poolTier: PoolTier
  selectServer: (id: number | null) => void
  toggleDatabase: (name: string) => void
  selectAllDatabases: (names: readonly string[]) => void
  clearDatabaseSelection: () => void
  setTargetPercentile: (value: number) => void
  setSafetyFactor: (value: number) => void
  setMaxDatabasesPerPool: (value: number) => void
  setPoolTier: (value: PoolTier) => void
}

export const usePoolBuilderUiStore = create<PoolBuilderUiState>((set) => ({
  selectedServerId: null,
  selectedDatabaseNames: new Set<string>(),
  targetPercentile: 0.99,
  safetyFactor: 1.10,
  maxDatabasesPerPool: 50,
  poolTier: 'standard',
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
  setMaxDatabasesPerPool: (value) => set({ maxDatabasesPerPool: value }),
  setPoolTier: (value) => set({ poolTier: value }),
}))
