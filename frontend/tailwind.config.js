/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        accent: {
          50: '#eef4ff',
          100: '#dce7fd',
          200: '#c0d3fc',
          300: '#94b4fa',
          400: '#618df6',
          500: '#3d68f0',
          600: '#274fe4',
          700: '#1f3ed1',
          800: '#2034a9',
          900: '#1f3187',
          950: '#171e4e',
        },
      },
      boxShadow: {
        card: '0 1px 2px rgb(0 0 0 / 0.06), 0 8px 24px -12px rgb(0 0 0 / 0.15)',
      },
      borderRadius: {
        card: '1rem',
      },
    },
  },
  plugins: [],
};
