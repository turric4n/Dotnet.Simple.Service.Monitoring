// Example HealthStatus enum (adapt as needed)
export enum HealthStatus {
    Healthy = "Healthy",
    Degraded = "Degraded",
    Unhealthy = "Unhealthy"
}

export interface HealthCheckData {
    name: string;
    lastUpdated: string; // ISO string
    status: HealthStatus;
    duration: number; // milliseconds
    description: string;
}