import type { RescaleRecommendation, RescaleResult } from '../../domain/rescale'
import type { PoolTier } from '../../domain/azure-pricing'

interface PrintRescaleReportData {
  readonly serverName: string
  readonly tier: PoolTier
  readonly targetPercentile: number
  readonly safetyFactor: number
  readonly result: RescaleResult
}

export function printRescaleReport(data: PrintRescaleReportData): void {
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

function directionLabel(direction: string): string {
  if (direction === 'downgrade') return 'Downgrade'
  if (direction === 'upgrade') return 'Upgrade'
  return 'No Change'
}

function directionClass(direction: string): string {
  if (direction === 'downgrade') return 'positive'
  if (direction === 'upgrade') return 'negative'
  return ''
}

function buildDbRow(rec: RescaleRecommendation): string {
  const savingsClass = rec.savingsDollars > 0 ? 'positive' : rec.savingsDollars < 0 ? 'negative' : ''
  const savingsSign = rec.savingsDollars > 0 ? '-' : rec.savingsDollars < 0 ? '+' : ''

  return `<tr>
    <td>${rec.databaseName}</td>
    <td class="${directionClass(rec.direction)}">${directionLabel(rec.direction)}</td>
    <td class="mono right">${rec.currentDtuLimit}</td>
    <td class="mono right">${rec.recommendedTier.dtu}</td>
    <td class="mono right">${rec.p95Dtu.toFixed(0)}</td>
    <td class="mono right">${rec.p99Dtu.toFixed(0)}</td>
    <td class="mono right">${rec.peakDtu.toFixed(0)}</td>
    <td class="mono right">${rec.meanDtu.toFixed(0)}</td>
    <td class="mono right">${dollarsExact(rec.currentMonthlyCost)}</td>
    <td class="mono right">${dollarsExact(rec.recommendedMonthlyCost)}</td>
    <td class="mono right ${savingsClass}">${savingsSign}${dollarsExact(Math.abs(rec.savingsDollars))}</td>
  </tr>`
}

function buildReportHtml(data: PrintRescaleReportData): string {
  const { serverName, tier, targetPercentile, safetyFactor, result } = data
  const generatedAt = new Date().toLocaleString()
  const percentileLabel = targetPercentile >= 0.99 ? 'P99' : 'P95'
  const tierLabel = tier === 'premium' ? 'Premium' : 'Standard'

  const sorted = [...result.recommendations].sort((a, b) => b.savingsDollars - a.savingsDollars)
  const downgrades = sorted.filter((r) => r.direction === 'downgrade')
  const upgrades = sorted.filter((r) => r.direction === 'upgrade')
  const keeps = sorted.filter((r) => r.direction === 'keep')

  const allRows = sorted.map(buildDbRow).join('')
  const downgradeRows = downgrades.map(buildDbRow).join('')
  const upgradeRows = upgrades.map(buildDbRow).join('')
  const keepRows = keeps.map(buildDbRow).join('')

  const tableHead = `<thead><tr>
    <th>Database</th><th>Direction</th>
    <th class="right">Current DTU</th><th class="right">Recommended</th>
    <th class="right">P95</th><th class="right">P99</th>
    <th class="right">Peak</th><th class="right">Mean</th>
    <th class="right">Current $/mo</th><th class="right">New $/mo</th>
    <th class="right">Savings</th>
  </tr></thead>`

  const downgradeSection = downgrades.length > 0 ? `
    <h2>Downgrades (${downgrades.length})</h2>
    <table class="db-table">${tableHead}<tbody>${downgradeRows}</tbody></table>` : ''

  const upgradeSection = upgrades.length > 0 ? `
    <h2>Upgrades (${upgrades.length})</h2>
    <table class="db-table">${tableHead}<tbody>${upgradeRows}</tbody></table>` : ''

  const keepSection = keeps.length > 0 ? `
    <h2>No Change (${keeps.length})</h2>
    <table class="db-table">${tableHead}<tbody>${keepRows}</tbody></table>` : ''

  return `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>Rescale Builder Report &mdash; ${serverName}</title>
<style>
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 11pt; color: #111; padding: 24px; max-width: 1200px; margin: 0 auto; }
  h1 { font-size: 18pt; margin-bottom: 4px; }
  h2 { font-size: 13pt; margin-top: 20px; margin-bottom: 8px; border-bottom: 1px solid #ccc; padding-bottom: 4px; }
  .subtitle { color: #555; font-size: 9pt; margin-bottom: 16px; }
  .params { display: flex; gap: 24px; flex-wrap: wrap; margin-bottom: 16px; padding: 8px 12px; background: #f5f5f5; border-radius: 4px; font-size: 9pt; }
  .params span { white-space: nowrap; }
  .params strong { font-weight: 600; }
  .summary-grid { display: grid; grid-template-columns: repeat(5, 1fr); gap: 8px; margin-bottom: 8px; }
  .summary-card { text-align: center; padding: 8px; border: 1px solid #ddd; border-radius: 4px; background: #fafafa; }
  .summary-card.positive { background: #ecfdf5; border-color: #22c55e; }
  .summary-card.negative { background: #fef2f2; border-color: #ef4444; }
  .summary-label { display: block; font-size: 8pt; text-transform: uppercase; letter-spacing: 0.05em; color: #666; margin-bottom: 2px; }
  .summary-value { font-size: 14pt; font-weight: 700; font-family: 'Consolas', 'Courier New', monospace; }
  .positive .summary-value { color: #15803d; }
  .negative .summary-value { color: #dc2626; }
  .savings-row { display: grid; grid-template-columns: repeat(2, 1fr); gap: 8px; margin-bottom: 16px; }
  .savings-card { text-align: center; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
  .savings-card.positive { background: #ecfdf5; border-color: #22c55e; }
  .savings-card.negative { background: #fef2f2; border-color: #ef4444; }
  .savings-card .summary-label { display: block; font-size: 8pt; text-transform: uppercase; letter-spacing: 0.05em; color: #666; margin-bottom: 2px; }
  .savings-card .summary-value { font-size: 14pt; font-weight: 700; font-family: 'Consolas', 'Courier New', monospace; }
  .savings-card.positive .summary-value { color: #15803d; }
  .savings-card.negative .summary-value { color: #dc2626; }
  .insight { margin-top: 12px; padding: 8px 12px; border-radius: 4px; font-size: 9pt; background: #f0f9ff; border: 1px solid #bae6fd; color: #0369a1; }
  .insight-positive { margin-top: 12px; padding: 8px 12px; border-radius: 4px; font-size: 9pt; background: #ecfdf5; border: 1px solid #22c55e; color: #15803d; }
  table { width: 100%; border-collapse: collapse; font-size: 9pt; }
  .db-table { margin-top: 8px; }
  .db-table th { text-align: left; padding: 4px 8px; border-bottom: 1px solid #ccc; font-weight: 600; color: #555; }
  .db-table td { padding: 3px 8px; border-bottom: 1px solid #eee; }
  .mono { font-family: 'Consolas', 'Courier New', monospace; }
  .right { text-align: right; }
  .positive { color: #15803d; }
  .negative { color: #dc2626; }
  @media print {
    body { padding: 0; font-size: 10pt; }
    .no-print { display: none; }
    h2 { page-break-after: avoid; }
    .db-table { page-break-inside: auto; }
    .db-table tr { page-break-inside: avoid; }
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

  <h1>Rescale Builder Report</h1>
  <div class="subtitle">${serverName} &mdash; Generated ${generatedAt}</div>

  <div class="params">
    <span><strong>Tier:</strong> ${tierLabel}</span>
    <span><strong>Target:</strong> ${percentileLabel} (${(targetPercentile * 100).toFixed(0)}th)</span>
    <span><strong>Safety Factor:</strong> ${safetyFactor.toFixed(2)}x</span>
    <span><strong>Total Databases:</strong> ${result.recommendations.length}</span>
  </div>

  <div class="summary-grid">
    <div class="summary-card">
      <span class="summary-label">Databases</span>
      <span class="summary-value">${result.recommendations.length}</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Downgrades</span>
      <span class="summary-value">${result.downgradeCount}</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Upgrades</span>
      <span class="summary-value">${result.upgradeCount}</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Current Cost</span>
      <span class="summary-value">${dollars(result.totalCurrentCost)}/mo</span>
    </div>
    <div class="summary-card">
      <span class="summary-label">Recommended Cost</span>
      <span class="summary-value">${dollars(result.totalRecommendedCost)}/mo</span>
    </div>
  </div>

  <div class="savings-row">
    <div class="savings-card ${result.totalSavingsDollars >= 0 ? 'positive' : 'negative'}">
      <span class="summary-label">${result.totalSavingsDollars >= 0 ? 'Savings' : 'Increase'}</span>
      <span class="summary-value">${Math.abs(result.totalSavingsPercent).toFixed(1)}%</span>
    </div>
    <div class="savings-card ${result.totalSavingsDollars >= 0 ? 'positive' : 'negative'}">
      <span class="summary-label">Est. Monthly</span>
      <span class="summary-value">${result.totalSavingsDollars >= 0 ? '' : '+'}${dollars(Math.abs(result.totalSavingsDollars))}/mo</span>
    </div>
  </div>

  ${result.downgradeCount > 0 && result.totalSavingsDollars > 0 ? `
  <div class="insight-positive">
    &#10003; ${result.downgradeCount} database${result.downgradeCount !== 1 ? 's are' : ' is'} over-provisioned.
    Rescaling to recommended tiers saves ${result.totalSavingsPercent.toFixed(1)}% (${dollars(Math.abs(result.totalSavingsDollars))}/mo).
  </div>` : ''}

  ${result.upgradeCount > 0 ? `
  <div class="insight">
    ! ${result.upgradeCount} database${result.upgradeCount !== 1 ? 's are' : ' is'} under-provisioned and may experience throttling.
    Consider upgrading to ensure consistent performance.
  </div>` : ''}

  ${result.downgradeCount === 0 && result.upgradeCount === 0 ? `
  <div class="insight">
    &mdash; All databases are appropriately sized for the selected percentile and safety factor.
  </div>` : ''}

  <h2>All Databases (${result.recommendations.length})</h2>
  <table class="db-table">
    ${tableHead}
    <tbody>${allRows}</tbody>
  </table>

  ${downgradeSection}
  ${upgradeSection}
  ${keepSection}
</body>
</html>`
}
