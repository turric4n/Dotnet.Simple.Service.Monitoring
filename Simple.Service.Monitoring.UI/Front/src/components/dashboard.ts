import { MonitoringService } from '../services/monitoringService';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';
import { TimelineComponent, TimelineSegment } from './timelineComponent';
import { DataTable } from './dataTable';
import "vis-timeline/styles/vis-timeline-graph2d.css";

export class Dashboard {
    private monitoringService: MonitoringService;
    private tableElement: HTMLTableElement | null = null;
    private statusBadgeElement: HTMLElement | null = null;
    private lastUpdatedElement: HTMLElement | null = null;
    private timelineComponent: TimelineComponent | null = null;
    private dataTable: DataTable | null = null;

    constructor() {
        this.monitoringService = new MonitoringService();

        // Initialize UI references
        this.tableElement = document.querySelector('table') as HTMLTableElement;
        this.statusBadgeElement = document.querySelector('.card-header .badge') as HTMLElement;
        this.lastUpdatedElement = document.querySelector('.card-header small') as HTMLElement;
        
        // Create timeline component
        this.timelineComponent = new TimelineComponent('timeline-chart');
        
        // Initialize DataTable if the table exists
        if (this.tableElement) {
            // Add necessary attributes if they're not already present
            if (!this.tableElement.id) {
                this.tableElement.id = 'health-checks-table';
            }
            
            this.dataTable = new DataTable({
                tableSelector: `#${this.tableElement.id}`,
                searchable: true,
                sortable: true,
                perPage: 10
            });
        }

        // Set up event handlers
        this.monitoringService.onHealthChecksReportReceived = this.updateDashboard.bind(this);
        this.monitoringService.onHealthChecksTimelineReceived = this.updateTimeline.bind(this);
        this.monitoringService.onConnectionChange = this.updateConnectionStatus.bind(this);

        // Initialize connection
        this.initializeConnection();
        
        // Set up timeline view buttons for different time ranges
        this.initializeTimelineControls();
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

    private renderHealthChecks(healthChecks: HealthCheckData[]): void {
        if (!this.tableElement) return;

        // Get the table body element
        const tbody = this.tableElement.querySelector('tbody');
        if (!tbody) {
            console.error('Table body not found');
            return;
        }

        // Clear existing rows
        tbody.innerHTML = '';

        const fragment = document.createDocumentFragment();
        healthChecks.forEach(check => {
            if (!check) return;

            try {
                const row = document.createElement('tr');
                const status = check.status || 'Unknown';
                row.className = this.getRowClass(status);

                const name = check.name || '';
                const checkError = check.checkError || '';
                const lastUpdated = check.lastUpdated
                    ? new Date(check.lastUpdated).toISOString().replace('T', ' ').substring(0, 19)
                    : '';

                row.innerHTML = `
                    <td><strong>${this.escapeHtml(name)}</strong></td>
                    <td>${check.serviceType || ''}</td>
                    <td>${check.machineName || ''}</td>
                    <td><span class="badge bg-${this.getStatusColor(status)}">${this.escapeHtml(status)}</span></td>                    
                    <td>${this.escapeHtml(checkError)}</td>
                    <td>${check.duration} ms</td>
                    <td>${lastUpdated} UTC</td>
                `;

                fragment.appendChild(row);
            } catch (error) {
                console.error('Error rendering health check row:', error, check);
            }
        });

        tbody.appendChild(fragment);

        // Update the DataTable after content changes
        if (this.dataTable) {
            // Use setTimeout to ensure DOM is updated before refreshing the DataTable
            setTimeout(() => {
                this.dataTable?.refresh();
            }, 0);
        }
    }

    private updateDashboard(report: HealthReport): void {
        if (!report) return;

        const healthChecks = report.healthChecks || [];

        // Update the table
        if (this.tableElement) {
            this.renderHealthChecks(healthChecks);
        }

        // Update status badge and last updated
        const overallStatus = report.status || 'Unknown';
        if (this.statusBadgeElement) {
            this.statusBadgeElement.textContent = overallStatus;
            this.statusBadgeElement.className = `badge bg-${this.getStatusColor(overallStatus)} text-white`;
        }

        if (this.lastUpdatedElement) {
            const lastUpdated = report.lastUpdated
                ? new Date(report.lastUpdated).toISOString().replace('T', ' ').substring(0, 19)
                : new Date().toISOString().replace('T', ' ').substring(0, 19);
            this.lastUpdatedElement.textContent = `Last Updated: ${lastUpdated} UTC`;
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
    }

    private getStatusColor(status: string): string {
        switch (status) {
            case 'Healthy': return 'success';
            case 'Degraded': return 'warning';
            case 'Unhealthy': return 'danger';
            default: return 'secondary';
        }
    }

    private getRowClass(status: string): string {
        switch (status) {
            case 'Healthy': return 'table-success';
            case 'Degraded': return 'table-warning';
            case 'Unhealthy': return 'table-danger';
            default: return '';
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
