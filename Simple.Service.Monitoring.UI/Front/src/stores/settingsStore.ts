import { create } from 'zustand';

interface SettingsStore {
  refreshInterval: number;
  defaultTimeRange: number;
  tablePageSize: number;
  notificationsEnabled: boolean;
  notificationSound: boolean;
  displayDensity: 'compact' | 'comfortable';
  setRefreshInterval: (ms: number) => void;
  setDefaultTimeRange: (hours: number) => void;
  setTablePageSize: (size: number) => void;
  setNotificationsEnabled: (enabled: boolean) => void;
  setNotificationSound: (enabled: boolean) => void;
  setDisplayDensity: (density: 'compact' | 'comfortable') => void;
  resetToDefaults: () => void;
}

type SettingsData = {
  refreshInterval: number;
  defaultTimeRange: number;
  tablePageSize: number;
  notificationsEnabled: boolean;
  notificationSound: boolean;
  displayDensity: 'compact' | 'comfortable';
};

const DEFAULTS: SettingsData = {
  refreshInterval: 30000,
  defaultTimeRange: 24,
  tablePageSize: 20,
  notificationsEnabled: true,
  notificationSound: false,
  displayDensity: 'comfortable',
};

function loadSettings(): Partial<SettingsData> {
  try {
    const stored = localStorage.getItem('monitoring-settings');
    return stored ? JSON.parse(stored) : {};
  } catch {
    return {};
  }
}

function getSettingsData(state: SettingsStore): SettingsData {
  return {
    refreshInterval: state.refreshInterval,
    defaultTimeRange: state.defaultTimeRange,
    tablePageSize: state.tablePageSize,
    notificationsEnabled: state.notificationsEnabled,
    notificationSound: state.notificationSound,
    displayDensity: state.displayDensity,
  };
}

function persist(state: SettingsStore) {
  localStorage.setItem('monitoring-settings', JSON.stringify(getSettingsData(state)));
}

const initial = { ...DEFAULTS, ...loadSettings() };

export const useSettingsStore = create<SettingsStore>((set, get) => ({
  ...initial,
  setRefreshInterval: (ms) => { set({ refreshInterval: ms }); persist(get()); },
  setDefaultTimeRange: (hours) => { set({ defaultTimeRange: hours }); persist(get()); },
  setTablePageSize: (size) => { set({ tablePageSize: size }); persist(get()); },
  setNotificationsEnabled: (enabled) => { set({ notificationsEnabled: enabled }); persist(get()); },
  setNotificationSound: (enabled) => { set({ notificationSound: enabled }); persist(get()); },
  setDisplayDensity: (density) => { set({ displayDensity: density }); persist(get()); },
  resetToDefaults: () => { set(DEFAULTS); persist(get()); },
}));
