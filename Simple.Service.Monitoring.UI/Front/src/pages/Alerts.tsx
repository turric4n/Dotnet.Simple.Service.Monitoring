import { useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { StatusDot } from '@/components/shared/StatusDot';
import { useSignalR } from '@/hooks/useSignalR';
import { useStatusChange } from '@/hooks/useStatusChange';
import { useHealthReport } from '@/hooks/useHealthReport';
import { formatRelativeTime, formatDateTime } from '@/lib/utils';
import { Bell, BellOff, ArrowRight } from 'lucide-react';
import { Skeleton } from '@/components/ui/skeleton';

export default function Alerts() {
  useSignalR();
  const { history, lastChange } = useStatusChange();
  const { report, isLoading } = useHealthReport();

  const failedChecks = useMemo(() => {
    if (!report) return [];
    return report.healthChecks.filter((c) => c.status !== 'Healthy');
  }, [report]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Alerts</h1>
        <p className="text-muted-foreground">Status changes and failed health checks</p>
      </div>

      {/* Currently failing */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Bell className="h-5 w-5 text-red-500" />
            Active Issues ({failedChecks.length})
          </CardTitle>
        </CardHeader>
        <CardContent>
          {failedChecks.length === 0 ? (
            <div className="flex flex-col items-center py-8 text-muted-foreground">
              <BellOff className="h-12 w-12 mb-2 opacity-30" />
              <p>All services are healthy</p>
            </div>
          ) : (
            <div className="space-y-3">
              {failedChecks.map((check) => (
                <div
                  key={check.id || check.name}
                  className="flex items-center gap-3 rounded-lg border p-3"
                >
                  <StatusDot status={check.status} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium">{check.name}</span>
                      <StatusBadge status={check.status} />
                    </div>
                    <p className="text-sm text-muted-foreground truncate">
                      {check.description || check.checkError || 'No details'}
                    </p>
                  </div>
                  <span className="text-xs text-muted-foreground whitespace-nowrap">
                    {formatRelativeTime(check.lastUpdated)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Status Change History */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ArrowRight className="h-5 w-5" />
            Status Change History
          </CardTitle>
        </CardHeader>
        <CardContent>
          {history.length === 0 ? (
            <p className="text-center text-muted-foreground py-8">
              No status changes recorded yet. Changes will appear here in real-time.
            </p>
          ) : (
            <div className="space-y-2">
              {history.map((change, i) => (
                <div
                  key={`${change.serviceName}-${change.lastUpdated}-${i}`}
                  className="flex items-center gap-3 rounded-md border p-3"
                >
                  <div className="flex items-center gap-1 text-sm">
                    <StatusBadge status={change.previousStatus} />
                    <ArrowRight className="h-3 w-3 text-muted-foreground" />
                    <StatusBadge status={change.currentStatus} />
                  </div>
                  <div className="flex-1 min-w-0">
                    <span className="font-medium text-sm">{change.serviceName}</span>
                  </div>
                  <span className="text-xs text-muted-foreground whitespace-nowrap">
                    {formatRelativeTime(change.lastUpdated)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
