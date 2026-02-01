/**
 * Upravljanje globalnim stanjem aplikacije
 */

export let state = {
    currentRole: 'user',
    username: '',
    lastPostTime: null,
    postCount: 0,
    lastPostId: null,
    eventSource: null,
    trendingRefreshInterval: null
};

// Ažurira stanje
export function updateState(updates) {
    state = { ...state, ...updates };
}

// Resetuje rate limiter brojač
export function resetRateLimit() {
    state.lastPostTime = null;
    state.postCount = 0;
}
