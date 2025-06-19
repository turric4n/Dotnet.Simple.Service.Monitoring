import { MonitoringService } from '../services/monitoringService';
import { HealthReport } from '../models/healthReport';
import { HealthCheckData } from '../models/healthCheckData';

export class DashboardComponent {
  private monitoringService: MonitoringService;
  private tableElement: HTMLTableElement | null = null;
  private statusBadgeElement: HTMLElement | null = null;
  private lastUpdatedElement: HTMLElement | null = null;
  
  constructor() {
    this.monitoringService = new MonitoringService();
    
    // Initialize UI references
    this.tableElement = document.querySelector('table tbody') as HTMLTableElement;
    this.statusBadgeElement = document.querySelector('.card-header .badge') as HTMLElement;
    this.lastUpdatedElement = document.querySelector('.card-header small') as HTMLElement;
    
    // Set up event handlers - match the property name in MonitoringService
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
    if (!this.tableElement || !report) return;
    
    const healthChecks: HealthCheckData[] = report.healthChecks || [];
    
    // Clear existing rows
    while (this.tableElement.firstChild) {
      this.tableElement.removeChild(this.tableElement.firstChild);
    }
    
    // Use the overall status from the report directly
    const overallStatus = report.status || 'Unknown';
    
    // Update status badge
    if (this.statusBadgeElement) {
      this.statusBadgeElement.textContent = overallStatus;
      this.statusBadgeElement.className = `badge bg-${this.getStatusColor(overallStatus)} text-white`;
    }
    
    // Update last updated timestamp from the report
    if (this.lastUpdatedElement) {
      const lastUpdated = report.lastUpdated 
        ? new Date(report.lastUpdated).toISOString().replace('T', ' ').substring(0, 19)
        : new Date().toISOString().replace('T', ' ').substring(0, 19);
      this.lastUpdatedElement.textContent = `Last Updated: ${lastUpdated} UTC`;
    }
    
    // Use document fragment for better performance
    const fragment = document.createDocumentFragment();
    
    // Add new rows with proper error handling
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
          <td>${check.type}</td>
          <td>${this.escapeHtml(checkError)}</td>
          <td>${check.duration} ms</td>
          <td>${lastUpdated} UTC</td>
        `;
        
        fragment.appendChild(row);
      } catch (error) {
        console.error('Error rendering health check row:', error, check);
      }
    });
    
    // Append all rows at once
    this.tableElement.appendChild(fragment);
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
