window.setFullscreenElement = async (playerId, containerId) => {
    // Wait for Mux Player to likely be ready
    if (customElements.get('mux-player')) {
        await customElements.whenDefined('mux-player');
    }

    const player = document.querySelector(playerId);
    const container = document.getElementById(containerId);

    if (player && container) {
        // 1. Set the property (requires actual Element reference)
        player.fullscreenElement = container;

        // 2. Set the attribute (requires ID string)
        player.setAttribute('fullscreen-element', containerId);

    } else {
        console.warn(`[Project65] Fullscreen setup failed. Player: ${!!player}, Container: ${!!container}`);
    }
};
