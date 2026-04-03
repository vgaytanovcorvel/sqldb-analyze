import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ServersPage } from './pages/servers/servers-page'
import { AnalysisPage } from './pages/analysis/analysis-page'

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ServersPage />} />
        <Route path="/analysis" element={<AnalysisPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
