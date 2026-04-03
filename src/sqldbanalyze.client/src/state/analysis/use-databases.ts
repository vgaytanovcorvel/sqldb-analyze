import { useQuery } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useDatabases(serverId: number | null) {
  const { analysisRepository } = useServices()

  return useQuery({
    queryKey: ['databases', serverId],
    queryFn: () => analysisRepository.analysisFetchDatabases(serverId!),
    enabled: serverId !== null,
  })
}
