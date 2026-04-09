import { useEffect, useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { getConnection, startConnection } from '@/services/signalrService';
import type { HealthReport } from '@/models/types';
import { useConnectionStore } from '@/stores/connectionStore';

export const HEALTH_REPORT_KEY = ['health-report'] as const;

export function useHealthReport() {
  const queryClient = useQueryClient();
  const setState = useConnectionStore((s) => s.setState);

  useEffect(() => {
    let cancelled = false;
    const conn = getConnection();

    const onReport = (report: HealthReport) => {
      if (!cancelled) queryClient.setQueryData(HEALTH_REPORT_KEY, report);
    };

    conn.on('ReceiveHealthChecksReport', onReport);

    conn.onreconnecting(() => { if (!cancelled) setState('reconnecting'); });
    conn.onreconnected(() => {
      if (cancelled) return;
      setState('connected');
      conn.invoke('RetrieveHealthChecksReport').catch(() => {});
    });
    conn.onclose(() => { if (!cancelled) setState('disconnected'); });

    startConnection()
      .then(() => {
        if (cancelled) return;
        setState('connected');
        return conn.invoke('RetrieveHealthChecksReport');
      })
      .then((report: HealthReport | void) => {
        if (!cancelled && report) queryClient.setQueryData(HEALTH_REPORT_KEY, report);
      })
      .catch(() => { if (!cancelled) setState('disconnected'); });

    return () => {
      cancelled = true;
      conn.off('ReceiveHealthChecksReport', onReport);
    };
  }, [queryClient, setState]);

  const report = queryClient.getQueryData<HealthReport>(HEALTH_REPORT_KEY);
  const isLoading = !report && useConnectionStore.getState().state !== 'disconnected';

  const refresh = useCallback(async () => {
    const conn = getConnection();
    try {
      const report = await conn.invoke<HealthReport>('RetrieveHealthChecksReport');
      if (report) queryClient.setQueryData(HEALTH_REPORT_KEY, report);
    } catch { /* reconnect will handle */}
  }, [queryClient]);

  return { report, isLoading, refresh };
}
