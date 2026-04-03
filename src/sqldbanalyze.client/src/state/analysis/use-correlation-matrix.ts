import { useQuery } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useCorrelationMatrix(serverId: number | null) {
  const { analysisRepository } = useServices()

  return useQuery({
    queryKey: ['correlation', serverId],
    queryFn: () => analysisRepository.analysisFetchCorrelationMatrix(serverId!),
    enabled: serverId !== null,
  })
}
