import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import clsx from 'clsx'
import styles from './app-layout.module.css'

interface AppLayoutProps {
  readonly title: string
  readonly description?: string
  readonly children: ReactNode
}

export function AppLayout({ title, description, children }: AppLayoutProps) {
  return (
    <div className={styles.shell}>
      <aside className={styles.sidebar}>
        <div className={styles.brand}>
          <div className={styles.brandMark}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <ellipse cx="12" cy="5" rx="9" ry="3" />
              <path d="M3 5v14c0 1.66 4.03 3 9 3s9-1.34 9-3V5" />
              <path d="M3 12c0 1.66 4.03 3 9 3s9-1.34 9-3" />
            </svg>
          </div>
          <div>
            <div className={styles.brandName}>SqlDbAnalyze</div>
            <div className={styles.brandSub}>Azure SQL Analytics</div>
          </div>
        </div>
        <nav>
          <ul className={styles.navList}>
            <li>
              <NavLink
                to="/"
                end
                className={({ isActive }) => clsx(styles.navItem, isActive && styles.navItemActive)}
              >
                {({ isActive }) => (
                  <>
                    <ServerIcon filled={isActive} />
                    Servers
                  </>
                )}
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/analysis"
                className={({ isActive }) => clsx(styles.navItem, isActive && styles.navItemActive)}
              >
                {({ isActive }) => (
                  <>
                    <ChartIcon filled={isActive} />
                    Analysis
                  </>
                )}
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/pool-builder"
                className={({ isActive }) => clsx(styles.navItem, isActive && styles.navItemActive)}
              >
                {({ isActive }) => (
                  <>
                    <PoolIcon filled={isActive} />
                    Pool Builder
                  </>
                )}
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/rescale-builder"
                className={({ isActive }) => clsx(styles.navItem, isActive && styles.navItemActive)}
              >
                {({ isActive }) => (
                  <>
                    <RescaleIcon filled={isActive} />
                    Rescale Builder
                  </>
                )}
              </NavLink>
            </li>
          </ul>
        </nav>
        <div className={styles.sidebarFooter}>
          <div className={styles.versionBadge}>v1.0</div>
        </div>
      </aside>

      <main className={styles.main}>
        <div className={styles.content}>
          <div className={styles.pageHeader}>
            <h1 className={styles.pageTitle}>{title}</h1>
            {description && <p className={styles.pageDescription}>{description}</p>}
          </div>
          {children}
        </div>
      </main>
    </div>
  )
}

function ServerIcon({ filled }: { filled: boolean }) {
  if (filled) {
    return (
      <svg className={styles.navIcon} viewBox="0 0 24 24" fill="currentColor">
        <path d="M4 1h16a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2V3a2 2 0 012-2zm0 10h16a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2v-4a2 2 0 012-2zm14 3a1 1 0 100-2 1 1 0 000 2zm0-10a1 1 0 100-2 1 1 0 000 2z" />
      </svg>
    )
  }
  return (
    <svg className={styles.navIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="2" width="20" height="8" rx="2" />
      <rect x="2" y="14" width="20" height="8" rx="2" />
      <circle cx="18" cy="6" r="1" fill="currentColor" />
      <circle cx="18" cy="18" r="1" fill="currentColor" />
    </svg>
  )
}

function ChartIcon({ filled }: { filled: boolean }) {
  if (filled) {
    return (
      <svg className={styles.navIcon} viewBox="0 0 24 24" fill="currentColor">
        <path d="M3 13a1 1 0 011-1h2a1 1 0 011 1v7a1 1 0 01-1 1H4a1 1 0 01-1-1v-7zm6-5a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V8zm6-4a1 1 0 011-1h2a1 1 0 011 1v16a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
      </svg>
    )
  }
  return (
    <svg className={styles.navIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 3v18h18" />
      <path d="M7 16l4-4 4 4 5-6" />
    </svg>
  )
}

function PoolIcon({ filled }: { filled: boolean }) {
  if (filled) {
    return (
      <svg className={styles.navIcon} viewBox="0 0 24 24" fill="currentColor">
        <path d="M4 4a2 2 0 012-2h12a2 2 0 012 2v16l-4-2-4 2-4-2-4 2V4z" />
      </svg>
    )
  }
  return (
    <svg className={styles.navIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 2L2 7l10 5 10-5-10-5z" />
      <path d="M2 17l10 5 10-5" />
      <path d="M2 12l10 5 10-5" />
    </svg>
  )
}

function RescaleIcon({ filled }: { filled: boolean }) {
  if (filled) {
    return (
      <svg className={styles.navIcon} viewBox="0 0 24 24" fill="currentColor">
        <path d="M4 4h4v16H4V4zm6 6h4v10h-4V10zm6-2h4v12h-4V8z" />
        <path d="M2 20h20v2H2z" />
      </svg>
    )
  }
  return (
    <svg className={styles.navIcon} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
      <path d="M4 20V10m0 0l3-3m-3 3l-3-3" />
      <path d="M12 20V6m0 0l3-3m-3 3L9 3" />
      <path d="M20 20V14m0 0l3 3m-3-3l-3 3" />
    </svg>
  )
}
