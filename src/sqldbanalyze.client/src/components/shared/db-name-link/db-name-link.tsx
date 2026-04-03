import styles from './db-name-link.module.css'

interface DbNameLinkProps {
  readonly name: string
  readonly focused?: boolean
  readonly onClick: (name: string) => void
}

export function DbNameLink({ name, focused, onClick }: DbNameLinkProps) {
  return (
    <button
      className={`${styles.link} ${focused ? styles.linkFocused : ''}`}
      onClick={(e) => {
        e.stopPropagation()
        onClick(name)
      }}
      title="Click to view DTU chart"
    >
      {name}
    </button>
  )
}
