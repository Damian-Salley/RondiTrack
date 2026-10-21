import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";

type Theme = "light" | "dark" | "system";

const STORAGE_KEY = "ronditrack-theme";

function getSavedTheme(): Theme {
    try {
        const saved = localStorage.getItem(STORAGE_KEY);

        if (saved === "light" || saved === "dark" || saved === "system") {
            return saved;
        }
    } catch {
        // Storage may be unavailable.
    }

    return "system";
}

export function ThemeToggle() {
    const [theme, setTheme] = useState<Theme>(getSavedTheme);

    useEffect(() => {
        const media = window.matchMedia("(prefers-color-scheme: dark)");

        function applyTheme() {
            const useDark =
                theme === "dark" || (theme === "system" && media.matches);

            document.documentElement.classList.toggle("dark", useDark);
        }

        applyTheme();

        media.addEventListener("change", applyTheme);

        return () => media.removeEventListener("change", applyTheme);
    }, [theme]);

    function changeTheme(value: Theme) {
        setTheme(value);

        try {
            localStorage.setItem(STORAGE_KEY, value);
        } catch {
            // The selected theme still works without storage.
        }
    }

    return (
        <div className="flex flex-wrap gap-2" aria-label="Appearance">
            {(["light", "dark", "system"] as const).map((option) => (
                <Button
                    key={option}
                    type="button"
                    variant={theme === option ? "default" : "outline"}
                    aria-pressed={theme === option}
                    onClick={() => changeTheme(option)}
                >
                    {option.charAt(0).toUpperCase() + option.slice(1)}
                </Button>
            ))}
        </div>
    );
}