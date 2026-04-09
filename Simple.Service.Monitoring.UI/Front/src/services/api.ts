import type { HealthReport } from '@/models/types';

const BASE = '/monitoring/api';

async function fetchJson<T>(url: string): Promise<T> {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`API error: ${res.status} ${res.statusText}`);
  return res.json();
}

export const api = {
  getHealthReport: () => fetchJson<HealthReport>(`${BASE}/health-report`),

  getOverallStatus: () => fetchJson<{ status: string }>(`${BASE}/status`),

  getTimeline: (hours = 24) => fetchJson<Record<string, unknown[]>>(`${BASE}/timeline?hours=${hours}`),

  getTimelineGrouped: (hours = 24, activeOnly = false, activeThresholdMinutes = 60) =>
    fetchJson<Record<string, unknown[]>>(
      `${BASE}/timeline/grouped?hours=${hours}&activeOnly=${activeOnly}&activeThresholdMinutes=${activeThresholdMinutes}`
    ),

  getHealthReportByRange: (from: Date, to: Date) =>
    fetchJson<HealthReport>(
      `${BASE}/health-report/range?from=${from.toISOString()}&to=${to.toISOString()}`
    ),

  getSettings: () => fetchJson<{ companyName: string; headerLogoUrl: string }>(`${BASE}/settings`),
};
