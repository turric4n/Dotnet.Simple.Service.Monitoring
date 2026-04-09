import { useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { type ColumnDef } from '@tanstack/react-table';
import type { GroupedHealthCheck } from '@/models/types';
import { HealthStatus } from '@/models/types';
import { StatusCard } from '@/components/shared/StatusCard';
import { StatusBadge } from '@/components/shared/StatusBadge';
import { StatusDot } from '@/components/shared/StatusDot';
import { DataTable, SortableHeader } from '@/components/shared/DataTable';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { formatRelativeTime } from '@/lib/utils';
import { Activity, AlertTriangle, CheckCircle2, RefreshCw, Server, XCircle } from 'lucide-react';
import { useHealthReport } from '@/hooks/useHealthReport';
import { useGroupedHealthChecks } from '@/hooks/useGroupedHealthChecks';
import { useSignalR } from '@/hooks/useSignalR';

function DashboardSkeleton() {
  return (
    <div className="space-y-6">
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-28" />
        ))}
      </div>
      <Skeleton className="h-96" />
    </div>
  );
}

const columns: ColumnDef<GroupedHealthCheck, unknown>[] = [
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => (
      <div className="flex items-center gap-2">
        <StatusDot status={row.original.status} size="sm" />
        <StatusBadge status={row.original.status} />
      </div>
    ),
    filterFn: (row, _id, value) => value === '' || row.original.status === value,
  },
  {
    accessorKey: 'name',
    header: ({ column }) => <SortableHeader column={column}>Name</SortableHeader>,
    cell: ({ row }) => (
      <div>
        <p className="font-medium">{row.original.name}</p>
        <p className="text-xs text-muted-foreground">{row.original.serviceType}</p>
      </div>
    ),
  },
  {
    id: 'machines',
    header: 'Machines',
    cell: ({ row }) => {
      const { machines } = row.original;
      if (machines.length === 1) {
        return (
          <div className="flex items-center gap-1.5">
            <StatusDot status={machines[0].status} size="sm" />
            <span className="text-sm">{machines[0].machineName}</span>
          </div>
        );
      }
      return (
        <div className="flex flex-wrap gap-1">
          {machines.map((m) => (
            <Tooltip key={m.machineName}>
              <TooltipTrigger asChild>
                <Badge variant="outline" className="gap-1 text-xs cursor-default">
                  <StatusDot status={m.status} size="sm" />
                  {m.machineName}
                </Badge>
              </TooltipTrigger>
              <TooltipContent>
                <p>{m.status} — {m.duration}ms</p>
                <p className="text-xs text-muted-foreground">{formatRelativeTime(m.lastUpdated)}</p>
              </TooltipContent>
            </Tooltip>
          ))}
        </div>
      );
    },
  },
  {
    accessorKey: 'duration',
    header: ({ column }) => <SortableHeader column={column}>Duration</SortableHeader>,
    cell: ({ row }) => <span className="font-mono text-sm">{row.original.duration}</span>,
  },
  {
    accessorKey: 'lastUpdated',
    header: ({ column }) => <SortableHeader column={column}>Last Updated</SortableHeader>,
    cell: ({ row }) => (
      <span className="text-sm text-muted-foreground">
        {formatRelativeTime(row.original.lastUpdated)}
      </span>
    ),
  },
  {
    accessorKey: 'description',
    header: 'Description',
    cell: ({ row }) => (
      <span className="text-sm text-muted-foreground line-clamp-2 max-w-xs">
        {row.original.description || row.original.checkError || '—'}
      </span>
    ),
  },
];

export default function Dashboard() {
  useSignalR();
  const { report, isLoading, refresh } = useHealthReport();
  const grouped = useGroupedHealthChecks(report);
  const navigate = useNavigate();

  const stats = useMemo(() => {
    if (!grouped.length) return { total: 0, healthy: 0, degraded: 0, unhealthy: 0 };
    return {
      total: grouped.length,
      healthy: grouped.filter((c) => c.status === HealthStatus.Healthy).length,
      degraded: grouped.filter((c) => c.status === HealthStatus.Degraded).length,
      unhealthy: grouped.filter((c) => c.status === HealthStatus.Unhealthy).length,
    };
  }, [grouped]);

  if (isLoading) return <DashboardSkeleton />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            {report
              ? `Last updated ${formatRelativeTime(report.lastUpdated)}`
              : 'Waiting for data...'}
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={refresh}>
          <RefreshCw className="mr-2 h-4 w-4" />
          Refresh
        </Button>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <StatusCard
          title="Overall Status"
          value={report?.status ?? 'Unknown'}
          status={report?.status}
          description={`${stats.total} service${stats.total !== 1 ? 's' : ''} monitored`}
        />
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-muted-foreground">Healthy</p>
              <CheckCircle2 className="h-4 w-4 text-green-500" />
            </div>
            <p className="mt-2 text-2xl font-bold text-green-600">{stats.healthy}</p>
            <p className="mt-1 text-xs text-muted-foreground">
              {stats.total > 0 ? `${((stats.healthy / stats.total) * 100).toFixed(0)}%` : '—'} of total
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-muted-foreground">Degraded</p>
              <AlertTriangle className="h-4 w-4 text-yellow-500" />
            </div>
            <p className="mt-2 text-2xl font-bold text-yellow-600">{stats.degraded}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-muted-foreground">Unhealthy</p>
              <XCircle className="h-4 w-4 text-red-500" />
            </div>
            <p className="mt-2 text-2xl font-bold text-red-600">{stats.unhealthy}</p>
          </CardContent>
        </Card>
      </div>

      {/* Health Checks Table */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="h-5 w-5" />
            Health Checks
          </CardTitle>
        </CardHeader>
        <CardContent>
          <DataTable
            columns={columns}
            data={grouped}
            searchKey="name"
            searchPlaceholder="Filter health checks..."
            pageSize={20}
            onRowClick={(row) => navigate(`/service/${encodeURIComponent(row.name)}`)}
          />
        </CardContent>
      </Card>
    </div>
  );
}
