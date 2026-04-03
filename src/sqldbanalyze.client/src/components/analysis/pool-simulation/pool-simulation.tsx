import type { PoolSimulationResult } from '../../../domain/models'
import styles from './pool-simulation.module.css'

interface PoolSimulationProps {
  readonly result: PoolSimulationResult
}

export function PoolSimulation({ result }: PoolSimulationProps) {
  const savingsClass = result.estimatedSavingsPercent >= 0 ? styles.savingsPositive : styles.savingsNegative

  return (
    <div className={styles.container}>
      <div className={styles.grid}>
        <StatCard label="P95 Combined" value={result.p95Dtu.toFixed(1)} unit="DTU" />
        <StatCard label="P99 Combined" value={result.p99Dtu.toFixed(1)} unit="DTU" />
        <StatCard label="Peak Combined" value={result.peakDtu.toFixed(1)} unit="DTU" />
        <StatCard label="Mean Combined" value={result.meanDtu.toFixed(1)} unit="DTU" />
        <StatCard label="Diversification" value={result.diversificationRatio.toFixed(2)} unit="x" />
        <StatCard label="Overload Fraction" value={(result.overloadFraction * 100).toFixed(3)} unit="%" />
        <StatCard label="Recommended Pool" value={result.recommendedPoolDtu.toFixed(0)} unit="DTU" />
        <StatCard label="Sum Individual Limits" value={result.sumIndividualDtuLimits.toFixed(0)} unit="DTU" />
        <div className={styles.card}>
          <div className={styles.cardLabel}>Estimated Savings</div>
          <div className={`${styles.cardValue} ${savingsClass}`}>
            {result.estimatedSavingsPercent.toFixed(1)}
            <span className={styles.cardUnit}>%</span>
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
