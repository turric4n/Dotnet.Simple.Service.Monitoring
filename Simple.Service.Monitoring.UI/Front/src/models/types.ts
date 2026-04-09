export enum HealthStatus {
  Unhealthy = 'Unhealthy',
  Degraded = 'Degraded',
  Healthy = 'Healthy',
  Unknown = 'Unknown',
}

export interface HealthCheckData {
  id: string;
  creationDate: string;
  name: string;
  lastUpdated: string;
  status: HealthStatus;
  duration: string;
  description: string;
  checkError: string;
  serviceType: string;
  machineName: string;
  tags: Record<string, string>;
}

export interface HealthReport {
  status: string;
  lastUpdated: string;
  totalDuration: string;
  healthChecks: HealthCheckData[];
}

export interface MachineEntry {
  machineName: string;
  status: HealthStatus;
  duration: string;
  lastUpdated: string;
  description: string;
  checkError: string;
}

export interface GroupedHealthCheck {
  name: string;
  serviceType: string;
  status: HealthStatus;
  duration: string;
  lastUpdated: string;
  description: string;
  checkError: string;
  tags: Record<string, string>;
  machines: MachineEntry[];
}

export interface StatusChange {
  serviceName: string;
  previousStatus: string;
  currentStatus: string;
  lastUpdated: string;
}

export interface TimelineSegment {
  startTime: string;
  endTime: string;
  status: string;
  uptimePercentage: number;
  serviceName: string;
  serviceType: string;
}

export type TimelineData = Record<string, TimelineSegment[]>;

export interface ServiceHealthCheck {
  name: string;
  serviceType: string;
  endpointOrHost: string;
  connectionString?: string;
  alert: boolean;
  alertBehaviour?: AlertBehaviour[];
  healthCheckConditions?: HealthCheckConditions;
}

export interface AlertBehaviour {
  transportMethod: string;
  transportName: string;
  alertOnce?: boolean;
  alertOnServiceRecovered?: boolean;
  alertEvery?: string;
}

export interface HealthCheckConditions {
  httpBehaviour?: {
    expectedHttpCode?: number;
    httpVerb?: string;
    timeOutMs?: number;
  };
  sqlBehaviour?: {
    query?: string;
    resultExpression?: string;
    expectedResult?: string;
  };
}

export interface MonitoringUiSettings {
  companyName: string;
  headerLogoUrl: string;
  dataRepositoryType: string;
}

export type ConnectionState = 'connected' | 'reconnecting' | 'disconnected';
