import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useServers } from '../../state/servers/use-servers'
import { useCreateServer } from '../../state/servers/use-create-server'
import { useDeleteServer } from '../../state/servers/use-delete-server'
import styles from './servers-page.module.css'

export function ServersPage() {
  const { data: servers = [], isLoading, error } = useServers()
  const createServer = useCreateServer()
  const deleteServer = useDeleteServer()

  const [name, setName] = useState('')
  const [subscriptionId, setSubscriptionId] = useState('')
  const [resourceGroup, setResourceGroup] = useState('')
  const [serverName, setServerName] = useState('')

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
      },
    })
  }

  return (
    <main className={styles.page}>
      <nav className={styles.nav}>
        <Link to="/analysis" className={styles.navLink}>Analysis</Link>
        <span className={styles.navSep}>/</span>
        <Link to="/pool-builder" className={styles.navLink}>Pool Builder</Link>
      </nav>

      <div className={styles.header}>
        <h1 className={styles.title}>Registered Servers</h1>
      </div>

      {error && <div className={styles.error}>{error.message}</div>}

      <form className={styles.form} onSubmit={handleSubmit}>
        <input
          className={styles.input}
          placeholder="Display name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <input
          className={styles.input}
          placeholder="Subscription ID"
          value={subscriptionId}
          onChange={(e) => setSubscriptionId(e.target.value)}
        />
        <input
          className={styles.input}
          placeholder="Resource group"
          value={resourceGroup}
          onChange={(e) => setResourceGroup(e.target.value)}
        />
        <input
          className={styles.input}
          placeholder="Server name"
          value={serverName}
          onChange={(e) => setServerName(e.target.value)}
        />
        <button
          type="submit"
          className={styles.addButton}
          disabled={!canSubmit || createServer.isPending}
        >
          {createServer.isPending ? 'Adding...' : 'Add Server'}
        </button>
      </form>

      {isLoading ? (
        <div className={styles.empty}>Loading...</div>
      ) : servers.length === 0 ? (
        <div className={styles.empty}>No servers registered. Add one above.</div>
      ) : (
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
                <td>{server.name}</td>
                <td>{server.subscriptionId}</td>
                <td>{server.resourceGroupName}</td>
                <td>{server.serverName}</td>
                <td>
                  <button
                    className={styles.deleteButton}
                    onClick={() => deleteServer.mutate(server.registeredServerId)}
                    disabled={deleteServer.isPending}
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}
