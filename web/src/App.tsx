import { useEffect, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { login, logout, refreshAccessToken } from "./auth/session";
import {
  useHome,
  useCycles,
  useContributions,
} from "./api/queries";
import { StokvelCard } from "./components/StokvelCard";
import { ContributionCard } from "./components/ContributionCard";
import { ContributionList } from "./components/ContributionList";
import { RecordContributionForm } from "./components/RecordContributionForm";
import { ThemeToggle } from "./components/ThemeToggle";
import { ErrorView } from "./components/ErrorView";
import "./App.css";

function Dashboard({ onLogout }: { onLogout: () => Promise<void> }) {
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const home = useHome();

  const stokvels = home.data ?? [];

  const activeId =
    selectedId !== null &&
      stokvels.some((stokvel) => Number(stokvel.stokvelId) === selectedId)
      ? selectedId
      : stokvels.length > 0
        ? Number(stokvels[0].stokvelId)
        : null;

  const cycles = useCycles(activeId);

  // Fetch contributions for the currently selected stokvel.
  const contributions = useContributions(activeId);

  // Convert API cycle data into the format expected by ContributionList.
  const cycleItems = (cycles.data ?? []).map(
    (cycle: {
      id?: number | string;
      number?: number | string;
      status?: string;
      dueDateUtc?: string;
      totalCollectedCents?: number | string;
      amountDueCents?: number | string;
    }) => ({
      id: Number(cycle.id),
      number: Number(cycle.number),
      status: cycle.status ?? "",
      dueDateUtc: cycle.dueDateUtc ?? "",
      totalCollectedCents: Number(cycle.totalCollectedCents ?? 0),
      amountDueCents: Number(cycle.amountDueCents ?? 0),
    })
  );

  const contributionItems =
    contributions.data?.pages.flatMap((page) => page.items ?? []) ?? [];

  const selectedStokvel = stokvels.find(
    (stokvel) => Number(stokvel.stokvelId) === activeId
  );

  if (home.isPending) return <p>Loading your stokvels...</p>;

  if (home.isError) {
    return (
      <main className="dashboard">
        <ErrorView
          status={(home.error as { status?: number })?.status}
          onRetry={() => void home.refetch()}
        />
        <button onClick={onLogout}>Log out</button>
      </main>
    );
  }

  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <p className="eyebrow">RondiTrack</p>
        <h1>Stokvel Dashboard</h1>
        <p>Assignment 7.3 — Live Data</p>
        <ThemeToggle />
        <button onClick={onLogout}>Log out</button>
      </header>

      <section aria-labelledby="stokvel-heading">
        <h2 id="stokvel-heading">My Stokvels</h2>

        {stokvels.length === 0 && <p>No stokvels found.</p>}

        <div className="card-grid">
          {stokvels.map((stokvel) => (
            <div key={stokvel.stokvelId}>
              <button
                type="button"
                aria-pressed={Number(stokvel.stokvelId) === activeId}
                onClick={() => setSelectedId(Number(stokvel.stokvelId))}
              >
                {Number(stokvel.stokvelId) === activeId
                  ? "Selected"
                  : "Select stokvel"}
              </button>
              <StokvelCard stokvel={stokvel} />
            </div>
          ))}
        </div>
      </section>

      {activeId !== null && (
        <section aria-labelledby="contribution-heading">
          <h2 id="contribution-heading">
            Contributions — {selectedStokvel?.stokvelName}
          </h2>

          {(cycles.isPending || contributions.isPending) && (
            <p>Loading contributions...</p>
          )}

          {(cycles.isError || contributions.isError) && (
            <ErrorView
              status={
                (
                  (cycles.error ?? contributions.error) as {
                    status?: number;
                  }
                )?.status
              }
              onRetry={() => {
                void cycles.refetch();
                void contributions.refetch();
              }}
            />
          )}

          {cycles.isSuccess && contributions.isSuccess && (
            <>
              {/* Show the form only when the selected stokvel has an active cycle. */}
              {selectedStokvel?.cycleStatus === "Active" &&
                selectedStokvel.currentCycle != null &&
                cycleItems
                  .filter(
                    (cycle) =>
                      cycle.number === Number(selectedStokvel.currentCycle)
                  )
                  .map((cycle) => (
                    <RecordContributionForm
                      key={cycle.id}
                      stokvelId={activeId}
                      cycleId={cycle.id}
                      stokvelName={selectedStokvel.stokvelName ?? "Stokvel"}
                      cycleNumber={cycle.number}
                      cycleStatus={selectedStokvel.cycleStatus ?? ""}
                      role={selectedStokvel.role ?? ""}
                    />
                  ))}

              {/* Display existing contributions grouped by cycle. */}
              <ContributionList
                key={activeId}
                cycles={cycleItems}
                contributions={contributionItems}
                stokvelNames={Object.fromEntries(
                  cycleItems.map((cycle) => [
                    cycle.id,
                    selectedStokvel?.stokvelName ?? "Stokvel",
                  ])
                )}
              />

              <div className="card-grid">
                {contributionItems.map((contribution) => (
                  <ContributionCard
                    key={contribution.id}
                    contribution={contribution}
                  />
                ))}
              </div>

              {contributions.hasNextPage && (
                <button
                  type="button"
                  disabled={contributions.isFetchingNextPage}
                  onClick={() => void contributions.fetchNextPage()}
                >
                  {contributions.isFetchingNextPage
                    ? "Loading..."
                    : "Load more"}
                </button>
              )}
            </>
          )}
        </section>
      )}

      <footer className="dashboard-footer">
        Live data • South African Rand (ZAR)
      </footer>
    </main>
  );
}

function App() {
  const queryClient = useQueryClient();
  const [authenticated, setAuthenticated] = useState(false);
  const [checkingSession, setCheckingSession] = useState(true);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    void refreshAccessToken()
      .then((token) => {
        if (active) setAuthenticated(token !== null);
      })
      .finally(() => {
        if (active) setCheckingSession(false);
      });

    return () => {
      active = false;
    };
  }, []);

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    try {
      await login(email, password);
      setPassword("");
      setAuthenticated(true);
    } catch {
      setError("Login failed. Check your email and password.");
    }
  }

  async function handleLogout() {
    try {
      await logout();
    } finally {
      queryClient.clear();
      setAuthenticated(false);
      setPassword("");
    }
  }

  if (checkingSession) return <p>Checking session...</p>;

  if (!authenticated) {
    return (
      <main className="dashboard">
        <h1>RondiTrack Login</h1>

        <form onSubmit={handleLogin}>
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

          <button type="submit">Log in</button>
          {error && <p role="alert">{error}</p>}
        </form>
      </main>
    );
  }

  return <Dashboard onLogout={handleLogout} />;
}

export default App;