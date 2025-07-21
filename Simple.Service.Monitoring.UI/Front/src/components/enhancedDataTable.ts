import './enhancedDataTable.css';
import './dataTableFilters.css';


export interface DataTableColumn {
    key: string;
    label: string;
    sortable?: boolean;
    filterable?: boolean;
    render?: (value: any, row: any) => string;
    className?: string;
    width?: string;
}

export interface DataTableOptions {
    searchable?: boolean;
    sortable?: boolean;
    perPage?: number;
    pageButtonCount?: number;
    containerSelector: string;
    columns: DataTableColumn[];
    emptyMessage?: string;
    className?: string;
    groupingFilters?: {
        enabled: boolean;
        columns: GroupingFilterColumn[];
    };
    customFilters?: {
        enabled: boolean;
        showActiveOnly?: boolean;
        activeThresholdMinutes?: number;
    };
}

export interface GroupingFilterColumn {
    columnKey: string;
    label: string;
    filterType: 'dropdown' | 'multiselect';
}

export interface FilterState {
    [columnKey: string]: Set<string>;
}

export class EnhancedDataTable<T = any> {
    private container: HTMLElement;
    private options: DataTableOptions;
    private table: HTMLTableElement | null = null;
    private tableBody: HTMLTableSectionElement | null = null;
    private searchInput: HTMLInputElement | null = null;
    private paginationContainer: HTMLDivElement | null = null;
    private filtersContainer: HTMLDivElement | null = null;
    private currentPage = 1;
    private totalPages = 1;
    private sortColumn = '';
    private sortAscending = true;    private originalData: T[] = [];
    private filteredData: T[] = [];
    private filterState: FilterState = {};
    private searchTerm = '';
    private showActiveOnly = false;    constructor(options: DataTableOptions) {
        this.options = {
            searchable: true,
            sortable: true,
            perPage: 10,
            pageButtonCount: 5,
            emptyMessage: 'No data available',
            className: 'table table-striped table-hover',
            customFilters: {
                enabled: false,
                showActiveOnly: false,
                activeThresholdMinutes: 30
            },
            ...options
        };

        // Set default active filter state
        this.showActiveOnly = this.options.customFilters?.showActiveOnly || false;

        const container = document.querySelector(options.containerSelector);
        if (!container) {
            throw new Error(`Container element not found: ${options.containerSelector}`);
        }
        this.container = container as HTMLElement;

        this.showLoading();
        this.initializeComponent();
    }

    private showLoading(): void {
        this.container.innerHTML = `
            <div class="datatable-loading">
                <div class="datatable-loading-spinner"></div>
                <div class="datatable-loading-text">Loading health checks...</div>
                <div class="datatable-skeleton">
                    <div class="skeleton-row">
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                    </div>
                    <div class="skeleton-row">
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                    </div>
                    <div class="skeleton-row">
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                        <div class="skeleton-cell"></div>
                    </div>
                </div>
            </div>
        `;
    }

    private hideLoading(): void {
        const loadingElement = this.container.querySelector('.datatable-loading');
        if (loadingElement) {
            loadingElement.remove();
        }
    }    private initializeComponent(): void {
        this.hideLoading();
        
        // Clear container
        this.container.innerHTML = '';
        this.container.className = 'enhanced-datatable-container';

        // Create main structure
        this.createFilters();
        this.createSearchInput();
        this.createTable();
        this.createPagination();

        // Initialize filter state
        this.initializeFilterState();
    }

    private createFilters(): void {
        if (!this.options.groupingFilters?.enabled || !this.options.groupingFilters.columns.length) {
            return;
        }

        this.filtersContainer = document.createElement('div');
        this.filtersContainer.className = 'datatable-filters mb-3';

        const filtersRow = document.createElement('div');
        filtersRow.className = 'row g-2 align-items-end';

        // Clear all filters button
        const clearCol = document.createElement('div');
        clearCol.className = 'col-auto';
        const clearButton = document.createElement('button');
        clearButton.type = 'button';
        clearButton.className = 'btn btn-outline-secondary btn-sm';
        clearButton.innerHTML = '<i class="bi bi-x-circle"></i> Clear Filters';
        clearButton.addEventListener('click', () => this.clearAllFilters());
        clearCol.appendChild(clearButton);
        filtersRow.appendChild(clearCol);        // Create filter dropdowns
        this.options.groupingFilters.columns.forEach(filterConfig => {
            const col = document.createElement('div');
            col.className = 'col-auto';

            const label = document.createElement('label');
            label.className = 'form-label small text-muted mb-1';
            label.textContent = filterConfig.label;

            const select = document.createElement('select');
            select.className = 'form-select form-select-sm';
            select.id = `filter-${filterConfig.columnKey}`;
            select.style.minWidth = '150px';

            const defaultOption = document.createElement('option');
            defaultOption.value = '';
            defaultOption.textContent = `All ${filterConfig.label}`;
            select.appendChild(defaultOption);

            select.addEventListener('change', (e) => {
                this.handleFilterChange(filterConfig.columnKey, (e.target as HTMLSelectElement).value);
            });

            col.appendChild(label);
            col.appendChild(select);
            filtersRow.appendChild(col);
        });

        // Add custom active filter checkbox if enabled
        if (this.options.customFilters?.enabled) {
            const activeCol = document.createElement('div');
            activeCol.className = 'col-auto';

            const checkboxContainer = document.createElement('div');
            checkboxContainer.className = 'form-check';

            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.className = 'form-check-input';
            checkbox.id = 'active-filter-checkbox';
            checkbox.checked = this.showActiveOnly;
            checkbox.addEventListener('change', (e) => {
                this.showActiveOnly = (e.target as HTMLInputElement).checked;
                this.currentPage = 1;
                this.applyFilters();
            });

            const checkboxLabel = document.createElement('label');
            checkboxLabel.className = 'form-check-label small text-muted';
            checkboxLabel.htmlFor = 'active-filter-checkbox';
            checkboxLabel.textContent = 'Show Active Only';

            checkboxContainer.appendChild(checkbox);
            checkboxContainer.appendChild(checkboxLabel);
            activeCol.appendChild(checkboxContainer);
            filtersRow.appendChild(activeCol);
        }

        this.filtersContainer.appendChild(filtersRow);
        this.container.appendChild(this.filtersContainer);
    }

    private createSearchInput(): void {
        if (!this.options.searchable) return;

        const searchContainer = document.createElement('div');
        searchContainer.className = 'datatable-search mb-3';

        const searchGroup = document.createElement('div');
        searchGroup.className = 'input-group';

        this.searchInput = document.createElement('input');
        this.searchInput.type = 'text';
        this.searchInput.className = 'form-control';
        this.searchInput.placeholder = 'Search...';
        this.searchInput.addEventListener('input', () => this.handleSearch());

        const searchIcon = document.createElement('span');
        searchIcon.className = 'input-group-text';
        searchIcon.innerHTML = '<i class="bi bi-search"></i>';

        searchGroup.appendChild(this.searchInput);
        searchGroup.appendChild(searchIcon);
        searchContainer.appendChild(searchGroup);
        this.container.appendChild(searchContainer);
    }

    private createTable(): void {
        const tableContainer = document.createElement('div');
        tableContainer.className = 'table-responsive';

        this.table = document.createElement('table');
        this.table.className = this.options.className || 'table table-striped table-hover';

        // Create header
        const thead = document.createElement('thead');
        thead.className = 'table-dark';
        const headerRow = document.createElement('tr');

        this.options.columns.forEach(column => {
            const th = document.createElement('th');
            th.textContent = column.label;
            
            if (column.width) {
                th.style.width = column.width;
            }

            if (column.className) {
                th.className = column.className;
            }

            if (this.options.sortable && column.sortable !== false) {
                th.style.cursor = 'pointer';
                th.addEventListener('click', () => this.handleSort(column.key));
                
                const sortIcon = document.createElement('i');
                sortIcon.className = 'bi bi-arrow-down-up ms-1 text-muted';
                th.appendChild(sortIcon);
            }

            headerRow.appendChild(th);
        });

        thead.appendChild(headerRow);
        this.table.appendChild(thead);

        // Create body
        this.tableBody = document.createElement('tbody');
        this.table.appendChild(this.tableBody);

        tableContainer.appendChild(this.table);
        this.container.appendChild(tableContainer);
    }

    private createPagination(): void {
        this.paginationContainer = document.createElement('div');
        this.paginationContainer.className = 'datatable-pagination d-flex justify-content-between align-items-center mt-3';
        this.container.appendChild(this.paginationContainer);
    }    

    public setData(data: T[]): void {
        this.showDataLoading();
        
        // Use setTimeout to allow the loading animation to show
        setTimeout(() => {
            this.originalData = [...data];
            this.refreshFilterOptions();
            this.applyFilters();
            this.hideDataLoading();
        }, 100);
    }

    private showDataLoading(): void {
        if (this.tableBody) {
            this.tableBody.innerHTML = `
                <tr>
                    <td colspan="${this.options.columns.length}" class="text-center py-4">
                        <div class="datatable-loading-spinner mx-auto mb-2"></div>
                        <div class="datatable-loading-text">Updating data...</div>
                    </td>
                </tr>
            `;
        }
    }

    private hideDataLoading(): void {
        // The loading will be replaced by actual data in renderTable()
    }

    public addRow(rowData: T): void {
        this.originalData.push(rowData);
        this.refreshFilterOptions();
        this.applyFilters();
    }

    public updateRow(index: number, rowData: T): void {
        if (index >= 0 && index < this.originalData.length) {
            this.originalData[index] = rowData;
            this.refreshFilterOptions();
            this.applyFilters();
        }
    }

    public removeRow(index: number): void {
        if (index >= 0 && index < this.originalData.length) {
            this.originalData.splice(index, 1);
            this.refreshFilterOptions();
            this.applyFilters();
        }
    }

    public refresh(): void {
        this.applyFilters();
    }

    private initializeFilterState(): void {
        if (this.options.groupingFilters?.enabled) {
            this.options.groupingFilters.columns.forEach(filterConfig => {
                this.filterState[filterConfig.columnKey] = new Set<string>();
            });
        }
    }    private refreshFilterOptions(): void {
        if (!this.filtersContainer || !this.options.groupingFilters?.enabled) return;

        const selects = this.filtersContainer.querySelectorAll('select');
        
        this.options.groupingFilters.columns.forEach((filterConfig, index) => {
            const select = selects[index] as HTMLSelectElement; // Direct index mapping - clear button is not a select element
            if (!select) return;

            // Get unique values for this column
            const uniqueValues = this.getUniqueColumnValues(filterConfig.columnKey);
            
            // Store current selection
            const currentValue = select.value;
            
            // Clear and rebuild options
            select.innerHTML = '';
            
            const defaultOption = document.createElement('option');
            defaultOption.value = '';
            defaultOption.textContent = `All ${filterConfig.label}`;
            select.appendChild(defaultOption);

            uniqueValues.forEach(value => {
                const option = document.createElement('option');
                option.value = value;
                option.textContent = value;
                select.appendChild(option);
            });

            // Restore selection if still valid
            if (currentValue && uniqueValues.includes(currentValue)) {
                select.value = currentValue;
            }
        });
    }

    private getUniqueColumnValues(columnKey: string): string[] {
        const values = new Set<string>();
        
        this.originalData.forEach(row => {
            const value = this.getColumnValue(row, columnKey);
            if (value !== null && value !== undefined && value !== '') {
                values.add(String(value));
            }
        });

        return Array.from(values).sort();
    }

    private getColumnValue(row: T, columnKey: string): any {
        return (row as any)[columnKey];
    }

    private handleFilterChange(columnKey: string, value: string): void {
        if (!this.filterState[columnKey]) {
            this.filterState[columnKey] = new Set<string>();
        }

        this.filterState[columnKey].clear();
        if (value) {
            this.filterState[columnKey].add(value);
        }

        this.currentPage = 1;
        this.applyFilters();
    }    private clearAllFilters(): void {
        // Clear filter state
        Object.keys(this.filterState).forEach(key => {
            this.filterState[key].clear();
        });

        // Reset UI dropdowns
        if (this.filtersContainer) {
            const selects = this.filtersContainer.querySelectorAll('select');
            selects.forEach(select => {
                (select as HTMLSelectElement).value = '';
            });
            
            // Reset active filter checkbox
            const activeCheckbox = this.filtersContainer.querySelector('#active-filter-checkbox') as HTMLInputElement;
            if (activeCheckbox) {
                this.showActiveOnly = false;
                activeCheckbox.checked = false;
            }
        }

        this.currentPage = 1;
        this.applyFilters();
    }

    private handleSearch(): void {
        if (!this.searchInput) return;
        
        this.searchTerm = this.searchInput.value.toLowerCase();
        this.currentPage = 1;
        this.applyFilters();
    }

    private handleSort(columnKey: string): void {
        if (this.sortColumn === columnKey) {
            this.sortAscending = !this.sortAscending;
        } else {
            this.sortColumn = columnKey;
            this.sortAscending = true;
        }

        this.updateSortIcons();
        this.applyFilters();
    }

    private updateSortIcons(): void {
        if (!this.table) return;

        const headers = this.table.querySelectorAll('thead th');
        headers.forEach((header, index) => {
            const icon = header.querySelector('i');
            if (!icon) return;

            const column = this.options.columns[index];
            if (column.key === this.sortColumn) {
                icon.className = this.sortAscending ? 'bi bi-arrow-up ms-1' : 'bi bi-arrow-down ms-1';
                icon.classList.remove('text-muted');
            } else {
                icon.className = 'bi bi-arrow-down-up ms-1 text-muted';
            }
        });
    }    private applyFilters(): void {
        // Start with original data
        let data = [...this.originalData];

        // Apply column filters
        if (this.options.groupingFilters?.enabled) {
            data = data.filter(row => {
                return this.options.groupingFilters!.columns.every(filterConfig => {
                    const filterValues = this.filterState[filterConfig.columnKey];
                    if (!filterValues || filterValues.size === 0) return true;

                    const cellValue = String(this.getColumnValue(row, filterConfig.columnKey));
                    return filterValues.has(cellValue);
                });
            });
        }

        // Apply custom active filter
        if (this.options.customFilters?.enabled && this.showActiveOnly) {
            const thresholdMinutes = this.options.customFilters.activeThresholdMinutes || 30;
            const thresholdTime = new Date(Date.now() - thresholdMinutes * 60 * 1000);
            
            data = data.filter(row => {
                const lastUpdatedValue = this.getColumnValue(row, 'lastUpdated');
                if (!lastUpdatedValue) return false;
                
                const lastUpdatedDate = new Date(lastUpdatedValue);
                return lastUpdatedDate > thresholdTime;
            });
        }

        // Apply search filter
        if (this.searchTerm) {
            data = data.filter(row => {
                return this.options.columns.some(column => {
                    const value = this.getColumnValue(row, column.key);
                    return String(value).toLowerCase().includes(this.searchTerm);
                });
            });
        }

        // Apply sorting
        if (this.sortColumn) {
            data.sort((a, b) => {
                const aValue = this.getColumnValue(a, this.sortColumn);
                const bValue = this.getColumnValue(b, this.sortColumn);

                let comparison = 0;
                if (aValue < bValue) comparison = -1;
                else if (aValue > bValue) comparison = 1;

                return this.sortAscending ? comparison : -comparison;
            });
        }

        this.filteredData = data;
        this.updatePagination();
        this.renderTable();
    }

    private updatePagination(): void {
        if (!this.paginationContainer) return;

        const totalItems = this.filteredData.length;
        this.totalPages = Math.ceil(totalItems / this.options.perPage!);

        // Ensure current page is valid
        if (this.currentPage > this.totalPages) {
            this.currentPage = Math.max(1, this.totalPages);
        }

        this.paginationContainer.innerHTML = '';

        // Info text
        const info = document.createElement('div');
        info.className = 'pagination-info text-muted';
        const start = (this.currentPage - 1) * this.options.perPage! + 1;
        const end = Math.min(this.currentPage * this.options.perPage!, totalItems);
        info.textContent = `Showing ${start} to ${end} of ${totalItems} entries`;
        this.paginationContainer.appendChild(info);

        // Pagination buttons
        if (this.totalPages > 1) {
            const pagination = document.createElement('nav');
            const ul = document.createElement('ul');
            ul.className = 'pagination pagination-sm mb-0';

            // Previous button
            const prevLi = document.createElement('li');
            prevLi.className = `page-item ${this.currentPage === 1 ? 'disabled' : ''}`;
            const prevLink = document.createElement('a');
            prevLink.className = 'page-link';
            prevLink.href = '#';
            prevLink.textContent = 'Previous';
            prevLink.addEventListener('click', (e) => {
                e.preventDefault();
                if (this.currentPage > 1) {
                    this.currentPage--;
                    this.renderTable();
                    this.updatePagination();
                }
            });
            prevLi.appendChild(prevLink);
            ul.appendChild(prevLi);

            // Page numbers
            const startPage = Math.max(1, this.currentPage - Math.floor(this.options.pageButtonCount! / 2));
            const endPage = Math.min(this.totalPages, startPage + this.options.pageButtonCount! - 1);

            for (let i = startPage; i <= endPage; i++) {
                const li = document.createElement('li');
                li.className = `page-item ${i === this.currentPage ? 'active' : ''}`;
                const link = document.createElement('a');
                link.className = 'page-link';
                link.href = '#';
                link.textContent = String(i);
                link.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.currentPage = i;
                    this.renderTable();
                    this.updatePagination();
                });
                li.appendChild(link);
                ul.appendChild(li);
            }

            // Next button
            const nextLi = document.createElement('li');
            nextLi.className = `page-item ${this.currentPage === this.totalPages ? 'disabled' : ''}`;
            const nextLink = document.createElement('a');
            nextLink.className = 'page-link';
            nextLink.href = '#';
            nextLink.textContent = 'Next';
            nextLink.addEventListener('click', (e) => {
                e.preventDefault();
                if (this.currentPage < this.totalPages) {
                    this.currentPage++;
                    this.renderTable();
                    this.updatePagination();
                }
            });
            nextLi.appendChild(nextLink);
            ul.appendChild(nextLi);

            pagination.appendChild(ul);
            this.paginationContainer.appendChild(pagination);
        }
    }

    private renderTable(): void {
        if (!this.tableBody) return;

        this.tableBody.innerHTML = '';

        if (this.filteredData.length === 0) {
            const row = document.createElement('tr');
            const cell = document.createElement('td');
            cell.colSpan = this.options.columns.length;
            cell.className = 'text-center text-muted py-4';
            cell.textContent = this.options.emptyMessage!;
            row.appendChild(cell);
            this.tableBody.appendChild(row);
            return;
        }

        // Calculate pagination
        const start = (this.currentPage - 1) * this.options.perPage!;
        const end = start + this.options.perPage!;
        const pageData = this.filteredData.slice(start, end);

        // Render rows
        pageData.forEach(rowData => {
            const row = document.createElement('tr');

            this.options.columns.forEach(column => {
                const cell = document.createElement('td');
                const value = this.getColumnValue(rowData, column.key);

                if (column.render) {
                    cell.innerHTML = column.render(value, rowData);
                } else {
                    cell.textContent = String(value || '');
                }

                if (column.className) {
                    cell.className = column.className;
                }

                row.appendChild(cell);            });

            if (this.tableBody) {
                this.tableBody.appendChild(row);
            }
        });
    }

    // Public API methods
    public getFilterState(): FilterState {
        return { ...this.filterState };
    }    public setFilterState(newFilterState: FilterState): void {
        this.filterState = { ...newFilterState };
        
        // Update UI dropdowns to reflect new state
        if (this.filtersContainer && this.options.groupingFilters?.columns) {
            const selects = this.filtersContainer.querySelectorAll('select');
            this.options.groupingFilters.columns.forEach((filterConfig, index) => {
                const select = selects[index] as HTMLSelectElement; // Direct index mapping - clear button is not a select element
                if (select) {
                    const filterValues = this.filterState[filterConfig.columnKey];
                    if (filterValues && filterValues.size === 1) {
                        select.value = Array.from(filterValues)[0];
                    } else {
                        select.value = '';
                    }
                }
            });
        }
        
        this.applyFilters();
    }

    public destroy(): void {
        this.container.innerHTML = '';
    }
}
