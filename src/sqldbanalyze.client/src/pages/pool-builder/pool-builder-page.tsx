import { useMemo } from 'react'
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

  const hasIntervals = intervals.length > 0
  const allDatabaseNames = intervals.map((i) => i.databaseName)
  const selectionCount = selectedDatabaseNames.size
  const allSelected = selectionCount > 0 && selectionCount === allDatabaseNames.length

  const dtuLimitsMap = useMemo(() => buildDtuLimitsMap(databases), [databases])

  const result = buildPools.data

  // Split pools: multi-DB pools are real pools, single-DB pools become standalone
  const { multiDbPools, standaloneDatabaseNames } = useMemo(
    () => splitPools(result?.pools ?? [], result?.isolatedDatabases ?? []),
    [result],
  )

  // Compute summary costs using real pricing
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
    <main className={styles.page}>
      <nav className={styles.nav}>
        <Link to="/" className={styles.navLink}>Registered Servers</Link>
        <span className={styles.navSep}>/</span>
        <Link to="/analysis" className={styles.navLink}>Analysis</Link>
      </nav>

      <div className={styles.header}>
        <h1 className={styles.title}>Pool Builder</h1>
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
      </div>

      {selectedServerId !== null && !hasIntervals && (
        <div className={styles.empty}>
          No cached metrics. Go to <Link to="/analysis" className={styles.inlineLink}>Analysis</Link> to fetch metrics first.
        </div>
      )}

      {selectedServerId !== null && hasIntervals && (
        <>
          <div className={styles.twoColumn}>
            <section className={styles.selectionPanel}>
              <div className={styles.sectionHeader}>
                <h2 className={styles.sectionTitle}>Select Databases</h2>
                <div className={styles.selectionControls}>
                  <button className={styles.selectButton} onClick={handleToggleAll}>
                    {allSelected ? 'Clear All' : 'Select All'}
                  </button>
                  {selectionCount > 0 && (
                    <span className={styles.selectionBadge}>{selectionCount} selected</span>
                  )}
                </div>
              </div>

              <div className={styles.dbListContainer}>
                {intervals.map((interval) => {
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
                <div className={styles.summaryGrid}>
                  <SummaryCard label="Pools" value={String(multiDbPools.length)} />
                  <SummaryCard label="Standalone DBs" value={String(standaloneDatabaseNames.length)} />
                  <SummaryCard label="Total Pool Cost" value={`$${summary.totalPoolCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo`} />
                  <SummaryCard label="Total Standalone Cost" value={`$${summary.totalStandaloneCost.toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo`} />
                  <div className={`${styles.summaryCard} ${summary.savingsDollars >= 0 ? styles.summaryPositive : styles.summaryNegative}`}>
                    <span className={styles.summaryLabel}>Savings</span>
                    <span className={styles.summaryValue}>{summary.savingsPercent.toFixed(1)}%</span>
                  </div>
                  <div className={`${styles.summaryCard} ${summary.savingsDollars >= 0 ? styles.summaryPositive : styles.summaryNegative}`}>
                    <span className={styles.summaryLabel}>Est. Monthly Savings</span>
                    <span className={styles.summaryValue}>
                      {summary.savingsDollars >= 0 ? '' : '-'}${Math.abs(summary.savingsDollars).toLocaleString(undefined, { maximumFractionDigits: 0 })}/mo
                    </span>
                  </div>
                </div>
                {standaloneDatabaseNames.length > 0 && (
                  <div className={styles.isolated}>
                    <span className={styles.isolatedLabel}>Standalone (not pooled):</span>
                    {standaloneDatabaseNames.map((name) => (
                      <span key={name} className={styles.isolatedTag}>
                        {name}
                        <span className={styles.isolatedCost}>
                          ${getSingleDbMonthlyCost(dtuLimitsMap[name] ?? 0, poolTier).toFixed(0)}/mo
                        </span>
                      </span>
                    ))}
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
    </main>
  )
}

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.summaryCard}>
      <span className={styles.summaryLabel}>{label}</span>
      <span className={styles.summaryValue}>{value}</span>
    </div>
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
  // Cost of all pools at snapped tier sizes
  const totalPoolCost = pools.reduce(
    (sum, pool) => sum + snapToPoolTier(pool.recommendedCapacity, tier).monthlyPrice,
    0,
  )

  // Cost of standalone databases as individual single-DB plans
  const totalStandaloneCost = standaloneDbs.reduce(
    (sum, name) => sum + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, tier),
    0,
  )

  // What it would cost if every pooled DB was standalone
  const pooledDbsIndividualCost = pools.reduce(
    (sum, pool) => sum + pool.databaseNames.reduce(
      (s, name) => s + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, tier),
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
