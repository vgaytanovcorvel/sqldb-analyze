import { createContext, useContext, type ReactNode } from 'react'
import { ApiClient } from './api-client'

interface Services {
  apiClient: ApiClient
}

const apiClient = new ApiClient(import.meta.env.VITE_API_BASE_URL ?? '/api')
const defaultServices: Services = {
  apiClient,
}

const ServicesContext = createContext<Services>(defaultServices)

export function ServicesProvider({ children, services = defaultServices }: {
  children: ReactNode
  services?: Services
}) {
  return <ServicesContext.Provider value={services}>{children}</ServicesContext.Provider>
}

export const useServices = () => useContext(ServicesContext)
