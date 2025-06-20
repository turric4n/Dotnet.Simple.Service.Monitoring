import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';
import { StatusChange } from '../models/statusChange';

export class MonitoringService {
    private connection: HubConnection;
    private connectionPromise: Promise<void> | null = null;

    // Simulated local database of received HealthReports
    private healthReportHistory: HealthReport[] = [];
    private latestReport: HealthReport | null = null;

    // Event handlers using the HealthReport type
    private _onHealthChecksReportReceived: ((data: HealthReport) => void) | null = null;
    private _onStatusChanged: ((data: StatusChange) => void) | null = null;
    private _onConnectionChange: ((isConnected: boolean) => void) | null = null;

    // Public setters for event handlers
    set onHealthChecksReportReceived(handler: ((data: HealthReport) => void) | null) {
        this._onHealthChecksReportReceived = handler;
    }
    
    set onStatusChanged(handler: ((data: StatusChange) => void) | null) {
        this._onStatusChanged = handler;
    }
    
    set onConnectionChange(handler: ((isConnected: boolean) => void) | null) {
        this._onConnectionChange = handler;
    }

    constructor() {
        this.connection = new HubConnectionBuilder()
            .withUrl('/monitoringHub')
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(LogLevel.Information)
            .build();

        this.registerHandlers();
    }

    private registerHandlers() {
        this.connection.on('ReceiveHealthChecksReport', (data: HealthReport) => {
            // Store the received report in the local history
            this.healthReportHistory.push(data);
            this.latestReport = data;
            
            // Keep history to a reasonable size (last 100 reports)
            if (this.healthReportHistory.length > 100) {
                this.healthReportHistory.shift(); // Remove oldest
            }

            if (this._onHealthChecksReportReceived) {
                this._onHealthChecksReportReceived(data);
            }
        });

        this.connection.on('ReceiveStatusChange', (data: StatusChange) => {
            if (this._onStatusChanged) {
                this._onStatusChanged(data);
            }
        });

        this.connection.onreconnecting(() => {
            this._onConnectionChange?.(false);
        });

        this.connection.onreconnected(() => {
            this._onConnectionChange?.(true);
            // Refresh data after reconnecting
            this.refreshMonitoringData();
        });

        this.connection.onclose(() => {
            this._onConnectionChange?.(false);
        });
    }

    public get state(): HubConnectionState {
        return this.connection.state;
    }

    public async start(): Promise<void> {
        if (this.connection.state === HubConnectionState.Connected) {
            return;
        }
        if (this.connectionPromise) {
            return this.connectionPromise;
        }
        this.connectionPromise = this.connection.start()
            .then(() => {
                this._onConnectionChange?.(true);
                // Immediately get initial data
                return this.refreshMonitoringData();
            })
            .catch(error => {
                this._onConnectionChange?.(false);
                throw error;
            })
            .finally(() => {
                this.connectionPromise = null;
            });
        return this.connectionPromise;
    }

    public async stop(): Promise<void> {
        if (this.connection.state !== HubConnectionState.Disconnected) {
            await this.connection.stop();
            this._onConnectionChange?.(false);
        }
    }

    private async ensureConnected(): Promise<void> {
        if (this.connection.state !== HubConnectionState.Connected) {
            await this.start();
        }
    }

    private async retrieveHealthChecksReport(): Promise<HealthReport | undefined> {
        try {
            await this.ensureConnected();
            return await this.connection.invoke<HealthReport>('RetrieveHealthChecksReport');
        } catch (error) {
            console.error('Error calling RetrieveHealthChecksReport():', error);
            return undefined;
        }
    }

    public async refreshMonitoringData(): Promise<void> {
        const healthReport = await this.retrieveHealthChecksReport();
        if (healthReport) {
            // Store the retrieved report
            this.healthReportHistory.push(healthReport);
            this.latestReport = healthReport;
            
            // Keep history to a reasonable size
            if (this.healthReportHistory.length > 100) {
                this.healthReportHistory.shift();
            }
            
            if (this._onHealthChecksReportReceived) {
                this._onHealthChecksReportReceived(healthReport);
            }
        }
    }

    // --- Timeline/History Methods ---

    /**
     * Returns the latest health report
     */
    public getLatestReport(): HealthReport | null {
        return this.latestReport;
    }
    
    /**
     * Returns all HealthReports in the history
     */
    public getAllReports(): HealthReport[] {
        return [...this.healthReportHistory];
    }

    /**
     * Returns all HealthReports received between the given date range (inclusive).
     */
    public getHealthReportsByDateRange(from: Date, to: Date): HealthReport[] {
        return this.healthReportHistory.filter(
            r => new Date(r.lastUpdated) >= from && new Date(r.lastUpdated) <= to
        );
    }

    /**
     * Returns the last five HealthCheckData entries (most recent, across all reports).
     */
    public getLastFiveHealthChecks(): HealthCheckData[] {
        if (!this.healthReportHistory.length) {
            return [];
        }
        
        // Flatten all healthChecks from all reports, sort by lastUpdated descending, take 5
        const allChecks = this.healthReportHistory
            .flatMap(r => r.healthChecks || []);
        
        return allChecks
            .sort((a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime())
            .slice(0, 5);
    }

    /**
     * Returns all failed HealthCheckData entries (status !== "Healthy"), sorted by lastUpdated descending.
     */
    public getFailedHealthChecksTimeline(): HealthCheckData[] {
        if (!this.healthReportHistory.length) {
            return [];
        }
        
        const allChecks = this.healthReportHistory
            .flatMap(r => r.healthChecks || []);
        
        return allChecks
            .filter(hc => hc.status !== 'Healthy')
            .sort((a, b) => new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime());
    }
    
    /**
     * Returns HealthCheckData entries grouped by service name
     */
    public getHealthChecksByService(): Record<string, HealthCheckData[]> {
        if (!this.healthReportHistory.length) {
            return {};
        }
        
        const allChecks = this.healthReportHistory
            .flatMap(r => r.healthChecks || []);
            
        const groupedByService: Record<string, HealthCheckData[]> = {};
        
        allChecks.forEach(check => {
            const serviceName = check.name || 'Unknown';
            if (!groupedByService[serviceName]) {
                groupedByService[serviceName] = [];
            }
            groupedByService[serviceName].push(check);
        });
        
        // Sort each group by date
        Object.values(groupedByService).forEach(checks => {
            checks.sort((a, b) => new Date(a.lastUpdated).getTime() - new Date(b.lastUpdated).getTime());
        });
        
        return groupedByService;
    }
    
    /**
     * Returns health check status distribution data suitable for charts
     */
    public getStatusDistribution(): { status: string; count: number }[] {
        if (!this.healthReportHistory.length) {
            return [];
        }
        
        const allChecks = this.healthReportHistory
            .flatMap(r => r.healthChecks || []);
            
        const statusCounts: Record<string, number> = {
            'Healthy': 0,
            'Degraded': 0,
            'Unhealthy': 0,
            'Unknown': 0
        };
        
        allChecks.forEach(check => {
            const status = check.status || 'Unknown';
            statusCounts[status] = (statusCounts[status] || 0) + 1;
        });
        
        return Object.entries(statusCounts).map(([status, count]) => ({ status, count }));
    }
}

