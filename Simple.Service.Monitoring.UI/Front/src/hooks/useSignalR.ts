import { useEffect, useCallback } from 'react';
import { getConnection, startConnection } from '@/services/signalrService';
import { useConnectionStore } from '@/stores/connectionStore';

export function useSignalR() {
  const setState = useConnectionStore((s) => s.setState);

  useEffect(() => {
    let cancelled = false;
    const conn = getConnection();

    const onReconnecting = () => { if (!cancelled) setState('reconnecting'); };
    const onReconnected = () => { if (!cancelled) setState('connected'); };
    const onClose = () => { if (!cancelled) setState('disconnected'); };

    conn.onreconnecting(onReconnecting);
    conn.onreconnected(onReconnected);
    conn.onclose(onClose);

    startConnection()
      .then(() => { if (!cancelled) setState('connected'); })
      .catch(() => { if (!cancelled) setState('disconnected'); });

    return () => {
      cancelled = true;
    };
  }, [setState]);

  const invoke = useCallback(
    async <T = void>(method: string, ...args: unknown[]): Promise<T> => {
      const conn = getConnection();
      return conn.invoke<T>(method, ...args);
    },
    [],
  );

  return { invoke };
}
