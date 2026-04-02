# Correlation-Aware Elastic Pool Optimization Algorithm

## Overview

Traditional elastic pool sizing sums individual database peaks or averages, ignoring workload correlation. This wastes money when databases spike at different times (oversized pools) or causes throttling when they spike together (undersized pools).

This algorithm treats pool assignment as **stochastic bin packing under uncertainty**: each pool is a bin, each database is a time-varying load, and the objective is to minimize total pool capacity while keeping overload risk below a configurable threshold.

The key insight: **what matters for pooling is not each database's peak, but how often their peaks overlap.**

## Two-Phase Workflow

### Phase 1: Capture (`capture` command)

Connects to Azure Monitor, fetches DTU percentage metrics at 5-minute granularity for all databases on a SQL Server, aligns timestamps, and writes a CSV file.

```
sqldb-analyze capture <server-name> -s <sub-id> -g <rg> [--hours 168] [-o metrics.csv]
```

CSV format: `Timestamp,db1,db2,...,dbN` with one row per 5-minute interval.

### Phase 2: Build Pools (`build-pools` command)

Reads CSV files, computes pairwise correlation metrics, and runs a greedy optimizer with local search improvement.

```
sqldb-analyze build-pools metrics.csv [--target-percentile 0.99] [--safety-factor 1.10]
```

## Statistical Foundations

### Percentile Calculation

For a sorted array of N values, the p-th percentile uses linear interpolation:

```
index = (N - 1) * p
result = values[floor(index)] + (values[ceil(index)] - values[floor(index)]) * (index - floor(index))
```

### Pearson Correlation

Measures linear relationship between two time series:

```
r = cov(X,Y) / (std(X) * std(Y))
```

Range: -1 (perfectly anti-correlated) to +1 (perfectly correlated).

- **Negative correlation** is excellent for pooling: when one DB spikes, the other drops.
- **Near zero** is good: independent workloads share well.
- **High positive** is bad: coincident spikes waste the pooling benefit.

### Overload Fraction

Fraction of time intervals where combined pool load exceeds capacity:

```
overload_fraction = count(combined_load[t] > capacity) / total_intervals
```

## Database Profiling

For each database, compute from its DTU time series:

| Metric | Description |
|--------|-------------|
| Mean | Average DTU across all intervals |
| P95 | 95th percentile DTU |
| P99 | 99th percentile DTU |
| Peak | Maximum observed DTU |

Databases are sorted by P99 descending for greedy placement — larger, harder-to-place databases go first.

## Pairwise Poolability Metrics

For each pair of databases (A, B), compute four metrics:

### 1. Full Pearson Correlation

Standard correlation across the entire time series. Captures overall workload similarity.

### 2. Peak Correlation

Correlation restricted to intervals where **either** database exceeds its peak threshold (default: 90th percentile). This isolates correlation during high-load periods, which is when pool capacity matters most.

### 3. Peak Overlap Fraction

Fraction of peak intervals where **both** databases are simultaneously above their respective thresholds:

```
peak_overlap = count(A_peak AND B_peak) / count(A_peak OR B_peak)
```

High overlap means peaks align — bad for pooling.

### 4. Bad-Together Score

Weighted combination of the three metrics (only positive correlation counts as "bad"):

```
bad_together = 0.20 * max(0, full_corr) + 0.40 * max(0, peak_corr) + 0.40 * peak_overlap
```

Range: 0 (perfect for pooling) to 1 (worst case for pooling).

The weights emphasize peak behavior over overall correlation because elastic pool pain comes from coincident spikes, not quiet periods.

## Pool Sizing

For a candidate pool with combined historical load:

```
recommended_capacity = percentile(combined_load, target_percentile) * safety_factor
```

With optional hard cap: `min(recommended_capacity, max_pool_capacity)`.

Feasibility check: `overload_fraction(combined_load, recommended_capacity) <= max_overload_fraction`.

## Greedy Pool Construction

```
1. Separate isolated databases (user-specified) into their own pools.
2. Sort remaining databases by P99 descending.
3. For each database:
   a. Score placement against every existing pool.
   b. Reject pools at max capacity or that would cause overload.
   c. Place in pool with lowest total score, or create a new pool.
```

### Placement Scoring Function

For placing database D into pool P:

```
score = capacity_increase + 10 * pairwise_penalty + overload_penalty

where:
  capacity_increase = new_pool_capacity - current_pool_capacity
  pairwise_penalty  = average(bad_together_score(D, member)) for each member in P
  overload_penalty   = overload_fraction * 1,000,000
```

The overload penalty is deliberately extreme to make infeasible placements unattractive.

## Local Search Improvement

After greedy construction, iteratively improve by moving databases between pools:

```
for pass in 1..max_search_passes:
  for each database D in each source pool:
    for each target pool (not source):
      if moving D reduces total capacity:
        apply the move
        restart the pass
  if no improvement found: stop
```

Each "move" rebuilds pool assignments immutably. Empty pools are removed.

## Diversification Ratio

A key output metric for each pool:

```
diversification_ratio = sum(individual_P99) / pooled_P99
```

- **Ratio > 1**: pooling provides capacity savings. Higher is better.
- **Ratio ~ 1**: workloads move together; pooling gives little benefit.

Example: Three databases with P99 of 40, 50, 30 (sum = 120). If pooled P99 from actual concurrent history is 70, the diversification ratio is 120/70 = 1.71 — a strong candidate for pooling.

## Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `--target-percentile` | 0.99 | Percentile of combined load used for sizing |
| `--safety-factor` | 1.10 | Multiplier on top of percentile (10% buffer) |
| `--max-overload` | 0.001 | Maximum acceptable fraction of intervals above capacity |
| `--max-dbs-per-pool` | 50 | Maximum databases per elastic pool |
| `--peak-threshold` | 0.90 | Percentile used to define "peak" intervals |
| `--max-pool-capacity` | none | Optional hard cap on pool DTU capacity |
| `--isolate` | none | Database names that must have their own pool |
| `--max-search-passes` | 10 | Maximum local search improvement iterations |

## Practical Recommendations

### Use actual DTUs, not percentages

If databases have different standalone tiers, 80% of a 100-DTU database is not the same as 80% of a 10-DTU database. Normalize to actual DTU values when possible.

### Capture enough history

- **Minimum**: 24 hours (captures daily patterns)
- **Recommended**: 7 days (captures weekday/weekend variation)
- **Ideal**: 30 days (captures monthly patterns)

### Time windows

Use `--window-start` / `--window-end` to focus on business hours if overnight quiescence inflates the diversification ratio.

### What not to do

- Do not size pools by summing each database's max DTU (wasteful).
- Do not size pools by summing each database's P95 blindly (ignores correlation).
- Do not use average load alone (misses spikes entirely).
- Do not rely on plain correlation without examining peak overlap.

## Production Constraints

The algorithm does not automatically enforce:

- Same server / region requirements
- Failover group boundaries
- Storage limits per pool
- Noisy-neighbor concerns
- Criticality tiers or compliance segregation

Use `--isolate` for databases that must never share a pool, and manually review pool assignments for operational constraints.

## Algorithmic Complexity

- **Pairwise metrics**: O(N^2 * T) where N = databases, T = time points
- **Greedy placement**: O(N * P * T) where P = pools formed
- **Local search**: O(passes * N * P * T) per pass

For typical workloads (< 100 databases, < 10,000 time points), execution completes in seconds.

## Formal Framing

This problem is **clustered stochastic bin packing** or **chance-constrained capacity planning**:

- Each pool is a bin with capacity to be determined.
- Each database is a stochastic, time-varying load.
- The objective is to minimize total bin capacity subject to: overload probability < threshold, bin size limits, and placement constraints.

The greedy + local search approach is a practical near-optimal solver. For provably optimal solutions, formulate as a mixed-integer program — but the greedy approach typically achieves within 5-15% of optimal for practical workload distributions.
