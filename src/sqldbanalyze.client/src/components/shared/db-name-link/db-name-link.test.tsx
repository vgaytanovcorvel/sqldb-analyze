import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { DbNameLink } from './db-name-link'

describe('DbNameLink', () => {
  it('renders the database name as button text', () => {
    render(<DbNameLink name="MyDatabase" onClick={vi.fn()} />)

    expect(screen.getByRole('button', { name: 'MyDatabase' })).toBeInTheDocument()
  })

  it('calls onClick with the database name when clicked', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(<DbNameLink name="TestDb" onClick={onClick} />)

    await user.click(screen.getByRole('button', { name: 'TestDb' }))

    expect(onClick).toHaveBeenCalledOnce()
    expect(onClick).toHaveBeenCalledWith('TestDb')
  })

  it('has a tooltip indicating the action', () => {
    render(<DbNameLink name="SomeDb" onClick={vi.fn()} />)

    expect(screen.getByTitle('Click to view DTU chart')).toBeInTheDocument()
  })

  it('stops event propagation on click', async () => {
    const user = userEvent.setup()
    const parentClick = vi.fn()
    const onClick = vi.fn()

    render(
      <div onClick={parentClick}>
        <DbNameLink name="Db" onClick={onClick} />
      </div>,
    )

    await user.click(screen.getByRole('button', { name: 'Db' }))

    expect(onClick).toHaveBeenCalledOnce()
    expect(parentClick).not.toHaveBeenCalled()
  })
})
