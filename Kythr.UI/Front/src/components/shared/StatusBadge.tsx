import { Badge } from '@/components/ui/badge';
import type { HealthStatus } from '@/models/types';

const statusVariantMap: Record<string, 'healthy' | 'degraded' | 'unhealthy' | 'unknown'> = {
  Healthy: 'healthy',
  Degraded: 'degraded',
  Unhealthy: 'unhealthy',
  Unknown: 'unknown',
};

interface StatusBadgeProps {
  status: HealthStatus | string;
  className?: string;
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const variant = statusVariantMap[status] ?? 'unknown';
  return (
    <Badge variant={variant} className={className} aria-label={`Status: ${status}`}>
      {status}
    </Badge>
  );
}
