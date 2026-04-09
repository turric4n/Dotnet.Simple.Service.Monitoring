import { useEffect, useState } from 'react';
import { getConnection } from '@/services/signalrService';
import type { StatusChange } from '@/models/types';

export function useStatusChange() {
  const [lastChange, setLastChange] = useState<StatusChange | null>(null);
  const [history, setHistory] = useState<StatusChange[]>([]);

  useEffect(() => {
    const conn = getConnection();

    const handler = (change: StatusChange) => {
      setLastChange(change);
      setHistory((prev) => [change, ...prev].slice(0, 100));
    };

    conn.on('ReceiveStatusChange', handler);

    return () => {
      conn.off('ReceiveStatusChange', handler);
    };
  }, []);

  return { lastChange, history };
}
