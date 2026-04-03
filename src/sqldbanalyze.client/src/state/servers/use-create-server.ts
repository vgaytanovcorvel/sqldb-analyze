import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useServices } from '../../core/providers'
import type { CreateRegisteredServerRequest } from '../../domain/models'

export function useCreateServer() {
  const { registeredServerRepository } = useServices()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: CreateRegisteredServerRequest) =>
      registeredServerRepository.registeredServerCreate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['servers'] })
    },
  })
}
