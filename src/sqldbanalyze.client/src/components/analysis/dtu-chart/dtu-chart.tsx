import { useEffect, useRef } from 'react'
import type { DtuTimeSeries } from '../../../domain/models'
import styles from './dtu-chart.module.css'

interface DtuChartProps {
  readonly timeSeries: DtuTimeSeries
  readonly selectedDatabases: ReadonlySet<string>
  readonly dtuLimits: Readonly<Record<string, number>>
  readonly p95?: number
  readonly p99?: number
  readonly onDatabaseClick?: (name: string) => void
  readonly focusedDatabase?: string | null
}

const DB_COLORS = [
  '#2563eb', '#dc2626', '#16a34a', '#ca8a04', '#9333ea',
  '#0891b2', '#e11d48', '#65a30d', '#c2410c', '#7c3aed',
  '#0d9488', '#b91c1c', '#4f46e5', '#ea580c', '#059669',
]

export function DtuChart({ timeSeries, selectedDatabases, dtuLimits, p95, p99, onDatabaseClick, focusedDatabase }: DtuChartProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  const selectedNames = Array.from(selectedDatabases).filter(
    (name) => name in timeSeries.databaseValues,
  )

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas || selectedNames.length === 0) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    const dpr = window.devicePixelRatio || 1
    const rect = canvas.getBoundingClientRect()
    canvas.width = rect.width * dpr
    canvas.height = rect.height * dpr
    ctx.scale(dpr, dpr)

    const width = rect.width
    const height = rect.height
    const padding = { top: 20, right: 20, bottom: 30, left: 50 }
    const chartWidth = width - padding.left - padding.right
    const chartHeight = height - padding.top - padding.bottom

    ctx.clearRect(0, 0, width, height)

    const combinedLoad = computeCombinedLoad(timeSeries, selectedNames, dtuLimits)
    const maxVal = Math.max(...combinedLoad, p95 ?? 0, p99 ?? 0, 1) * 1.05

    drawGrid(ctx, padding, chartWidth, chartHeight, maxVal, 'DTU')
    drawThresholdLine(ctx, padding, chartWidth, chartHeight, maxVal, p95, '#16a34a', 'P95')
    drawThresholdLine(ctx, padding, chartWidth, chartHeight, maxVal, p99, '#dc2626', 'P99')
    drawCombinedLine(ctx, padding, chartWidth, chartHeight, maxVal, combinedLoad)
    drawIndividualLines(ctx, padding, chartWidth, chartHeight, maxVal, timeSeries, selectedNames, dtuLimits)
    drawTimeAxis(ctx, padding, chartWidth, chartHeight, timeSeries.timestamps)
  }, [timeSeries, selectedNames, p95, p99])

  return (
    <div className={styles.container}>
      <canvas ref={canvasRef} className={styles.canvas} />
      <div className={styles.legend}>
        <span className={styles.legendItem}>
          <span className={styles.legendSwatch} style={{ background: '#111827', height: '3px' }} />
          Combined
        </span>
        {selectedNames.map((name, i) => (
          <span
            key={name}
            className={`${styles.legendItem} ${onDatabaseClick ? styles.legendClickable : ''} ${focusedDatabase === name ? styles.legendFocused : ''}`}
            onClick={onDatabaseClick ? () => onDatabaseClick(name) : undefined}
          >
            <span
              className={styles.legendSwatch}
              style={{ background: DB_COLORS[i % DB_COLORS.length], opacity: 0.5 }}
            />
            {name}
          </span>
        ))}
        {p95 !== undefined && (
          <span className={styles.legendItem}>
            <span className={styles.legendSwatch} style={{ background: '#16a34a' }} />
            P95 ({p95.toFixed(1)} DTU)
          </span>
        )}
        {p99 !== undefined && (
          <span className={styles.legendItem}>
            <span className={styles.legendSwatch} style={{ background: '#dc2626' }} />
            P99 ({p99.toFixed(1)} DTU)
          </span>
        )}
      </div>
    </div>
  )
}

function computeCombinedLoad(
  timeSeries: DtuTimeSeries,
  names: string[],
  dtuLimits: Readonly<Record<string, number>>,
): number[] {
  const length = timeSeries.timestamps.length
  const result = new Array<number>(length).fill(0)
  for (const name of names) {
    const values = timeSeries.databaseValues[name]
    if (!values) continue
    const limit = dtuLimits[name] ?? 0
    const scale = limit > 0 ? limit / 100 : 1
    for (let i = 0; i < length; i++) {
      result[i]! += (values[i] ?? 0) * scale
    }
  }
  return result
}

function drawGrid(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  unit: string,
) {
  ctx.strokeStyle = '#e5e7eb'
  ctx.lineWidth = 1
  ctx.font = '10px Inter, system-ui, sans-serif'
  ctx.fillStyle = '#6b7280'
  ctx.textAlign = 'right'

  const steps = 5
  for (let i = 0; i <= steps; i++) {
    const y = p.top + h - (i / steps) * h
    const val = (i / steps) * maxVal
    ctx.beginPath()
    ctx.moveTo(p.left, y)
    ctx.lineTo(p.left + w, y)
    ctx.stroke()
    ctx.fillText(`${val.toFixed(0)} ${unit}`, p.left - 5, y + 3)
  }
}

function drawThresholdLine(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  value: number | undefined,
  color: string,
  label: string,
) {
  if (value === undefined) return
  const y = p.top + h - (value / maxVal) * h
  ctx.strokeStyle = color
  ctx.lineWidth = 1.5
  ctx.setLineDash([6, 4])
  ctx.beginPath()
  ctx.moveTo(p.left, y)
  ctx.lineTo(p.left + w, y)
  ctx.stroke()
  ctx.setLineDash([])

  ctx.fillStyle = color
  ctx.font = '10px Inter, system-ui, sans-serif'
  ctx.textAlign = 'left'
  ctx.fillText(label, p.left + w + 2, y + 3)
}

function drawCombinedLine(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  data: number[],
) {
  if (data.length === 0) return
  ctx.strokeStyle = '#111827'
  ctx.lineWidth = 2
  ctx.beginPath()
  for (let i = 0; i < data.length; i++) {
    const x = p.left + (i / (data.length - 1)) * w
    const y = p.top + h - ((data[i] ?? 0) / maxVal) * h
    if (i === 0) ctx.moveTo(x, y)
    else ctx.lineTo(x, y)
  }
  ctx.stroke()
}

function drawIndividualLines(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  timeSeries: DtuTimeSeries,
  names: string[],
  dtuLimits: Readonly<Record<string, number>>,
) {
  names.forEach((name, idx) => {
    const values = timeSeries.databaseValues[name]
    if (!values || values.length === 0) return
    const limit = dtuLimits[name] ?? 0
    const scale = limit > 0 ? limit / 100 : 1
    ctx.strokeStyle = DB_COLORS[idx % DB_COLORS.length] ?? '#2563eb'
    ctx.lineWidth = 1
    ctx.globalAlpha = 0.35
    ctx.beginPath()
    for (let i = 0; i < values.length; i++) {
      const x = p.left + (i / (values.length - 1)) * w
      const y = p.top + h - (((values[i] ?? 0) * scale) / maxVal) * h
      if (i === 0) ctx.moveTo(x, y)
      else ctx.lineTo(x, y)
    }
    ctx.stroke()
    ctx.globalAlpha = 1
  })
}

function drawTimeAxis(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  timestamps: readonly string[],
) {
  if (timestamps.length === 0) return
  ctx.fillStyle = '#6b7280'
  ctx.font = '10px Inter, system-ui, sans-serif'
  ctx.textAlign = 'center'

  const labelCount = Math.min(6, timestamps.length)
  for (let i = 0; i < labelCount; i++) {
    const idx = Math.floor((i / (labelCount - 1)) * (timestamps.length - 1))
    const x = p.left + (idx / (timestamps.length - 1)) * w
    const y = p.top + h + 15
    const ts = timestamps[idx]
    if (!ts) continue
    const date = new Date(ts)
    ctx.fillText(formatShortDate(date), x, y)
  }
}

function formatShortDate(date: Date): string {
  const month = (date.getMonth() + 1).toString().padStart(2, '0')
  const day = date.getDate().toString().padStart(2, '0')
  const hours = date.getHours().toString().padStart(2, '0')
  const mins = date.getMinutes().toString().padStart(2, '0')
  return `${month}/${day} ${hours}:${mins}`
}
