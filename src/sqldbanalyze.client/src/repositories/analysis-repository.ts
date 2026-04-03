import type { ApiClient } from '../core/api-client'
import type { DatabaseMetricsInterval, DtuTimeSeries, PoolabilityMetrics } from '../domain/models'

export class AnalysisRepository {
  constructor(private readonly http: ApiClient) {}

  async analysisFetchDatabases(serverId: number): Promise<readonly string[]> {
    return this.http.get<string[]>(`/analysis/${serverId}/databases`)
  }

  async analysisFetchIntervals(serverId: number): Promise<readonly DatabaseMetricsInterval[]> {
    return this.http.get<DatabaseMetricsInterval[]>(`/analysis/${serverId}/intervals`)
  }

  async analysisRefreshMetrics(serverId: number, hours: number): Promise<DtuTimeSeries> {
    return this.http.post<DtuTimeSeries>(`/analysis/${serverId}/refresh?hours=${hours}`)
  }

  async analysisFetchTimeSeries(serverId: number): Promise<DtuTimeSeries> {
    return this.http.get<DtuTimeSeries>(`/analysis/${serverId}/time-series`)
  }

  async analysisFetchCorrelationMatrix(serverId: number): Promise<readonly PoolabilityMetrics[]> {
    return this.http.get<PoolabilityMetrics[]>(`/analysis/${serverId}/correlation-matrix`)
  }
}
