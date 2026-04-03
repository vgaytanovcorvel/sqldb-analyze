import { useState } from 'react'
import { useServers } from '../../state/servers/use-servers'
import { useCreateServer } from '../../state/servers/use-create-server'
import { useDeleteServer } from '../../state/servers/use-delete-server'
import { AppLayout } from '../../components/layout/app-layout/app-layout'
import styles from './servers-page.module.css'

export function ServersPage() {
  const { data: servers = [], isLoading, error } = useServers()
  const createServer = useCreateServer()
  const deleteServer = useDeleteServer()

  const [name, setName] = useState('')
  const [subscriptionId, setSubscriptionId] = useState('')
  const [resourceGroup, setResourceGroup] = useState('')
  const [serverName, setServerName] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null)
  const [showSuccess, setShowSuccess] = useState(false)

  const canSubmit = name.trim() && subscriptionId.trim() && resourceGroup.trim() && serverName.trim()

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!canSubmit) return

    createServer.mutate({
      name: name.trim(),
      subscriptionId: subscriptionId.trim(),
      resourceGroupName: resourceGroup.trim(),
      serverName: serverName.trim(),
    }, {
      onSuccess: () => {
        setName('')
        setSubscriptionId('')
        setResourceGroup('')
        setServerName('')
        setShowSuccess(true)
        setTimeout(() => setShowSuccess(false), 3000)
      },
    })
  }

  function handleDeleteConfirm() {
    if (!deleteTarget) return
    deleteServer.mutate(deleteTarget.id, {
      onSettled: () => setDeleteTarget(null),
    })
  }

  return (
    <AppLayout title="Registered Servers" description="Manage your Azure SQL server connections">
      {error && <div className={styles.error}>{error.message}</div>}

      {showSuccess && (
        <div className={styles.toast}>Server added successfully.</div>
      )}

      <form className={styles.form} onSubmit={handleSubmit}>
        <div className={styles.formBody}>
          <div className={styles.formGrid}>
            <div className={styles.fieldGroup}>
              <label className={styles.fieldLabel}>Display Name</label>
              <input
                className={styles.input}
                placeholder="e.g. Production SQL"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </div>
            <div className={styles.fieldGroup}>
              <label className={styles.fieldLabel}>Subscription ID</label>
              <input
                className={styles.input}
                placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                value={subscriptionId}
                onChange={(e) => setSubscriptionId(e.target.value)}
              />
            </div>
            <div className={styles.fieldGroup}>
              <label className={styles.fieldLabel}>Resource Group</label>
              <input
                className={styles.input}
                placeholder="e.g. my-resource-group"
                value={resourceGroup}
                onChange={(e) => setResourceGroup(e.target.value)}
              />
            </div>
            <div className={styles.fieldGroup}>
              <label className={styles.fieldLabel}>Server Name</label>
              <input
                className={styles.input}
                placeholder="e.g. my-sql-server"
                value={serverName}
                onChange={(e) => setServerName(e.target.value)}
              />
            </div>
          </div>
        </div>
        <div className={styles.formFooter}>
          <button
            type="submit"
            className={styles.addButton}
            disabled={!canSubmit || createServer.isPending}
          >
            {createServer.isPending ? 'Adding...' : 'Add Server'}
          </button>
        </div>
      </form>

      {isLoading ? (
        <div className={styles.empty}>
          <div className={styles.emptyIcon}>
            <LoadingIcon />
          </div>
          <div className={styles.emptyTitle}>Loading servers...</div>
        </div>
      ) : servers.length === 0 ? (
        <div className={styles.empty}>
          <div className={styles.emptyIcon}>
            <EmptyServerIcon />
          </div>
          <div className={styles.emptyTitle}>No servers registered</div>
          <div className={styles.emptyDescription}>
            Add your first Azure SQL server using the form above to get started with DTU analysis.
          </div>
        </div>
      ) : (
        <div className={styles.tableContainer}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Name</th>
                <th>Subscription</th>
                <th>Resource Group</th>
                <th>Server</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {servers.map((server) => (
                <tr key={server.registeredServerId}>
                  <td>
                    <span className={styles.serverName}>{server.name}</span>
                  </td>
                  <td>
                    <span className={styles.mono}>{server.subscriptionId}</span>
                  </td>
                  <td>{server.resourceGroupName}</td>
                  <td>{server.serverName}</td>
                  <td>
                    <button
                      className={styles.deleteButton}
                      onClick={() => setDeleteTarget({ id: server.registeredServerId, name: server.name })}
                      aria-label={`Delete ${server.name} server`}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {deleteTarget && (
        <div className={styles.modalOverlay} onClick={() => setDeleteTarget(null)}>
          <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
            <div className={styles.modalIcon}>
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
                <line x1="12" y1="9" x2="12" y2="13" />
                <line x1="12" y1="17" x2="12.01" y2="17" />
              </svg>
            </div>
            <h3 className={styles.modalTitle}>Delete Server</h3>
            <p className={styles.modalDescription}>
              Are you sure you want to delete <strong>{deleteTarget.name}</strong>? This action cannot be undone.
            </p>
            <div className={styles.modalActions}>
              <button className={styles.modalCancel} onClick={() => setDeleteTarget(null)}>
                Cancel
              </button>
              <button
                className={styles.modalConfirmDelete}
                onClick={handleDeleteConfirm}
                disabled={deleteServer.isPending}
              >
                {deleteServer.isPending ? 'Deleting...' : 'Delete Server'}
              </button>
            </div>
          </div>
        </div>
      )}
    </AppLayout>
  )
}

function EmptyServerIcon() {
  return (
    <svg width="48" height="48" viewBox="0 0 48 48" fill="none" stroke="currentColor" strokeWidth="1.5">
      <rect x="6" y="8" width="36" height="12" rx="3" />
      <rect x="6" y="28" width="36" height="12" rx="3" />
      <circle cx="34" cy="14" r="2" fill="currentColor" />
      <circle cx="34" cy="34" r="2" fill="currentColor" />
      <line x1="12" y1="14" x2="24" y2="14" />
      <line x1="12" y1="34" x2="24" y2="34" />
    </svg>
  )
}

function LoadingIcon() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
      <path d="M12 2v4m0 12v4m-8-10H2m20 0h-2m-2.93-6.07l-1.41 1.41m-7.32 7.32l-1.41 1.41m12.14 0l-1.41-1.41M6.34 6.34L4.93 4.93" />
    </svg>
  )
}
