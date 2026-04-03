import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { PoolAssignmentCard } from './pool-assignment-card'
import type { PoolAssignment } from '../../../domain/models'

const basePool: PoolAssignment = {
  poolIndex: 0,
  databaseNames: ['db1', 'db2'],
  recommendedCapacity: 150,
  p95Load: 80,
  p99Load: 120,
  peakLoad: 200,
  diversificationRatio: 1.5,
  overloadFraction: 0.01,
}

const dtuLimits: Record<string, number> = {
  db1: 100,
  db2: 50,
}

describe('PoolAssignmentCard', () => {
  it('displays the pool number (1-based)', () => {
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    expect(screen.getByText('Pool 1')).toBeInTheDocument()
  })

  it('displays the database count', () => {
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    expect(screen.getByText('2 databases')).toBeInTheDocument()
  })

  it('shows singular form for single database', () => {
    const singlePool = { ...basePool, databaseNames: ['db1'] }
    render(<PoolAssignmentCard pool={singlePool} dtuLimits={dtuLimits} poolTier="standard" />)

    expect(screen.getByText('1 database')).toBeInTheDocument()
  })

  it('displays the snapped eDTU value', () => {
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    // 150 DTU snaps to 200 eDTU standard pool tier
    expect(screen.getByText('200 eDTU')).toBeInTheDocument()
  })

  it('shows the stats grid with P95, P99, Peak, and Diversification values', () => {
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    expect(screen.getByText('P95')).toBeInTheDocument()
    expect(screen.getByText('P99')).toBeInTheDocument()
    expect(screen.getByText('Peak')).toBeInTheDocument()
    expect(screen.getByText('Diversification')).toBeInTheDocument()
  })

  it('does not show database list initially (collapsed)', () => {
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    expect(screen.queryByText('db1')).not.toBeInTheDocument()
  })

  it('shows database list when expanded', async () => {
    const user = userEvent.setup()
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    await user.click(screen.getByText('Databases'))

    expect(screen.getByText('db1')).toBeInTheDocument()
    expect(screen.getByText('db2')).toBeInTheDocument()
  })

  it('shows DbNameLink when onDatabaseClick is provided', async () => {
    const user = userEvent.setup()
    const onDbClick = vi.fn()
    render(
      <PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" onDatabaseClick={onDbClick} />,
    )

    await user.click(screen.getByText('Databases'))

    // With onDatabaseClick, database names should be buttons
    expect(screen.getByRole('button', { name: 'db1' })).toBeInTheDocument()
  })

  it('shows plain text when onDatabaseClick is not provided', async () => {
    const user = userEvent.setup()
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    await user.click(screen.getByText('Databases'))

    // Without onDatabaseClick, database names should be plain text (not buttons)
    expect(screen.queryByRole('button', { name: 'db1' })).not.toBeInTheDocument()
    expect(screen.getByText('db1')).toBeInTheDocument()
  })

  it('collapses database list on second toggle click', async () => {
    const user = userEvent.setup()
    render(<PoolAssignmentCard pool={basePool} dtuLimits={dtuLimits} poolTier="standard" />)

    await user.click(screen.getByText('Databases'))
    expect(screen.getByText('db1')).toBeInTheDocument()

    await user.click(screen.getByText('Databases'))
    expect(screen.queryByText('db1')).not.toBeInTheDocument()
  })
})
