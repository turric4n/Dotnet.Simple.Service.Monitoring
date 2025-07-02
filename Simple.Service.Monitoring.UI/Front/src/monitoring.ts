import { Dashboard } from './components/dashboard';
import { DarkModeComponent } from './components/darkMode';
import './components/dataTable.css';

// Initialize the dashboard and dark mode when the DOM is fully loaded
document.addEventListener('DOMContentLoaded', () => {
  // Initialize the dashboard which will set up the DataTable component
  const dashboard = new Dashboard();
  
  // Initialize dark mode
  new DarkModeComponent();
  
  console.log('Monitoring dashboard initialized');
});