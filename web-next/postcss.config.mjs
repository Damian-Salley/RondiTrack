// Configure PostCSS to process Tailwind CSS during Next.js builds.
const config = {
    plugins: {
        // Tailwind v4 uses its dedicated PostCSS plugin.
        "@tailwindcss/postcss": {},
    },
};

export default config;