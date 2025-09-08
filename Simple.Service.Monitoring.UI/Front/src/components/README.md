# Simple Status Timeline Component

A lightweight TypeScript component for displaying service status timelines, inspired by OpenAI's status page design.

## Features

- 📊 **Clean Visual Design** - Minimalist timeline bars showing service status over time
- 🎨 **Multiple Status Types** - Operational, Degraded, Outage, and Maintenance states
- 🕒 **Flexible Time Ranges** - Support for 30, 60, 90+ day views
- 📱 **Responsive Design** - Works on desktop and mobile devices
- 🌙 **Dark Mode Support** - Automatic dark/light theme detection
- 💬 **Interactive Tooltips** - Hover for detailed event information
- 📈 **Uptime Percentages** - Optional uptime display for each service
- ⚡ **Lightweight** - No external dependencies, pure TypeScript/JavaScript

## Installation

Simply copy the following files to your project:

- `simpleStatusTimeline.ts` - Main component class
- `simpleStatusTimeline.css` - Styling
- `simpleStatusTimeline.demo.html` - Example usage (optional)

## Quick Start

### HTML
```html
<div id="status-timeline"></div>
<link rel="stylesheet" href="simpleStatusTimeline.css">
```

### TypeScript/JavaScript
```typescript
import { SimpleStatusTimeline, ServiceData } from './simpleStatusTimeline';

// Create timeline instance
const timeline = new SimpleStatusTimeline('status-timeline', {
    days: 90,
    showTooltips: true,
    showUptime: true
});

// Prepare your data
const serviceData: ServiceData[] = [
    {
        name: 'API Service',
        uptime: 99.95,
        events: [
            {
                serviceName: 'API Service',
                status: 'outage',
                startTime: new Date('2025-09-01T10:00:00Z'),
                endTime: new Date('2025-09-01T10:30:00Z'),
                message: 'Brief API outage due to database connection issues'
            }
        ]
    },
    {
        name: 'Database',
        uptime: 99.99,
        events: []
    }
];

// Render the timeline
timeline.setData(serviceData);
```

## Configuration Options

### StatusTimelineOptions

```typescript
interface StatusTimelineOptions {
    days: number;           // Number of days to display (default: 90)
    showTooltips: boolean;  // Enable hover tooltips (default: true)
    showUptime: boolean;    // Display uptime percentages (default: true)
}
```

### Data Structure

#### ServiceData
```typescript
interface ServiceData {
    name: string;          // Service display name
    events: StatusEvent[]; // Array of status events
    uptime?: number;       // Optional uptime percentage (0-100)
}
```

#### StatusEvent
```typescript
interface StatusEvent {
    serviceName: string;   // Name of the affected service
    status: 'operational' | 'degraded' | 'outage' | 'maintenance';
    startTime: Date;       // Event start time
    endTime: Date;         // Event end time
    message?: string;      // Optional description
}
```

## API Methods

### Constructor
```typescript
new SimpleStatusTimeline(containerId: string, options?: Partial<StatusTimelineOptions>)
```

### Public Methods

#### `setData(serviceData: ServiceData[]): void`
Sets the timeline data and renders the component.

#### `updateOptions(options: Partial<StatusTimelineOptions>): void`
Updates component options and re-renders.

#### `destroy(): void`
Cleans up the component and removes event listeners.

#### `static generateSampleData(): ServiceData[]`
Generates sample data for testing and demos.

## Status Types

| Status | Color | Description |
|--------|-------|-------------|
| `operational` | 🟢 Green | Service running normally |
| `degraded` | 🟡 Amber | Reduced performance |
| `outage` | 🔴 Red | Service unavailable |
| `maintenance` | 🔵 Blue | Planned maintenance |

## Styling

The component uses CSS custom properties and supports both light and dark themes. Key CSS classes:

- `.simple-status-timeline` - Main container
- `.timeline-service-row` - Individual service row
- `.timeline-day-block` - Individual day status block
- `.status-operational`, `.status-degraded`, `.status-outage`, `.status-maintenance` - Status-specific styling

### Custom Styling Example
```css
.simple-status-timeline {
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.timeline-day-block.status-operational {
    background: #22c55e; /* Custom green */
}
```

## Real-World Integration

### With REST API
```typescript
async function loadTimelineData() {
    try {
        const response = await fetch('/api/status-events');
        const data = await response.json();
        timeline.setData(data);
    } catch (error) {
        console.error('Failed to load timeline data:', error);
    }
}

// Load data every 5 minutes
setInterval(loadTimelineData, 5 * 60 * 1000);
```

### With WebSocket Updates
```typescript
const ws = new WebSocket('wss://your-api.com/status-updates');

ws.onmessage = (event) => {
    const statusUpdate = JSON.parse(event.data);
    // Update your data and refresh timeline
    timeline.setData(updatedServiceData);
};
```

## Browser Support

- Chrome 88+
- Firefox 85+
- Safari 14+
- Edge 88+

## Examples

### Basic Timeline
```typescript
const timeline = new SimpleStatusTimeline('timeline-container');
timeline.setData(SimpleStatusTimeline.generateSampleData());
```

### 30-Day View with No Tooltips
```typescript
const timeline = new SimpleStatusTimeline('timeline-container', {
    days: 30,
    showTooltips: false,
    showUptime: true
});
```

### Dynamic Options Update
```typescript
// Change to 60-day view
timeline.updateOptions({ days: 60 });

// Hide uptime percentages
timeline.updateOptions({ showUptime: false });
```

## Demo

Open `simpleStatusTimeline.demo.html` in your browser to see the component in action with interactive controls.

## License

This component is provided as-is for use in your projects. Feel free to modify and customize as needed.

## Contributing

This is a standalone component. To suggest improvements:

1. Test your changes with the demo file
2. Ensure TypeScript compilation passes
3. Verify responsive design works
4. Check dark mode compatibility

## Troubleshooting

### Common Issues

**Timeline not rendering:**
- Ensure the container element exists in the DOM
- Check that CSS file is properly loaded
- Verify data format matches ServiceData interface

**Tooltips not showing:**
- Confirm `showTooltips: true` in options
- Check that tooltip element isn't being clipped by parent containers

**Styling issues:**
- Ensure CSS file is loaded after any CSS framework
- Check for CSS conflicts with existing styles
- Verify dark mode CSS variables are properly set
