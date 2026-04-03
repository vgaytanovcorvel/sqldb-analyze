import { createContext, useContext, type ReactNode } from 'react'
import { ApiClient } from './api-client'
import { RegisteredServerRepository } from '../repositories/registered-server-repository'
import { AnalysisRepository } from '../repositories/analysis-repository'

export interface Services {
  registeredServerRepository: RegisteredServerRepository
  analysisRepository: AnalysisRepository
}

const apiClient = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? '/api')
const defaultServices: Services = {
  registeredServerRepository: new RegisteredServerRepository(apiClient),
  analysisRepository: new AnalysisRepository(apiClient),
}

const ServicesContext = createContext<Services>(defaultServices)

export function ServicesProvider({ children, services = defaultServices }: {
  children: ReactNode
  services?: Services
}) {
  return <ServicesContext.Provider value={services}>{children}</ServicesContext.Provider>
}

export const useServices = () => useContext(ServicesContext)
