import { create } from 'zustand';

interface DashboardStore {
  selectedTimeRange: number;
  groupBy: 'service' | 'type' | 'machine' | 'none';
  showOnlyFailed: boolean;
  searchQuery: string;
  setTimeRange: (hours: number) => void;
  setGroupBy: (groupBy: 'service' | 'type' | 'machine' | 'none') => void;
  setShowOnlyFailed: (show: boolean) => void;
  setSearchQuery: (query: string) => void;
}

export const useDashboardStore = create<DashboardStore>((set) => ({
  selectedTimeRange: 24,
  groupBy: 'none',
  showOnlyFailed: false,
  searchQuery: '',
  setTimeRange: (hours) => set({ selectedTimeRange: hours }),
  setGroupBy: (groupBy) => set({ groupBy }),
  setShowOnlyFailed: (show) => set({ showOnlyFailed: show }),
  setSearchQuery: (query) => set({ searchQuery: query }),
}));
