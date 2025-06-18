import { HealthCheckData } from "./healthCheckData";

export interface HealthReport {
  status: string;
  lastUpdated: string;
  totalDuration: string;
  healthChecks: HealthCheckData[];
}