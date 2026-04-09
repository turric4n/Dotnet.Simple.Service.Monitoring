import { useEffect, useCallback, useState } from 'react';
import { getConnection } from '@/services/signalrService';
import type { TimelineData } from '@/models/types';

export function useTimeline() {
  const [timelineData, setTimelineData] = useState<TimelineData>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const conn = getConnection();

    const onPerMachine = (data: TimelineData) => {
      setTimelineData(data);
      setLoading(false);
    };

    const onGrouped = (data: TimelineData) => {
      setTimelineData(data);
      setLoading(false);
    };

    conn.on('ReceiveHealthChecksTimeline', onPerMachine);
    conn.on('ReceiveHealthChecksTimelineGrouped', onGrouped);

    return () => {
      conn.off('ReceiveHealthChecksTimeline', onPerMachine);
      conn.off('ReceiveHealthChecksTimelineGrouped', onGrouped);
    };
  }, []);

  const requestTimeline = useCallback(async (hours: number) => {
    const conn = getConnection();
    setLoading(true);
    try {
      await conn.invoke('RequestHealthChecksTimeline', hours);
    } catch {
      setLoading(false);
    }
  }, []);

  const requestTimelineGroupedByService = useCallback(
    async (hours: number, activeOnly = false, activeThresholdMinutes = 60) => {
      const conn = getConnection();
      setLoading(true);
      try {
        await conn.invoke('RequestHealthChecksTimelineGroupedByService', hours, activeOnly, activeThresholdMinutes);
      } catch {
        setLoading(false);
      }
    },
    [],
  );

  return { timelineData, loading, requestTimeline, requestTimelineGroupedByService };
}
