import { create } from 'zustand'

interface AnalysisUiState {
  selectedServerId: number | null
  selectServer: (id: number | null) => void
}

export const useAnalysisUiStore = create<AnalysisUiState>((set) => ({
  selectedServerId: null,
  selectServer: (id) => set({ selectedServerId: id }),
}))
