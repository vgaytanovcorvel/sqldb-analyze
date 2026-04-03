import type { PoolSimulationResult } from '../../../domain/models'
import { snapToPoolTier, getSingleDbMonthlyCost } from '../../../domain/azure-pricing'
import type { PoolTier } from '../../../domain/azure-pricing'
import styles from './pool-simulation.module.css'

interface PoolSimulationProps {
  readonly result: PoolSimulationResult
  readonly poolTier?: PoolTier
}

export function PoolSimulation({ result, poolTier = 'standard' }: PoolSimulationProps) {
  const savingsClass = result.estimatedSavingsPercent >= 0 ? styles.savingsPositive : styles.savingsNegative

  const snappedPool = snapToPoolTier(result.recommendedPoolDtu, poolTier)
  const poolMonthlyCost = snappedPool.monthlyPrice
  const individualMonthlyCost = result.sumIndividualDtuLimits > 0
    ? getSingleDbMonthlyCost(result.sumIndividualDtuLimits, poolTier)
    : 0
  const dollarSavings = individualMonthlyCost - poolMonthlyCost

  return (
    <div className={styles.container}>
      <div className={styles.grid}>
        <StatCard label="P95 Combined" value={result.p95Dtu.toFixed(1)} unit="DTU" />
        <StatCard label="P99 Combined" value={result.p99Dtu.toFixed(1)} unit="DTU" />
        <StatCard label="Peak Combined" value={result.peakDtu.toFixed(1)} unit="DTU" />
        <StatCard label="Mean Combined" value={result.meanDtu.toFixed(1)} unit="DTU" />
        <StatCard label="Diversification" value={result.diversificationRatio.toFixed(2)} unit="x" />
        <StatCard label="Overload Fraction" value={(result.overloadFraction * 100).toFixed(3)} unit="%" />
        <StatCard label="Pool Size" value={`${snappedPool.eDtu}`} unit="eDTU" />
        <StatCard label="Sum Individual Limits" value={result.sumIndividualDtuLimits.toFixed(0)} unit="DTU" />
        <div className={styles.card}>
          <div className={styles.cardLabel}>Estimated Savings</div>
          <div className={`${styles.cardValue} ${savingsClass}`}>
            {result.estimatedSavingsPercent.toFixed(1)}
            <span className={styles.cardUnit}>%</span>
          </div>
        </div>
        <div className={styles.card}>
          <div className={styles.cardLabel}>Est. Monthly Savings</div>
          <div className={`${styles.cardValue} ${savingsClass}`}>
            {dollarSavings >= 0 ? '' : '-'}${Math.abs(dollarSavings).toFixed(0)}
            <span className={styles.cardUnit}>/mo</span>
          </div>
        </div>
      </div>
    </div>
  )
}

function StatCard({ label, value, unit }: { label: string; value: string; unit: string }) {
  return (
    <div className={styles.card}>
      <div className={styles.cardLabel}>{label}</div>
      <div className={styles.cardValue}>
        {value}
        <span className={styles.cardUnit}>{unit}</span>
      </div>
    </div>
  )
}
