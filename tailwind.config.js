/** @type {import('tailwindcss').Config} */
export default {
  content: [
    './src/Client/**/*.{fs,html}'
  ],
  safelist: [
    // Navigation icon colors - ensure JIT compiles these
    'text-nav-home',
    'text-nav-library',
    'text-nav-search',
    'text-nav-friends',
    'text-nav-tags',
    'text-nav-collections',
    'text-nav-stats',
    'text-nav-timeline',
    'text-nav-graph',
    'text-nav-import',
    'text-nav-cache',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      colors: {
        // Rating colors
        'rating-brilliant': '#fbbf24',
        'rating-good': '#22c55e',
        'rating-decent': '#3b82f6',
        'rating-meh': '#9ca3af',
        'rating-nope': '#ef4444',
        // Navigation icon colors - Cinema palette
        'nav-home': '#fbbf24',       // Amber - theater marquee
        'nav-library': '#3b82f6',    // Blue - film archives
        'nav-search': '#7c3aed',     // Purple - brand primary
        'nav-friends': '#22c55e',    // Emerald - social
        'nav-tags': '#14b8a6',       // Teal - organization
        'nav-collections': '#ec4899', // Pink - curated
        'nav-stats': '#f97316',      // Orange - metrics
        'nav-timeline': '#0ea5e9',   // Sky - time
        'nav-graph': '#8b5cf6',      // Violet - connections
        'nav-import': '#84cc16',     // Lime - fresh data
        'nav-cache': '#64748b',      // Slate - system
      },
      animation: {
        'fade-in': 'fadeIn 200ms ease-out',
        'fade-in-up': 'fadeInUp 200ms ease-out',
        'fade-out': 'fadeOut 150ms ease-in',
        'scale-in': 'scaleIn 200ms ease-out',
        'slide-in-right': 'slideInRight 300ms ease-out',
        'slide-in-up': 'slideInUp 300ms ease-out',
        'pulse-subtle': 'pulseSubtle 2s ease-in-out infinite',
        'shimmer': 'shimmer 1.5s infinite',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        fadeInUp: {
          '0%': { opacity: '0', transform: 'translateY(10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        fadeOut: {
          '0%': { opacity: '1' },
          '100%': { opacity: '0' },
        },
        scaleIn: {
          '0%': { opacity: '0', transform: 'scale(0.95)' },
          '100%': { opacity: '1', transform: 'scale(1)' },
        },
        slideInRight: {
          '0%': { opacity: '0', transform: 'translateX(-20px)' },
          '100%': { opacity: '1', transform: 'translateX(0)' },
        },
        slideInUp: {
          '0%': { opacity: '0', transform: 'translateY(20px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        pulseSubtle: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.8' },
        },
        shimmer: {
          '0%': { backgroundPosition: '200% 0' },
          '100%': { backgroundPosition: '-200% 0' },
        },
      },
      transitionTimingFunction: {
        'bounce-in': 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
      },
      backdropBlur: {
        xs: '2px',
      },
      boxShadow: {
        'glow-primary': '0 0 20px -5px rgba(124, 58, 237, 0.4)',
        'glow-secondary': '0 0 20px -5px rgba(219, 39, 119, 0.4)',
        'glow-accent': '0 0 20px -5px rgba(20, 184, 166, 0.4)',
        'inner-glow': 'inset 0 1px 0 0 rgba(255, 255, 255, 0.05)',
      },
    },
  },
  plugins: [
    require('daisyui')
  ],
  daisyui: {
    themes: [
      {
        cinemarco: {
          // Primary brand colors
          "primary": "#7c3aed",
          "primary-focus": "#6d28d9",
          "primary-content": "#ffffff",

          // Secondary accent
          "secondary": "#db2777",
          "secondary-focus": "#be185d",
          "secondary-content": "#ffffff",

          // Accent (teal)
          "accent": "#14b8a6",
          "accent-focus": "#0d9488",
          "accent-content": "#ffffff",

          // Neutral colors
          "neutral": "#1f2937",
          "neutral-focus": "#111827",
          "neutral-content": "#d1d5db",

          // Base colors - Deep dark theme
          "base-100": "#0f0f0f",    // Deepest black for main bg
          "base-200": "#171717",    // Slightly lighter for cards
          "base-300": "#262626",    // Lighter for borders/highlights
          "base-content": "#e5e5e5", // Light text

          // State colors
          "info": "#3b82f6",
          "success": "#22c55e",
          "warning": "#f59e0b",
          "error": "#ef4444",

          // Component styling
          "--rounded-box": "0.75rem",
          "--rounded-btn": "0.5rem",
          "--rounded-badge": "1rem",
          "--animation-btn": "0.2s",
          "--animation-input": "0.2s",
          "--btn-focus-scale": "0.98",
          "--border-btn": "1px",
          "--tab-border": "1px",
          "--tab-radius": "0.5rem",
        },
      },
    ],
    darkTheme: "cinemarco",
  }
}
