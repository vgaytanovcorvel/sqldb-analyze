import { useState } from 'react'
import type { PoolAssignment } from '../../../domain/models'
import type { PoolTier } from '../../../domain/azure-pricing'
import { snapToPoolTier, getSingleDbMonthlyCost } from '../../../domain/azure-pricing'
import { DbNameLink } from '../../shared/db-name-link/db-name-link'
import styles from './pool-assignment-card.module.css'

interface PoolAssignmentCardProps {
  readonly pool: PoolAssignment
  readonly dtuLimits: Readonly<Record<string, number>>
  readonly poolTier: PoolTier
  readonly onDatabaseClick?: (name: string) => void
}

export function PoolAssignmentCard({ pool, dtuLimits, poolTier, onDatabaseClick }: PoolAssignmentCardProps) {
  const [dbExpanded, setDbExpanded] = useState(false)
  const snapped = snapToPoolTier(pool.recommendedCapacity, poolTier)
  const poolMonthlyCost = snapped.monthlyPrice

  const individualMonthlyCost = pool.databaseNames.reduce(
    (sum, name) => sum + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, poolTier),
    0,
  )

  const savingsDollars = individualMonthlyCost - poolMonthlyCost
  const savingsPercent = individualMonthlyCost > 0
    ? (savingsDollars / individualMonthlyCost) * 100
    : 0

  const savingsClass = savingsDollars >= 0 ? styles.savingsPositive : styles.savingsNegative

  return (
    <div className={styles.card}>
      <div className={styles.header}>
        <h3 className={styles.poolTitle}>Pool {pool.poolIndex + 1}</h3>
        <span className={styles.dbCount}>{pool.databaseNames.length} database{pool.databaseNames.length !== 1 ? 's' : ''}</span>
      </div>

      <div className={styles.tierRow}>
        <span className={styles.tierLabel}>Pool Size</span>
        <span className={styles.tierValue}>{snapped.eDtu} eDTU</span>
        <span className={styles.tierCost}>${poolMonthlyCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}/mo</span>
      </div>

      <div className={styles.statsGrid}>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Computed Need</span>
          <span className={styles.statValue}>{pool.recommendedCapacity.toFixed(0)} <span className={styles.statUnit}>DTU</span></span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>P95</span>
          <span className={styles.statValue}>{pool.p95Load.toFixed(0)} <span className={styles.statUnit}>DTU</span></span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>P99</span>
          <span className={styles.statValue}>{pool.p99Load.toFixed(0)} <span className={styles.statUnit}>DTU</span></span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Peak</span>
          <span className={styles.statValue}>{pool.peakLoad.toFixed(0)} <span className={styles.statUnit}>DTU</span></span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Diversification</span>
          <span className={styles.statValue}>{pool.diversificationRatio.toFixed(2)}<span className={styles.statUnit}>x</span></span>
        </div>
        <div className={styles.stat}>
          <span className={styles.statLabel}>Standalone Cost</span>
          <span className={styles.statValue}>${individualMonthlyCost.toFixed(0)}<span className={styles.statUnit}>/mo</span></span>
        </div>
      </div>

      <div className={styles.savingsRow}>
        <div className={`${styles.savingsCard} ${savingsClass}`}>
          <span className={styles.savingsLabel}>Savings</span>
          <span className={styles.savingsValue}>{savingsPercent.toFixed(1)}%</span>
        </div>
        <div className={`${styles.savingsCard} ${savingsClass}`}>
          <span className={styles.savingsLabel}>Est. Monthly</span>
          <span className={styles.savingsValue}>
            {savingsDollars >= 0 ? '' : '-'}${Math.abs(savingsDollars).toFixed(0)}/mo
          </span>
        </div>
      </div>

      <div className={styles.databases}>
        <button className={styles.dbToggle} onClick={() => setDbExpanded(!dbExpanded)}>
          <svg
            className={`${styles.dbChevron} ${dbExpanded ? styles.dbChevronOpen : ''}`}
            width="14" height="14" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2"
          >
            <path d="M6 4l4 4-4 4" />
          </svg>
          <span className={styles.dbLabel}>Databases</span>
          <span className={styles.dbToggleCount}>{pool.databaseNames.length}</span>
        </button>
        {dbExpanded && (
          <ul className={styles.dbList}>
            {pool.databaseNames.map((name) => (
              <li key={name} className={styles.dbItem}>
                {onDatabaseClick
                  ? <DbNameLink name={name} onClick={onDatabaseClick} />
                  : name}
                <span className={styles.dbDtu}>{dtuLimits[name] ?? '?'} DTU</span>
                <span className={styles.dbCost}>${getSingleDbMonthlyCost(dtuLimits[name] ?? 0, poolTier).toFixed(0)}/mo</span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
