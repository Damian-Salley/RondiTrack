import type { NextConfig } from "next";

// Configure how the Next.js application behaves.
const nextConfig: NextConfig = {
  async headers() {
    return [{ source: "/bff/:path*", headers: [
      { key: "X-Robots-Tag", value: "noindex, nofollow" },
      { key: "Cache-Control", value: "private, no-store" },
    ] }];
  },

  // Enable Cache Components for streaming and caching.
  // This allows supported components to use Next.js caching features.
  cacheComponents: true,

  // Configure experimental Next.js features.
  experimental: {

    // Allow Server Actions to receive request bodies up to 6 MB.
    //
    // Proof-of-payment files can be up to 5 MiB.
    // The additional space accommodates multipart form data.
    //
    // File validation still happens in the frontend
    // and independently in the ASP.NET Core backend.
    serverActions: {
      bodySizeLimit: "6mb"
    }
  },

  // Prefetch available page content to improve navigation speed.
  partialPrefetching: true
};

// Export the configuration for Next.js to use.
export default nextConfig;
