import { cn } from '@/lib/utils';

interface UptimeBarProps {
  percentage: number;
  className?: string;
  showLabel?: boolean;
}

function getUptimeColor(pct: number): string {
  if (pct >= 99.9) return 'bg-green-500';
  if (pct >= 99) return 'bg-green-400';
  if (pct >= 95) return 'bg-yellow-500';
  if (pct >= 90) return 'bg-orange-500';
  return 'bg-red-500';
}

export function UptimeBar({ percentage, className, showLabel = true }: UptimeBarProps) {
  const clamped = Math.min(100, Math.max(0, percentage));
  const color = getUptimeColor(clamped);

  return (
    <div className={cn('flex items-center gap-2', className)}>
      <div className="h-2 flex-1 overflow-hidden rounded-full bg-muted">
        <div
          className={cn('h-full rounded-full transition-all duration-500', color)}
          style={{ width: `${clamped}%` }}
          role="progressbar"
          aria-valuenow={clamped}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`Uptime: ${clamped.toFixed(1)}%`}
        />
      </div>
      {showLabel && <span className="text-xs font-medium text-muted-foreground w-14 text-right">{clamped.toFixed(1)}%</span>}
    </div>
  );
}
