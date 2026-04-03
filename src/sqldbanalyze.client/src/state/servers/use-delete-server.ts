import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useDeleteServer() {
  const { registeredServerRepository } = useServices()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      registeredServerRepository.registeredServerDelete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['servers'] })
    },
  })
}
