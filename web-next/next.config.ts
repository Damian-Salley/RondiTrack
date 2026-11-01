import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Enable Cache Components for streaming and caching.
  cacheComponents: true,

  // Prefetch available page content for faster navigation.
  partialPrefetching: true,
};

export default nextConfig;