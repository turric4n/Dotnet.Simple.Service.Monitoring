import { cn } from '@/lib/utils';
import type { HealthStatus } from '@/models/types';

const dotColorMap: Record<string, string> = {
  Healthy: 'bg-green-500',
  Degraded: 'bg-yellow-500',
  Unhealthy: 'bg-red-500',
  Unknown: 'bg-gray-400',
  connected: 'bg-green-500',
  reconnecting: 'bg-yellow-500',
  disconnected: 'bg-red-500',
};

interface StatusDotProps {
  status: HealthStatus | string;
  animate?: boolean;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeMap = {
  sm: 'h-2 w-2',
  md: 'h-3 w-3',
  lg: 'h-4 w-4',
};

export function StatusDot({ status, animate = true, size = 'md', className }: StatusDotProps) {
  const color = dotColorMap[status] ?? 'bg-gray-400';
  return (
    <span className={cn('relative inline-flex', className)} aria-label={`Status: ${status}`}>
      {animate && (
        <span className={cn('absolute inline-flex h-full w-full animate-ping rounded-full opacity-75', color)} />
      )}
      <span className={cn('relative inline-flex rounded-full', color, sizeMap[size])} />
    </span>
  );
}
