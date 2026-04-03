import { useMutation } from '@tanstack/react-query'
import { useServices } from '../../core/providers'
import type { PoolSimulationRequest } from '../../domain/models'

export function useSimulatePool() {
  const { analysisRepository } = useServices()

  return useMutation({
    mutationFn: ({ serverId, request }: { serverId: number; request: PoolSimulationRequest }) =>
      analysisRepository.analysisSimulatePool(serverId, request),
  })
}
