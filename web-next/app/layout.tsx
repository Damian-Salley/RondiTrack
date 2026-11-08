import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { NuqsAdapter } from "nuqs/adapters/next/app";

import QueryProvider from "@/app/components/QueryProvider";
import PinnedStokvelProvider from "@/app/components/PinnedStokvelProvider";

import "./globals.css";

// Load the Geist Sans font and expose it through a CSS variable.
// next/font optimizes font loading to improve performance.
const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

// Load the Geist Mono font for text that needs a monospace style.
const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

// Define the default metadata shared across the application.
// Individual pages or nested layouts can override these values.
export const metadata: Metadata = {
  // Use the configured website URL in production.
  // Fall back to localhost when developing locally.
  metadataBase: new URL(
    process.env.SITE_URL ?? "http://localhost:3000"
  ),

  title: "RondiTrack",
  description: "Stokvel contribution management",

  // Default information shown when public links are shared.
  openGraph: {
    title: "RondiTrack",
    description: "Manage your stokvel contributions securely.",
    images: ["/opengraph-image"],
  },
};

// Root layout shared by every page in the application.
export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable}`}
    >
      <body>
        {/* Keep URL query parameters synchronized with application state. */}
        <NuqsAdapter>
          {/* Make React Query available throughout the application. */}
          <QueryProvider>
            {/* Make pinned stokvel information available to child components. */}
            <PinnedStokvelProvider>
              {children}
            </PinnedStokvelProvider>
          </QueryProvider>
        </NuqsAdapter>
      </body>
    </html>
  );
}