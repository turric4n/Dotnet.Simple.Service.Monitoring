import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { useHealthReport } from '@/hooks/useHealthReport';
import { useSignalR } from '@/hooks/useSignalR';
import type { HealthCheckData } from '@/models/types';
import { api } from '@/services/api';
import { FileCode, Server, Settings } from 'lucide-react';

export default function Configuration() {
  useSignalR();
  const { report, isLoading } = useHealthReport();
  const { data: settings } = useQuery({
    queryKey: ['settings'],
    queryFn: api.getSettings,
    staleTime: 60_000,
  });

  const serviceGroups = useMemo(() => {
    if (!report) return new Map<string, HealthCheckData[]>();
    const groups = new Map<string, HealthCheckData[]>();
    for (const check of report.healthChecks) {
      const type = check.serviceType || 'Unknown';
      const existing = groups.get(type) ?? [];
      existing.push(check);
      groups.set(type, existing);
    }
    return groups;
  }, [report]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-48" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Configuration</h1>
        <p className="text-muted-foreground">Health check services and their configuration</p>
      </div>

      {/* App Info */}
      {settings && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Settings className="h-5 w-5" />
              Application Settings
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-sm font-medium text-muted-foreground">Company Name</p>
                <p className="mt-1">{settings.companyName || '—'}</p>
              </div>
              <div>
                <p className="text-sm font-medium text-muted-foreground">Header Logo</p>
                <p className="mt-1">{settings.headerLogoUrl || '—'}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Services by type */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold flex items-center gap-2">
          <Server className="h-5 w-5" />
          Monitored Services ({report?.healthChecks.length ?? 0})
        </h2>

        {Array.from(serviceGroups.entries()).map(([type, checks]) => (
          <Card key={type}>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span className="flex items-center gap-2">
                  <FileCode className="h-4 w-4" />
                  {type}
                </span>
                <Badge variant="secondary">{checks.length}</Badge>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {checks.map((check) => (
                  <div
                    key={check.id || check.name}
                    className="flex items-center justify-between rounded-md border p-3"
                  >
                    <div className="flex items-center gap-3 min-w-0">
                      <StatusBadge status={check.status} />
                      <div className="min-w-0">
                        <p className="font-medium truncate">{check.name}</p>
                        {check.description && (
                          <p className="text-xs text-muted-foreground truncate">{check.description}</p>
                        )}
                      </div>
                    </div>
                    <div className="text-right text-sm text-muted-foreground whitespace-nowrap ml-4">
                      <p className="font-mono">{check.duration}</p>
                      <p>{check.machineName}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        ))}

        {serviceGroups.size === 0 && (
          <Card>
            <CardContent className="py-8 text-center text-muted-foreground">
              No health checks configured.
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
