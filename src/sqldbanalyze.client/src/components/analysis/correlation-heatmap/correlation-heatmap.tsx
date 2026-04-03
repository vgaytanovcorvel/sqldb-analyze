import type { PoolabilityMetrics } from '../../../domain/models'
import styles from './correlation-heatmap.module.css'

interface CorrelationHeatmapProps {
  readonly data: readonly PoolabilityMetrics[]
}

export function CorrelationHeatmap({ data }: CorrelationHeatmapProps) {
  const dbNames = extractDatabaseNames(data)
  const correlationMap = buildCorrelationMap(data)

  return (
    <div>
      <div className={styles.container}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th />
              {dbNames.map((name) => (
                <th key={name} className={styles.headerCell} title={name}>
                  {name}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {dbNames.map((rowDb) => (
              <tr key={rowDb}>
                <td className={styles.rowHeader} title={rowDb}>{rowDb}</td>
                {dbNames.map((colDb) => {
                  if (rowDb === colDb) {
                    return (
                      <td key={colDb} className={styles.diagonal}>1.00</td>
                    )
                  }
                  const correlation = getCorrelation(correlationMap, rowDb, colDb)
                  return (
                    <td
                      key={colDb}
                      style={{ background: correlationToColor(correlation) }}
                      title={`${rowDb} / ${colDb}: ${correlation.toFixed(3)}`}
                    >
                      {correlation.toFixed(2)}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className={styles.legend}>
        <span>-1.0</span>
        <div className={styles.legendBar}>
          <div className={styles.legendSegment} style={{ background: correlationToColor(-1) }} />
          <div className={styles.legendSegment} style={{ background: correlationToColor(-0.5) }} />
          <div className={styles.legendSegment} style={{ background: correlationToColor(0) }} />
          <div className={styles.legendSegment} style={{ background: correlationToColor(0.5) }} />
          <div className={styles.legendSegment} style={{ background: correlationToColor(1) }} />
        </div>
        <span>+1.0</span>
        <span>(green = negatively correlated = good pool candidates)</span>
      </div>
    </div>
  )
}

function extractDatabaseNames(data: readonly PoolabilityMetrics[]): string[] {
  const names = new Set<string>()
  for (const item of data) {
    names.add(item.databaseA)
    names.add(item.databaseB)
  }
  return [...names].sort()
}

function buildCorrelationMap(data: readonly PoolabilityMetrics[]): Map<string, number> {
  const map = new Map<string, number>()
  for (const item of data) {
    const keyAB = `${item.databaseA}|${item.databaseB}`
    const keyBA = `${item.databaseB}|${item.databaseA}`
    map.set(keyAB, item.pearsonCorrelation)
    map.set(keyBA, item.pearsonCorrelation)
  }
  return map
}

function getCorrelation(map: Map<string, number>, a: string, b: string): number {
  return map.get(`${a}|${b}`) ?? 0
}

function correlationToColor(r: number): string {
  const clamped = Math.max(-1, Math.min(1, r))

  if (clamped < 0) {
    const t = Math.abs(clamped)
    const red = Math.round(34 + (1 - t) * (255 - 34))
    const green = Math.round(197 + (1 - t) * (255 - 197))
    const blue = Math.round(94 + (1 - t) * (255 - 94))
    return `rgb(${red}, ${green}, ${blue})`
  }

  const t = clamped
  const red = Math.round(255 - t * (255 - 239))
  const green = Math.round(255 - t * (255 - 68))
  const blue = Math.round(255 - t * (255 - 68))
  return `rgb(${red}, ${green}, ${blue})`
}
