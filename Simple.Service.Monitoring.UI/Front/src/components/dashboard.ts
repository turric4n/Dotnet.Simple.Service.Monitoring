import { MonitoringService } from '../services/monitoringService';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';

import {
    Chart,
    ChartDataset,
    CategoryScale,
    LinearScale,
    BarController,
    BarElement,
    Tooltip,
    Legend
} from "chart.js";
import ChartjsPluginStacked100 from "chartjs-plugin-stacked100";

Chart.register(
    CategoryScale,
    LinearScale,
    BarController,
    BarElement,
    Tooltip,
    Legend,
    ChartjsPluginStacked100
);

export class DashboardComponent {
    private monitoringService: MonitoringService;
    private tableElement: HTMLTableElement | null = null;
    private statusBadgeElement: HTMLElement | null = null;
    private lastUpdatedElement: HTMLElement | null = null;
    private timelineChartElement: HTMLCanvasElement | null = null;
    private timelineChart: Chart | null = null;

    constructor() {
        this.monitoringService = new MonitoringService();

        // Initialize UI references
        this.tableElement = document.querySelector('table tbody') as HTMLTableElement;
        this.statusBadgeElement = document.querySelector('.card-header .badge') as HTMLElement;
        this.lastUpdatedElement = document.querySelector('.card-header small') as HTMLElement;
        this.timelineChartElement = document.getElementById('timeline-chart') as HTMLCanvasElement;

        // Set up event handlers
        this.monitoringService.onHealthChecksReportReceived = this.updateDashboard.bind(this);
        this.monitoringService.onConnectionChange = this.updateConnectionStatus.bind(this);

        // Initialize connection
        this.initializeConnection();

        // Set up refresh button if it exists
        const refreshButton = document.getElementById('refresh-monitoring');
        if (refreshButton) {
            refreshButton.addEventListener('click', () => {
                this.monitoringService.refreshMonitoringData();
            });
        }

        // Set up timeline view buttons
        const lastFiveBtn = document.getElementById('show-last-five');
        if (lastFiveBtn) {
            lastFiveBtn.addEventListener('click', () => {
                const checks = this.monitoringService.getLastFiveHealthChecks();
                this.renderHealthChecks(checks);
                this.renderTimelineChart(checks);
            });
        }
        const failedTimelineBtn = document.getElementById('show-failed-timeline');
        if (failedTimelineBtn) {
            failedTimelineBtn.addEventListener('click', () => {
                const checks = this.monitoringService.getFailedHealthChecksTimeline();
                this.renderHealthChecks(checks);
                this.renderTimelineChart(checks);
            });
        }

        const allChecksBtn = document.getElementById('show-all-timeline');
        if (allChecksBtn) {
            allChecksBtn.addEventListener('click', () => {
                const report = this.monitoringService.getLatestReport();
                if (report) {
                    const checks = report.healthChecks || [];
                    this.renderHealthChecks(checks);
                    this.renderTimelineChart(checks);
                }
            });
        }
    }

private renderTimelineChart(healthChecks: HealthCheckData[]): void {
    if (this.timelineChart) {
        this.timelineChart.destroy();
        this.timelineChart = null;
    }

    if (!this.timelineChartElement || !healthChecks.length) return;

    // Agrupar por servicio y por hora
    const groupedByService: Record<string, Record<string, HealthCheckData[]>> = {};
    healthChecks.forEach(check => {
        const service = check.name || 'Unknown';
        const hour = new Date(check.lastUpdated).toISOString().substring(0, 13); // yyyy-MM-ddTHH
        if (!groupedByService[service]) groupedByService[service] = {};
        if (!groupedByService[service][hour]) groupedByService[service][hour] = [];
        groupedByService[service][hour].push(check);
    });

    // Obtener todas las horas únicas ordenadas
    const allHours = Array.from(
        new Set(healthChecks.map(c => new Date(c.lastUpdated).toISOString().substring(0, 13)))
    ).sort();

    // Obtener todos los servicios únicos
    const allServices = Object.keys(groupedByService);

    // Estados posibles
    const statusList = ['Healthy', 'Degraded', 'Unhealthy', 'Unknown'];
    const statusColors: Record<string, string> = {
        'Healthy': '#28a745',
        'Degraded': '#ffc107',
        'Unhealthy': '#dc3545',
        'Unknown': '#6c757d'
    };

    // Crear datasets para cada status
    const datasets: ChartDataset<'bar'>[] = statusList.map(status => {
        return {
            label: status,
            backgroundColor: statusColors[status],
            data: allServices.map(service => {
                // Para cada servicio, calcular cuántos checks hay de este status
                let total = 0;
                allHours.forEach(hour => {
                    const checks = groupedByService[service][hour] || [];
                    total += checks.filter(c => (c.status || 'Unknown') === status).length;
                });
                return total;
            })
        };
    });

    // Crea el nuevo gráfico con orientación horizontal
    this.timelineChart = new Chart(this.timelineChartElement, {
        type: "bar",
        data: {
            labels: allServices,
            datasets: datasets
        },
        options: {
            indexAxis: 'y', // Esta es la clave para hacerlo horizontal
            responsive: true,
            plugins: {
                stacked100: { enable: true, replaceTooltipLabel: false },
                tooltip: {
                    callbacks: {
                        label: (tooltipItem: any) => {
                            const data = tooltipItem.chart.data as any;
                            const datasetIndex = tooltipItem.datasetIndex;
                            const index = tooltipItem.dataIndex;
                            const status = data.datasets[datasetIndex].label || "";
                            const service = data.labels[index];
                            const originalValue = data.originalData
                                ? data.originalData[datasetIndex][index]
                                : tooltipItem.raw;
                            const rateValue = data.calculatedData
                                ? data.calculatedData[datasetIndex][index]
                                : tooltipItem.parsed.x;
                            return `${service} - ${status}: ${rateValue}% (raw ${originalValue})`;
                        }
                    }
                }
            },
            scales: {
                x: { 
                    stacked: true,
                    title: { display: true, text: 'Porcentaje (%)' },
                    beginAtZero: true,
                    max: 100
                },
                y: {
                    stacked: true,
                    title: { display: true, text: 'Servicio' }
                }
            },
            interaction: { mode: 'index', intersect: false }
        },
        plugins: [ChartjsPluginStacked100]
    });
}



    private renderHealthChecks(healthChecks: HealthCheckData[]): void {
        if (!this.tableElement) return;

        // Clear existing rows
        while (this.tableElement.firstChild) {
            this.tableElement.removeChild(this.tableElement.firstChild);
        }

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
                    <td><span class="badge bg-${this.getStatusColor(status)}">${this.escapeHtml(status)}</span></td>
                    <td>${check.serviceType || ''}</td>
                    <td>${this.escapeHtml(checkError)}</td>
                    <td>${check.duration} ms</td>
                    <td>${lastUpdated} UTC</td>
                `;

                fragment.appendChild(row);
            } catch (error) {
                console.error('Error rendering health check row:', error, check);
            }
        });

        this.tableElement.appendChild(fragment);
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

    private updateDashboard(report: HealthReport): void {
        if (!report) return;

        const healthChecks = report.healthChecks || [];

        // Update the table
        if (this.tableElement) {
            this.renderHealthChecks(healthChecks);
        }

        // Update the timeline chart
        if (this.timelineChartElement) {
            this.renderTimelineChart(healthChecks);
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
    }

    private updateConnectionStatus(isConnected: boolean): void {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.className = isConnected ? 'text-success' : 'text-danger';
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
}
