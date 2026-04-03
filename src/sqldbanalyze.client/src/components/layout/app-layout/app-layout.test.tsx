import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { AppLayout } from './app-layout'

function renderWithRouter(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>)
}

describe('AppLayout', () => {
  it('renders the page title', () => {
    renderWithRouter(
      <AppLayout title="Test Page">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.getByText('Test Page')).toBeInTheDocument()
  })

  it('renders the description when provided', () => {
    renderWithRouter(
      <AppLayout title="Title" description="Some description">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.getByText('Some description')).toBeInTheDocument()
  })

  it('does not render description element when not provided', () => {
    renderWithRouter(
      <AppLayout title="Title">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.queryByText('Some description')).not.toBeInTheDocument()
  })

  it('renders children', () => {
    renderWithRouter(
      <AppLayout title="Title">
        <div data-testid="child">Hello</div>
      </AppLayout>,
    )

    expect(screen.getByTestId('child')).toBeInTheDocument()
  })

  it('renders navigation links', () => {
    renderWithRouter(
      <AppLayout title="Title">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.getByText('Servers')).toBeInTheDocument()
    expect(screen.getByText('Analysis')).toBeInTheDocument()
    expect(screen.getByText('Pool Builder')).toBeInTheDocument()
    expect(screen.getByText('Rescale Builder')).toBeInTheDocument()
  })

  it('renders the brand name', () => {
    renderWithRouter(
      <AppLayout title="Title">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.getByText('SqlDbAnalyze')).toBeInTheDocument()
  })

  it('renders version badge', () => {
    renderWithRouter(
      <AppLayout title="Title">
        <div>content</div>
      </AppLayout>,
    )

    expect(screen.getByText('v1.0')).toBeInTheDocument()
  })
})
