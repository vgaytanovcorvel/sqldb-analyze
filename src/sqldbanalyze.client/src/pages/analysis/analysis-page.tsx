import { useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import type { DatabaseInfo, DtuTimeSeries } from '../../domain/models'
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
import { DatabaseDtuChart } from '../../components/analysis/database-dtu-chart/database-dtu-chart'
import { DbNameLink } from '../../components/shared/db-name-link/db-name-link'
import { AppLayout } from '../../components/layout/app-layout/app-layout'
import { useServices } from '../../core/providers'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import styles from './analysis-page.module.css'

const DTU_TIERS = [5, 10, 20, 50, 100, 125, 200, 250, 400, 500, 800, 1000, 1600, 1750, 3000, 4000] as const

export function AnalysisPage() {
  const { data: servers = [], isLoading: serversLoading } = useServers()
  const {
    selectedServerId,
    selectedDatabaseNames,
    focusedDatabaseName,
    selectServer,
    toggleDatabase,
    selectAllDatabases,
    clearDatabaseSelection,
    focusDatabase,
  } = useAnalysisUiStore()
  const { data: databases = [], isLoading: databasesLoading } = useDatabases(selectedServerId)
  const { data: intervals = [], isLoading: intervalsLoading } = useCachedIntervals(selectedServerId)
  const { data: correlationData = [], isLoading: correlationLoading } = useCorrelationMatrix(
    selectedServerId && intervals.length > 0 ? selectedServerId : null,
  )
  const queryClient = useQueryClient()
  const refreshMetrics = useRefreshMetrics()
  const simulatePool = useSimulatePool()
  const [hours, setHours] = useState(168)
  const [dbSearch, setDbSearch] = useState('')
  const [isRefreshingDatabases, setIsRefreshingDatabases] = useState(false)

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

  const filteredIntervals = useMemo(
    () => dbSearch
      ? intervals.filter((i) => i.databaseName.toLowerCase().includes(dbSearch.toLowerCase()))
      : intervals,
    [intervals, dbSearch],
  )

  const dtuLimitsMap = useMemo(() => buildDtuLimitsMap(databases, selectedDatabaseNames), [databases, selectedDatabaseNames])

  const recommendations = useMemo(
    () => (timeSeries ? computeRecommendations(timeSeries, databases) : new Map<string, number>()),
    [timeSeries, databases],
  )

  const prevSelectionRef = useRef<string>('')
  useEffect(() => {
    const key = Array.from(selectedDatabaseNames).sort().join(',')
    if (key === prevSelectionRef.current) return
    prevSelectionRef.current = key

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
  }, [selectedDatabaseNames, selectedServerId, databases, selectionCount])

  function handleServerChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const value = e.target.value
    selectServer(value ? Number(value) : null)
    simulatePool.reset()
  }

  async function handleRefreshDatabases() {
    if (selectedServerId === null) return
    setIsRefreshingDatabases(true)
    await queryClient.invalidateQueries({ queryKey: ['databases', selectedServerId] })
    setIsRefreshingDatabases(false)
  }

  function handleFetchMetrics() {
    if (selectedServerId === null) return
    refreshMetrics.mutate({ serverId: selectedServerId, hours })
  }

  async function handleRefreshAll() {
    if (selectedServerId === null) return
    setIsRefreshingDatabases(true)
    await queryClient.invalidateQueries({ queryKey: ['databases', selectedServerId] })
    setIsRefreshingDatabases(false)
    refreshMetrics.mutate({ serverId: selectedServerId, hours })
  }

  function handleToggleAll() {
    if (allSelected) {
      clearDatabaseSelection()
    } else {
      selectAllDatabases(allDatabaseNames)
    }
  }

  function handleDatabaseFocus(name: string) {
    focusDatabase(focusedDatabaseName === name ? null : name)
  }

  const focusedDbInfo = focusedDatabaseName ? databases.find((d) => d.databaseName === focusedDatabaseName) : null

  return (
    <AppLayout title="Performance Analysis" description="Analyze DTU usage and identify optimization opportunities">
      <div className={styles.controls}>
        <div className={styles.controlGroup}>
          <label className={styles.controlLabel}>Server</label>
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
        </div>

        <div className={styles.controlGroup}>
          <label className={styles.controlLabel}>Hours</label>
          <input
            type="number"
            className={styles.hoursInput}
            value={hours}
            onChange={(e) => setHours(Number(e.target.value))}
            min={1}
            max={720}
          />
        </div>

        <div className={styles.controlGroupAction}>
          <button
            className={styles.secondaryButton}
            onClick={handleRefreshDatabases}
            disabled={selectedServerId === null || isRefreshingDatabases}
          >
            {isRefreshingDatabases ? 'Refreshing...' : 'Refresh DBs'}
          </button>
          <button
            className={styles.secondaryButton}
            onClick={handleFetchMetrics}
            disabled={selectedServerId === null || refreshMetrics.isPending}
          >
            {refreshMetrics.isPending ? 'Fetching...' : 'Fetch Metrics'}
          </button>
          <button
            className={styles.refreshButton}
            onClick={handleRefreshAll}
            disabled={selectedServerId === null || isRefreshingDatabases || refreshMetrics.isPending}
          >
            {isRefreshingDatabases || refreshMetrics.isPending ? 'Refreshing...' : 'Refresh All'}
          </button>
        </div>
      </div>

      {refreshMetrics.error && (
        <div className={styles.error}>{refreshMetrics.error.message}</div>
      )}

      {selectedServerId === null && (
        <div className={styles.empty}>
          <div className={styles.emptyIcon}>
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.5">
              <rect x="6" y="12" width="36" height="24" rx="3" />
              <path d="M12 30l6-8 4 4 8-10 6 8" />
            </svg>
          </div>
          <div className={styles.emptyTitle}>Select a server to begin</div>
          <div className={styles.emptyDescription}>
            {servers.length === 0
              ? <>No servers have been added yet. <Link to="/" className={styles.emptyLink}>Add a server</Link> to begin analysis.</>
              : 'Choose an Azure SQL server from the dropdown above to view its database metrics and performance data.'}
          </div>
        </div>
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
                  {selectionCount >= 2 && (
                    <span className={styles.selectionBadge}>
                      {selectionCount} selected
                    </span>
                  )}
                </div>
              )}
            </div>
            {intervalsLoading || databasesLoading ? (
              <div className={styles.emptySection}>Loading databases...</div>
            ) : !hasIntervals ? (
              <div className={styles.emptySection}>
                No cached metrics. Click &quot;Fetch Metrics&quot; to load data from Azure.
              </div>
            ) : (
              <>
              <div className={styles.dbSearch}>
                <input
                  type="text"
                  className={styles.dbSearchInput}
                  placeholder="Filter databases..."
                  value={dbSearch}
                  onChange={(e) => setDbSearch(e.target.value)}
                />
              </div>
              <div className={styles.tableContainer}>
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
                      <th>Recommended</th>
                      <th>Elastic Pool</th>
                      <th>Earliest</th>
                      <th>Latest</th>
                      <th>Data Points</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredIntervals.map((interval) => {
                      const dbInfo = databases.find(
                        (d) => d.databaseName === interval.databaseName,
                      )
                      const isSelected = selectedDatabaseNames.has(interval.databaseName)
                      const recommended = recommendations.get(interval.databaseName)
                      const currentLimit = dbInfo?.dtuLimit ?? 0
                      const isDifferent = recommended !== undefined && currentLimit > 0 && recommended !== currentLimit
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
                          <td>
                            <DbNameLink
                              name={interval.databaseName}
                              focused={focusedDatabaseName === interval.databaseName}
                              onClick={handleDatabaseFocus}
                            />
                          </td>
                          <td className={styles.numericCell}>
                            {dbInfo ? formatSize(dbInfo.dataSizeMB) : '-'}
                          </td>
                          <td className={styles.numericCell}>
                            {dbInfo ? (dbInfo.dtuLimit > 0 ? dbInfo.dtuLimit : '-') : '-'}
                          </td>
                          <td className={`${styles.numericCell} ${isDifferent ? styles.recommendedDifferent : ''}`}>
                            {recommended !== undefined && currentLimit > 0 ? (
                              <>
                                {recommended}
                                {isDifferent && (
                                  <span className={recommended < currentLimit ? styles.recommendedDown : styles.recommendedUp}>
                                    {recommended < currentLimit ? ' \u25BC' : ' \u25B2'}
                                  </span>
                                )}
                              </>
                            ) : '-'}
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
              </div>
              </>
            )}
          </section>

          {simulatePool.error && (
            <div className={styles.error}>{simulatePool.error.message}</div>
          )}

          {simulatePool.data && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>
                Pool Simulation Results
                {simulatePool.isPending && <span className={styles.recalculating}> (recalculating...)</span>}
              </h2>
              <PoolSimulation result={simulatePool.data} />
            </section>
          )}

          {focusedDatabaseName && timeSeries && focusedDbInfo && (
            <DatabaseDtuChart
              timeSeries={timeSeries}
              databaseName={focusedDatabaseName}
              dtuLimit={focusedDbInfo.dtuLimit}
              recommendedDtu={recommendations.get(focusedDatabaseName) ?? null}
              onClose={() => focusDatabase(null)}
            />
          )}

          {timeSeries && selectionCount > 0 && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Combined DTU Usage</h2>
              <DtuChart
                timeSeries={timeSeries}
                selectedDatabases={selectedDatabaseNames}
                dtuLimits={dtuLimitsMap}
                p95={simulatePool.data?.p95Dtu}
                p99={simulatePool.data?.p99Dtu}
                onDatabaseClick={handleDatabaseFocus}
                focusedDatabase={focusedDatabaseName}
              />
            </section>
          )}

          {hasIntervals && (
            <section className={styles.section}>
              <h2 className={styles.sectionTitle}>Performance Correlation Matrix</h2>
              {correlationLoading ? (
                <div className={styles.emptySection}>Computing correlations...</div>
              ) : correlationData.length === 0 ? (
                <div className={styles.emptySection}>
                  Not enough data to compute correlations.
                </div>
              ) : (
                <CorrelationHeatmap data={correlationData} onDatabaseClick={handleDatabaseFocus} />
              )}
            </section>
          )}
        </>
      )}
    </AppLayout>
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

function computeRecommendations(
  timeSeries: DtuTimeSeries,
  databases: readonly DatabaseInfo[],
): Map<string, number> {
  const result = new Map<string, number>()

  for (const db of databases) {
    const values = timeSeries.databaseValues[db.databaseName]
    if (!values || values.length === 0 || db.dtuLimit <= 0) continue

    const absoluteValues = values.map((pct) => (pct * db.dtuLimit) / 100)
    const sorted = [...absoluteValues].sort((a, b) => a - b)
    const p95Index = Math.ceil(sorted.length * 0.95) - 1
    const p95 = sorted[Math.max(0, p95Index)] ?? 0

    const tier = findSmallestTier(p95)
    if (tier !== null) {
      result.set(db.databaseName, tier)
    }
  }

  return result
}

function findSmallestTier(requiredDtu: number): number | null {
  for (const tier of DTU_TIERS) {
    if (tier >= requiredDtu) return tier
  }
  return DTU_TIERS[DTU_TIERS.length - 1] ?? null
}
