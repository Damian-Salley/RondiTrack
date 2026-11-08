import type { Metadata } from "next";

// Metadata shared by all pages inside the (signed-in) route group.
// These pages contain private information and must not be indexed.
export const metadata: Metadata = {
  title: "RondiTrack",
  description: "RondiTrack",

  // Prevent search engines from indexing authenticated pages
  // or following links found on those pages.
  robots: {
    index: false,
    follow: false,
  },

  // Keep link-preview information generic to avoid exposing
  // private stokvel details.
  openGraph: {
    title: "RondiTrack",
    description: "RondiTrack",
    images: [],
  },
};

// Shared layout for authenticated pages.
// The parentheses in (signed-in) create a route group,
// so they do not appear in the actual URL.
export default function SignedInLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}