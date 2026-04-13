import { Card, CardContent } from '@/components/ui/card';
import { StatusDot } from './StatusDot';
import { cn } from '@/lib/utils';

interface StatusCardProps {
  title: string;
  value: string | number;
  status?: string;
  description?: string;
  className?: string;
}

export function StatusCard({ title, value, status, description, className }: StatusCardProps) {
  return (
    <Card className={cn('transition-all hover:shadow-md', className)}>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          {status && <StatusDot status={status} size="sm" />}
        </div>
        <p className="mt-2 text-2xl font-bold">{value}</p>
        {description && <p className="mt-1 text-xs text-muted-foreground">{description}</p>}
      </CardContent>
    </Card>
  );
}
