/**
 * Glavna inicijalizaciona datoteka
 * Učitava sve module i pokreće aplikaciju
 */

import { initializeEventListeners } from './handlers.js';
import { loadPosts, loadTrending } from './ui.js';
import { startSSE, stopSSE } from './sse.js';
import { state, updateState } from './state.js';
import { usernameInput } from './dom.js';
import { CONFIG } from './config.js';


// Inicijalizacija aplikacije
document.addEventListener('DOMContentLoaded', () => {
    // Inicijalizuj event listenere
    initializeEventListeners();
    
    // Učitaj početni sadržaj na osnovu uloge
    if (state.currentRole === 'user') {
        loadPosts();
        startSSE();
    } else {
        loadTrending();
    }
});

// Cleanup pri zatvaranju stranice
window.addEventListener('beforeunload', () => {
    stopSSE();
    if (state.trendingRefreshInterval) {
        clearInterval(state.trendingRefreshInterval);
    }
});

// Inicijalizuj module, ovo omogućava pristup iz konzole za debugging
window.TrendingAPI = {
    state,
    updateState,
    loadPosts,
    loadTrending,
    startSSE,
    stopSSE
};
