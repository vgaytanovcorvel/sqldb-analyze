import type { PoolAssignment } from '../../domain/models'
import type { PoolTier } from '../../domain/azure-pricing'
import { snapToPoolTier, getSingleDbMonthlyCost } from '../../domain/azure-pricing'

interface PrintReportData {
  readonly serverName: string
  readonly poolTier: PoolTier
  readonly targetPercentile: number
  readonly safetyFactor: number
  readonly maxDatabasesPerPool: number
  readonly pools: readonly PoolAssignment[]
  readonly standaloneDatabaseNames: readonly string[]
  readonly dtuLimits: Readonly<Record<string, number>>
  readonly summary: {
    readonly totalPoolCost: number
    readonly totalStandaloneCost: number
    readonly totalIndividualCost: number
    readonly savingsDollars: number
    readonly savingsPercent: number
  }
}

export function printPoolReport(data: PrintReportData): void {
  const html = buildReportHtml(data)
  const printWindow = window.open('', '_blank')
  if (!printWindow) return
  printWindow.document.write(html)
  printWindow.document.close()
}

function dollars(value: number): string {
  const abs = Math.abs(value)
  const formatted = abs.toLocaleString('en-US', { maximumFractionDigits: 0 })
  return value < 0 ? `-$${formatted}` : `$${formatted}`
}

function dollarsExact(value: number): string {
  return `$${value.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function buildPoolSection(pool: PoolAssignment, dtuLimits: Readonly<Record<string, number>>, poolTier: PoolTier): string {
  const snapped = snapToPoolTier(pool.recommendedCapacity, poolTier)
  const poolCost = snapped.monthlyPrice
  const individualCost = pool.databaseNames.reduce(
    (sum, name) => sum + getSingleDbMonthlyCost(dtuLimits[name] ?? 0, 'standard'),
    0,
  )
  const savings = individualCost - poolCost
  const savingsPct = individualCost > 0 ? (savings / individualCost) * 100 : 0

  const dbRows = pool.databaseNames.map((name) => {
    const dtu = dtuLimits[name] ?? 0
    const cost = getSingleDbMonthlyCost(dtu, poolTier)
    return `<tr><td>${name}</td><td class="mono right">${dtu}</td><td class="mono right">${dollarsExact(cost)}</td></tr>`
  }).join('')

  return `
    <div class="pool-card">
      <div class="pool-header">
        <h3>Pool ${pool.poolIndex + 1}</h3>
        <span class="badge">${pool.databaseNames.length} database${pool.databaseNames.length !== 1 ? 's' : ''}</span>
        <span class="tier">${snapped.eDtu} eDTU &mdash; ${dollarsExact(poolCost)}/mo</span>
      </div>
      <table class="stats-table">
        <tr>
          <td>Computed Need</td><td class="mono right">${pool.recommendedCapacity.toFixed(0)} DTU</td>
          <td>P95 Load</td><td class="mono right">${pool.p95Load.toFixed(0)} DTU</td>
        </tr>
        <tr>
          <td>P99 Load</td><td class="mono right">${pool.p99Load.toFixed(0)} DTU</td>
          <td>Peak Load</td><td class="mono right">${pool.peakLoad.toFixed(0)} DTU</td>
        </tr>
        <tr>
          <td>Diversification</td><td class="mono right">${pool.diversificationRatio.toFixed(2)}x</td>
          <td>Standalone Cost</td><td class="mono right">${dollars(individualCost)}/mo</td>
        </tr>
        <tr>
          <td>Savings</td>
          <td class="mono right ${savings >= 0 ? 'positive' : 'negative'}">${savingsPct.toFixed(1)}%</td>
          <td>Est. Monthly</td>
          <td class="mono right ${savings >= 0 ? 'positive' : 'negative'}">${dollars(savings)}/mo</td>
        </tr>
      </table>
      <table class="db-table">
        <thead><tr><th>Database</th><th class="right">DTU Limit</th><th class="right">Standalone Cost</th></tr></thead>
        <tbody>${dbRows}</tbody>
      </table>
    </div>`
}

function buildReportHtml(data: PrintReportData): string {
  const { serverName, poolTier, targetPercentile, safetyFactor, maxDatabasesPerPool, pools, standaloneDatabaseNames, dtuLimits, summary } = data
  const generatedAt = new Date().toLocaleString()
  const totalDatabases = pools.reduce((n, p) => n + p.databaseNames.length, 0) + standaloneDatabaseNames.length
  const percentileLabel = targetPercentile >= 0.99 ? 'P99' : 'P95'
  const tierLabel = poolTier === 'premium' ? 'Premium' : 'Standard'

  const poolSections = pools.map((p) => buildPoolSection(p, dtuLimits, poolTier)).join('')

  const standaloneRows = standaloneDatabaseNames.map((name) => {
    const dtu = dtuLimits[name] ?? 0
    const cost = getSingleDbMonthlyCost(dtu, poolTier)
    return `<tr><td>${name}</td><td class="mono right">${dtu}</td><td class="mono right">${dollarsExact(cost)}</td></tr>`
  }).join('')

  const standaloneSection = standaloneDatabaseNames.length > 0 ? `
    <h2>Standalone Databases</h2>
    <table class="db-table">
      <thead><tr><th>Database</th><th class="right">DTU Limit</th><th class="right">Monthly Cost</th></tr></thead>
      <tbody>${standaloneRows}</tbody>
    </table>` : ''

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Pool Builder Report &mdash; ${serverName}</title>
<style>
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 11pt; color: #111; padding: 24px; max-width: 1100px; margin: 0 auto; }
  h1 { font-size: 18pt; margin-bottom: 4px; }
  h2 { font-size: 13pt; margin-top: 20px; margin-bottom: 8px; border-bottom: 1px solid #ccc; padding-bottom: 4px; }
  h3 { font-size: 12pt; margin: 0; }
  .subtitle { color: #555; font-size: 9pt; margin-bottom: 16px; }
  .params { display: flex; gap: 24px; flex-wrap: wrap; margin-bottom: 16px; padding: 8px 12px; background: #f5f5f5; border-radius: 4px; font-size: 9pt; }
  .params span { white-space: nowrap; }
  .params strong { font-weight: 600; }
  .summary-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px; margin-bottom: 16px; }
  .summary-card { text-align: center; padding: 8px; border: 1px solid #ddd; border-radius: 4px; background: #fafafa; }
  .summary-card.positive { background: #ecfdf5; border-color: #22c55e; }
  .summary-card.negative { background: #fef2f2; border-color: #ef4444; }
  .summary-label { display: block; font-size: 8pt; text-transform: uppercase; letter-spacing: 0.05em; color: #666; margin-bottom: 2px; }
  .summary-value { font-size: 14pt; font-weight: 700; font-family: 'Consolas', 'Courier New', monospace; }
  .positive .summary-value { color: #15803d; }
  .negative .summary-value { color: #dc2626; }
  .pool-card { border: 1px solid #ddd; border-radius: 4px; padding: 12px; margin-bottom: 12px; page-break-inside: avoid; }
  .pool-header { display: flex; align-items: baseline; gap: 12px; margin-bottom: 8px; }
  .badge { font-size: 9pt; color: #555; }
  .tier { font-size: 9pt; font-family: 'Consolas', 'Courier New', monospace; color: #333; margin-left: auto; }
  table { width: 100%; border-collapse: collapse; font-size: 9pt; }
  .stats-table td { padding: 3px 8px; }
  .stats-table td:nth-child(odd) { color: #555; width: 20%; }
  .stats-table td:nth-child(even) { width: 30%; }
  .db-table { margin-top: 8px; }
  .db-table th { text-align: left; padding: 4px 8px; border-bottom: 1px solid #ccc; font-weight: 600; color: #555; }
  .db-table td { padding: 3px 8px; border-bottom: 1px solid #eee; }
  .mono { font-family: 'Consolas', 'Courier New', monospace; }
  .right { text-align: right; }
  .positive { color: #15803d; }
  .negative { color: #dc2626; }
  @media print {
    body { padding: 0; font-size: 10pt; }
    .pool-card { page-break-inside: avoid; }
    .no-print { display: none; }
  }
  .print-bar { text-align: center; margin-bottom: 16px; }
  .print-bar button { padding: 8px 24px; font-size: 11pt; cursor: pointer; background: #2563eb; color: #fff; border: none; border-radius: 4px; }
  .print-bar button:hover { background: #1d4ed8; }
</style>
</head>
<body>
  <div class="print-bar no-print">
    <button onclick="window.print()">Print / Save as PDF</button>
  </div>

  <h1>Elastic Pool Builder Report</h1>
  <div class="subtitle">${serverName} &mdash; Generated ${generatedAt}</div>

  <div class="params">
    <span><strong>Tier:</strong> ${tierLabel}</span>
    <span><strong>Target:</strong> ${percentileLabel} (${(targetPercentile * 100).toFixed(0)}th)</span>
    <span><strong>Safety Factor:</strong> ${safetyFactor.toFixed(2)}x</span>
    <span><strong>Max DBs/Pool:</strong> ${maxDatabasesPerPool}</span>
    <span><strong>Total Databases:</strong> ${totalDatabases}</span>
  </div>

  <div class="summary-grid">
    <div class="summary-card">
      <span class="summary-label">Pools</span>
      <span class="summary-value">${pools.length}</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Standalone DBs</span>
      <span class="summary-value">${standaloneDatabaseNames.length}</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Pool Cost</span>
      <span class="summary-value">${dollars(summary.totalPoolCost)}/mo</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Standalone Cost</span>
      <span class="summary-value">${dollars(summary.totalStandaloneCost)}/mo</span>
    </div>
    <div class="summary-card ${summary.savingsDollars >= 0 ? 'positive' : 'negative'}">
      <span class="summary-label">Savings</span>
      <span class="summary-value">${summary.savingsPercent.toFixed(1)}%</span>
    </div>
    <div class="summary-card ${summary.savingsDollars >= 0 ? 'positive' : 'negative'}">
      <span class="summary-label">Monthly Savings</span>
      <span class="summary-value">${dollars(summary.savingsDollars)}/mo</span>
    </div>
  </div>

  <h2>Elastic Pools (${pools.length})</h2>
  ${poolSections}

  ${standaloneSection}
</body>
</html>`
}
