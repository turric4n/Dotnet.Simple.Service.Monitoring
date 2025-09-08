# Timeline Component - Filters and Grouping

The Timeline Component now supports advanced filtering and grouping capabilities to help users better organize and view their service monitoring data.

## Features Added

### 1. Filtering Capabilities

#### Service Name Filter
- Filter timeline data by specific service names
- Multi-select dropdown showing all available services
- Useful for focusing on specific services of interest

#### Service Type Filter
- Filter by service categories (e.g., "Gateway", "Microservice", "Database")
- Requires `serviceType` property in timeline segment data
- Multi-select dropdown for selecting multiple types

#### Status Filter
- Filter by health status (e.g., "healthy", "degraded", "unhealthy")
- Multi-select dropdown for selecting multiple statuses
- Helps focus on services with specific health conditions

#### Active Only Filter
- Checkbox to show only services updated within a specified time threshold
- Default threshold: 30 minutes (configurable)
- Useful for identifying recently active vs. stale services

### 2. Grouping Capabilities

#### Group by Service Type
- Groups services by their `serviceType` property
- Shows grouped timeline rows with service counts
- Helps organize services by architectural layer

#### Group by Status
- Groups services by their current/latest status
- Useful for quickly identifying all healthy vs. problematic services

#### No Grouping (Default)
- Standard individual service timeline view
- Each service gets its own timeline row

## Usage

### Basic Setup with Filters

```typescript
import { TimelineComponent } from './timelineComponent';

// Create timeline instance
const timeline = new TimelineComponent('timeline-container');

// Enable filters
timeline.enableFilters({
    enabled: true,
    showActiveOnly: false,
    activeThresholdMinutes: 30
});

// Enable grouping
timeline.enableGrouping({
    enabled: true,
    groupBy: 'none'
});

// Render timeline data
timeline.renderTimeline(timelineData);
```

### Programmatic Filter Updates

```typescript
// Update filters programmatically
timeline.updateFilters({
    serviceNameFilter: new Set(['API Gateway', 'User Service']),
    statusFilter: new Set(['healthy', 'degraded']),
    showActiveOnly: true
});

// Update grouping
timeline.updateGrouping({
    groupBy: 'serviceType'
});
```

### Data Format Requirements

For full functionality, timeline segment data should include these properties:

```typescript
interface TimelineSegment {
    startTime: string | number | Date;
    endTime: string | number | Date;
    status: string;
    uptimePercentage?: number;
    serviceName?: string;      // For service name filtering
    serviceType?: string;      // For service type filtering and grouping
}
```

### Example Data

```typescript
const timelineData = {
    'API Gateway': [
        {
            startTime: new Date(Date.now() - 2 * 60 * 60 * 1000),
            endTime: new Date(),
            status: 'healthy',
            uptimePercentage: 99.5,
            serviceName: 'API Gateway',
            serviceType: 'Gateway'
        }
    ],
    'User Service': [
        {
            startTime: new Date(Date.now() - 3 * 60 * 60 * 1000),
            endTime: new Date(),
            status: 'degraded',
            uptimePercentage: 95.2,
            serviceName: 'User Service',
            serviceType: 'Microservice'
        }
    ]
};
```

## UI Components

### Filter Controls

The filter UI automatically generates based on available data:

- **Service Name**: Multi-select dropdown (appears when multiple services exist)
- **Service Type**: Multi-select dropdown (appears when `serviceType` data exists)
- **Status**: Multi-select dropdown (appears when multiple statuses exist)
- **Group By**: Single-select dropdown (when grouping is enabled)
- **Active Only**: Checkbox with configurable threshold

### Styling

Filter controls use Bootstrap classes and respect dark mode:

- `.timeline-filters` - Main container with card-like styling
- `.datatable-controls` - Consistent with data table filter styling
- Responsive grid layout with proper spacing
- Dark mode compatible with CSS custom properties

## Configuration Options

### TimelineFilterOptions

```typescript
interface TimelineFilterOptions {
    enabled: boolean;
    serviceNameFilter?: Set<string>;
    serviceTypeFilter?: Set<string>;
    statusFilter?: Set<string>;
    showActiveOnly?: boolean;
    activeThresholdMinutes?: number;
}
```

### TimelineGroupingOptions

```typescript
interface TimelineGroupingOptions {
    enabled: boolean;
    groupBy: 'none' | 'serviceType' | 'status';
}
```

## Benefits

1. **Improved Navigation**: Quickly find specific services or service types
2. **Status Monitoring**: Focus on problematic or healthy services
3. **Data Organization**: Group related services together
4. **Performance**: Filter out inactive services to reduce visual clutter
5. **User Experience**: Intuitive UI controls with immediate visual feedback

## Compatibility

- Works with existing timeline data (graceful degradation if new properties missing)
- Backward compatible with existing timeline component usage
- Responsive design works on desktop and mobile devices
- Supports both light and dark themes
