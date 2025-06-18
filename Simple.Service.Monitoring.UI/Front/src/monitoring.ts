import { DashboardComponent } from './components/dashboard';

// Initialize the dashboard when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
  new DashboardComponent();
  
  console.log('Monitoring dashboard initialized');
});