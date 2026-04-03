import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ServersPage } from './pages/servers/servers-page'
import { AnalysisPage } from './pages/analysis/analysis-page'
import { PoolBuilderPage } from './pages/pool-builder/pool-builder-page'
import { RescaleBuilderPage } from './pages/rescale-builder/rescale-builder-page'

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ServersPage />} />
        <Route path="/analysis" element={<AnalysisPage />} />
        <Route path="/pool-builder" element={<PoolBuilderPage />} />
        <Route path="/rescale-builder" element={<RescaleBuilderPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
