import { defineConfig, presetIcons, presetWind4 } from 'unocss'

export default defineConfig({
  presets: [
    presetWind4(),
    // Icons are CSS classes like `i-lucide-watch` (set: @iconify-json/lucide, ISC license).
    // The class names must be written out in full in the source, UnoCSS reads them as text.
    presetIcons({ extraProperties: { display: 'inline-block', 'vertical-align': 'middle' } }),
  ],
  shortcuts: {
    // The central column of the app: full width on a phone, a column in the middle on a PC.
    // Change the width here and the header, the content and the bottom bar follow.
    column: 'mx-auto w-full max-w-3xl px-4',
    // The filled button of the app; min-h-11 is a 44 px touch target
    btn: 'min-h-11 py-2 px-4 border border-accent rounded-md bg-accent text-accent-text cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed',
  },
  theme: {
    // The colors point to the CSS variables of main.css, which already switch with the dark theme:
    // no `dark:` prefix is needed in the templates.
    colors: {
      accent: {
        DEFAULT: 'var(--color-accent)',
        text: 'var(--color-accent-text)',
      },
      success: 'var(--color-success)',
      error: 'var(--color-error)',
      surface: {
        DEFAULT: 'var(--color-background)',
        soft: 'var(--color-background-soft)',
        mute: 'var(--color-background-mute)',
      },
      line: {
        DEFAULT: 'var(--color-border)',
        hover: 'var(--color-border-hover)',
      },
      heading: 'var(--color-heading)',
    },
  },
})
