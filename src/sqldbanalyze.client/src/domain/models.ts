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
