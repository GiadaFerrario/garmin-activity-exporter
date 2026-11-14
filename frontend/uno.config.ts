import { defineConfig, presetWind4 } from 'unocss'

export default defineConfig({
  presets: [presetWind4()],
  shortcuts: {
    // The filled button of the app (it used to be a global `button` rule in main.css)
    btn: 'py-2 px-4 border border-accent rounded-md bg-accent text-accent-text cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed',
  },
  theme: {
    // The colors point to the CSS variables of base.css, which already switch with the dark theme:
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
