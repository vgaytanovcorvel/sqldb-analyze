import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { DatabaseInfo } from '../../domain/models'
import { useServers } from '../../state/servers/use-servers'
import { useDatabases } from '../../state/analysis/use-databases'
import { useCachedIntervals } from '../../state/analysis/use-cached-intervals'
import { useRefreshMetrics } from '../../state/analysis/use-refresh-metrics'
import { useCorrelationMatrix } from '../../state/analysis/use-correlation-matrix'
import { useSimulatePool } from '../../state/analysis/use-simulate-pool'
import { useAnalysisUiStore } from '../../state/analysis/analysis-ui-store'
import { CorrelationHeatmap } from '../../components/analysis/correlation-heatmap/correlation-heatmap'
import { PoolSimulation } from '../../components/analysis/pool-simulation/pool-simulation'
import { DtuChart } from '../../components/analysis/dtu-chart/dtu-chart'
import { useServices } from '../../core/providers'
import { useQuery } from '@tanstack/react-query'
import styles from './analysis-page.module.css'

export function AnalysisPage() {
  const { data: servers = [], isLoading: serversLoading } = useServers()
  const {
    selectedServerId,
    selectedDatabaseNames,
    selectServer,
    toggleDatabase,
    selectAllDatabases,
    clearDatabaseSelection,
  } = useAnalysisUiStore()
  const { data: databases = [], isLoading: databasesLoading } = useDatabases(selectedServerId)
  const { data: intervals = [], isLoading: intervalsLoading } = useCachedIntervals(selectedServerId)
  const { data: correlationData = [], isLoading: correlationLoading } = useCorrelationMatrix(
    selectedServerId && intervals.length > 0 ? selectedServerId : null,
  )
  const refreshMetrics = useRefreshMetrics()
  const simulatePool = useSimulatePool()
  const [hours, setHours] = useState(168)

  const { analysisRepository } = useServices()
  const { data: timeSeries } = useQuery({
    queryKey: ['time-series', selectedServerId],
    queryFn: () => analysisRepository.analysisFetchTimeSeries(selectedServerId!),
    enabled: selectedServerId !== null && intervals.length > 0,
  })

  const hasIntervals = intervals.length > 0
  const selectionCount = selectedDatabaseNames.size
  const allDatabaseNames = intervals.map((i) => i.databaseName)
  const allSelected = selectionCount > 0 && selectionCount === allDatabaseNames.length

  function handleServerChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const value = e.target.value
    selectServer(value ? Number(value) : null)
    simulatePool.reset()
  }

  function handleRefresh() {
    if (selectedServerId === null) return
    refreshMetrics.mutate({ serverId: selectedServerId, hours })
  }

  function handleSimulate() {
    if (selectedServerId === null || selectionCount < 2) return

    const dtuLimits: Record<string, number> = {}
    for (const db of databases) {
      if (selectedDatabaseNames.has(db.databaseName)) {
        dtuLimits[db.databaseName] = db.dtuLimit
      }
    }

    simulatePool.mutate({
      serverId: selectedServerId,
      request: { databaseNames: Array.from(selectedDatabaseNames), dtuLimits },
    })
  }

  function handleToggleAll() {
    if (allSelected) {
      clearDatabaseSelection()
    } else {
      selectAllDatabases(allDatabaseNames)
    }
  }

  return (
    <main className={styles.page}>
      <nav className={styles.nav}>
        <Link to="/" className={styles.navLink}>Registered Servers</Link>
      </nav>

      <div className={styles.header}>
        <h1 className={styles.title}>Performance Analysis</h1>
      </div>

      <div className={styles.controls}>
        <label className={styles.label}>Server:</label>
        <select
          className={styles.select}
          value={selectedServerId ?? ''}
          onChange={handleServerChange}
          disabled={serversLoading}
        >
          <option value="">Select a server...</option>
          {servers.map((s) => (
            <option key={s.registeredServerId} value={s.registeredServerId}>
              {s.name} ({s.serverName})
            </option>
          ))}
        </select>

        <label className={styles.label}>Hours:</label>
        <input
          type="number"
          className={styles.hoursInput}
          value={hours}
          onChange={(e) => setHours(Number(e.target.value))}
          min={1}
          max={720}
        />

        <button
          className={styles.refreshButton}
          onClick={handleRefresh}
          disabled={selectedServerId === null || refreshMetrics.isPending}
        >
          {refreshMetrics.isPending ? 'Fetching...' : 'Fetch / Refresh Metrics'}
        </button>
      </div>

      {refreshMetrics.error && (
        <div className={styles.error}>{refreshMetrics.error.message}</div>
      )}

      {selectedServerId !== null && (
        <>
          <section className={styles.section}>
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>Databases</h2>
              {hasIntervals && (
                <div className={styles.selectionControls}>
                  <button className={styles.selectButton} onClick={handleToggleAll}>
                    {allSelected ? 'Clear All' : 'Select All'}
                  </button>
                  <button
                    className={styles.simulateButton}
                    onClick={handleSimulate}
                    disabled={selectionCount < 2 || simulatePool.isPending}
                  >
                    {simulatePool.isPending
                      ? 'Simulating...'
                      : `Simulate Pool (${selectionCount} selected)`}
                  </button>
                </div>
              )}
            </div>
            {intervalsLoading || databasesLoading ? (
              <div className={styles.empty}>Loading...</div>
            ) : !hasIntervals ? (
              <div className={styles.empty}>
                No cached metrics. Click &quot;Fetch / Refresh Metrics&quot; to load data from Azure.
              </div>
            ) : (
              <table className={styles.intervalsTable}>
                <thead>
                  <tr>
                    <th className={styles.checkboxCol}>
                      <input
                        type="checkbox"
                        checked={allSelected}
                        onChange={handleToggleAll}
                      />
                    </th>
                    <th>Database</th>
                    <th>Size (MB)</th>
                    <th>DTU Limit</th>
                    <th>Elastic Pool</th>
                    <th>Earliest</th>
                    <th>Latest</th>
                    <th>Data Points</th>
                  </tr>
                </thead>
                <tbody>
                  {intervals.map((interval) => {
                    const dbInfo = databases.find(
                      (d) => d.databaseName === interval.databaseName,
                    )
                    const isSelected = selectedDatabaseNames.has(interval.databaseName)
                    return (
                      <tr
                        key={interval.databaseName}
                        className={isSelected ? styles.selectedRow : undefined}
                        onClick={() => toggleDatabase(interval.databaseName)}
                      >
                        <td className={styles.checkboxCol}>
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => toggleDatabase(interval.databaseName)}
                            onClick={(e) => e.stopPropagation()}
                          />
                        </td>
                        <td>{interval.databaseName}</td>
                        <td className={styles.numericCell}>
                          {dbInfo ? formatSize(dbInfo.dataSizeMB) : '-'}
                        </td>
                        <td className={styles.numericCell}>
                          {dbInfo ? (dbInfo.dtuLimit > 0 ? dbInfo.dtuLimit : '-') : '-'}
                        </td>
                        <td>{dbInfo?.elasticPoolName ?? '-'}</td>
                        <td>{formatTimestamp(interval.earliestTimestamp)}</td>
                        <td>{formatTimestamp(interval.latestTimestamp)}</td>
                        <td className={styles.numericCell}>{interval.metricCount}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            )}
          </section>

          {simulatePool.error && (
            <div className={styles.error}>{simulatePool.error.message}</div>
          )}

          {simulatePool.data && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Pool Simulation Results</h2>
              <PoolSimulation result={simulatePool.data} />
            </section>
          )}

          {simulatePool.data && timeSeries && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Combined DTU Usage</h2>
              <DtuChart
                timeSeries={timeSeries}
                selectedDatabases={selectedDatabaseNames}
                dtuLimits={buildDtuLimitsMap(databases, selectedDatabaseNames)}
                p95={simulatePool.data.p95Dtu}
                p99={simulatePool.data.p99Dtu}
              />
            </section>
          )}

          {hasIntervals && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Performance Correlation Matrix</h2>
              {correlationLoading ? (
                <div className={styles.empty}>Computing correlations...</div>
              ) : correlationData.length === 0 ? (
                <div className={styles.empty}>
                  Not enough data to compute correlations.
                </div>
              ) : (
                <CorrelationHeatmap data={correlationData} />
              )}
            </section>
          )}
        </>
      )}
    </main>
  )
}

function formatTimestamp(timestamp: string | null): string {
  if (!timestamp) return '-'
  return new Date(timestamp).toLocaleString()
}

function formatSize(mb: number): string {
  if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`
  return mb.toFixed(0)
}

function buildDtuLimitsMap(
  databases: readonly DatabaseInfo[],
  selected: ReadonlySet<string>,
): Record<string, number> {
  const map: Record<string, number> = {}
  for (const db of databases) {
    if (selected.has(db.databaseName)) {
      map[db.databaseName] = db.dtuLimit
    }
  }
  return map
}
