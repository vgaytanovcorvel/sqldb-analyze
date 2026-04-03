import { create } from 'zustand'

interface AnalysisUiState {
  selectedServerId: number | null
  selectedDatabaseNames: ReadonlySet<string>
  selectServer: (id: number | null) => void
  toggleDatabase: (name: string) => void
  selectAllDatabases: (names: readonly string[]) => void
  clearDatabaseSelection: () => void
}

export const useAnalysisUiStore = create<AnalysisUiState>((set) => ({
  selectedServerId: null,
  selectedDatabaseNames: new Set<string>(),
  selectServer: (id) => set({ selectedServerId: id, selectedDatabaseNames: new Set<string>() }),
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
}))
