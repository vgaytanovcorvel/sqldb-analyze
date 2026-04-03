import { useQuery } from '@tanstack/react-query'
import { useServices } from '../../core/providers'

export function useServers() {
  const { registeredServerRepository } = useServices()

  return useQuery({
    queryKey: ['servers'],
    queryFn: () => registeredServerRepository.registeredServerFindAll(),
  })
}
