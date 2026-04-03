import { create } from 'zustand'

interface AnalysisUiState {
  selectedServerId: number | null
  selectedDatabaseNames: ReadonlySet<string>
  focusedDatabaseName: string | null
  selectServer: (id: number | null) => void
  toggleDatabase: (name: string) => void
  selectAllDatabases: (names: readonly string[]) => void
  clearDatabaseSelection: () => void
  focusDatabase: (name: string | null) => void
}

export const useAnalysisUiStore = create<AnalysisUiState>((set) => ({
  selectedServerId: null,
  selectedDatabaseNames: new Set<string>(),
  focusedDatabaseName: null,
  selectServer: (id) =>
    set({ selectedServerId: id, selectedDatabaseNames: new Set<string>(), focusedDatabaseName: null }),
  toggleDatabase: (name) =>
    set((state) => {
      const next = new Set(state.selectedDatabaseNames)
      if (next.has(name)) {
        next.delete(name)
      } else {
        next.add(name)
      }
      const focused = next.has(state.focusedDatabaseName ?? '') ? state.focusedDatabaseName : null
      return { selectedDatabaseNames: next, focusedDatabaseName: focused }
    }),
  selectAllDatabases: (names) => set({ selectedDatabaseNames: new Set(names) }),
  clearDatabaseSelection: () => set({ selectedDatabaseNames: new Set<string>(), focusedDatabaseName: null }),
  focusDatabase: (name) => set({ focusedDatabaseName: name }),
}))
