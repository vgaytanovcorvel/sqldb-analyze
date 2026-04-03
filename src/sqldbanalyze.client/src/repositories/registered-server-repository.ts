import type { ApiClient } from '../core/api-client'
import type { RegisteredServer, CreateRegisteredServerRequest } from '../domain/models'

export class RegisteredServerRepository {
  constructor(private readonly http: ApiClient) {}

  async registeredServerFindAll(): Promise<readonly RegisteredServer[]> {
    return this.http.get<RegisteredServer[]>('/registered-servers')
  }

  async registeredServerCreate(data: CreateRegisteredServerRequest): Promise<RegisteredServer> {
    return this.http.post<RegisteredServer>('/registered-servers', data)
  }

  async registeredServerDelete(id: number): Promise<void> {
    await this.http.delete(`/registered-servers/${id}`)
  }
}
