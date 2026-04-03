import { describe, it, expect, beforeEach } from 'vitest'
import { useAnalysisUiStore } from './analysis-ui-store'

describe('analysisUiStore', () => {
  beforeEach(() => {
    useAnalysisUiStore.setState({
      selectedServerId: null,
      selectedDatabaseNames: new Set<string>(),
      focusedDatabaseName: null,
    })
  })

  it('has correct initial state', () => {
    const state = useAnalysisUiStore.getState()

    expect(state.selectedServerId).toBeNull()
    expect(state.selectedDatabaseNames.size).toBe(0)
    expect(state.focusedDatabaseName).toBeNull()
  })

  describe('selectServer', () => {
    it('sets the selected server ID', () => {
      useAnalysisUiStore.getState().selectServer(5)

      expect(useAnalysisUiStore.getState().selectedServerId).toBe(5)
    })

    it('clears database selection and focus when server changes', () => {
      const store = useAnalysisUiStore.getState()
      store.selectAllDatabases(['db1', 'db2'])
      store.focusDatabase('db1')

      store.selectServer(10)

      const state = useAnalysisUiStore.getState()
      expect(state.selectedDatabaseNames.size).toBe(0)
      expect(state.focusedDatabaseName).toBeNull()
    })

    it('sets server to null', () => {
      useAnalysisUiStore.getState().selectServer(5)
      useAnalysisUiStore.getState().selectServer(null)

      expect(useAnalysisUiStore.getState().selectedServerId).toBeNull()
    })
  })

  describe('toggleDatabase', () => {
    it('adds a database when not selected', () => {
      useAnalysisUiStore.getState().toggleDatabase('db1')

      expect(useAnalysisUiStore.getState().selectedDatabaseNames.has('db1')).toBe(true)
    })

    it('removes a database when already selected', () => {
      useAnalysisUiStore.getState().toggleDatabase('db1')
      useAnalysisUiStore.getState().toggleDatabase('db1')

      expect(useAnalysisUiStore.getState().selectedDatabaseNames.has('db1')).toBe(false)
    })

    it('clears focused database when it is deselected', () => {
      const store = useAnalysisUiStore.getState()
      store.toggleDatabase('db1')
      store.focusDatabase('db1')
      store.toggleDatabase('db1')

      expect(useAnalysisUiStore.getState().focusedDatabaseName).toBeNull()
    })

    it('retains focused database when another database is toggled off', () => {
      const store = useAnalysisUiStore.getState()
      store.toggleDatabase('db1')
      store.toggleDatabase('db2')
      store.focusDatabase('db1')
      store.toggleDatabase('db2')

      expect(useAnalysisUiStore.getState().focusedDatabaseName).toBe('db1')
    })
  })

  describe('selectAllDatabases', () => {
    it('selects all provided database names', () => {
      useAnalysisUiStore.getState().selectAllDatabases(['db1', 'db2', 'db3'])

      const names = useAnalysisUiStore.getState().selectedDatabaseNames
      expect(names.size).toBe(3)
      expect(names.has('db1')).toBe(true)
      expect(names.has('db2')).toBe(true)
      expect(names.has('db3')).toBe(true)
    })

    it('replaces existing selection', () => {
      const store = useAnalysisUiStore.getState()
      store.selectAllDatabases(['db1'])
      store.selectAllDatabases(['db2', 'db3'])

      const names = useAnalysisUiStore.getState().selectedDatabaseNames
      expect(names.size).toBe(2)
      expect(names.has('db1')).toBe(false)
    })
  })

  describe('clearDatabaseSelection', () => {
    it('clears all selected databases and focus', () => {
      const store = useAnalysisUiStore.getState()
      store.selectAllDatabases(['db1', 'db2'])
      store.focusDatabase('db1')

      store.clearDatabaseSelection()

      const state = useAnalysisUiStore.getState()
      expect(state.selectedDatabaseNames.size).toBe(0)
      expect(state.focusedDatabaseName).toBeNull()
    })
  })

  describe('focusDatabase', () => {
    it('sets the focused database name', () => {
      useAnalysisUiStore.getState().focusDatabase('db1')

      expect(useAnalysisUiStore.getState().focusedDatabaseName).toBe('db1')
    })

    it('sets focused database to null', () => {
      useAnalysisUiStore.getState().focusDatabase('db1')
      useAnalysisUiStore.getState().focusDatabase(null)

      expect(useAnalysisUiStore.getState().focusedDatabaseName).toBeNull()
    })
  })
})
