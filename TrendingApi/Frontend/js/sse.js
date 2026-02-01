/**
 * Server-Sent Events za real-time ažuriranje
 */

import { ROUTES, CONFIG } from './config.js';
import { state, updateState } from './state.js';
import { loadPosts } from './ui.js';


// Pokretanje SSE konekcije
export function startSSE() {
    if (state.eventSource) return;

    state.eventSource = new EventSource(ROUTES.EVENTS);
    
    state.eventSource.onmessage = (event) => {
        console.log('New event:', event.data);
        loadPosts(); // Reload posts na svaki novi event
    };
    
    state.eventSource.onerror = (error) => {
        console.error('SSE error:', error);
        stopSSE();
        // Retry nakon XXX sekundi
        setTimeout(() => {
            if (state.currentRole === 'user') {
                startSSE();
            }
        }, CONFIG.SSE_RETRY_DELAY);
    };
}

// Zaustavljanje SSE konekcije
export function stopSSE() {
    if (state.eventSource) {
        state.eventSource.close();
        updateState({ eventSource: null });
    }
}
