import { create } from 'zustand';
import type { ConnectionState } from '@/models/types';

interface ConnectionStore {
  state: ConnectionState;
  lastConnected: Date | null;
  setState: (state: ConnectionState) => void;
}

export const useConnectionStore = create<ConnectionStore>((set) => ({
  state: 'disconnected',
  lastConnected: null,
  setState: (state) =>
    set({
      state,
      ...(state === 'connected' ? { lastConnected: new Date() } : {}),
    }),
}));
