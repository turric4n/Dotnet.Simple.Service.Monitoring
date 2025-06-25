import './darkMode.css';

export class DarkModeComponent {
    private darkModeToggle: HTMLElement | null = null;
    private darkModeIcon: HTMLElement | null = null;
    private darkModeStorageKey = 'monitoring-dark-mode';
    private timelineStorageKey = 'monitoring-timeline-preference';
    private darkModeClass = 'dark-mode';

    constructor() {
        // Initialize UI references
        this.darkModeToggle = document.getElementById('dark-mode-toggle');
        this.darkModeIcon = document.getElementById('dark-mode-icon');
        
        // Set up dark mode event listeners
        this.setupDarkModeEventListeners();
        
        // Apply saved dark mode preference on load
        this.applyDarkModePreference();
        
        // Set up timeline preference event listeners
        this.setupTimelinePreferenceListeners();
        
        // Apply saved timeline preference on load
        this.applyTimelinePreference();
    }

    private setupDarkModeEventListeners(): void {
        if (this.darkModeToggle) {
            this.darkModeToggle.addEventListener('click', () => this.toggleDarkMode());
        }
        
        // Also listen for system preference changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
            if (!this.hasDarkModeStoredPreference()) {
                this.setDarkMode(e.matches);
            }
        });
    }

    private toggleDarkMode(): void {
        const isDarkMode = document.body.classList.contains(this.darkModeClass);
        this.setDarkMode(!isDarkMode);
        this.saveDarkModePreference(!isDarkMode);
    }

    private setDarkMode(enable: boolean): void {
        if (enable) {
            document.body.classList.add(this.darkModeClass);
            if (this.darkModeIcon) {
                this.darkModeIcon.classList.remove('bi-moon');
                this.darkModeIcon.classList.add('bi-sun');
                this.darkModeToggle?.setAttribute('title', 'Switch to light mode');
            }
        } else {
            document.body.classList.remove(this.darkModeClass);
            if (this.darkModeIcon) {
                this.darkModeIcon.classList.remove('bi-sun');
                this.darkModeIcon.classList.add('bi-moon');
                this.darkModeToggle?.setAttribute('title', 'Switch to dark mode');
            }
        }
    }

    private saveDarkModePreference(isDarkMode: boolean): void {
        localStorage.setItem(this.darkModeStorageKey, isDarkMode ? 'true' : 'false');
    }

    private hasDarkModeStoredPreference(): boolean {
        return localStorage.getItem(this.darkModeStorageKey) !== null;
    }

    private applyDarkModePreference(): void {
        // Check localStorage first
        const storedPreference = localStorage.getItem(this.darkModeStorageKey);
        
        if (storedPreference !== null) {
            this.setDarkMode(storedPreference === 'true');
        } else {
            // Fall back to system preference
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            this.setDarkMode(prefersDark);
        }
    }
    
    // Timeline preference methods
    private setupTimelinePreferenceListeners(): void {
        const timelineButtons = ['timeline-1h', 'timeline-24h', 'timeline-7d'];
        
        timelineButtons.forEach(buttonId => {
            const button = document.getElementById(buttonId);
            if (button) {
                button.addEventListener('click', () => {
                    this.saveTimelinePreference(buttonId);
                    this.setActiveTimelineButton(buttonId);
                });
            }
        });
    }
    
    private saveTimelinePreference(buttonId: string): void {
        localStorage.setItem(this.timelineStorageKey, buttonId);
    }
    
    private applyTimelinePreference(): void {
        const storedPreference = localStorage.getItem(this.timelineStorageKey);
        
        if (storedPreference) {
            // Set the active button based on stored preference
            this.setActiveTimelineButton(storedPreference);
            
            // Trigger the timeline data loading
            const button = document.getElementById(storedPreference);
            if (button) {
                button.click();
            }
        } else {
            // Default to 24h if no preference stored
            this.setActiveTimelineButton('timeline-24h');
            const defaultButton = document.getElementById('timeline-24h');
            if (defaultButton) {
                defaultButton.click();
            }
        }
    }
    
    private setActiveTimelineButton(activeButtonId: string): void {
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
    }
}