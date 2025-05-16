/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        border: "hsl(var(--border))",
        input: "hsl(var(--input))",
        ring: "hsl(var(--ring))",
        background: "hsl(var(--background))",
        foreground: "hsl(var(--foreground))",
        primary: {
          DEFAULT: "hsl(var(--primary))",
          foreground: "hsl(var(--primary-foreground))",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary))",
          foreground: "hsl(var(--secondary-foreground))",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive))",
          foreground: "hsl(var(--destructive-foreground))",
        },
        muted: {
          DEFAULT: "hsl(var(--muted))",
          foreground: "hsl(var(--muted-foreground))",
        },
        accent: {
          DEFAULT: "hsl(var(--accent))",
          foreground: "hsl(var(--accent-foreground))",
        },
        popover: {
          DEFAULT: "hsl(var(--popover))",
          foreground: "hsl(var(--popover-foreground))",
        },
        card: {
          DEFAULT: "hsl(var(--card))",
          foreground: "hsl(var(--card-foreground))",
        },
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      animation: {
        'spin-slow': 'spin 3s linear infinite',
      },
      fontSize: {
        // Text size variants for accessibility
        'base-large': 'var(--text-base-large)',
        'base-xlarge': 'var(--text-base-xlarge)',
        'sm-large': 'var(--text-sm-large)',
        'sm-xlarge': 'var(--text-sm-xlarge)',
        'lg-large': 'var(--text-lg-large)',
        'lg-xlarge': 'var(--text-lg-xlarge)',
        'xl-large': 'var(--text-xl-large)',
        'xl-xlarge': 'var(--text-xl-xlarge)',
        '2xl-large': 'var(--text-2xl-large)',
        '2xl-xlarge': 'var(--text-2xl-xlarge)',
      }
    },
  },
  plugins: [],
  // Add variant for high contrast and color blind modes
  safelist: [
    { pattern: /^contrast-/ },
    { pattern: /^colorblind-/ },
    { pattern: /^text-(normal|large|x-large)$/ },
    { pattern: /^text-.*-(large|xlarge)$/ },
  ],
} 