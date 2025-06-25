import { DashboardComponent } from './components/dashboard';
import { DarkModeComponent } from './components/darkMode';

// Initialize the dashboard and dark mode when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
  new DashboardComponent();
  new DarkModeComponent();
  
  console.log('Monitoring dashboard initialized');
});