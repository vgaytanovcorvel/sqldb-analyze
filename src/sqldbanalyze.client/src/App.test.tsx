import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ServicesProvider } from './core/providers'
import { App } from './App'

// Mock the page components to avoid rendering their full trees
vi.mock('./pages/servers/servers-page', () => ({
  ServersPage: () => <div data-testid="servers-page">Servers</div>,
}))
vi.mock('./pages/analysis/analysis-page', () => ({
  AnalysisPage: () => <div data-testid="analysis-page">Analysis</div>,
}))
vi.mock('./pages/pool-builder/pool-builder-page', () => ({
  PoolBuilderPage: () => <div data-testid="pool-builder-page">Pool Builder</div>,
}))
vi.mock('./pages/rescale-builder/rescale-builder-page', () => ({
  RescaleBuilderPage: () => <div data-testid="rescale-builder-page">Rescale Builder</div>,
}))

function renderApp() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return render(
    <ServicesProvider>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </ServicesProvider>,
  )
}

describe('App', () => {
  it('renders the servers page at root route', () => {
    // BrowserRouter defaults to current URL which is "/" in test
    renderApp()

    expect(screen.getByTestId('servers-page')).toBeInTheDocument()
  })
})
