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
    serviceName?: string;
    serviceType?: string;
}

export interface TimelineFilterOptions {
    enabled: boolean;
    serviceNameFilter?: Set<string>;
    serviceTypeFilter?: Set<string>;
    statusFilter?: Set<string>;
    showActiveOnly?: boolean;
    activeThresholdMinutes?: number;
}

export interface TimelineGroupingOptions {
    enabled: boolean;
    groupBy: 'none' | 'serviceType' | 'status';
}

export class TimelineComponent {
    private containerElement: HTMLElement | null = null;
    private timeline: Timeline | null = null;
    private timeRangeHours: number = 24; // Default time range is 24 hours
    private items: DataSet<TimelineItem> | null = null;
    private groups: DataSet<TimelineGroup> | null = null;
    private lastTimelineData: Record<string, TimelineSegment[]> | null = null;
    private filterOptions: TimelineFilterOptions = {
        enabled: false,
        serviceNameFilter: new Set(),
        serviceTypeFilter: new Set(),
        statusFilter: new Set(),
        showActiveOnly: false,
        activeThresholdMinutes: 30
    };
    private groupingOptions: TimelineGroupingOptions = {
        enabled: false,
        groupBy: 'none'
    };
    private filtersContainer: HTMLElement | null = null;    constructor(containerId: string) {
        this.containerElement = document.getElementById(containerId);
    }

    // Enable filters functionality
    public enableFilters(options: Partial<TimelineFilterOptions> = {}): void {
        this.filterOptions = {
            ...this.filterOptions,
            enabled: true,
            ...options
        };
        
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
        }
    }

    // Enable grouping functionality
    public enableGrouping(options: Partial<TimelineGroupingOptions> = {}): void {
        this.groupingOptions = {
            ...this.groupingOptions,
            enabled: true,
            ...options
        };
        
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
        }
    }

    // Update filter values
    public updateFilters(updates: Partial<TimelineFilterOptions>): void {
        this.filterOptions = { ...this.filterOptions, ...updates };
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
        }
    }

    // Update grouping options
    public updateGrouping(updates: Partial<TimelineGroupingOptions>): void {
        this.groupingOptions = { ...this.groupingOptions, ...updates };
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
        }
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
    
    private createFiltersContainer(timelineData: Record<string, TimelineSegment[]>): void {
        if (!this.containerElement) return;
        
        // Create filters container
        this.filtersContainer = document.createElement('div');
        this.filtersContainer.className = 'timeline-filters datatable-controls mb-3';
          // Extract unique values for filters
        const uniqueServiceNames = new Set<string>();
        const uniqueServiceTypes = new Set<string>();
        const uniqueStatuses = new Set<string>();
        
        Object.keys(timelineData).forEach(serviceName => {
            uniqueServiceNames.add(serviceName);
            timelineData[serviceName].forEach(segment => {
                uniqueStatuses.add(segment.status);
                  // Extract service type from segment or determine from service name
                let serviceType = segment.serviceType;
                if (!serviceType) {
                    serviceType = this.determineServiceTypeFromName(serviceName);
                    // Enrich the segment with the determined service type
                    segment.serviceType = serviceType;
                }
                
                // Enrich segment with service name if not present
                if (!segment.serviceName) {
                    segment.serviceName = serviceName;
                }
                
                if (serviceType) {
                    uniqueServiceTypes.add(serviceType);
                }
            });
        });
        
        // Create filter controls row
        const filtersRow = document.createElement('div');
        filtersRow.className = 'row g-3 datatable-filters';
        
        // Service Name Filter
        if (uniqueServiceNames.size > 1) {
            const serviceNameCol = document.createElement('div');
            serviceNameCol.className = 'col-md-3';
            
            const serviceNameLabel = document.createElement('label');
            serviceNameLabel.className = 'form-label small text-muted';
            serviceNameLabel.textContent = 'Service Name';
            
            const serviceNameSelect = document.createElement('select');
            serviceNameSelect.className = 'form-select form-select-sm';
            serviceNameSelect.multiple = true;
            serviceNameSelect.id = 'timeline-service-name-filter';
            
            const allServicesOption = document.createElement('option');
            allServicesOption.value = '';
            allServicesOption.textContent = 'All Services';
            serviceNameSelect.appendChild(allServicesOption);
            
            uniqueServiceNames.forEach(name => {
                const option = document.createElement('option');
                option.value = name;
                option.textContent = name;
                serviceNameSelect.appendChild(option);
            });
            
            serviceNameSelect.addEventListener('change', () => {
                const selectedValues = Array.from(serviceNameSelect.selectedOptions)
                    .map(option => option.value)
                    .filter(value => value !== '');
                this.filterOptions.serviceNameFilter = new Set(selectedValues);
                this.applyFilters();
            });
            
            serviceNameCol.appendChild(serviceNameLabel);
            serviceNameCol.appendChild(serviceNameSelect);
            filtersRow.appendChild(serviceNameCol);
        }
        
        // Service Type Filter
        if (uniqueServiceTypes.size > 1) {
            const serviceTypeCol = document.createElement('div');
            serviceTypeCol.className = 'col-md-3';
            
            const serviceTypeLabel = document.createElement('label');
            serviceTypeLabel.className = 'form-label small text-muted';
            serviceTypeLabel.textContent = 'Service Type';
            
            const serviceTypeSelect = document.createElement('select');
            serviceTypeSelect.className = 'form-select form-select-sm';
            serviceTypeSelect.multiple = true;
            serviceTypeSelect.id = 'timeline-service-type-filter';
            
            const allTypesOption = document.createElement('option');
            allTypesOption.value = '';
            allTypesOption.textContent = 'All Types';
            serviceTypeSelect.appendChild(allTypesOption);
            
            uniqueServiceTypes.forEach(type => {
                const option = document.createElement('option');
                option.value = type;
                option.textContent = type;
                serviceTypeSelect.appendChild(option);
            });
            
            serviceTypeSelect.addEventListener('change', () => {
                const selectedValues = Array.from(serviceTypeSelect.selectedOptions)
                    .map(option => option.value)
                    .filter(value => value !== '');
                this.filterOptions.serviceTypeFilter = new Set(selectedValues);
                this.applyFilters();
            });
            
            serviceTypeCol.appendChild(serviceTypeLabel);
            serviceTypeCol.appendChild(serviceTypeSelect);
            filtersRow.appendChild(serviceTypeCol);
        }
        
        // Status Filter
        if (uniqueStatuses.size > 1) {
            const statusCol = document.createElement('div');
            statusCol.className = 'col-md-3';
            
            const statusLabel = document.createElement('label');
            statusLabel.className = 'form-label small text-muted';
            statusLabel.textContent = 'Status';
            
            const statusSelect = document.createElement('select');
            statusSelect.className = 'form-select form-select-sm';
            statusSelect.multiple = true;
            statusSelect.id = 'timeline-status-filter';
            
            const allStatusOption = document.createElement('option');
            allStatusOption.value = '';
            allStatusOption.textContent = 'All Statuses';
            statusSelect.appendChild(allStatusOption);
            
            uniqueStatuses.forEach(status => {
                const option = document.createElement('option');
                option.value = status;
                option.textContent = status.charAt(0).toUpperCase() + status.slice(1);
                statusSelect.appendChild(option);
            });
            
            statusSelect.addEventListener('change', () => {
                const selectedValues = Array.from(statusSelect.selectedOptions)
                    .map(option => option.value)
                    .filter(value => value !== '');
                this.filterOptions.statusFilter = new Set(selectedValues);
                this.applyFilters();
            });
            
            statusCol.appendChild(statusLabel);
            statusCol.appendChild(statusSelect);
            filtersRow.appendChild(statusCol);
        }
        
        // Grouping Options
        if (this.groupingOptions.enabled) {
            const groupingCol = document.createElement('div');
            groupingCol.className = 'col-md-3';
            
            const groupingLabel = document.createElement('label');
            groupingLabel.className = 'form-label small text-muted';
            groupingLabel.textContent = 'Group By';
            
            const groupingSelect = document.createElement('select');
            groupingSelect.className = 'form-select form-select-sm';
            groupingSelect.id = 'timeline-grouping-select';
            
            const groupingOptions = [
                { value: 'none', text: 'No Grouping' },
                { value: 'serviceType', text: 'Service Type' },
                { value: 'status', text: 'Status' }
            ];
            
            groupingOptions.forEach(option => {
                const optionElement = document.createElement('option');
                optionElement.value = option.value;
                optionElement.textContent = option.text;
                if (option.value === this.groupingOptions.groupBy) {
                    optionElement.selected = true;
                }
                groupingSelect.appendChild(optionElement);
            });
            
            groupingSelect.addEventListener('change', () => {
                this.groupingOptions.groupBy = groupingSelect.value as 'none' | 'serviceType' | 'status';
                this.applyFilters();
            });
            
            groupingCol.appendChild(groupingLabel);
            groupingCol.appendChild(groupingSelect);
            filtersRow.appendChild(groupingCol);
        }
        
        // Active Only Filter
        const activeFilterCol = document.createElement('div');
        activeFilterCol.className = 'col-md-3 d-flex align-items-end';
        
        const activeFilterDiv = document.createElement('div');
        activeFilterDiv.className = 'form-check';
        
        const activeFilterCheckbox = document.createElement('input');
        activeFilterCheckbox.type = 'checkbox';
        activeFilterCheckbox.className = 'form-check-input';
        activeFilterCheckbox.id = 'timeline-active-filter';
        activeFilterCheckbox.checked = this.filterOptions.showActiveOnly || false;
        
        const activeFilterLabel = document.createElement('label');
        activeFilterLabel.className = 'form-check-label small';
        activeFilterLabel.htmlFor = 'timeline-active-filter';
        activeFilterLabel.textContent = `Active Only (${this.filterOptions.activeThresholdMinutes}min)`;
        
        activeFilterCheckbox.addEventListener('change', () => {
            this.filterOptions.showActiveOnly = activeFilterCheckbox.checked;
            this.applyFilters();
        });
        
        activeFilterDiv.appendChild(activeFilterCheckbox);
        activeFilterDiv.appendChild(activeFilterLabel);
        activeFilterCol.appendChild(activeFilterDiv);
        filtersRow.appendChild(activeFilterCol);
        
        this.filtersContainer.appendChild(filtersRow);
        this.containerElement.appendChild(this.filtersContainer);
    }
    
    private applyFilters(): void {
        if (this.lastTimelineData) {
            this.renderTimeline(this.lastTimelineData);
        }
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
        
        // Create filters container if filters are enabled
        if (this.filterOptions.enabled) {
            this.createFiltersContainer(timelineData);
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
            const options: TimelineOptions = this.createTimelineOptions(minTime, maxTime);
            
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
        
        try {
            // Clear all existing items and groups first
            this.items.clear();
            this.groups.clear();
            
            // Apply filters and grouping just like in createNewTimeline
            this.populateItemsAndGroups(timelineData, uptimePercentages);
            
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
        
        // Apply filters to the timeline data
        const filteredData = this.applyDataFilters(timelineData);
        
        // Apply grouping if enabled
        const groupedData = this.applyGrouping(filteredData);
        
        let itemId = 1;
        Object.keys(groupedData).forEach(groupKey => {
            const groupData = groupedData[groupKey];
            
            // Calculate group uptime percentage
            let totalUptime = 0;
            let serviceCount = 0;
            
            Object.keys(groupData).forEach(serviceName => {
                const segments = groupData[serviceName];
                
                // Skip if segments is not an array
                if (!Array.isArray(segments)) {
                    console.error(`Segments for ${serviceName} is not an array:`, segments);
                    return;
                }
                
                // Store uptime percentage
                if (segments.length > 0 && segments[0].uptimePercentage !== undefined) {
                    const uptime = segments[0].uptimePercentage;
                    uptimePercentages[serviceName] = uptime;
                    totalUptime += uptime;
                    serviceCount++;
                } else {
                    uptimePercentages[serviceName] = 0;
                }
                
                // Determine group display name based on grouping
                let groupDisplayName = groupKey;
                if (this.groupingOptions.groupBy !== 'none' && serviceCount > 1) {
                    groupDisplayName = `${groupKey} (${Object.keys(groupData).length} services)`;
                }
                
                // Add this service as a group (or use grouped name)
                const finalGroupId = this.groupingOptions.groupBy === 'none' ? serviceName : groupKey;
                const finalGroupName = this.groupingOptions.groupBy === 'none' ? serviceName : groupDisplayName;
                
                this.groups?.add({
                    id: finalGroupId,
                    content: `<div class="timeline-group-name">${finalGroupName}</div>`,
                    title: finalGroupName
                });
                
                // Add timeline items for each segment
                segments.forEach(segment => {
                    // Parse dates from UTC to local timezone
                    const startDate = new Date(segment.startTime);
                    const endDate = new Date(segment.endTime);
                    
                    this.items?.add({
                        id: itemId++,
                        group: finalGroupId,
                        start: startDate,
                        end: endDate,
                        content: '',
                        className: `status-${segment.status.toLowerCase()}`,
                        title: `${serviceName}: ${segment.status}<br>${startDate.toLocaleString()} to ${endDate.toLocaleString()}`
                    });
                });
            });
            
            // Store group uptime if we have multiple services
            if (serviceCount > 1 && this.groupingOptions.groupBy !== 'none') {
                uptimePercentages[groupKey] = totalUptime / serviceCount;
            }
        });
    }
      private applyDataFilters(timelineData: Record<string, TimelineSegment[]>): Record<string, TimelineSegment[]> {
        console.log('Applying filters with options:', this.filterOptions);
        const filteredData: Record<string, TimelineSegment[]> = {};
        const now = new Date();
        const activeThreshold = new Date(now.getTime() - (this.filterOptions.activeThresholdMinutes || 30) * 60 * 1000);        Object.keys(timelineData).forEach(serviceName => {
            const segments = timelineData[serviceName];
            
            // Enrich segments with service metadata if missing
            segments.forEach(segment => {
                if (!segment.serviceName) {
                    segment.serviceName = serviceName;
                }
                if (!segment.serviceType) {
                    segment.serviceType = this.determineServiceTypeFromName(serviceName);
                }
            });
            
            console.log(`Service: ${serviceName}, ServiceType: ${segments[0]?.serviceType}, Status: ${segments[0]?.status}`);
            
            // Apply service name filter
            if (this.filterOptions.serviceNameFilter && this.filterOptions.serviceNameFilter.size > 0) {
                console.log(`Checking service name filter:`, this.filterOptions.serviceNameFilter);
                if (!this.filterOptions.serviceNameFilter.has(serviceName)) {
                    console.log(`Service ${serviceName} filtered out by service name filter`);
                    return; // Skip this service
                }
            }
            
            // Filter segments based on criteria
            const filteredSegments = segments.filter(segment => {
                // Apply service type filter
                if (this.filterOptions.serviceTypeFilter && this.filterOptions.serviceTypeFilter.size > 0) {
                    console.log(`Checking service type filter:`, this.filterOptions.serviceTypeFilter);
                    if (!segment.serviceType || !this.filterOptions.serviceTypeFilter.has(segment.serviceType)) {
                        console.log(`Segment filtered out by service type filter: ${segment.serviceType}`);
                        return false;
                    }
                }
                
                // Apply status filter
                if (this.filterOptions.statusFilter && this.filterOptions.statusFilter.size > 0) {
                    console.log(`Checking status filter:`, this.filterOptions.statusFilter);
                    if (!this.filterOptions.statusFilter.has(segment.status)) {
                        console.log(`Segment filtered out by status filter: ${segment.status}`);
                        return false;
                    }
                }
                
                // Apply active only filter
                if (this.filterOptions.showActiveOnly) {
                    const segmentEndTime = new Date(segment.endTime);
                    if (segmentEndTime < activeThreshold) {
                        console.log(`Segment filtered out by active only filter`);
                        return false;
                    }
                }
                
                return true;
            });
            
            // Only include services that have segments after filtering
            if (filteredSegments.length > 0) {
                filteredData[serviceName] = filteredSegments;
                console.log(`Service ${serviceName} included with ${filteredSegments.length} segments`);
            } else {
                console.log(`Service ${serviceName} excluded - no segments after filtering`);
            }
        });
        
        console.log('Filtered data result:', Object.keys(filteredData));
        return filteredData;
    }
    
    private applyGrouping(timelineData: Record<string, TimelineSegment[]>): Record<string, Record<string, TimelineSegment[]>> {
        if (!this.groupingOptions.enabled || this.groupingOptions.groupBy === 'none') {
            // Return data in the same format but nested
            const result: Record<string, Record<string, TimelineSegment[]>> = {};
            Object.keys(timelineData).forEach(serviceName => {
                result[serviceName] = { [serviceName]: timelineData[serviceName] };
            });
            return result;
        }
        
        const groupedData: Record<string, Record<string, TimelineSegment[]>> = {};
        
        Object.keys(timelineData).forEach(serviceName => {
            const segments = timelineData[serviceName];
            let groupKey = serviceName; // Default group key
            
            if (this.groupingOptions.groupBy === 'serviceType') {
                // Group by service type - use the serviceType from the first segment
                const firstSegment = segments[0];
                groupKey = firstSegment?.serviceType || 'Unknown Type';
            } else if (this.groupingOptions.groupBy === 'status') {
                // Group by the most recent status
                const lastSegment = segments[segments.length - 1];
                groupKey = lastSegment?.status || 'Unknown Status';
            }
            
            if (!groupedData[groupKey]) {
                groupedData[groupKey] = {};
            }
            
            groupedData[groupKey][serviceName] = segments;
        });
          return groupedData;
    }
    
    private determineServiceTypeFromName(serviceName: string): string {
        if (!serviceName) return "Unknown";
        
        const lowerServiceName = serviceName.toLowerCase();
        
        // Determine service type based on common patterns in service names
        if (lowerServiceName.includes("gateway") || lowerServiceName.includes("proxy")) {
            return "Gateway";
        } else if (lowerServiceName.includes("api") || lowerServiceName.includes("service") || lowerServiceName.includes("microservice")) {
            return "API/Service";
        } else if (lowerServiceName.includes("database") || lowerServiceName.includes("db") || lowerServiceName.includes("sql") || lowerServiceName.includes("redis") || lowerServiceName.includes("mongo")) {
            return "Database";
        } else if (lowerServiceName.includes("queue") || lowerServiceName.includes("message") || lowerServiceName.includes("kafka") || lowerServiceName.includes("rabbit")) {
            return "Messaging";
        } else if (lowerServiceName.includes("cache") || lowerServiceName.includes("memory")) {
            return "Cache";
        } else if (lowerServiceName.includes("auth") || lowerServiceName.includes("identity") || lowerServiceName.includes("login")) {
            return "Authentication";
        } else if (lowerServiceName.includes("notification") || lowerServiceName.includes("email") || lowerServiceName.includes("sms")) {
            return "Notification";
        } else if (lowerServiceName.includes("file") || lowerServiceName.includes("storage") || lowerServiceName.includes("blob")) {
            return "Storage";
        } else if (lowerServiceName.includes("web") || lowerServiceName.includes("ui") || lowerServiceName.includes("frontend")) {
            return "Web/UI";
        } else {
            return "Service";
        }
    }
    
    private createTimelineOptions(minTime: number, maxTime: number): TimelineOptions {        
        return {
            stack: false,
            horizontalScroll: true,
            align: 'center',            
            zoomKey: 'ctrlKey',
            orientation: 'top',            
            autoResize: true,
            min: new Date(minTime),  // Set min time based on our range
            max: new Date(maxTime),  // Set max time to now
            start: new Date(minTime), // Initialize view at our min time
            end: new Date(maxTime)   // End view at max time
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
