import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';
import { StatusChange } from '../models/statusChange';

export class MonitoringService {
    private connection: HubConnection;
    private connectionPromise: Promise<void> | null = null;

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
        // Handle array of HealthReport objects
        this.connection.on('ReceiveHealthChecksReport', (data: HealthReport) => {
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

    // Return array of HealthReport objects
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
        if (healthReport && this._onHealthChecksReportReceived) {
            this._onHealthChecksReportReceived(healthReport);
        }
    }
}
