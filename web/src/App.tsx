import './App.css'

function App() {
  return (
    <main className="app-shell">
      <header>
        <p className="eyebrow">Interview Simulator</p>
        <h1>Frontend Workspace Ready</h1>
        <p>
          Start building features in <code>src/App.tsx</code> and connect to the
          API with <code>VITE_API_BASE_URL</code>.
        </p>
      </header>

      <section className="panel" aria-label="Development checklist">
        <h2>Development Checklist</h2>
        <ul>
          <li>Run <code>npm run dev</code> for local development.</li>
          <li>Run <code>npm run check</code> before opening a PR.</li>
          <li>Copy <code>.env.example</code> to <code>.env.local</code>.</li>
        </ul>
      </section>
    </main>
  )
}

export default App
