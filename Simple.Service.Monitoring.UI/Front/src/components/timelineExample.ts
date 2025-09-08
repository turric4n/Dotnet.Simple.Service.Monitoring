// Example usage of TimelineComponent with filters and grouping
import { TimelineComponent } from './timelineComponent';

// Example function showing how to initialize timeline with filters and grouping
export function initializeTimelineWithFilters(containerId: string): TimelineComponent {
    const timeline = new TimelineComponent(containerId);
    
    // Enable filters with default options
    timeline.enableFilters({
        enabled: true,
        showActiveOnly: false,
        activeThresholdMinutes: 30
    });
    
    // Enable grouping with default options
    timeline.enableGrouping({
        enabled: true,
        groupBy: 'none' // Start with no grouping
    });
    
    return timeline;
}

// Example function showing how to update filters programmatically
export function configureTimelineFilters(timeline: TimelineComponent) {
    // Update filters to show only healthy services
    timeline.updateFilters({
        statusFilter: new Set(['healthy']),
        showActiveOnly: true
    });
    
    // Change grouping to group by service type
    timeline.updateGrouping({
        groupBy: 'serviceType'
    });
}

// Example timeline data with the new properties
export const exampleTimelineData = {
    'API Gateway': [
        {
            startTime: new Date(Date.now() - 2 * 60 * 60 * 1000), // 2 hours ago
            endTime: new Date(),
            status: 'healthy',
            uptimePercentage: 99.5,
            serviceName: 'API Gateway',
            serviceType: 'Gateway'
        }
    ],
    'User Service': [
        {
            startTime: new Date(Date.now() - 3 * 60 * 60 * 1000), // 3 hours ago
            endTime: new Date(Date.now() - 1 * 60 * 60 * 1000), // 1 hour ago
            status: 'degraded',
            uptimePercentage: 95.2,
            serviceName: 'User Service',
            serviceType: 'Microservice'
        },
        {
            startTime: new Date(Date.now() - 1 * 60 * 60 * 1000), // 1 hour ago
            endTime: new Date(),
            status: 'healthy',
            uptimePercentage: 95.2,
            serviceName: 'User Service',
            serviceType: 'Microservice'
        }
    ],
    'Payment Service': [
        {
            startTime: new Date(Date.now() - 4 * 60 * 60 * 1000), // 4 hours ago
            endTime: new Date(),
            status: 'healthy',
            uptimePercentage: 98.8,
            serviceName: 'Payment Service',
            serviceType: 'Microservice'
        }
    ],
    'Database': [
        {
            startTime: new Date(Date.now() - 5 * 60 * 60 * 1000), // 5 hours ago
            endTime: new Date(Date.now() - 45 * 60 * 1000), // 45 minutes ago (inactive)
            status: 'unhealthy',
            uptimePercentage: 87.3,
            serviceName: 'Database',
            serviceType: 'Database'
        }
    ]
};

// Usage example:
/*
const timeline = initializeTimelineWithFilters('timeline-container');
timeline.renderTimeline(exampleTimelineData);

// Later, apply some filters
configureTimelineFilters(timeline);
*/
