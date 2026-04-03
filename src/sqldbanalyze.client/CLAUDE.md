# sqldbanalyze.client

React SPA frontend for the SqlDbAnalyze application.

## Rules

@../../rules/common/coding-style.md
@../../rules/common/security.md
@../../rules/typescript/coding-style.md
@../../rules/typescript/css.md
@../../rules/typescript/frontend-arch.md
@../../rules/typescript/react.md
@../../rules/typescript/patterns.md
@../../rules/typescript/security.md

## Module Purpose

React + TypeScript SPA providing a web UI for DTU analysis and elastic pool optimization. Built with Vite, uses React Query for server state, Zustand for client state, and CSS Modules for component styling.

## Key Contents

- `src/domain/` — types, interfaces, domain errors, Result<T>
- `src/repositories/` — HTTP service implementations (API clients)
- `src/services/` — business logic services
- `src/state/` — React Query hooks, Zustand stores
- `src/components/` — presentational components + shared/ + layout/
- `src/pages/` — smart page components
- `src/core/` — composition root (providers.tsx), API client
- `src/styles/` — ITCSS global styles, design tokens

## Dependency Constraints

- **Allowed**: npm packages (React, React Query, Zustand, Zod, clsx)
- **Forbidden**: No .NET assembly dependencies. Communicates with backend exclusively via HTTP.
