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

// Initial sync
themeManager.init();
