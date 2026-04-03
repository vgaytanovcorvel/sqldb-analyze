import { useQuery } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useCachedIntervals(serverId: number | null) {
  const { analysisRepository } = useServices()

  return useQuery({
    queryKey: ['intervals', serverId],
    queryFn: () => analysisRepository.analysisFetchIntervals(serverId!),
    enabled: serverId !== null,
  })
}
