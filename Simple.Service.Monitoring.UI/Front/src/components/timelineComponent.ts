import { Timeline, DataSet, TimelineOptions } from "vis-timeline/standalone";
import "./timelineComponent.css"; // Import the externalized CSS

// Define interfaces for timeline items and groups
interface TimelineItem {
    id: number;
    group: string;
    start: Date;
    end: Date;
    content: string;
    className: string;
    title: string;
}

interface TimelineGroup {
    id: string;
    content: string;
    title: string;
}

export interface TimelineSegment {
    startTime: string | number | Date;
    endTime: string | number | Date;
    status: string;
    uptimePercentage?: number;
}

export class TimelineComponent {
    private containerElement: HTMLElement | null = null;
    private timeline: Timeline | null = null;
    private timeRangeHours: number = 24; // Default time range is 24 hours

    constructor(containerId: string) {
        this.containerElement = document.getElementById(containerId);
        // No need to call addTimelineStyles() anymore since we're importing the CSS file
    }

    // New method to set the time range
    public setTimeRange(hours: number): void {
        this.timeRangeHours = hours;
        
        // If we already have a timeline, update its visible window
        if (this.timeline) {
            const now = new Date();
            const start = new Date(now.getTime() - (this.timeRangeHours * 60 * 60 * 1000));
            
            this.timeline.setWindow(start, now);
            
            // Also update the header text if available
            const headerElement = this.containerElement?.querySelector('.timeline-header h5');
            if (headerElement) {
                const startDate = start.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
                const endDate = now.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
                headerElement.textContent = `System status: ${startDate} - ${endDate}`;
            }
        }
    }

    public renderTimeline(timelineData: Record<string, TimelineSegment[]>): void {
        if (!this.containerElement) return;
        
        // If there's an existing timeline, destroy it
        if (this.timeline) {
            try {
                this.timeline.destroy();
            } catch (error) {
                console.warn('Error destroying timeline:', error);
            }
            this.timeline = null;
        }
        
        // Clear the container
        this.containerElement.innerHTML = '';
        
        // Create container for the timeline
        const timelineElement = document.createElement('div');
        timelineElement.className = 'timeline-chart';
        this.containerElement.appendChild(timelineElement);
        
        // Create items for the timeline
        const items = new DataSet<TimelineItem>();
        let itemId = 1;
        
        // Create groups for each service
        const groups = new DataSet<TimelineGroup>();
        
        // Process timeline data
        const serviceNames = Object.keys(timelineData);
        const uptimePercentages: Record<string, number> = {};
        
        // Wrap in try-catch to help debug issues
        try {
            serviceNames.forEach(name => {
                const segments = timelineData[name];
                
                // Validate that segments is an array
                if (!Array.isArray(segments)) {
                    console.error(`Segments for ${name} is not an array:`, segments);
                    return; // Skip this iteration
                }
                
                // Store uptime percentage
                if (segments.length > 0 && segments[0].uptimePercentage !== undefined) {
                    uptimePercentages[name] = segments[0].uptimePercentage;
                } else {
                    uptimePercentages[name] = 0;
                }
                
                // Add this service as a group
                groups.add({
                    id: name,
                    content: `<div class="timeline-group-name">${name}</div>`,
                    title: name
                });
                
                // Add timeline items for each segment
                segments.forEach(segment => {
                    items.add({
                        id: itemId++,
                        group: name,
                        start: new Date(segment.startTime),
                        end: new Date(segment.endTime),
                        content: '',
                        className: `status-${segment.status.toLowerCase()}`,
                        title: `${name}: ${segment.status}<br>${new Date(segment.startTime).toLocaleString()} to ${new Date(segment.endTime).toLocaleString()}`
                    });
                });
            });
            
            // Calculate global date range based on selected time range instead of data
            const now = new Date();
            const maxTime = now.getTime();
            const minTime = maxTime - (this.timeRangeHours * 60 * 60 * 1000);
            
            // Create a header for the timeline with the current time range
            const startDate = new Date(minTime).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
            const endDate = new Date(maxTime).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
            const headerElement = document.createElement('div');
            headerElement.className = 'timeline-header';
            headerElement.innerHTML = `<h5>System status: ${startDate} - ${endDate}</h5>`;
            
            // Insert header at the beginning of container
            this.containerElement.insertBefore(headerElement, timelineElement);
            
            // Create container for the uptime percentages
            const uptimeContainer = document.createElement('div');
            uptimeContainer.className = 'uptime-container';
            
            // Add uptime percentages to the container
            serviceNames.forEach(name => {
                const uptimeElement = document.createElement('div');
                uptimeElement.className = 'uptime-item';
                uptimeElement.innerHTML = `<span class="service-name">${name}:</span> <span class="uptime-value">${uptimePercentages[name].toFixed(2)}% uptime</span>`;
                uptimeContainer.appendChild(uptimeElement);
            });
            
            this.containerElement.appendChild(uptimeContainer);
            
            // Create timeline with options that include our fixed time window
            const options: TimelineOptions = {
                stack: false,
                horizontalScroll: true,
                zoomKey: 'ctrlKey',
                orientation: 'top',
                height: serviceNames.length * 40 + 40, // Height based on number of services
                min: new Date(minTime),  // Set min time based on our range
                max: new Date(maxTime),  // Set max time to now
                start: new Date(minTime), // Initialize view at our min time
                end: new Date(maxTime),   // End view at max time
                margin: {
                    item: {
                        vertical: 10
                    }
                }
            };
            
            // Only create timeline if we have valid data
            if (serviceNames.length > 0) {
                this.timeline = new Timeline(timelineElement, items, groups, options);
                
                // Add event listener to maintain the view when the user zooms or pans
                this.timeline.on('rangechanged', (properties: any) => {
                    // Only if the change was user-initiated (not our own setWindow calls)
                    if (properties.byUser) {
                        // Calculate the new time range in hours
                        const start = properties.start.getTime();
                        const end = properties.end.getTime();
                        const rangeHours = (end - start) / (1000 * 60 * 60);
                        
                        // Update our stored time range
                        this.timeRangeHours = Math.round(rangeHours);
                        
                        // Don't need to call setWindow here as the user has already changed it
                    }
                });
            } else {
                timelineElement.innerHTML = '<div class="alert alert-info">No timeline data available</div>';
            }
        } catch (error) {
            console.error('Error creating timeline:', error);
            timelineElement.innerHTML = '<div class="alert alert-danger">Error creating timeline</div>';
        }
    }
    
    public destroy(): void {
        if (this.timeline) {
            try {
                this.timeline.destroy();
            } catch (error) {
                console.warn('Error destroying timeline:', error);
            }
            this.timeline = null;
        }
    }

    // Set active timeline range method
    public setActiveTimelineRange(hours: number): void {
        // Set the time range
        this.setTimeRange(hours);
        
        // Update the active button state
        const buttonMap: {[key: number]: string} = {
            1: 'timeline-1h',
            24: 'timeline-24h',
            168: 'timeline-7d'  // 7 days = 168 hours
        };
        
        const buttonId = buttonMap[hours] || 'timeline-24h';
        this.setActiveButton(buttonId);
    }

    private setActiveButton(activeButtonId: string): void {
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
        
        // Store the preference
        localStorage.setItem('monitoring-timeline-preference', activeButtonId);
    }
}