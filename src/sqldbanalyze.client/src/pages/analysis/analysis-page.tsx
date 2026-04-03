import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useServers } from '../../state/servers/use-servers'
import { useCachedIntervals } from '../../state/analysis/use-cached-intervals'
import { useRefreshMetrics } from '../../state/analysis/use-refresh-metrics'
import { useCorrelationMatrix } from '../../state/analysis/use-correlation-matrix'
import { useAnalysisUiStore } from '../../state/analysis/analysis-ui-store'
import { CorrelationHeatmap } from '../../components/analysis/correlation-heatmap/correlation-heatmap'
import styles from './analysis-page.module.css'

export function AnalysisPage() {
  const { data: servers = [], isLoading: serversLoading } = useServers()
  const { selectedServerId, selectServer } = useAnalysisUiStore()
  const { data: intervals = [], isLoading: intervalsLoading } = useCachedIntervals(selectedServerId)
  const { data: correlationData = [], isLoading: correlationLoading } = useCorrelationMatrix(
    selectedServerId && intervals.length > 0 ? selectedServerId : null,
  )
  const refreshMetrics = useRefreshMetrics()
  const [hours, setHours] = useState(168)

  function handleServerChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const value = e.target.value
    selectServer(value ? Number(value) : null)
  }

  function handleRefresh() {
    if (selectedServerId === null) return
    refreshMetrics.mutate({ serverId: selectedServerId, hours })
  }

  const hasIntervals = intervals.length > 0

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
            <h2 className={styles.sectionTitle}>Cached Metrics Intervals</h2>
            {intervalsLoading ? (
              <div className={styles.empty}>Loading intervals...</div>
            ) : !hasIntervals ? (
              <div className={styles.empty}>
                No cached metrics. Click "Fetch / Refresh Metrics" to load data from Azure.
              </div>
            ) : (
              <table className={styles.intervalsTable}>
                <thead>
                  <tr>
                    <th>Database</th>
                    <th>Earliest</th>
                    <th>Latest</th>
                    <th>Data Points</th>
                  </tr>
                </thead>
                <tbody>
                  {intervals.map((interval) => (
                    <tr key={interval.databaseName}>
                      <td>{interval.databaseName}</td>
                      <td>{formatTimestamp(interval.earliestTimestamp)}</td>
                      <td>{formatTimestamp(interval.latestTimestamp)}</td>
                      <td>{interval.metricCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

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
