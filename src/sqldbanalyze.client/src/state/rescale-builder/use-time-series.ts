import { useQuery } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useTimeSeries(serverId: number | null) {
  const { analysisRepository } = useServices()

  return useQuery({
    queryKey: ['time-series', serverId],
    queryFn: () => analysisRepository.analysisFetchTimeSeries(serverId!),
    enabled: serverId !== null,
  })
}
