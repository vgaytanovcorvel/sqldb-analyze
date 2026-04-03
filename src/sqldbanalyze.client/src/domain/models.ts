export interface RegisteredServer {
  readonly registeredServerId: number
  readonly name: string
  readonly subscriptionId: string
  readonly resourceGroupName: string
  readonly serverName: string
  readonly createdAt: string
}

export interface CreateRegisteredServerRequest {
  readonly name: string
  readonly subscriptionId: string
  readonly resourceGroupName: string
  readonly serverName: string
}

export interface DatabaseMetricsInterval {
  readonly databaseName: string
  readonly earliestTimestamp: string | null
  readonly latestTimestamp: string | null
  readonly metricCount: number
}

export interface DtuTimeSeries {
  readonly timestamps: readonly string[]
  readonly databaseValues: Readonly<Record<string, readonly number[]>>
}

export interface PoolabilityMetrics {
  readonly databaseA: string
  readonly databaseB: string
  readonly pearsonCorrelation: number
  readonly peakCorrelation: number
  readonly peakOverlapFraction: number
  readonly badTogetherScore: number
}

export interface DatabaseInfo {
  readonly databaseName: string
  readonly dataSizeMB: number
  readonly dtuLimit: number
  readonly elasticPoolName: string | null
}

export interface PoolSimulationRequest {
  readonly databaseNames: readonly string[]
  readonly dtuLimits: Readonly<Record<string, number>>
}

export interface PoolSimulationResult {
  readonly databaseNames: readonly string[]
  readonly p95Dtu: number
  readonly p99Dtu: number
  readonly peakDtu: number
  readonly meanDtu: number
  readonly diversificationRatio: number
  readonly overloadFraction: number
  readonly recommendedPoolDtu: number
  readonly sumIndividualDtuLimits: number
  readonly estimatedSavingsPercent: number
}
