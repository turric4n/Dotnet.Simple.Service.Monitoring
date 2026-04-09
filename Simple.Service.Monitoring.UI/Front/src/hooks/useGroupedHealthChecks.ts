import { useMemo } from 'react';
import type { HealthCheckData, GroupedHealthCheck, MachineEntry, HealthReport } from '@/models/types';
import { HealthStatus } from '@/models/types';

const STATUS_PRIORITY: Record<string, number> = {
  [HealthStatus.Unhealthy]: 0,
  [HealthStatus.Degraded]: 1,
  [HealthStatus.Healthy]: 2,
  [HealthStatus.Unknown]: -1,
};

function worstStatus(statuses: HealthStatus[]): HealthStatus {
  if (statuses.length === 0) return HealthStatus.Unknown;
  return statuses.reduce((worst, s) =>
    (STATUS_PRIORITY[s] ?? -1) < (STATUS_PRIORITY[worst] ?? -1) ? s : worst,
  );
}

export function groupHealthChecks(checks: HealthCheckData[]): GroupedHealthCheck[] {
  const map = new Map<string, HealthCheckData[]>();

  for (const check of checks) {
    const existing = map.get(check.name);
    if (existing) {
      existing.push(check);
    } else {
      map.set(check.name, [check]);
    }
  }

  const grouped: GroupedHealthCheck[] = [];

  for (const [name, entries] of map) {
    // Sort entries by lastUpdated descending to pick the most recent as primary
    const sorted = [...entries].sort(
      (a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime(),
    );
    const primary = sorted[0];

    const machines: MachineEntry[] = sorted.map((e) => ({
      machineName: e.machineName,
      status: e.status,
      duration: e.duration,
      lastUpdated: e.lastUpdated,
      description: e.description,
      checkError: e.checkError,
    }));

    // Merge tags from all entries
    const mergedTags: Record<string, string> = {};
    for (const e of entries) {
      if (e.tags) Object.assign(mergedTags, e.tags);
    }

    grouped.push({
      name,
      serviceType: primary.serviceType,
      status: worstStatus(entries.map((e) => e.status)),
      duration: primary.duration,
      lastUpdated: primary.lastUpdated,
      description: primary.description,
      checkError: primary.checkError,
      tags: mergedTags,
      machines,
    });
  }

  return grouped;
}

export function useGroupedHealthChecks(report: HealthReport | undefined) {
  return useMemo(() => {
    if (!report) return [];
    return groupHealthChecks(report.healthChecks ?? []);
  }, [report]);
}
