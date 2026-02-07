import { useMemo, useState } from "react";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8000";

interface LoginResponse {
  access_token: string;
  token_type: string;
}

interface OverviewResponse {
  total_parents: number;
  total_admins: number;
  total_children: number;
  total_sessions: number;
  total_transactions: number;
}

interface FunnelResponse {
  registrations: number;
  parents_with_children: number;
  children_with_sessions: number;
}

interface LevelProblem {
  level_id: string;
  avg_mastery: number;
  total_attempts: number;
  total_errors: number;
}

interface EconomyHealth {
  total_earned: number;
  total_spent: number;
  active_children: number;
}

export default function App() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authToken, setAuthToken] = useState<string | null>(
    localStorage.getItem("adminToken"),
  );
  const [loginError, setLoginError] = useState<string | null>(null);
  const [overview, setOverview] = useState<OverviewResponse | null>(null);
  const [funnel, setFunnel] = useState<FunnelResponse | null>(null);
  const [levels, setLevels] = useState<LevelProblem[]>([]);
  const [economy, setEconomy] = useState<EconomyHealth | null>(null);
  const [dataError, setDataError] = useState<string | null>(null);

  const authHeaders = useMemo(() => {
    if (!authToken) {
      return {};
    }
    return { Authorization: `Bearer ${authToken}` };
  }, [authToken]);

  const handleLogin = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoginError(null);

    try {
      const response = await fetch(`${API_BASE}/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });
      if (!response.ok) {
        throw new Error("Invalid credentials.");
      }
      const data: LoginResponse = await response.json();
      localStorage.setItem("adminToken", data.access_token);
      setAuthToken(data.access_token);
    } catch (err) {
      setLoginError(err instanceof Error ? err.message : "Login failed.");
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("adminToken");
    setAuthToken(null);
    setOverview(null);
    setFunnel(null);
    setLevels([]);
    setEconomy(null);
  };

  const handleLoad = async () => {
    if (!authToken) {
      return;
    }

    try {
      const [overviewRes, funnelRes, levelsRes, economyRes] = await Promise.all([
        fetch(`${API_BASE}/admin/overview`, { headers: { ...authHeaders } }),
        fetch(`${API_BASE}/admin/funnel`, { headers: { ...authHeaders } }),
        fetch(`${API_BASE}/admin/levels/problems`, { headers: { ...authHeaders } }),
        fetch(`${API_BASE}/admin/economy/health`, { headers: { ...authHeaders } }),
      ]);

      if (!overviewRes.ok || !funnelRes.ok || !levelsRes.ok || !economyRes.ok) {
        throw new Error("Unable to load admin metrics.");
      }

      setOverview(await overviewRes.json());
      setFunnel(await funnelRes.json());
      setLevels(await levelsRes.json());
      setEconomy(await economyRes.json());
      setDataError(null);
    } catch (err) {
      setDataError(err instanceof Error ? err.message : "Unable to load data.");
    }
  };

  return (
    <div className="page">
      <header className="header">
        <div>
          <p className="eyebrow">LinguaStars</p>
          <h1>Admin Console</h1>
          <p className="subtitle">
            Review engagement, content health, and economy signals from synced sessions.
          </p>
        </div>
        {authToken ? (
          <div className="header-actions">
            <button className="secondary" onClick={handleLogout} type="button">
              Sign out
            </button>
            <button className="primary" onClick={handleLoad} type="button">
              Refresh data
            </button>
          </div>
        ) : null}
      </header>

      {!authToken ? (
        <section className="card auth-card">
          <h2>Admin login</h2>
          <form className="form" onSubmit={handleLogin}>
            <label>
              Email
              <input
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </label>
            <label>
              Password
              <input
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </label>
            {loginError ? <p className="error">{loginError}</p> : null}
            <button className="primary" type="submit">
              Sign in
            </button>
          </form>
        </section>
      ) : (
        <section className="grid two">
          <article className="card">
            <h2>Overview</h2>
            {overview ? (
              <ul className="metrics">
                <li>
                  <span>Total parents</span>
                  <strong>{overview.total_parents}</strong>
                </li>
                <li>
                  <span>Total admins</span>
                  <strong>{overview.total_admins}</strong>
                </li>
                <li>
                  <span>Children onboarded</span>
                  <strong>{overview.total_children}</strong>
                </li>
                <li>
                  <span>Sessions logged</span>
                  <strong>{overview.total_sessions}</strong>
                </li>
                <li>
                  <span>Transactions</span>
                  <strong>{overview.total_transactions}</strong>
                </li>
              </ul>
            ) : (
              <p>Load data to see overview metrics.</p>
            )}
          </article>
          <article className="card">
            <h2>Funnel</h2>
            {funnel ? (
              <ul className="metrics">
                <li>
                  <span>Total registrations</span>
                  <strong>{funnel.registrations}</strong>
                </li>
                <li>
                  <span>Parents with children</span>
                  <strong>{funnel.parents_with_children}</strong>
                </li>
                <li>
                  <span>Children with sessions</span>
                  <strong>{funnel.children_with_sessions}</strong>
                </li>
              </ul>
            ) : (
              <p>Load data to see onboarding funnel.</p>
            )}
          </article>
          <article className="card">
            <h2>Problem levels</h2>
            {levels.length === 0 ? (
              <p>No level mastery data yet.</p>
            ) : (
              <ul className="list">
                {levels.map((level) => (
                  <li key={level.level_id}>
                    <div>
                      <strong>{level.level_id}</strong>
                      <p>Avg mastery: {level.avg_mastery.toFixed(1)}%</p>
                    </div>
                    <small>
                      Attempts {level.total_attempts} · Errors {level.total_errors}
                    </small>
                  </li>
                ))}
              </ul>
            )}
          </article>
          <article className="card">
            <h2>Economy health</h2>
            {economy ? (
              <ul className="metrics">
                <li>
                  <span>Total earned</span>
                  <strong>{economy.total_earned}</strong>
                </li>
                <li>
                  <span>Total spent</span>
                  <strong>{economy.total_spent}</strong>
                </li>
                <li>
                  <span>Active children</span>
                  <strong>{economy.active_children}</strong>
                </li>
              </ul>
            ) : (
              <p>Load data to see economy metrics.</p>
            )}
          </article>
        </section>
      )}

      {dataError ? <p className="error notice">{dataError}</p> : null}
    </div>
  );
}
