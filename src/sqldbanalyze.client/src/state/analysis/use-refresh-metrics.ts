import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useRefreshMetrics() {
  const { analysisRepository } = useServices()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ serverId, hours }: { serverId: number; hours: number }) =>
      analysisRepository.analysisRefreshMetrics(serverId, hours),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['intervals', variables.serverId] })
      queryClient.invalidateQueries({ queryKey: ['correlation', variables.serverId] })
    },
  })
}
