import './App.css'

function App() {
  return (
    <main className="app-shell">
      <aside className="sidebar" aria-label="TMS modules">
        <div className="brand">
          <span className="brand-mark">T</span>
          <div>
            <strong>TMS</strong>
            <span>Treasury Platform</span>
          </div>
        </div>

        <nav className="module-list">
          <a className="active" href="#dashboard">Dashboard</a>
          <a href="#trades">Create Trade</a>
          <a href="#positions">Positions</a>
          <a href="#reconciliation">Reconciliation</a>
          <a href="#audit">Audit</a>
          <a href="#users">Users</a>
        </nav>
      </aside>

      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">Web Frontend</p>
            <h1>Treasury Management Dashboard</h1>
          </div>
          <button type="button">Sign in</button>
        </header>

        <section className="status-grid" aria-label="Treasury status summary">
          <article>
            <span>Open Trades</span>
            <strong>24</strong>
            <small>Waiting for backend API connection</small>
          </article>
          <article>
            <span>Positions</span>
            <strong>8</strong>
            <small>Will use CoreAPI position endpoints</small>
          </article>
          <article>
            <span>Audit Events</span>
            <strong>Live</strong>
            <small>SignalR can be added after API contracts settle</small>
          </article>
        </section>

        <section className="content-grid">
          <form className="login-panel">
            <div>
              <p className="eyebrow">Authentication</p>
              <h2>Login placeholder</h2>
              <p>
                This screen will call CoreAPI JWT login once we wire the first
                real endpoint.
              </p>
            </div>

            <label>
              Username
              <input type="text" placeholder="admin" />
            </label>

            <label>
              Password
              <input type="password" placeholder="password" />
            </label>

            <button type="button">Login</button>
          </form>

          <article className="api-panel">
            <p className="eyebrow">Backend Contract</p>
            <h2>Same CoreAPI backend</h2>
            <p>
              The React web app will call the same ASP.NET Core REST APIs used
              by the desktop clients. TypeScript types describe the JSON shape
              that crosses the API boundary.
            </p>

            <div className="flow">
              <span>React UI</span>
              <span>CoreAPI</span>
              <span>SQL Server</span>
            </div>
          </article>
        </section>
      </section>
    </main>
  )
}

export default App
