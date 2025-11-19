# Status Page Component

The Status Page Component provides a beautiful, modern visualization of your service health status, similar to popular status pages like status.io or statuspage.io.

## Overview

The status page displays:

- **Service uptime percentages** over the last 24 hours (configurable)
- **Timeline visualization** showing healthy/unhealthy periods
- **Real-time updates** via SignalR
- **Color-coded status indicators** (green, yellow, red)
- **Dark mode support**

## Visual Elements

### Status Indicators

The component uses color-coded indicators to show service health:

- 🟢 **Healthy** (Green) - Service is operating normally
- 🟡 **Degraded** (Yellow) - Service is experiencing issues
- 🔴 **Unhealthy** (Red) - Service is down or critically degraded
- ⚪ **Unknown** (Gray) - No recent data available

### Timeline Bars

Each service displays a timeline bar showing:

- **Time period**: Configurable (default: last 24 hours)
- **Status segments**: Color-coded periods of service health
- **Hover tooltips**: Detailed information on status and duration

### Uptime Percentage

Each service shows an uptime percentage calculated as:

```
Uptime % = (Healthy Time / Total Time) × 100
```

## Configuration

The status page component is automatically integrated when using the monitoring UI.

###Enabling the Component

```csharp
// In Program.cs
services
    .AddServiceMonitoring(configuration)
    .WithServiceMonitoringUi(services, configuration);
```

### Accessing the Status Page

The status page is part of the monitoring dashboard:

```
https://your-app.com/monitoring
```

It appears below the timeline visualization on the dashboard.

## Data Conversion

The component automatically converts timeline data to status page format:

### Data Processing

1. **Filter Invalid Segments**: Removes segments with invalid dates (DateTime.MinValue, etc.)
2. **Normalize Status Values**: Converts status to lowercase for CSS classes
3. **Calculate Uptime**: Computes percentage based on healthy vs total time
4. **Clamp Widths**: Ensures percentage widths are between 0-100%
5. **Handle Edge Cases**: Manages negative durations and missing data

### Timeline to Status Page Conversion

```typescript
// Raw timeline data
{
  "Service1": [
    {
      "startTime": "2025-07-23T10:00:00",
      "endTime": "2025-07-23T11:00:00",
      "status": "Healthy"
    },
    {
      "startTime": "2025-07-23T11:00:00",
      "endTime": "2025-07-23T12:00:00",
      "status": "Unhealthy"
    }
  ]
}

// Converted status page data
{
  "services": [
    {
      "name": "Service1",
      "currentStatus": "unhealthy",
      "uptimePercentage": 50.0,
      "segments": [...]
    }
  ],
  "timeRange": {
    "start": "2025-07-23T00:00:00",
    "end": "2025-07-23T12:00:00",
    "label": "abr 2025 - Jul 2025"
  }
}
```

## Troubleshooting

### Invalid Percentage Widths

**Problem**: Status bars showing massive percentages (e.g., 73945440%)

**Cause**: Invalid date values in timeline data (DateTime.MinValue)

**Solution**: The component now filters out invalid dates automatically.

### Status Colors Not Showing

**Problem**: Status bars are gray or unstyled

**Cause**: Status values are uppercase (e.g., "Unhealthy" instead of "unhealthy")

**Solution**: The component normalizes all status values to lowercase.

### Negative Durations

**Problem**: Tooltips showing negative time durations

**Cause**: End time before start time in segment data

**Solution**: The component validates durations and skips invalid segments.

### Missing Status Page

**Problem**: Status page not appearing on dashboard

**Cause**: DOM element timing issues

**Solution**: The component waits for DOM ready before initialization.

## Customization

### Time Range

Change the time range by modifying the conversion call:

```typescript
// Default: 24 hours
StatusPageComponent.convertTimelineToStatusPage(timelineData, 24);

// 48 hours
StatusPageComponent.convertTimelineToStatusPage(timelineData, 48);

// 7 days
StatusPageComponent.convertTimelineToStatusPage(timelineData, 168);
```

### Styling

The component uses CSS classes that can be customized:

```css
/* Status page container */
.status-page-container {
  background-color: #f8f9fa;
  padding: 20px;
  border-radius: 8px;
}

/* Service item */
.status-service-item {
  background-color: white;
  border: 1px solid #e9ecef;
  padding: 20px;
  margin-bottom: 16px;
}

/* Status indicators */
.status-indicator.healthy {
  background-color: #10b981;
}

.status-indicator.degraded {
  background-color: #f59e0b;
}

.status-indicator.unhealthy {
  background-color: #ef4444;
}

.status-indicator.unknown {
  background-color: #6b7280;
}

/* Status bar segments */
.status-bar-segment.healthy {
  background-color: #10b981;
}

.status-bar-segment.degraded {
  background-color: #f59e0b;
}

.status-bar-segment.unhealthy {
  background-color: #ef4444;
}

.status-bar-segment.unknown {
  background-color: #e5e7eb;
}
```

### Dark Mode

Dark mode styles are automatically applied:

```css
.dark-mode .status-page-container {
  background-color: #2d3748;
  color: #f0f0f0;
}

.dark-mode .status-service-item {
  background-color: #3a4553;
  border-color: #4a5568;
}
```

## Technical Implementation

### Component Architecture

```typescript
class StatusPageComponent {
  private containerElement: HTMLElement | null;
  private timeRangeHours: number = 24;

  // Initialize component
  constructor(containerId: string);

  // Update with new data
  updateData(data: StatusPageData): void;

  // Convert timeline to status page format
  static convertTimelineToStatusPage(
    timelineData: Record<string, any[]>,
    timeRangeHours: number
  ): StatusPageData;

  // Render demo data
  renderDemoData(): void;

  // Cleanup
  destroy(): void;
}
```

### Data Models

```typescript
interface StatusPageData {
  services: ServiceStatus[];
  timeRange: TimeRange;
}

interface ServiceStatus {
  name: string;
  currentStatus: 'healthy' | 'degraded' | 'unhealthy' | 'unknown';
  uptimePercentage: number;
  segments: StatusSegment[];
  componentCount?: number;
}

interface StatusSegment {
  startTime: Date | string;
  endTime: Date | string;
  status: 'healthy' | 'degraded' | 'unhealthy' | 'unknown';
  details?: string;
}

interface TimeRange {
  start: Date;
  end: Date;
  label: string;
}
```

## Real-Time Updates

The component receives automatic updates via SignalR when:

- Health check status changes
- New timeline data is available
- Service recovers from failure

### Update Flow

```
Health Check Runs
      ↓
Results Stored
      ↓
SignalR Notification
      ↓
Dashboard Receives Update
      ↓
Timeline Component Updates
      ↓
Status Page Component Updates
```

## Performance Considerations

### Data Filtering

The component filters timeline data to prevent rendering issues:

- Removes segments with invalid dates
- Validates duration calculations
- Clamps percentage widths to valid ranges
- Skips segments outside the time range

### Rendering Optimization

- Only re-renders when data changes
- Minimal DOM manipulation
- CSS transitions for smooth updates
- Efficient event listener management

## Best Practices

### 1. Configure Appropriate Time Ranges

```typescript
// Too short - not enough historical data
convertTimelineToStatusPage(data, 1);  // 1 hour ❌

// Good - 24 hours is ideal
convertTimelineToStatusPage(data, 24);  // 24 hours ✅

// Too long - may impact performance
convertTimelineToStatusPage(data, 720);  // 30 days ❌
```

### 2. Handle Missing Data

The component gracefully handles:
- No timeline data
- Services with no segments
- Invalid date ranges
- Missing status values

### 3. Monitor Console Output

In development mode, the component logs:
- Data conversion process
- Validation failures
- Rendering steps

Enable debugging:

```typescript
// Enhanced console logging available
console.log('🔄 Converting timeline data...');
console.log('✅ Status page rendered successfully');
```

## Related Documentation

- [Web Dashboard](Web-Dashboard.md) - Main dashboard features
- [Dark Mode](Dark-Mode.md) - Theme customization
- [Debugging Guide](Debugging-Guide.md) - TypeScript debugging
- [Service Types](Service-Types.md) - Monitored service configuration

## Next Steps

- Explore the [Web Dashboard](Web-Dashboard.md) features
- Learn about [Alert Configuration](Alert-Configuration.md)
- Customize with [Dark Mode](Dark-Mode.md)
