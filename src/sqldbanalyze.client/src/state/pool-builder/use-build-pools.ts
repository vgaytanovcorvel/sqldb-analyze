import { useMutation } from '@tanstack/react-query'
import { useServices } from '../../core/providers'
import type { BuildPoolsRequest } from '../../domain/models'

export function useBuildPools() {
  const { analysisRepository } = useServices()

  return useMutation({
    mutationFn: ({ serverId, request }: { serverId: number; request: BuildPoolsRequest }) =>
      analysisRepository.analysisBuildPools(serverId, request),
  })
}
