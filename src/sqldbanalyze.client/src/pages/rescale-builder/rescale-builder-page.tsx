import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { PoolTier } from '../../domain/azure-pricing'
import type { RescaleRecommendation, RescaleResult, RescaleDirection } from '../../domain/rescale'
import { computeRescaleRecommendations } from '../../domain/rescale'
import { useServers } from '../../state/servers/use-servers'
import { useDatabases } from '../../state/analysis/use-databases'
import { useCachedIntervals } from '../../state/analysis/use-cached-intervals'
import { useTimeSeries } from '../../state/rescale-builder/use-time-series'
import { useRescaleBuilderUiStore } from '../../state/rescale-builder/rescale-builder-ui-store'
import { DatabaseDtuChart } from '../../components/analysis/database-dtu-chart/database-dtu-chart'
import { DbNameLink } from '../../components/shared/db-name-link/db-name-link'
import { AppLayout } from '../../components/layout/app-layout/app-layout'
import styles from './rescale-builder-page.module.css'

type FilterMode = 'all' | 'downgrade' | 'upgrade' | 'keep'

export function RescaleBuilderPage() {
  const { data: servers = [], isLoading: serversLoading } = useServers()
  const {
    selectedServerId,
    selectedDatabaseNames,
    targetPercentile,
    safetyFactor,
    tier,
    selectServer,
    toggleDatabase,
    selectAllDatabases,
    clearDatabaseSelection,
    setTargetPercentile,
    setSafetyFactor,
    setTier,
  } = useRescaleBuilderUiStore()

  const { data: databases = [] } = useDatabases(selectedServerId)
  const { data: intervals = [] } = useCachedIntervals(selectedServerId)
  const { data: timeSeries } = useTimeSeries(selectedServerId)

  const [dbPanelExpanded, setDbPanelExpanded] = useState(false)
  const [dbSearch, setDbSearch] = useState('')
  const [result, setResult] = useState<RescaleResult | null>(null)
  const [filterMode, setFilterMode] = useState<FilterMode>('all')
  const [focusedDbName, setFocusedDbName] = useState<string | null>(null)

  const hasIntervals = intervals.length > 0
  const allDatabaseNames = intervals.map((i) => i.databaseName)
  const selectionCount = selectedDatabaseNames.size
  const allSelected = selectionCount > 0 && selectionCount === allDatabaseNames.length

  const filteredIntervals = useMemo(
    () => dbSearch
      ? intervals.filter((i) => i.databaseName.toLowerCase().includes(dbSearch.toLowerCase()))
      : intervals,
    [intervals, dbSearch],
  )

  useEffect(() => {
    if (hasIntervals && selectionCount === 0) {
      selectAllDatabases(allDatabaseNames)
    }
  }, [hasIntervals, allDatabaseNames.length])

  function handleServerChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const value = e.target.value
    selectServer(value ? Number(value) : null)
    setResult(null)
  }

  function handleToggleAll() {
    if (allSelected) {
      clearDatabaseSelection()
    } else {
      selectAllDatabases(allDatabaseNames)
    }
  }

  function handleAnalyze() {
    if (!timeSeries || selectionCount === 0) return
    const rescaleResult = computeRescaleRecommendations(
      Array.from(selectedDatabaseNames),
      databases,
      timeSeries,
      { targetPercentile, safetyFactor, tier },
    )
    setResult(rescaleResult)
    setFilterMode('all')
  }

  function handleDbFocus(name: string) {
    setFocusedDbName(focusedDbName === name ? null : name)
  }

  const filteredRecommendations = useMemo(() => {
    if (!result) return []
    const filtered = filterMode === 'all'
      ? [...result.recommendations]
      : result.recommendations.filter((r) => r.direction === filterMode)
    return filtered.sort((a, b) => b.savingsDollars - a.savingsDollars)
  }, [result, filterMode])

  const maxAbsSavings = useMemo(() => {
    if (filteredRecommendations.length === 0) return 1
    return Math.max(...filteredRecommendations.map((r) => Math.abs(r.savingsDollars)), 1)
  }, [filteredRecommendations])

  const focusedDbInfo = focusedDbName ? databases.find((d) => d.databaseName === focusedDbName) : null

  return (
    <AppLayout title="Rescale Builder" description="Analyze DTU usage and recommend tier upgrades or downgrades">
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
      </div>

      {selectedServerId === null && (
        <div className={styles.empty}>
          <div className={styles.emptyIcon}>
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.5">
              <path d="M14 8h20l6 8v20a4 4 0 01-4 4H12a4 4 0 01-4-4V16l6-8z" />
              <path d="M8 16h32" />
              <path d="M20 24l4 4 4-4" />
              <path d="M24 28V20" />
            </svg>
          </div>
          <div className={styles.emptyTitle}>Select a server to begin</div>
          <div className={styles.emptyDescription}>
            {servers.length === 0
              ? <>No servers have been added yet. <Link to="/" className={styles.emptyLink}>Add a server</Link> to get started.</>
              : 'Choose an Azure SQL server to analyze individual database DTU tiers and recommend rescaling.'}
          </div>
        </div>
      )}

      {selectedServerId !== null && !hasIntervals && (
        <div className={styles.emptySection}>
          No cached metrics. Go to <Link to="/analysis" className={styles.inlineLink}>Analysis</Link> to fetch metrics first.
        </div>
      )}

      {selectedServerId !== null && hasIntervals && (
        <>
          <div className={styles.twoColumn}>
            <section className={styles.selectionPanel}>
              <button
                className={styles.panelToggle}
                onClick={() => setDbPanelExpanded(!dbPanelExpanded)}
              >
                <div className={styles.panelToggleLeft}>
                  <svg
                    className={`${styles.chevron} ${dbPanelExpanded ? styles.chevronOpen : ''}`}
                    width="16" height="16" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2"
                  >
                    <path d="M6 4l4 4-4 4" />
                  </svg>
                  <h2 className={styles.sectionTitle}>Select Databases</h2>
                  <span className={styles.selectionBadge}>{selectionCount} / {allDatabaseNames.length}</span>
                </div>
                <div className={styles.selectionControls}>
                  <span
                    className={styles.selectButton}
                    role="button"
                    onClick={(e) => { e.stopPropagation(); handleToggleAll() }}
                  >
                    {allSelected ? 'Clear All' : 'Select All'}
                  </span>
                </div>
              </button>

              {dbPanelExpanded && (
                <>
                  <div className={styles.dbSearch}>
                    <input
                      type="text"
                      className={styles.dbSearchInput}
                      placeholder="Filter databases..."
                      value={dbSearch}
                      onChange={(e) => setDbSearch(e.target.value)}
                      onClick={(e) => e.stopPropagation()}
                    />
                  </div>
                  <div className={styles.dbListContainer}>
                    {filteredIntervals.map((interval) => {
                      const dbInfo = databases.find((d) => d.databaseName === interval.databaseName)
                      const isSelected = selectedDatabaseNames.has(interval.databaseName)
                      return (
                        <label
                          key={interval.databaseName}
                          className={`${styles.dbRow} ${isSelected ? styles.dbRowSelected : ''}`}
                        >
                          <input
                            type="checkbox"
                            checked={isSelected}
                            onChange={() => toggleDatabase(interval.databaseName)}
                          />
                          <span className={styles.dbName}>{interval.databaseName}</span>
                          <span className={styles.dbMeta}>
                            {dbInfo ? `${dbInfo.dtuLimit} DTU` : ''}
                            {dbInfo?.elasticPoolName ? ` \u00B7 ${dbInfo.elasticPoolName}` : ''}
                          </span>
                        </label>
                      )
                    })}
                  </div>
                </>
              )}
            </section>

            <section className={styles.optionsPanel}>
              <h2 className={styles.sectionTitle}>Options</h2>

              <div className={styles.optionGroup}>
                <label className={styles.optionLabel}>Pricing Tier</label>
                <select
                  className={styles.optionInput}
                  value={tier}
                  onChange={(e) => setTier(e.target.value as PoolTier)}
                >
                  <option value="standard">Standard</option>
                  <option value="premium">Premium</option>
                </select>
              </div>

              <div className={styles.optionGroup}>
                <label className={styles.optionLabel}>Target Percentile</label>
                <select
                  className={styles.optionInput}
                  value={targetPercentile}
                  onChange={(e) => setTargetPercentile(Number(e.target.value))}
                >
                  <option value={0.95}>P95 (95th)</option>
                  <option value={0.99}>P99 (99th)</option>
                </select>
              </div>

              <div className={styles.optionGroup}>
                <label className={styles.optionLabel}>Safety Factor</label>
                <input
                  type="number"
                  className={styles.optionInput}
                  value={safetyFactor}
                  onChange={(e) => setSafetyFactor(Number(e.target.value))}
                  min={1.0}
                  max={2.0}
                  step={0.05}
                />
              </div>

              <button
                className={styles.analyzeButton}
                onClick={handleAnalyze}
                disabled={selectionCount === 0 || !timeSeries}
              >
                {!timeSeries ? 'Loading metrics...' : `Analyze (${selectionCount} databases)`}
              </button>
            </section>
          </div>

          {result && (
            <>
              <ResultsSummary result={result} />

              <section className={styles.tableSection}>
                <div className={styles.filterTabs}>
                  <FilterTab mode="all" current={filterMode} count={result.recommendations.length} onClick={setFilterMode} />
                  <FilterTab mode="downgrade" current={filterMode} count={result.downgradeCount} onClick={setFilterMode} />
                  <FilterTab mode="upgrade" current={filterMode} count={result.upgradeCount} onClick={setFilterMode} />
                  <FilterTab mode="keep" current={filterMode} count={result.keepCount} onClick={setFilterMode} />
                </div>

                <div className={styles.tableContainer}>
                  <table className={styles.recTable}>
                    <thead>
                      <tr>
                        <th>Database</th>
                        <th>Direction</th>
                        <th className={styles.numericCol}>Current</th>
                        <th className={styles.numericCol}>Recommended</th>
                        <th className={styles.numericCol}>P95</th>
                        <th className={styles.numericCol}>P99</th>
                        <th className={styles.numericCol}>Peak</th>
                        <th className={styles.numericCol}>Mean</th>
                        <th className={styles.numericCol}>Current $/mo</th>
                        <th className={styles.numericCol}>New $/mo</th>
                        <th className={styles.numericCol}>Savings</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredRecommendations.map((rec) => (
                        <RecommendationRow
                          key={rec.databaseName}
                          rec={rec}
                          heat={Math.abs(rec.savingsDollars) / maxAbsSavings}
                          focused={focusedDbName === rec.databaseName}
                          onDbClick={handleDbFocus}
                        />
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            </>
          )}
        </>
      )}

      {focusedDbName && timeSeries && focusedDbInfo && (
        <DatabaseDtuChart
          timeSeries={timeSeries}
          databaseName={focusedDbName}
          dtuLimit={focusedDbInfo.dtuLimit}
          recommendedDtu={result?.recommendations.find((r) => r.databaseName === focusedDbName)?.recommendedTier.dtu ?? null}
          onClose={() => setFocusedDbName(null)}
        />
      )}
    </AppLayout>
  )
}

function ResultsSummary({ result }: { result: RescaleResult }) {
  return (
    <section className={styles.summary}>
      <h2 className={styles.summaryTitle}>Results</h2>
      <div className={styles.statsGrid}>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Databases</span>
          <span className={styles.statValue}>{result.recommendations.length}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Downgrades</span>
          <span className={styles.statValue}>{result.downgradeCount}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Upgrades</span>
          <span className={styles.statValue}>{result.upgradeCount}</span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Current Cost</span>
          <span className={styles.statValue}>
            ${result.totalCurrentCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}
            <span className={styles.statUnit}>/mo</span>
          </span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Recommended Cost</span>
          <span className={styles.statValue}>
            ${result.totalRecommendedCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}
            <span className={styles.statUnit}>/mo</span>
          </span>
        </div>
      </div>

      <div className={styles.savingsRow}>
        <div className={`${styles.savingsCard} ${result.totalSavingsDollars >= 0 ? styles.savingsPositive : styles.savingsNegative}`}>
          <span className={styles.savingsLabel}>{result.totalSavingsDollars >= 0 ? 'Savings' : 'Increase'}</span>
          <span className={styles.savingsValue}>
            <svg className={styles.savingsIcon} width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
              {result.totalSavingsDollars >= 0
                ? <path d="M8 3l5 6H3l5-6z" />
                : <path d="M8 13l5-6H3l5 6z" />}
            </svg>
            {Math.abs(result.totalSavingsPercent).toFixed(1)}%
          </span>
        </div>
        <div className={`${styles.savingsCard} ${result.totalSavingsDollars >= 0 ? styles.savingsPositive : styles.savingsNegative}`}>
          <span className={styles.savingsLabel}>Est. Monthly</span>
          <span className={styles.savingsValue}>
            {result.totalSavingsDollars >= 0 ? '' : '+'}${Math.abs(result.totalSavingsDollars).toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo
          </span>
        </div>
      </div>

      {result.downgradeCount > 0 && result.totalSavingsDollars > 0 && (
        <div className={styles.insightPositive}>
          <span className={styles.insightIcon}>&#10003;</span>
          <span>
            {result.downgradeCount} database{result.downgradeCount !== 1 ? 's are' : ' is'} over-provisioned.
            Rescaling to recommended tiers saves {result.totalSavingsPercent.toFixed(1)}% ({`$${Math.abs(result.totalSavingsDollars).toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo`}).
          </span>
        </div>
      )}
      {result.upgradeCount > 0 && (
        <div className={styles.insight}>
          <span className={styles.insightIcon}>!</span>
          <span>
            {result.upgradeCount} database{result.upgradeCount !== 1 ? 's are' : ' is'} under-provisioned and may experience throttling.
            Consider upgrading to ensure consistent performance.
          </span>
        </div>
      )}
      {result.downgradeCount === 0 && result.upgradeCount === 0 && (
        <div className={styles.insightNeutral}>
          <span className={styles.insightIcon}>&#8212;</span>
          <span>All databases are appropriately sized for the selected percentile and safety factor.</span>
        </div>
      )}
    </section>
  )
}

const FILTER_LABELS: Record<FilterMode, string> = {
  all: 'All',
  downgrade: 'Downgrades',
  upgrade: 'Upgrades',
  keep: 'No Change',
}

function FilterTab({ mode, current, count, onClick }: {
  mode: FilterMode
  current: FilterMode
  count: number
  onClick: (mode: FilterMode) => void
}) {
  return (
    <button
      className={`${styles.filterTab} ${mode === current ? styles.filterTabActive : ''}`}
      onClick={() => onClick(mode)}
    >
      {FILTER_LABELS[mode]}
      <span className={styles.filterBadge}>{count}</span>
    </button>
  )
}

const DIRECTION_BADGE_CLASS: Record<RescaleDirection, string | undefined> = {
  downgrade: styles.badgeDowngrade,
  upgrade: styles.badgeUpgrade,
  keep: styles.badgeKeep,
}

const DIRECTION_LABELS: Record<RescaleDirection, string> = {
  downgrade: 'Downgrade',
  upgrade: 'Upgrade',
  keep: 'No Change',
}

function RecommendationRow({ rec, heat, focused, onDbClick }: {
  rec: RescaleRecommendation
  heat: number
  focused: boolean
  onDbClick: (name: string) => void
}) {
  const savingsClass = rec.savingsDollars > 0
    ? styles.savingsCellPositive
    : rec.savingsDollars < 0
      ? styles.savingsCellNegative
      : ''

  return (
    <tr
      className={styles.recRow}
      style={{ '--heat': heat } as React.CSSProperties}
    >
      <td>
        <DbNameLink name={rec.databaseName} focused={focused} onClick={onDbClick} />
      </td>
      <td>
        <span className={`${styles.directionBadge} ${DIRECTION_BADGE_CLASS[rec.direction]}`}>
          {DIRECTION_LABELS[rec.direction]}
        </span>
      </td>
      <td className={styles.numericCell}>{rec.currentDtuLimit}</td>
      <td className={styles.numericCell}>{rec.recommendedTier.dtu}</td>
      <td className={styles.numericCell}>{rec.p95Dtu.toFixed(0)}</td>
      <td className={styles.numericCell}>{rec.p99Dtu.toFixed(0)}</td>
      <td className={styles.numericCell}>{rec.peakDtu.toFixed(0)}</td>
      <td className={styles.numericCell}>{rec.meanDtu.toFixed(0)}</td>
      <td className={styles.numericCell}>${rec.currentMonthlyCost.toFixed(0)}</td>
      <td className={styles.numericCell}>${rec.recommendedMonthlyCost.toFixed(0)}</td>
      <td className={`${styles.numericCell} ${savingsClass}`}>
        {rec.savingsDollars > 0 ? '-' : rec.savingsDollars < 0 ? '+' : ''}
        ${Math.abs(rec.savingsDollars).toFixed(0)}
      </td>
    </tr>
  )
}
