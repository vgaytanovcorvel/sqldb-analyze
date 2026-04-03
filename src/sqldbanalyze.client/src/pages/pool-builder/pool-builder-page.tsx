import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import type { DatabaseInfo, PoolAssignment } from '../../domain/models'
import type { PoolTier } from '../../domain/azure-pricing'
import { snapToPoolTier, getSingleDbMonthlyCost } from '../../domain/azure-pricing'
import { useServers } from '../../state/servers/use-servers'
import { useDatabases } from '../../state/analysis/use-databases'
import { useCachedIntervals } from '../../state/analysis/use-cached-intervals'
import { usePoolBuilderUiStore } from '../../state/pool-builder/pool-builder-ui-store'
import { useBuildPools } from '../../state/pool-builder/use-build-pools'
import { PoolAssignmentCard } from '../../components/pool-builder/pool-assignment-card/pool-assignment-card'
import { printPoolReport } from '../../components/pool-builder/print-pool-report'
import { AppLayout } from '../../components/layout/app-layout/app-layout'
import styles from './pool-builder-page.module.css'

export function PoolBuilderPage() {
  const { data: servers = [], isLoading: serversLoading } = useServers()
  const {
    selectedServerId,
    selectedDatabaseNames,
    targetPercentile,
    safetyFactor,
    maxDatabasesPerPool,
    poolTier,
    selectServer,
    toggleDatabase,
    selectAllDatabases,
    clearDatabaseSelection,
    setTargetPercentile,
    setSafetyFactor,
    setMaxDatabasesPerPool,
    setPoolTier,
  } = usePoolBuilderUiStore()

  const { data: databases = [] } = useDatabases(selectedServerId)
  const { data: intervals = [] } = useCachedIntervals(selectedServerId)
  const buildPools = useBuildPools()

  const [dbPanelExpanded, setDbPanelExpanded] = useState(false)
  const [dbSearch, setDbSearch] = useState('')

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

  const dtuLimitsMap = useMemo(() => buildDtuLimitsMap(databases), [databases])

  const result = buildPools.data

  const { multiDbPools, standaloneDatabaseNames } = useMemo(
    () => splitPools(result?.pools ?? [], result?.isolatedDatabases ?? []),
    [result],
  )

  const summary = useMemo(
    () => computeSummary(multiDbPools, standaloneDatabaseNames, dtuLimitsMap, poolTier),
    [multiDbPools, standaloneDatabaseNames, dtuLimitsMap, poolTier],
  )

  function handleServerChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const value = e.target.value
    selectServer(value ? Number(value) : null)
    buildPools.reset()
  }

  function handleToggleAll() {
    if (allSelected) {
      clearDatabaseSelection()
    } else {
      selectAllDatabases(allDatabaseNames)
    }
  }

  const selectedServer = servers.find((s) => s.registeredServerId === selectedServerId)

  function handlePrintReport() {
    if (!selectedServer || !result) return
    printPoolReport({
      serverName: `${selectedServer.name} (${selectedServer.serverName})`,
      poolTier,
      targetPercentile,
      safetyFactor,
      maxDatabasesPerPool,
      pools: multiDbPools,
      standaloneDatabaseNames,
      dtuLimits: dtuLimitsMap,
      summary,
    })
  }

  function handleBuildPools() {
    if (selectedServerId === null || selectionCount < 2) return

    const dtuLimits: Record<string, number> = {}
    for (const db of databases) {
      if (selectedDatabaseNames.has(db.databaseName)) {
        dtuLimits[db.databaseName] = db.dtuLimit
      }
    }

    buildPools.mutate({
      serverId: selectedServerId,
      request: {
        databaseNames: Array.from(selectedDatabaseNames),
        dtuLimits,
        targetPercentile,
        safetyFactor,
        maxDatabasesPerPool,
      },
    })
  }

  return (
    <AppLayout title="Pool Builder" description="Optimize elastic pool sizing with correlation-aware analysis">
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
              <path d="M8 16l16-8 16 8v16l-16 8-16-8V16z" />
              <path d="M8 16l16 8 16-8" />
              <path d="M24 24v16" />
            </svg>
          </div>
          <div className={styles.emptyTitle}>Select a server to begin</div>
          <div className={styles.emptyDescription}>
            {servers.length === 0
              ? <>No servers have been added yet. <Link to="/" className={styles.emptyLink}>Add a server</Link> to get started.</>
              : 'Choose an Azure SQL server to configure and build optimized elastic pool recommendations.'}
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
                <label className={styles.optionLabel}>Pool Tier</label>
                <select
                  className={styles.optionInput}
                  value={poolTier}
                  onChange={(e) => setPoolTier(e.target.value as PoolTier)}
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

              <div className={styles.optionGroup}>
                <label className={styles.optionLabel}>Max DBs per Pool</label>
                <input
                  type="number"
                  className={styles.optionInput}
                  value={maxDatabasesPerPool}
                  onChange={(e) => setMaxDatabasesPerPool(Number(e.target.value))}
                  min={2}
                  max={500}
                />
              </div>

              <button
                className={styles.buildButton}
                onClick={handleBuildPools}
                disabled={selectionCount < 2 || buildPools.isPending}
              >
                {buildPools.isPending ? 'Building...' : `Build Pools (${selectionCount} databases)`}
              </button>
            </section>
          </div>

          {buildPools.error && (
            <div className={styles.error}>{buildPools.error.message}</div>
          )}

          {result && (
            <>
              <section className={styles.summary}>
                <div className={styles.summaryHeader}>
                  <h2 className={styles.summaryTitle}>Results</h2>
                  <button className={styles.printButton} onClick={handlePrintReport}>
                    Print Report
                  </button>
                </div>
                <div className={styles.statsGrid}>
                  <div className={styles.stat}>
                    <span className={styles.statLabel}>Elastic Pools</span>
                    <span className={styles.statValue}>{multiDbPools.length}</span>
                  </div>
                  <div className={styles.stat}>
                    <span className={styles.statLabel}>Standalone</span>
                    <span className={styles.statValue}>{standaloneDatabaseNames.length}</span>
                  </div>
                  <div className={styles.stat}>
                    <span className={styles.statLabel}>Individual Cost</span>
                    <span className={styles.statValue}>${summary.totalIndividualCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}<span className={styles.statUnit}>/mo</span></span>
                  </div>
                  <div className={styles.stat}>
                    <span className={styles.statLabel}>Pool Cost</span>
                    <span className={styles.statValue}>${summary.totalPoolCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}<span className={styles.statUnit}>/mo</span></span>
                  </div>
                </div>

                <div className={styles.savingsRow}>
                  <div className={`${styles.savingsCard} ${summary.savingsDollars >= 0 ? styles.savingsPositive : styles.savingsNegative}`}>
                    <span className={styles.savingsLabel}>{summary.savingsDollars >= 0 ? 'Savings' : 'Increase'}</span>
                    <span className={styles.savingsValue}>
                      <svg className={styles.savingsIcon} width="14" height="14" viewBox="0 0 16 16" fill="currentColor">
                        {summary.savingsDollars >= 0
                          ? <path d="M8 3l5 6H3l5-6z" />
                          : <path d="M8 13l5-6H3l5 6z" />}
                      </svg>
                      {Math.abs(summary.savingsPercent).toFixed(1)}%
                    </span>
                  </div>
                  <div className={`${styles.savingsCard} ${summary.savingsDollars >= 0 ? styles.savingsPositive : styles.savingsNegative}`}>
                    <span className={styles.savingsLabel}>Est. Monthly</span>
                    <span className={styles.savingsValue}>
                      {summary.savingsDollars >= 0 ? '' : '+'}${Math.abs(summary.savingsDollars).toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo
                    </span>
                  </div>
                </div>

                {summary.savingsDollars < 0 && (
                  <div className={styles.insight}>
                    <span className={styles.insightIcon}>!</span>
                    <span>
                      These databases have low peak correlation. Transitioning to a pool at the current Safety Factor ({safetyFactor.toFixed(2)}) would result in a {Math.abs(summary.savingsPercent).toFixed(1)}% cost increase compared to standalone billing. Consider adjusting the safety factor or reviewing database selection.
                    </span>
                  </div>
                )}
                {summary.savingsDollars >= 0 && (
                  <div className={styles.insightPositive}>
                    <span className={styles.insightIcon}>&#10003;</span>
                    <span>
                      Pool consolidation saves {summary.savingsPercent.toFixed(1)}% over standalone billing. Correlated usage patterns across selected databases allow efficient resource sharing.
                    </span>
                  </div>
                )}
                {standaloneDatabaseNames.length > 0 && (
                  <div className={styles.standaloneSection}>
                    <h3 className={styles.standaloneTitle}>Standalone Databases</h3>
                    <p className={styles.standaloneDesc}>These databases were not assigned to a pool due to low correlation benefit.</p>
                    <div className={styles.standaloneList}>
                      {standaloneDatabaseNames.map((name) => (
                        <div key={name} className={styles.standaloneItem}>
                          <span className={styles.standaloneName}>{name}</span>
                          <span className={styles.standaloneCost}>
                            ${getSingleDbMonthlyCost(dtuLimitsMap[name] ?? 0, poolTier).toFixed(0)}/mo
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </section>

              <section className={styles.poolsGrid}>
                {multiDbPools.map((pool) => (
                  <PoolAssignmentCard
                    key={pool.poolIndex}
                    pool={pool}
                    dtuLimits={dtuLimitsMap}
                    poolTier={poolTier}
                  />
                ))}
              </section>
            </>
          )}
        </>
      )}
    </AppLayout>
  )
}


function buildDtuLimitsMap(databases: readonly DatabaseInfo[]): Record<string, number> {
  const map: Record<string, number> = {}
  for (const db of databases) {
    map[db.databaseName] = db.dtuLimit
  }
  return map
}

function splitPools(
  pools: readonly PoolAssignment[],
  isolatedDatabases: readonly string[],
): { multiDbPools: readonly PoolAssignment[]; standaloneDatabaseNames: readonly string[] } {
  const multiDbPools: PoolAssignment[] = []
  const standaloneDatabaseNames: string[] = [...isolatedDatabases]

  for (const pool of pools) {
    if (pool.databaseNames.length < 2) {
      standaloneDatabaseNames.push(...pool.databaseNames)
    } else {
      multiDbPools.push(pool)
    }
  }

  return { multiDbPools, standaloneDatabaseNames }
}

interface CostSummary {
  readonly totalPoolCost: number
  readonly totalStandaloneCost: number
  readonly totalIndividualCost: number
  readonly savingsDollars: number
  readonly savingsPercent: number
}

function computeSummary(
  pools: readonly PoolAssignment[],
  standaloneDbs: readonly string[],
  dtuLimits: Readonly<Record<string, number>>,
  tier: PoolTier,
): CostSummary {
  const totalPoolCost = pools.reduce(
    (sum, pool) => sum + snapToPoolTier(pool.recommendedCapacity, tier).monthlyPrice,
    0,
  )

  const totalStandaloneCost = standaloneDbs.reduce(
    (sum, name) => sum + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, 'standard'),
    0,
  )

  const pooledDbsIndividualCost = pools.reduce(
    (sum, pool) => sum + pool.databaseNames.reduce(
      (s, name) => s + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, 'standard'),
      0,
    ),
    0,
  )

  const totalIndividualCost = pooledDbsIndividualCost + totalStandaloneCost
  const totalActualCost = totalPoolCost + totalStandaloneCost
  const savingsDollars = totalIndividualCost - totalActualCost
  const savingsPercent = totalIndividualCost > 0
    ? (savingsDollars / totalIndividualCost) * 100
    : 0

  return { totalPoolCost, totalStandaloneCost, totalIndividualCost, savingsDollars, savingsPercent }
}
