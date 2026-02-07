const highlights = [
  "Daily practice summary",
  "Skill mastery snapshot",
  "Streaks and coin spend",
  "Inventory overview",
];

export default function App() {
  return (
    <div className="page">
      <header className="header">
        <div>
          <p className="eyebrow">LinguaStars</p>
          <h1>Parent Dashboard</h1>
          <p className="subtitle">
            Track progress, review mastery, and celebrate learning milestones.
          </p>
        </div>
        <button className="primary">View Child Profiles</button>
      </header>
      <section className="grid">
        {highlights.map((item) => (
          <article className="card" key={item}>
            <h2>{item}</h2>
            <p>
              Updated automatically from offline sessions when sync becomes available.
            </p>
          </article>
        ))}
      </section>
    </div>
  );
}
