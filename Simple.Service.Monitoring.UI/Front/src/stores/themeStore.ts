import { create } from 'zustand';

type Theme = 'light' | 'dark' | 'system';

interface ThemeStore {
  theme: Theme;
  resolved: 'light' | 'dark';
  setTheme: (theme: Theme) => void;
  toggleTheme: () => void;
}

function getSystemTheme(): 'light' | 'dark' {
  if (typeof window === 'undefined') return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function resolveTheme(theme: Theme): 'light' | 'dark' {
  return theme === 'system' ? getSystemTheme() : theme;
}

function applyTheme(resolved: 'light' | 'dark') {
  const root = document.documentElement;
  if (resolved === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

const stored = (typeof localStorage !== 'undefined'
  ? localStorage.getItem('theme')
  : null) as Theme | null;

const initialTheme: Theme = stored ?? 'system';
const initialResolved = resolveTheme(initialTheme);

// Apply on load
if (typeof document !== 'undefined') {
  applyTheme(initialResolved);
}

export const useThemeStore = create<ThemeStore>((set) => ({
  theme: initialTheme,
  resolved: initialResolved,
  setTheme: (theme) => {
    const resolved = resolveTheme(theme);
    localStorage.setItem('theme', theme);
    applyTheme(resolved);
    set({ theme, resolved });
  },
  toggleTheme: () => {
    const current = useThemeStore.getState();
    const next: Theme = current.resolved === 'dark' ? 'light' : 'dark';
    const resolved = resolveTheme(next);
    localStorage.setItem('theme', next);
    applyTheme(resolved);
    set({ theme: next, resolved });
  },
}));

// Listen for system theme changes
if (typeof window !== 'undefined') {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const state = useThemeStore.getState();
    if (state.theme === 'system') {
      const resolved = getSystemTheme();
      applyTheme(resolved);
      useThemeStore.setState({ resolved });
    }
  });
}
