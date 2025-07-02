export interface DataTableOptions {
    searchable?: boolean;
    sortable?: boolean;
    perPage?: number;
    pageButtonCount?: number;
    tableSelector: string;
}

export class DataTable {
    private table: HTMLTableElement;
    private options: DataTableOptions;
    private tableHeaders: HTMLTableHeaderCellElement[] = [];
    private tableBody: HTMLTableSectionElement | null = null;
    private tableRows: HTMLTableRowElement[] = [];
    private searchInput: HTMLInputElement | null = null;
    private paginationContainer: HTMLDivElement | null = null;
    private currentPage = 1;
    private totalPages = 1;
    private sortColumn = -1;
    private sortAscending = true;
    private originalData: HTMLTableRowElement[] = [];

    constructor(options: DataTableOptions) {
        this.options = {
            searchable: true,
            sortable: true,
            perPage: 10,
            pageButtonCount: 5,
            ...options
        };
        
        const table = document.querySelector(this.options.tableSelector) as HTMLTableElement;
        if (!table) {
            throw new Error(`Table not found with selector: ${this.options.tableSelector}`);
        }
        
        this.table = table;
        this.init();
    }
    
    private init(): void {
        // Get table components
        const thead = this.table.querySelector('thead');
        if (!thead) {
            throw new Error('Table must have a thead element');
        }
        
        this.tableBody = this.table.querySelector('tbody');
        if (!this.tableBody) {
            throw new Error('Table must have a tbody element');
        }
        
        // Store original rows for filtering and sorting
        this.originalData = Array.from(this.tableBody.querySelectorAll('tr'));
        this.tableRows = [...this.originalData];
        
        // Get all headers
        const headerRow = thead.querySelector('tr');
        if (!headerRow) {
            throw new Error('Table thead must have a tr element');
        }
        
        this.tableHeaders = Array.from(headerRow.querySelectorAll('th'));
        
        // Add container for search and pagination
        this.addControls();
        
        // Add event handlers for sorting if enabled
        if (this.options.sortable) {
            this.initSorting();
        }
        
        // Initialize pagination
        this.updateTable();
    }
    
    private addControls(): void {
        const controlsContainer = document.createElement('div');
        controlsContainer.className = 'datatable-controls mb-2';
        
        // Create search input if searchable
        if (this.options.searchable) {
            const searchContainer = document.createElement('div');
            searchContainer.className = 'datatable-search mb-2';
            
            this.searchInput = document.createElement('input');
            this.searchInput.type = 'text';
            this.searchInput.className = 'form-control';
            this.searchInput.placeholder = 'Search...';
            this.searchInput.addEventListener('input', () => this.handleSearch());
            
            searchContainer.appendChild(this.searchInput);
            controlsContainer.appendChild(searchContainer);
        }
        
        // Create pagination container
        this.paginationContainer = document.createElement('div');
        this.paginationContainer.className = 'datatable-pagination';
        controlsContainer.appendChild(this.paginationContainer);
        
        // Insert controls before the table
        this.table.parentNode?.insertBefore(controlsContainer, this.table);
    }
    
    private handleSearch(): void {
        const searchTerm = this.searchInput?.value.toLowerCase() || '';
        
        if (searchTerm === '') {
            // Reset to original data if search is empty
            this.tableRows = [...this.originalData];
        } else {
            // Filter rows based on search term
            this.tableRows = this.originalData.filter(row => {
                const text = row.textContent?.toLowerCase() || '';
                return text.includes(searchTerm);
            });
        }
        
        this.currentPage = 1;
        this.updateTable();
    }
    
private initSorting(): void {
    this.tableHeaders.forEach((header, index) => {
        // Add sorting indicators using Bootstrap Icons
        const sortIndicator = document.createElement('span');
        sortIndicator.className = 'sort-indicator ms-1';
        sortIndicator.innerHTML = '<i class="bi bi-arrow-down-up"></i>';
        header.appendChild(sortIndicator);
        
        // Add pointer cursor style
        header.style.cursor = 'pointer';
        
        // Add click event for sorting
        header.addEventListener('click', () => this.handleSort(index));
    });
}

private handleSort(columnIndex: number): void {
    // Update sort direction if clicking the same column
    if (this.sortColumn === columnIndex) {
        this.sortAscending = !this.sortAscending;
    } else {
        this.sortColumn = columnIndex;
        this.sortAscending = true;
    }
    
    // Update sort indicators with Bootstrap Icons
    this.tableHeaders.forEach((header, index) => {
        const indicator = header.querySelector('.sort-indicator');
        if (indicator) {
            if (index === this.sortColumn) {
                indicator.innerHTML = this.sortAscending 
                    ? '<i class="bi bi-arrow-up"></i>' 
                    : '<i class="bi bi-arrow-down"></i>';
            } else {
                indicator.innerHTML = '<i class="bi bi-arrow-down-up"></i>';
            }
        }
    });
    
    // Sort the rows
    this.tableRows.sort((a, b) => {
        // Rest of the sort function remains unchanged
        const cellA = a.cells[columnIndex]?.textContent?.trim() || '';
        const cellB = b.cells[columnIndex]?.textContent?.trim() || '';
        
        // Try to parse as numbers if possible
        const numA = parseFloat(cellA);
        const numB = parseFloat(cellB);
        
        if (!isNaN(numA) && !isNaN(numB)) {
            return this.sortAscending ? numA - numB : numB - numA;
        }
        
        // Handle date strings
        const dateA = this.tryParseDate(cellA);
        const dateB = this.tryParseDate(cellB);
        
        if (dateA && dateB) {
            return this.sortAscending ? 
                dateA.getTime() - dateB.getTime() : 
                dateB.getTime() - dateA.getTime();
        }
        
        // Sort as strings
        return this.sortAscending ? 
            cellA.localeCompare(cellB) : 
            cellB.localeCompare(cellA);
    });
    
    this.updateTable();
}

    
    private tryParseDate(dateStr: string): Date | null {
        // Try to parse various date formats
        const date = new Date(dateStr);
        return !isNaN(date.getTime()) ? date : null;
    }
    
    private updateTable(): void {
        // Calculate pagination
        if (this.options.perPage) {
            this.totalPages = Math.max(1, Math.ceil(this.tableRows.length / this.options.perPage));
            this.currentPage = Math.min(this.currentPage, this.totalPages);
        } else {
            this.totalPages = 1;
            this.currentPage = 1;
        }
        
        // Clear current table body
        if (this.tableBody) {
            this.tableBody.innerHTML = '';
            
            // Add visible rows to the table
            const startIndex = (this.currentPage - 1) * (this.options.perPage || this.tableRows.length);
            const endIndex = Math.min(startIndex + (this.options.perPage || this.tableRows.length), this.tableRows.length);
            
            for (let i = startIndex; i < endIndex; i++) {
                this.tableBody.appendChild(this.tableRows[i].cloneNode(true));
            }
        }
        
        this.updatePagination();
    }
    
    private updatePagination(): void {
        if (!this.paginationContainer) return;
        
        // Clear current pagination
        this.paginationContainer.innerHTML = '';
        
        // Don't show pagination if only one page
        if (this.totalPages <= 1) return;
        
        // Create pagination nav
        const paginationNav = document.createElement('nav');
        paginationNav.setAttribute('aria-label', 'Table pagination');
        
        const paginationList = document.createElement('ul');
        paginationList.className = 'pagination pagination-sm justify-content-end';
        
        // Previous button
        this.addPaginationButton(paginationList, this.currentPage > 1, '&laquo;', () => {
            this.currentPage = Math.max(1, this.currentPage - 1);
            this.updateTable();
        });
        
        // Page buttons
        const pageButtonCount = this.options.pageButtonCount || 5;
        let startPage = Math.max(1, this.currentPage - Math.floor(pageButtonCount / 2));
        const endPage = Math.min(this.totalPages, startPage + pageButtonCount - 1);
        
        // Adjust startPage if endPage is at maximum
        startPage = Math.max(1, endPage - pageButtonCount + 1);
        
        for (let i = startPage; i <= endPage; i++) {
            const isActive = i === this.currentPage;
            this.addPaginationButton(paginationList, true, i.toString(), () => {
                this.currentPage = i;
                this.updateTable();
            }, isActive);
        }
        
        // Next button
        this.addPaginationButton(paginationList, this.currentPage < this.totalPages, '&raquo;', () => {
            this.currentPage = Math.min(this.totalPages, this.currentPage + 1);
            this.updateTable();
        });
        
        paginationNav.appendChild(paginationList);
        this.paginationContainer.appendChild(paginationNav);
    }
    
    private addPaginationButton(
        container: HTMLUListElement, 
        enabled: boolean, 
        label: string, 
        onClick: () => void,
        active = false
    ): void {
        const item = document.createElement('li');
        item.className = `page-item ${active ? 'active' : ''} ${!enabled ? 'disabled' : ''}`;
        
        const link = document.createElement('a');
        link.className = 'page-link';
        link.href = '#';
        link.innerHTML = label;
        link.addEventListener('click', (e) => {
            e.preventDefault();
            if (enabled) onClick();
        });
        
        item.appendChild(link);
        container.appendChild(item);
    }
    
    // Public method to refresh the data (useful after data updates)
    public refresh(): void {
        if (this.tableBody) {
            this.originalData = Array.from(this.tableBody.querySelectorAll('tr'));
            this.tableRows = [...this.originalData];
            this.handleSearch(); // Apply current search filter
        }
    }
    
    // Method to handle real-time data updates
    public updateData(newData?: HTMLTableRowElement[]): void {
        if (newData) {
            // If new data is provided, replace the original data
            this.originalData = newData;
        } else if (this.tableBody) {
            // Otherwise, refresh from the current table
            this.originalData = Array.from(this.tableBody.querySelectorAll('tr'));
        }
        
        // Re-apply filtering and sorting
        this.handleSearch();
        if (this.sortColumn !== -1) {
            this.handleSort(this.sortColumn);
        } else {
            this.tableRows = [...this.originalData];
            this.updateTable();
        }
    }
}

// Utility function to create DataTable instances
export function initDataTables(): void {
    const tables = document.querySelectorAll('[data-datatable]');
    tables.forEach(table => {
        const options: DataTableOptions = {
            tableSelector: `#${table.id}`,
            searchable: table.getAttribute('data-searchable') !== 'false',
            sortable: table.getAttribute('data-sortable') !== 'false',
            perPage: parseInt(table.getAttribute('data-per-page') || '10', 10)
        };
        
        new DataTable(options);
    });
}
