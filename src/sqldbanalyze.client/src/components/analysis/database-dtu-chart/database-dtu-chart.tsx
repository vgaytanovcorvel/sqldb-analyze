import { useEffect, useMemo, useRef } from 'react'
import { createPortal } from 'react-dom'
import type { DtuTimeSeries } from '../../../domain/models'
import styles from './database-dtu-chart.module.css'

interface DatabaseDtuChartProps {
  readonly timeSeries: DtuTimeSeries
  readonly databaseName: string
  readonly dtuLimit: number
  readonly recommendedDtu: number | null
  readonly onClose: () => void
}

export function DatabaseDtuChart({ timeSeries, databaseName, dtuLimit, recommendedDtu, onClose }: DatabaseDtuChartProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const values = timeSeries.databaseValues[databaseName]

  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [onClose])

  const stats = useMemo(() => {
    if (!values || values.length === 0 || dtuLimit <= 0) return null
    const abs = values.map((v) => (v * dtuLimit) / 100)
    const sorted = [...abs].sort((a, b) => a - b)
    const p95 = sorted[Math.max(0, Math.ceil(sorted.length * 0.95) - 1)] ?? 0
    const p99 = sorted[Math.max(0, Math.ceil(sorted.length * 0.99) - 1)] ?? 0
    const peak = sorted[sorted.length - 1] ?? 0
    const mean = abs.reduce((a, b) => a + b, 0) / abs.length
    return { p95, p99, peak, mean }
  }, [values, dtuLimit])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas || !values || values.length === 0) return

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

    const absoluteValues = values.map((v) => (dtuLimit > 0 ? (v * dtuLimit) / 100 : v))
    const peakUsage = Math.max(...absoluteValues, 1)
    const maxVal = peakUsage * 1.15

    drawGrid(ctx, padding, chartWidth, chartHeight, maxVal)
    drawDtuLimitLine(ctx, padding, chartWidth, chartHeight, maxVal, dtuLimit)

    if (recommendedDtu !== null && recommendedDtu !== dtuLimit) {
      drawRecommendedLine(ctx, padding, chartWidth, chartHeight, maxVal, recommendedDtu)
    }

    drawDataLine(ctx, padding, chartWidth, chartHeight, maxVal, absoluteValues)
    drawTimeAxis(ctx, padding, chartWidth, chartHeight, timeSeries.timestamps)
  }, [timeSeries, databaseName, dtuLimit, recommendedDtu, values])

  if (!values || values.length === 0) return null

  const isDifferent = recommendedDtu !== null && dtuLimit > 0 && recommendedDtu !== dtuLimit

  return createPortal(
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.popup} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <span className={styles.title}>{databaseName}</span>
          <button className={styles.closeButton} onClick={onClose}>Close</button>
        </div>
        {stats && (
          <div className={styles.stats}>
            <span className={styles.stat}>
              Current: <span className={styles.statValue}>{dtuLimit} DTU</span>
            </span>
            {isDifferent && (
              <span className={styles.stat}>
                Recommended: <span className={`${styles.statValue} ${styles.statHighlight}`}>{recommendedDtu} DTU</span>
              </span>
            )}
            <span className={styles.stat}>
              P95: <span className={styles.statValue}>{stats.p95.toFixed(1)}</span>
            </span>
            <span className={styles.stat}>
              P99: <span className={styles.statValue}>{stats.p99.toFixed(1)}</span>
            </span>
            <span className={styles.stat}>
              Peak: <span className={styles.statValue}>{stats.peak.toFixed(1)}</span>
            </span>
            <span className={styles.stat}>
              Mean: <span className={styles.statValue}>{stats.mean.toFixed(1)}</span>
            </span>
          </div>
        )}
        <canvas ref={canvasRef} className={styles.canvas} />
        <div className={styles.legend}>
          <span className={styles.legendItem}>
            <span className={styles.legendSwatch} style={{ background: '#2563eb' }} />
            DTU Usage
          </span>
          <span className={styles.legendItem}>
            <span className={styles.legendSwatch} style={{ background: '#9ca3af', height: '2px' }} />
            Current Limit ({dtuLimit} DTU)
          </span>
          {isDifferent && (
            <span className={styles.legendItem}>
              <span className={styles.legendSwatch} style={{ background: '#16a34a' }} />
              Recommended ({recommendedDtu} DTU)
            </span>
          )}
        </div>
      </div>
    </div>,
    document.body,
  )
}

function drawGrid(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
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
    ctx.fillText(`${val.toFixed(0)} DTU`, p.left - 5, y + 3)
  }
}

function drawDtuLimitLine(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  limit: number,
) {
  if (limit <= 0) return
  const y = p.top + h - (limit / maxVal) * h
  ctx.strokeStyle = '#9ca3af'
  ctx.lineWidth = 1.5
  ctx.setLineDash([6, 4])
  ctx.beginPath()
  ctx.moveTo(p.left, y)
  ctx.lineTo(p.left + w, y)
  ctx.stroke()
  ctx.setLineDash([])
}

function drawRecommendedLine(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  recommended: number,
) {
  const y = p.top + h - (recommended / maxVal) * h
  ctx.strokeStyle = '#16a34a'
  ctx.lineWidth = 1.5
  ctx.setLineDash([6, 4])
  ctx.beginPath()
  ctx.moveTo(p.left, y)
  ctx.lineTo(p.left + w, y)
  ctx.stroke()
  ctx.setLineDash([])
}

function drawDataLine(
  ctx: CanvasRenderingContext2D,
  p: { top: number; right: number; bottom: number; left: number },
  w: number,
  h: number,
  maxVal: number,
  data: number[],
) {
  if (data.length === 0) return
  ctx.strokeStyle = '#2563eb'
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
    const month = (date.getMonth() + 1).toString().padStart(2, '0')
    const day = date.getDate().toString().padStart(2, '0')
    const hours = date.getHours().toString().padStart(2, '0')
    const mins = date.getMinutes().toString().padStart(2, '0')
    ctx.fillText(`${month}/${day} ${hours}:${mins}`, x, y)
  }
}
