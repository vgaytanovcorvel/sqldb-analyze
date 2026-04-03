import type { ApiClient } from '../core/api-client'
import type {
  DatabaseInfo,
  DatabaseMetricsInterval,
  DtuTimeSeries,
  PoolabilityMetrics,
  PoolSimulationRequest,
  PoolSimulationResult,
} from '../domain/models'

export class AnalysisRepository {
  constructor(private readonly http: ApiClient) {}

  async analysisFetchDatabases(serverId: number): Promise<readonly DatabaseInfo[]> {
    return this.http.get<DatabaseInfo[]>(`/analysis/${serverId}/databases`)
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

  async analysisSimulatePool(serverId: number, request: PoolSimulationRequest): Promise<PoolSimulationResult> {
    return this.http.post<PoolSimulationResult>(`/analysis/${serverId}/simulate-pool`, request)
  }
}
