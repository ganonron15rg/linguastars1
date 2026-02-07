const panels = [
  "Content health and world progress",
  "Sync queue monitoring",
  "Role management",
  "Feature flags and releases",
];

export default function App() {
  return (
    <div className="page">
      <header className="header">
        <div>
          <p className="eyebrow">LinguaStars</p>
          <h1>Admin Console</h1>
          <p className="subtitle">
            Oversee content, releases, and live operational status.
          </p>
        </div>
        <button className="primary">Open Content Manager</button>
      </header>
      <section className="grid">
        {panels.map((panel) => (
          <article className="card" key={panel}>
            <h2>{panel}</h2>
            <p>
              Live data reflects the latest synced sessions and content versions.
            </p>
          </article>
        ))}
      </section>
    </div>
  );
}
