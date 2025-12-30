window.VideoObserver = (function () {
    let observer;
    let currentlyPlaying = null;

    function init() {
        // Options: threshold 0.7 means 70% of the item must be visible
        // rootMargin: negative margin to focus on center of screen
        const options = {
            root: null,
            rootMargin: '0px',
            threshold: 0.7
        };

        observer = new IntersectionObserver(handleIntersect, options);

        // Desktop Hover Logic (Event Delegation with Capture)
        document.addEventListener('mouseenter', (e) => {
            if (e.target.classList && e.target.classList.contains('video-card')) {
                // Ignore if we are in a touch environment where we use intersection observer
                if (window.matchMedia('(hover: none)').matches) return;
                playPreview(e.target, true); // Force play
            }
        }, true);

        document.addEventListener('mouseleave', (e) => {
            if (e.target.classList && e.target.classList.contains('video-card')) {
                if (window.matchMedia('(hover: none)').matches) return;
                stopPreview(e.target);
            }
        }, true);
    }

    function handleIntersect(entries) {
        entries.forEach(entry => {
            const card = entry.target;

            if (entry.isIntersecting) {
                // Trigger only if touch
                if (window.matchMedia('(hover: none)').matches) {
                    playPreview(card);
                }
            } else {
                stopPreview(card);
            }
        });
    }

    function playPreview(card, isDesktop = false) {
        // Double check touch, unless forced by desktop hover
        const isTouch = window.matchMedia('(hover: none)').matches;
        if (!isTouch && !isDesktop) return;

        if (isTouch) card.classList.add('mobile-playing');

        const player = card.querySelector('mux-player');
        if (player) {
            // Muted required for autoplay usually
            player.muted = true;
            player.play().catch(e => {
                // Autoplay blocked
            });
        }
    }

    function stopPreview(card) {
        card.classList.remove('mobile-playing');
        const player = card.querySelector('mux-player');
        if (player) {
            player.pause();
        }
    }

    return {
        observe: function (element) {
            if (!observer) init();
            if (element) observer.observe(element);
        },
        unobserve: function (element) {
            if (observer && element) observer.unobserve(element);
        }
    };
})();
