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
    private items: DataSet<TimelineItem> | null = null;
    private groups: DataSet<TimelineGroup> | null = null;
    private lastTimelineData: Record<string, TimelineSegment[]> | null = null;

    constructor(containerId: string) {
        this.containerElement = document.getElementById(containerId);
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
            this.updateHeaderText(start, now);
        }
    }
    
    private updateHeaderText(start: Date, end: Date): void {
        const headerElement = this.containerElement?.querySelector('.timeline-header h5');
        if (headerElement) {
            const startDate = start.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
            const endDate = end.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
            headerElement.textContent = `System status: ${startDate} - ${endDate}`;
        }
    }
    
    private updateUptimeContainer(uptimePercentages: Record<string, number>): void {
        // Find or create the uptime container
        let uptimeContainer = this.containerElement?.querySelector('.uptime-container');
        if (!uptimeContainer) {
            uptimeContainer = document.createElement('div');
            uptimeContainer.className = 'uptime-container';
            this.containerElement?.appendChild(uptimeContainer);
        } else {
            // Clear existing content
            uptimeContainer.innerHTML = '';
        }
        
        // Add uptime percentages to the container
        Object.keys(uptimePercentages).forEach(name => {
            const uptimeElement = document.createElement('div');
            uptimeElement.className = 'uptime-item';
            uptimeElement.innerHTML = `<span class="service-name">${name}:</span> <span class="uptime-value">${uptimePercentages[name].toFixed(2)}% uptime</span>`;
            uptimeContainer.appendChild(uptimeElement);
        });
    }

    public renderTimeline(timelineData: Record<string, TimelineSegment[]>): void {
        if (!this.containerElement) return;
        
        this.lastTimelineData = timelineData;
        
        // Calculate global date range based on selected time range
        const now = new Date();
        const maxTime = now.getTime();
        const minTime = maxTime - (this.timeRangeHours * 60 * 60 * 1000);
        
        // Check if we need to create a new timeline or update an existing one
        if (!this.timeline) {
            // Create new timeline from scratch
            this.createNewTimeline(timelineData, minTime, maxTime);
        } else {
            // Update existing timeline without recreating it
            this.updateExistingTimeline(timelineData, minTime, maxTime);
        }
    }
    
    private createNewTimeline(timelineData: Record<string, TimelineSegment[]>, minTime: number, maxTime: number): void {
        // Clear the container
        if (this.containerElement) {
            this.containerElement.innerHTML = '';
        }
        
        // Create header for the timeline
        const startDate = new Date(minTime).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
        const endDate = new Date(maxTime).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
        const headerElement = document.createElement('div');
        headerElement.className = 'timeline-header';
        headerElement.innerHTML = `<h5>System status: ${startDate} - ${endDate}</h5>`;
        
        // Create container for the timeline
        const timelineElement = document.createElement('div');
        timelineElement.className = 'timeline-chart';
        
        // Insert elements into container
        this.containerElement?.appendChild(headerElement);
        this.containerElement?.appendChild(timelineElement);
        
        // Create items and groups for the timeline
        this.items = new DataSet<TimelineItem>();
        this.groups = new DataSet<TimelineGroup>();
        
        // Process timeline data
        const serviceNames = Object.keys(timelineData);
        const uptimePercentages: Record<string, number> = {};
        
        try {
            // Create items and groups
            this.populateItemsAndGroups(timelineData, uptimePercentages);
            
            // Create timeline options
            const options: TimelineOptions = this.createTimelineOptions(serviceNames, minTime, maxTime);
            
            // Create the timeline if we have valid data
            if (serviceNames.length > 0 && timelineElement) {
                this.timeline = new Timeline(timelineElement, this.items, this.groups, options);
                
                // Add event listener for range changes
                this.setupRangeChangeListener();
            } else {
                timelineElement.innerHTML = '<div class="alert alert-info">No timeline data available</div>';
            }
            
            // Create uptime percentages display
            this.updateUptimeContainer(uptimePercentages);
            
        } catch (error) {
            console.error('Error creating timeline:', error);
            if (timelineElement) {
                timelineElement.innerHTML = '<div class="alert alert-danger">Error creating timeline</div>';
            }
        }
    }
    
    private updateExistingTimeline(timelineData: Record<string, TimelineSegment[]>, minTime: number, maxTime: number): void {
        if (!this.items || !this.groups || !this.timeline) return;
        
        const uptimePercentages: Record<string, number> = {};
        const serviceNames = Object.keys(timelineData);
        const existingGroupIds = this.groups.getIds() as string[];
        
        try {
            // Track existing items to remove ones no longer needed
            const newItemIds: number[] = [];
            let itemId = 1;
            
            // Update or add groups
            serviceNames.forEach(name => {
                const segments = timelineData[name];
                
                // Store uptime percentage
                if (segments.length > 0 && segments[0].uptimePercentage !== undefined) {
                    uptimePercentages[name] = segments[0].uptimePercentage;
                } else {
                    uptimePercentages[name] = 0;
                }
                
                // Add this service as a group if it doesn't exist
                if (!existingGroupIds.includes(name) && this.groups) {
                    this.groups.add({
                        id: name,
                        content: `<div class="timeline-group-name">${name}</div>`,
                        title: name
                    });
                }
                
                // Add or update timeline items for each segment
                segments.forEach(segment => {
                    // Parse dates from UTC to local timezone
                    const startDate = new Date(segment.startTime);
                    const endDate = new Date(segment.endTime);
                    
                    const item = {
                        id: itemId,
                        group: name,
                        start: startDate,
                        end: endDate,
                        content: '',
                        className: `status-${segment.status.toLowerCase()}`,
                        title: `${name}: ${segment.status}<br>${startDate.toLocaleString()} to ${endDate.toLocaleString()}`
                    };
                    
                    newItemIds.push(itemId);
                    
                    // Update or add the item
                    if (this.items) {
                        this.items.update(item);
                    }
                    
                    itemId++;
                });
            });
            
            // Remove groups that no longer exist
            const groupsToRemove = existingGroupIds.filter(id => !serviceNames.includes(id));
            if (groupsToRemove.length > 0 && this.groups) {
                this.groups.remove(groupsToRemove);
            }
            
            // Remove items that are no longer in the data
            const existingItemIds = this.items.getIds() as number[];
            const itemsToRemove = existingItemIds.filter(id => !newItemIds.includes(id));
            if (itemsToRemove.length > 0) {
                this.items.remove(itemsToRemove);
            }
            
            // Update timeline window
            this.timeline.setOptions({
                min: new Date(minTime),
                max: new Date(maxTime)
            });
            
            // Update the header text
            this.updateHeaderText(new Date(minTime), new Date(maxTime));
            
            // Update uptime percentages display
            this.updateUptimeContainer(uptimePercentages);
            
        } catch (error) {
            console.error('Error updating timeline:', error);
        }
    }
    
    private populateItemsAndGroups(timelineData: Record<string, TimelineSegment[]>, uptimePercentages: Record<string, number>): void {
        if (!this.items || !this.groups) return;
        
        let itemId = 1;
        Object.keys(timelineData).forEach(name => {
            const segments = timelineData[name];
            
            // Skip if segments is not an array
            if (!Array.isArray(segments)) {
                console.error(`Segments for ${name} is not an array:`, segments);
                return;
            }
            
            // Store uptime percentage
            if (segments.length > 0 && segments[0].uptimePercentage !== undefined) {
                uptimePercentages[name] = segments[0].uptimePercentage;
            } else {
                uptimePercentages[name] = 0;
            }
            
            // Add this service as a group
            this.groups?.add({
                id: name,
                content: `<div class="timeline-group-name">${name}</div>`,
                title: name
            });
            
            // Add timeline items for each segment
            segments.forEach(segment => {
                // Parse dates from UTC to local timezone
                const startDate = new Date(segment.startTime);
                const endDate = new Date(segment.endTime);
                
                this.items?.add({
                    id: itemId++,
                    group: name,
                    start: startDate,
                    end: endDate,
                    content: '',
                    className: `status-${segment.status.toLowerCase()}`,
                    title: `${name}: ${segment.status}<br>${startDate.toLocaleString()} to ${endDate.toLocaleString()}`
                });
            });
        });
    }
    
    private createTimelineOptions(serviceNames: string[], minTime: number, maxTime: number): TimelineOptions {
        // Calculate height with a minimum to ensure proper display
        const calculatedHeight = serviceNames.length * 40 + 40;
        const minHeight = 150; // Minimum height for proper timeline display
        const finalHeight = Math.max(calculatedHeight, minHeight);
        
        return {
            stack: false,
            horizontalScroll: true,
            zoomKey: 'ctrlKey',
            orientation: 'top',
            height: finalHeight, // Height with minimum guarantee
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
    }
    
    private setupRangeChangeListener(): void {
        if (!this.timeline) return;
        
        this.timeline.on('rangechanged', (properties: any) => {
            // Only if the change was user-initiated (not our own setWindow calls)
            if (properties.byUser) {
                // Calculate the new time range in hours
                const start = properties.start.getTime();
                const end = properties.end.getTime();
                const rangeHours = (end - start) / (1000 * 60 * 60);
                
                // Update our stored time range
                this.timeRangeHours = Math.round(rangeHours);
            }
        });
    }
    
    // Method to refresh the timeline with the last data (useful for theme changes)
    public refresh(): void {
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
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
        
        this.items = null;
        this.groups = null;
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
