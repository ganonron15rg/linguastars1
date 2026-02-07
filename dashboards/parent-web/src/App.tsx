import { useEffect, useMemo, useState } from "react";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8000";

interface LoginResponse {
  access_token: string;
  token_type: string;
}

interface Child {
  id: number;
  parent_id: number;
  name: string;
  age: number;
  created_at: string;
}

interface Inventory {
  child_id: number;
  coins_balance: number;
  owned_items_json: Record<string, unknown>;
  equipped_json: Record<string, unknown>;
}

interface ChildSummary {
  child: Child;
  inventory: Inventory | null;
  progress_count: number;
  sessions_count: number;
  total_coins_earned: number;
}

interface ProgressEntry {
  child_id: number;
  level_id: string;
  mastery: number;
  attempts: number;
  errors: number;
  last_seen: string | null;
}

interface SessionEntry {
  id: number;
  child_id: number;
  date: string;
  duration_seconds: number;
  success_rate: number;
  activities_done: number;
}

interface EconomyTransaction {
  id: number;
  child_id: number;
  type: string;
  amount: number;
  meta_json: Record<string, unknown>;
  timestamp: string;
}

interface ChildTokenResponse {
  child_id: number;
  access_token: string;
  token_type: string;
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleString();
}

export default function App() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authToken, setAuthToken] = useState<string | null>(
    localStorage.getItem("parentToken"),
  );
  const [loginError, setLoginError] = useState<string | null>(null);
  const [children, setChildren] = useState<Child[]>([]);
  const [selectedChild, setSelectedChild] = useState<Child | null>(null);
  const [summary, setSummary] = useState<ChildSummary | null>(null);
  const [progress, setProgress] = useState<ProgressEntry[]>([]);
  const [sessions, setSessions] = useState<SessionEntry[]>([]);
  const [transactions, setTransactions] = useState<EconomyTransaction[]>([]);
  const [childToken, setChildToken] = useState<ChildTokenResponse | null>(null);
  const [dataError, setDataError] = useState<string | null>(null);

  const authHeaders = useMemo(() => {
    if (!authToken) {
      return {};
    }
    return { Authorization: `Bearer ${authToken}` };
  }, [authToken]);

  useEffect(() => {
    if (!authToken) {
      return;
    }

    fetch(`${API_BASE}/parent/children`, {
      headers: { ...authHeaders },
    })
      .then(async (res) => {
        if (!res.ok) {
          throw new Error("Unable to load children.");
        }
        const data: Child[] = await res.json();
        setChildren(data);
        if (data.length > 0) {
          setSelectedChild(data[0]);
        }
      })
      .catch((err) => {
        setDataError(err.message);
      });
  }, [authHeaders, authToken]);

  useEffect(() => {
    if (!authToken || !selectedChild) {
      return;
    }

    const childId = selectedChild.id;
    Promise.all([
      fetch(`${API_BASE}/parent/child/${childId}/summary`, {
        headers: { ...authHeaders },
      }),
      fetch(`${API_BASE}/parent/child/${childId}/progress`, {
        headers: { ...authHeaders },
      }),
      fetch(`${API_BASE}/parent/child/${childId}/sessions`, {
        headers: { ...authHeaders },
      }),
      fetch(`${API_BASE}/parent/child/${childId}/transactions`, {
        headers: { ...authHeaders },
      }),
    ])
      .then(async ([summaryRes, progressRes, sessionsRes, transactionsRes]) => {
        if (!summaryRes.ok) {
          throw new Error("Unable to load child summary.");
        }
        const summaryData: ChildSummary = await summaryRes.json();
        setSummary(summaryData);
        setProgress(progressRes.ok ? await progressRes.json() : []);
        setSessions(sessionsRes.ok ? await sessionsRes.json() : []);
        setTransactions(transactionsRes.ok ? await transactionsRes.json() : []);
        setDataError(null);
      })
      .catch((err) => {
        setDataError(err.message);
      });
  }, [authHeaders, authToken, selectedChild]);

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
      localStorage.setItem("parentToken", data.access_token);
      setAuthToken(data.access_token);
    } catch (err) {
      setLoginError(err instanceof Error ? err.message : "Login failed.");
    }
  };

  const handleGenerateChildToken = async () => {
    if (!selectedChild || !authToken) {
      return;
    }
    const response = await fetch(`${API_BASE}/parent/child/${selectedChild.id}/token`, {
      method: "POST",
      headers: { "Content-Type": "application/json", ...authHeaders },
    });
    if (response.ok) {
      const data: ChildTokenResponse = await response.json();
      setChildToken(data);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem("parentToken");
    setAuthToken(null);
    setChildren([]);
    setSelectedChild(null);
    setSummary(null);
    setProgress([]);
    setSessions([]);
    setTransactions([]);
    setChildToken(null);
  };

  return (
    <div className="page">
      <header className="header">
        <div>
          <p className="eyebrow">LinguaStars</p>
          <h1>Parent Dashboard</h1>
          <p className="subtitle">
            Track progress, review sessions, and share child pairing tokens for offline
            syncing.
          </p>
        </div>
        {authToken ? (
          <button className="secondary" onClick={handleLogout} type="button">
            Sign out
          </button>
        ) : null}
      </header>

      {!authToken ? (
        <section className="card auth-card">
          <h2>Parent login</h2>
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
            <h2>Children</h2>
            {children.length === 0 ? (
              <p>No children yet. Create a profile to begin syncing.</p>
            ) : (
              <ul className="list">
                {children.map((child) => (
                  <li key={child.id}>
                    <button
                      className={`list-button${
                        selectedChild?.id === child.id ? " active" : ""
                      }`}
                      type="button"
                      onClick={() => setSelectedChild(child)}
                    >
                      <span>{child.name}</span>
                      <small>Age {child.age}</small>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </article>
          <article className="card">
            <h2>Pairing token</h2>
            <p>
              Generate a child token for the Unity client. The client uses this token
              to authenticate sync uploads.
            </p>
            <button className="primary" onClick={handleGenerateChildToken} type="button">
              Generate token
            </button>
            {childToken ? (
              <div className="token-block">
                <p>
                  Child ID: <strong>{childToken.child_id}</strong>
                </p>
                <p className="token">{childToken.access_token}</p>
              </div>
            ) : null}
          </article>
        </section>
      )}

      {authToken && selectedChild ? (
        <section className="grid three">
          <article className="card">
            <h2>Summary</h2>
            {summary ? (
              <ul className="metrics">
                <li>
                  <span>Progress milestones</span>
                  <strong>{summary.progress_count}</strong>
                </li>
                <li>
                  <span>Sessions logged</span>
                  <strong>{summary.sessions_count}</strong>
                </li>
                <li>
                  <span>Total coins earned</span>
                  <strong>{summary.total_coins_earned}</strong>
                </li>
                <li>
                  <span>Coin balance</span>
                  <strong>{summary.inventory?.coins_balance ?? 0}</strong>
                </li>
              </ul>
            ) : (
              <p>Loading summary…</p>
            )}
          </article>
          <article className="card">
            <h2>Progress</h2>
            {progress.length === 0 ? (
              <p>No progress entries yet.</p>
            ) : (
              <ul className="list">
                {progress.map((entry) => (
                  <li key={entry.level_id}>
                    <div>
                      <strong>{entry.level_id}</strong>
                      <p>Mastery: {entry.mastery}%</p>
                    </div>
                    <small>
                      Attempts {entry.attempts} · Errors {entry.errors}
                    </small>
                  </li>
                ))}
              </ul>
            )}
          </article>
          <article className="card">
            <h2>Sessions</h2>
            {sessions.length === 0 ? (
              <p>No sessions synced yet.</p>
            ) : (
              <ul className="list">
                {sessions.map((session) => (
                  <li key={session.id}>
                    <div>
                      <strong>{formatDate(session.date)}</strong>
                      <p>{session.activities_done} activities</p>
                    </div>
                    <small>
                      {session.duration_seconds}s · {session.success_rate}% success
                    </small>
                  </li>
                ))}
              </ul>
            )}
          </article>
          <article className="card">
            <h2>Purchases & rewards</h2>
            {transactions.length === 0 ? (
              <p>No economy activity yet.</p>
            ) : (
              <ul className="list">
                {transactions.map((transaction) => (
                  <li key={transaction.id}>
                    <div>
                      <strong>
                        {transaction.type === "spend" ? "Purchase" : "Reward"}
                      </strong>
                      <p>{formatDate(transaction.timestamp)}</p>
                    </div>
                    <small>
                      {transaction.amount} coins ·{
                        " " + (transaction.meta_json.reason ?? "session")
                      }
                    </small>
                  </li>
                ))}
              </ul>
            )}
          </article>
        </section>
      ) : null}

      {dataError ? <p className="error notice">{dataError}</p> : null}
    </div>
  );
}
