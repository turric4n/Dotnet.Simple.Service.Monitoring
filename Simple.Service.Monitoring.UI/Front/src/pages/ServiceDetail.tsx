import { useMemo, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Skeleton } from '@/components/ui/skeleton';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { StatusDot } from '@/components/shared/StatusDot';
import { UptimeBar } from '@/components/shared/UptimeBar';
import { useHealthReport } from '@/hooks/useHealthReport';
import { useGroupedHealthChecks } from '@/hooks/useGroupedHealthChecks';
import { useSignalR } from '@/hooks/useSignalR';
import { useTimeline } from '@/hooks/useTimeline';
import { formatRelativeTime, formatDateTime } from '@/lib/utils';
import { ArrowLeft, Clock, Monitor, Server, Tag } from 'lucide-react';

export default function ServiceDetail() {
  const { name } = useParams<{ name: string }>();
  const decodedName = name ? decodeURIComponent(name) : '';
  useSignalR();
  const { report, isLoading } = useHealthReport();
  const grouped = useGroupedHealthChecks(report);
  const { timelineData, requestTimelineGroupedByService, loading: timelineLoading } = useTimeline();
  const [timeRange, setTimeRange] = useState(24);

  const service = useMemo(() => {
    return grouped.find((c) => c.name === decodedName) ?? null;
  }, [grouped, decodedName]);

  const serviceTimeline = useMemo(() => {
    if (!timelineData || !decodedName) return [];
    return timelineData[decodedName] ?? [];
  }, [timelineData, decodedName]);

  const handleTimeRange = (hours: number) => {
    setTimeRange(hours);
    requestTimelineGroupedByService(hours);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  if (!service) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" asChild>
          <Link to="/"><ArrowLeft className="mr-2 h-4 w-4" />Back to Dashboard</Link>
        </Button>
        <Card>
          <CardContent className="p-8 text-center">
            <p className="text-lg text-muted-foreground">Service "{decodedName}" not found</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Back nav + header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" asChild>
          <Link to="/"><ArrowLeft className="h-4 w-4" /></Link>
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight">{service.name}</h1>
            <StatusBadge status={service.status} />
          </div>
          <p className="text-muted-foreground">
            Last updated {formatRelativeTime(service.lastUpdated)}
          </p>
        </div>
      </div>

      {/* Overview cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <Server className="h-4 w-4" />
              Service Type
            </div>
            <p className="font-medium">{service.serviceType || '—'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <Clock className="h-4 w-4" />
              Response Time
            </div>
            <p className="font-mono font-medium">{service.duration}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center gap-2 text-sm text-muted-foreground mb-1">
              <Monitor className="h-4 w-4" />
              Machines
            </div>
            <div className="flex flex-wrap gap-1 mt-0.5">
              {service.machines.map((m) => (
                <Badge key={m.machineName} variant="outline" className="gap-1 text-xs">
                  <StatusDot status={m.status} size="sm" />
                  {m.machineName}
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Tabbed detail */}
      <Tabs defaultValue="details">
        <TabsList>
          <TabsTrigger value="details">Details</TabsTrigger>
          <TabsTrigger value="machines">Machines ({service.machines.length})</TabsTrigger>
          <TabsTrigger value="timeline">Timeline</TabsTrigger>
          <TabsTrigger value="tags">Tags</TabsTrigger>
        </TabsList>

        <TabsContent value="details" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Health Check Details</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Overall Status</p>
                  <div className="flex items-center gap-2 mt-1">
                    <StatusDot status={service.status} />
                    <span>{service.status}</span>
                  </div>
                </div>
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Last Updated</p>
                  <p className="mt-1">{formatDateTime(service.lastUpdated)}</p>
                </div>
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Duration</p>
                  <p className="font-mono mt-1">{service.duration}</p>
                </div>
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Machines</p>
                  <p className="mt-1">{service.machines.length}</p>
                </div>
              </div>
              {service.description && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground">Description</p>
                  <p className="mt-1">{service.description}</p>
                </div>
              )}
              {service.checkError && (
                <div>
                  <p className="text-sm font-medium text-muted-foreground text-red-500">Error</p>
                  <pre className="mt-1 rounded-md bg-red-50 p-3 text-sm text-red-800 dark:bg-red-950 dark:text-red-200 overflow-auto">
                    {service.checkError}
                  </pre>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="machines" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Machine Status</CardTitle></CardHeader>
            <CardContent>
              <div className="space-y-3">
                {service.machines.map((m) => (
                  <div key={m.machineName} className="flex items-start gap-3 rounded-md border p-3">
                    <StatusDot status={m.status} className="mt-1" />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <span className="font-medium">{m.machineName}</span>
                        <StatusBadge status={m.status} />
                      </div>
                      <div className="mt-1 grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
                        <span>Duration: <span className="font-mono">{m.duration}</span></span>
                        <span>Updated: {formatRelativeTime(m.lastUpdated)}</span>
                      </div>
                      {m.checkError && m.checkError !== m.description && (
                        <pre className="mt-2 rounded-md bg-red-50 p-2 text-xs text-red-800 dark:bg-red-950 dark:text-red-200 overflow-auto">
                          {m.checkError}
                        </pre>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="timeline" className="mt-4">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Status Timeline</CardTitle>
                <div className="flex gap-1">
                  {[1, 24, 168].map((h) => (
                    <Button
                      key={h}
                      variant={timeRange === h ? 'default' : 'outline'}
                      size="sm"
                      onClick={() => handleTimeRange(h)}
                    >
                      {h === 1 ? '1h' : h === 24 ? '24h' : '7d'}
                    </Button>
                  ))}
                </div>
              </div>
            </CardHeader>
            <CardContent>
              {timelineLoading ? (
                <Skeleton className="h-32" />
              ) : serviceTimeline.length === 0 ? (
                <p className="text-center text-muted-foreground py-8">
                  No timeline data available. Click a time range to load.
                </p>
              ) : (
                <div className="space-y-3">
                  {serviceTimeline.map((seg: { status: string; startTime: string; endTime: string; uptimePercentage: number }, i: number) => (
                    <div key={i} className="flex items-center gap-3 rounded-md border p-3">
                      <StatusDot status={seg.status} size="sm" animate={false} />
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between">
                          <span className="font-medium text-sm">{seg.status}</span>
                          <span className="text-xs text-muted-foreground">
                            {formatDateTime(seg.startTime)} → {formatDateTime(seg.endTime)}
                          </span>
                        </div>
                        <UptimeBar percentage={seg.uptimePercentage} className="mt-1" />
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="tags" className="mt-4">
          <Card>
            <CardHeader><CardTitle>Tags</CardTitle></CardHeader>
            <CardContent>
              {service.tags && Object.keys(service.tags).length > 0 ? (
                <div className="flex flex-wrap gap-2">
                  {Object.entries(service.tags).map(([key, value]) => (
                    <Badge key={key} variant="secondary">
                      {key}: {value}
                    </Badge>
                  ))}
                </div>
              ) : (
                <p className="text-muted-foreground">No tags configured for this service.</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
