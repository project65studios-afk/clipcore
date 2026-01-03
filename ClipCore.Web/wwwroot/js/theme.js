window.themeManager = {
    setTheme: function (isLight) {
        if (isLight) {
            document.documentElement.classList.add('light-theme');
            localStorage.setItem('theme', 'light');
        } else {
            document.documentElement.classList.remove('light-theme');
            localStorage.setItem('theme', 'dark');
        }
    },
    getTheme: function () {
        return localStorage.getItem('theme') || 'dark';
    },
    init: function () {
        const theme = this.getTheme();
        this.setTheme(theme === 'light');
    }
};

// Initial sync
themeManager.init();

// Handle Blazor Enhanced Navigation
if (window.Blazor) {
    Blazor.addEventListener('enhancedload', () => {
        themeManager.init();
    });
} else {
    // Falls back if Blazor isn't ready yet
    document.addEventListener('DOMContentLoaded', () => {
        if (window.Blazor) {
            Blazor.addEventListener('enhancedload', () => {
                themeManager.init();
            });
        }
    });
}
