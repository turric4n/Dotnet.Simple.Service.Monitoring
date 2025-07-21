import { MonitoringService } from '../services/monitoringService';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';
import { TimelineComponent, TimelineSegment } from './timelineComponent';
import { EnhancedDataTable, DataTableColumn } from './enhancedDataTable';
import "vis-timeline/styles/vis-timeline-graph2d.css";

export class Dashboard {
    private monitoringService: MonitoringService;
    private statusBadgeElement: HTMLElement | null = null;
    private lastUpdatedElement: HTMLElement | null = null;
    private timelineComponent: TimelineComponent | null = null;
    private dataTable: EnhancedDataTable<HealthCheckData> | null = null;    constructor() {
        this.monitoringService = new MonitoringService();

        // Initialize UI references
        this.statusBadgeElement = document.querySelector('.card-header .badge') as HTMLElement;
        this.lastUpdatedElement = document.querySelector('.card-header small') as HTMLElement;
        
        // Create timeline component
        this.timelineComponent = new TimelineComponent('timeline-chart');
        
        // Initialize the Enhanced DataTable
        this.initializeDataTable();

        // Set up event handlers
        this.monitoringService.onHealthChecksReportReceived = this.updateDashboard.bind(this);
        this.monitoringService.onHealthChecksTimelineReceived = this.updateTimeline.bind(this);
        this.monitoringService.onConnectionChange = this.updateConnectionStatus.bind(this);

        // Initialize connection
        this.initializeConnection();
          // Set up timeline view buttons for different time ranges
        this.initializeTimelineControls();

    }    private initializeDataTable(): void {
        // Define columns for the health checks table
        const columns: DataTableColumn[] = [
            {
                key: 'name',
                label: 'Service Name',
                sortable: true,
                width: '20%',
                render: (value: string) => `<strong>${this.escapeHtml(value || '')}</strong>`
            },
            {
                key: 'serviceType',
                label: 'Service Type',
                sortable: true,
                width: '12%'
            },
            {
                key: 'machineName',
                label: 'Machine',
                sortable: true,
                width: '12%'
            },
            {
                key: 'status',
                label: 'Status',
                sortable: true,
                width: '10%',
                render: (value: string) => {
                    const status = value || 'Unknown';
                    return `<span class="badge bg-${this.getStatusColor(status)}">${this.escapeHtml(status)}</span>`;
                }
            },
            {
                key: 'checkError',
                label: 'Error',
                sortable: true,
                width: '25%',
                render: (value: string) => this.escapeHtml(value || '')
            },
            {
                key: 'duration',
                label: 'Duration',
                sortable: true,
                width: '8%',
                render: (value: number) => `${value || 0} ms`
            },            {
                key: 'lastUpdated',
                label: 'Last Updated',
                sortable: true,
                width: '13%',
                render: (value: string) => {
                    return value 
                        ? new Date(value).toLocaleString()
                        : '';
                }
            }
        ];        // Initialize the Enhanced DataTable
        this.dataTable = new EnhancedDataTable<HealthCheckData>({
            containerSelector: '#health-checks-table-container',
            columns: columns,
            searchable: true,
            sortable: true,
            perPage: 10,
            emptyMessage: 'No health checks available',
            groupingFilters: {
                enabled: true,
                columns: [
                    { columnKey: 'status', label: 'Status', filterType: 'dropdown' },
                    { columnKey: 'serviceType', label: 'Service Type', filterType: 'dropdown' },
                    { columnKey: 'machineName', label: 'Machine', filterType: 'dropdown' }
                ]
            },
            customFilters: {
                enabled: true,
                showActiveOnly: true,
                activeThresholdMinutes: 30
            }
        });
    }

    private initializeTimelineControls(): void {
        const timelinePreference = localStorage.getItem('monitoring-timeline-preference') || 'timeline-24h';
        let timeRangeHours = 24; // Default
        
        if (timelinePreference === 'timeline-1h') timeRangeHours = 1;
        else if (timelinePreference === 'timeline-7d') timeRangeHours = 24 * 7;
        
        // Set up timeline view buttons for different time ranges
        const timeline1h = document.getElementById('timeline-1h');
        if (timeline1h) {
            timeline1h.addEventListener('click', () => {
                if (this.timelineComponent) {
                    this.timelineComponent.setTimeRange(1);
                    localStorage.setItem('monitoring-timeline-preference', 'timeline-1h');
                }
                this.monitoringService.requestTimelineData(1);
                this.setActiveTimelineButton('timeline-1h');
            });
        }

        const timeline24h = document.getElementById('timeline-24h');
        if (timeline24h) {
            timeline24h.addEventListener('click', () => {
                if (this.timelineComponent) {
                    this.timelineComponent.setTimeRange(24);
                    localStorage.setItem('monitoring-timeline-preference', 'timeline-24h');
                }
                this.monitoringService.requestTimelineData(24);
                this.setActiveTimelineButton('timeline-24h');
            });
        }

        const timeline7d = document.getElementById('timeline-7d');
        if (timeline7d) {
            timeline7d.addEventListener('click', () => {
                if (this.timelineComponent) {
                    this.timelineComponent.setTimeRange(24 * 7);
                    localStorage.setItem('monitoring-timeline-preference', 'timeline-7d');
                }
                this.monitoringService.requestTimelineData(24 * 7);
                this.setActiveTimelineButton('timeline-7d');
            });
        }
        
        // Initialize with stored preference
        this.setActiveTimelineButton(timelinePreference);
        this.monitoringService.requestTimelineData(timeRangeHours);
    }

    // Method to handle timeline data
    private updateTimeline(timelineData: Record<string, TimelineSegment[]>): void {
        if (!this.timelineComponent) return;
        
        console.log('Timeline data received:', timelineData);
        
        // Validate data format
        if (!timelineData || typeof timelineData !== 'object') {
            console.error('Invalid timeline data format');
            return;
        }
        
        try {
            this.timelineComponent.renderTimeline(timelineData);
        } catch (error) {
            console.error('Error rendering timeline:', error);
        }
    }    
    
    private updateDashboard(report: HealthReport): void {
        if (!report) return;

        const healthChecks = report.healthChecks || [];

        // Update the data table with new health checks data
        if (this.dataTable) {
            this.dataTable.setData(healthChecks);
        }

        // Update status badge and last updated
        const overallStatus = report.status || 'Unknown';
        if (this.statusBadgeElement) {
            this.statusBadgeElement.textContent = overallStatus;
            this.statusBadgeElement.className = `badge bg-${this.getStatusColor(overallStatus)} text-white`;
        }

        if (this.lastUpdatedElement) {
            const lastUpdated = report.lastUpdated
                ? new Date(report.lastUpdated).toLocaleTimeString(undefined, { hour12: false })
                : new Date().toLocaleTimeString(undefined, { hour12: false });
            this.lastUpdatedElement.textContent = `Last Updated: ${lastUpdated}`;
        }

        // Request timeline data (only if we don't receive it automatically)
        // this.monitoringService.requestTimelineData(24);
    }

    private async initializeConnection(): Promise<void> {
        try {
            await this.monitoringService.start();
        } catch (error) {
            console.error('Failed to start connection:', error);
            setTimeout(() => {
                this.initializeConnection();
            }, 5000);
        }
    }

    private updateConnectionStatus(isConnected: boolean): void {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            // Use CSS variables for better dark mode compatibility
            statusElement.style.color = isConnected ? 'var(--bs-success)' : 'var(--bs-danger)';
            statusElement.textContent = isConnected ? 'Connected' : 'Disconnected';
        }
    }    private getStatusColor(status: string): string {
        switch (status) {
            case 'Healthy': return 'success';
            case 'Degraded': return 'warning';
            case 'Unhealthy': return 'danger';
            default: return 'secondary';
        }
    }

    private escapeHtml(unsafe: string): string {
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    private setActiveTimelineButton(activeButtonId: string): void {
        const buttons = ['timeline-1h', 'timeline-24h', 'timeline-7d'];
        buttons.forEach(id => {
            const button = document.getElementById(id);
            if (button) {
                if (id === activeButtonId) {
                    button.classList.add('active');
                } else {
                    button.classList.remove('active');
                }
            }
        });
    }
}
