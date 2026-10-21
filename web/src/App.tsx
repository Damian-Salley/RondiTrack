import type { components } from "./api/schema";
import { StokvelCard } from "./components/StokvelCard";
import { ContributionCard } from "./components/ContributionCard";
import "./App.css";
import { ContributionList } from "./components/ContributionList";
import { ThemeToggle } from "./components/ThemeToggle";

// Use TypeScript types generated from the backend's OpenAPI specification.
type Stokvel = components["schemas"]["HomeStokvelResponse"];
type Contribution = components["schemas"]["ContributionResponse"];

// Verified response from GET /api/v1/me/home.
//
// These records were retrieved from the authenticated backend API.
// They are intentionally stored as static demonstration data.
// No live API requests or login forms are required for Assignment 7.1.
const demoStokvels: Stokvel[] = [
  {
    stokvelId: 9002,
    stokvelName: "Community Savings",
    role: "Treasurer",
    currentCycle: 1,
    cycleStatus: "Active",
    paidThisCycle: 25000,
    stillOwes: 0,
    nextPayoutDate: null,
  },
  {
    stokvelId: 9003,
    stokvelName: "Family Savings",
    role: "Member",
    currentCycle: 1,
    cycleStatus: "Active",
    paidThisCycle: 0,
    stillOwes: 10000,
    nextPayoutDate: null,
  },
];

// Verified responses from:
// GET /api/v1/stokvels/9002/contributions
// GET /api/v1/stokvels/9003/contributions
//
// Amounts are integer cents, not decimal Rand values.
// Payment timestamps are UTC strings returned by the backend.
const demoContributions: Contribution[] = [
  {
    id: "9002-100000-2",
    userId: 100000,
    stokvelId: 9002,
    cycle: 2,
    cycleNumber: 1,
    memberName: "Demo Member",
    amountCents: 25000,
    status: "Paid",
    paidAtUtc: "2026-10-31T22:30:00Z",
  },
  {
    id: "9002-100001-2",
    userId: 100001,
    stokvelId: 9002,
    cycle: 2,
    cycleNumber: 1,
    memberName: "Sample Contributor",
    amountCents: 10,
    status: "Confirmed",
    paidAtUtc: "2026-10-20T23:54:25.529683Z",
  },
  {
    id: "9003-100000-3",
    userId: 100000,
    stokvelId: 9003,
    cycle: 3,
    cycleNumber: 1,
    memberName: "Demo Member",
    amountCents: 10000,
    status: "Pending",
    paidAtUtc: null,
  },
];

// Demonstration cycles representing Open, Closed and PaidOut states.
// These will eventually be replaced with responses from the cycles endpoint.
const demoCycles = [
  {
    id: 2,
    number: 3,
    status: "Active",
    dueDateUtc: "2026-10-31T22:00:00Z",
    totalCollectedCents: 25010,
    amountDueCents: 50000,
  },
  {
    id: 4,
    number: 2,
    status: "Closed",
    dueDateUtc: "2026-09-30T22:00:00Z",
    totalCollectedCents: 40000,
    amountDueCents: 50000,
  },
  {
    id: 5,
    number: 1,
    status: "Paid",
    dueDateUtc: "2026-08-31T22:00:00Z",
    totalCollectedCents: 50000,
    amountDueCents: 50000,
  },
  {
    id: 3,
    number: 1,
    status: "Active",
    dueDateUtc: "2026-11-30T22:00:00Z",
    totalCollectedCents: 0,
    amountDueCents: 20000,
  },
];

function App() {
  return (
    <main className="dashboard">
      <header className="dashboard-header">
        <p className="eyebrow">RondiTrack</p>

        <h1>Stokvel Dashboard</h1>

        <p>Assignment 7.2 — Styled, Responsive Stokvel View</p>

        <ThemeToggle />
      </header>

      {/* Display the stokvels belonging to the demonstration user. */}



      <section aria-labelledby="stokvel-heading">
        <h2 id="stokvel-heading">My Stokvels</h2>

        <div className="card-grid">
          {demoStokvels.map((stokvel) => (
            <StokvelCard
              key={stokvel.stokvelId}
              stokvel={stokvel}
            />
          ))}
        </div>


        <ContributionList
          cycles={demoCycles}
          contributions={demoContributions}
          stokvelNames={{
            2: "Community Savings",
            4: "Community Savings",
            5: "Community Savings",
            3: "Family Savings",
          }}
        />
      </section>

      {/* Display the verified Paid, Confirmed, and Pending contributions. */}
      <section aria-labelledby="contribution-heading">
        <h2 id="contribution-heading">Contributions</h2>

        <div className="card-grid">
          {demoContributions.map((contribution) => (
            <ContributionCard
              key={contribution.id}
              contribution={contribution}
            />
          ))}
        </div>
      </section>

      <footer className="dashboard-footer">
        Demonstration interface • South African Rand (ZAR)
      </footer>
    </main>
  );
}

export default App;