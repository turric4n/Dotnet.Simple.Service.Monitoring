import { Link, useLocation } from 'react-router-dom';
import { cn } from '@/lib/utils';
import { ConnectionStatus } from '@/components/shared/ConnectionStatus';
import { useThemeStore } from '@/stores/themeStore';
import { Button } from '@/components/ui/button';
import { Activity, Moon, Sun } from 'lucide-react';

export function Navbar() {
  const { theme, toggleTheme } = useThemeStore();

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="flex h-14 items-center px-4 lg:px-6">
        <Link to="/" className="flex items-center gap-2 font-semibold">
          <Activity className="h-5 w-5 text-primary" />
          <span className="hidden sm:inline">Service Monitoring</span>
        </Link>

        <div className="ml-auto flex items-center gap-3">
          <ConnectionStatus />
          <Button variant="ghost" size="icon" onClick={toggleTheme} aria-label="Toggle theme">
            {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>
        </div>
      </div>
    </header>
  );
}
