/**
 * Konfiguracija aplikacije
 */

export const CONFIG = {
    API_BASE: 'http://localhost:5039/api',
    API_KEY: 'trending123',
    RATE_LIMIT_WINDOW: 60000, // 1 minut
    MAX_POSTS_PER_MINUTE: 5,
    TRENDING_REFRESH_INTERVAL: 5000, // 5 sekundi
    SSE_RETRY_DELAY: 5000 // 5 sekundi
};

export const ROUTES = {
    POSTS_CREATE: `${CONFIG.API_BASE}/posts`,
    POSTS_GET: `${CONFIG.API_BASE}/posts`,
    POSTS_EDIT: `${CONFIG.API_BASE}/posts/last`,
    TRENDING_CURRENT: `${CONFIG.API_BASE}/trending/current`,
    TRENDING_PERIOD: `${CONFIG.API_BASE}/trending/period`,
    EVENTS: `${CONFIG.API_BASE}/events`,
    USERS_ALL: `${CONFIG.API_BASE}/users/all`
};

export const MESSAGES = {
    RATE_LIMIT: '⚠️ Smanjite doživljaj, sačekajte malo',
    NO_POSTS: 'Nema poruka. Budite prvi! 👋',
    NO_TRENDING: 'Nema trending topika u ovom periodu',
    ERROR_LOAD_POSTS: 'Greška pri učitavanju poruka',
    ERROR_LOAD_TRENDING: 'Greška pri učitavanju trending topika',
    ERROR_SERVER: 'Greška pri komunikaciji sa serverom',
    SUCCESS_POST: 'Poruka poslana! 🎉',
    SUCCESS_EDIT: 'Poruka ažurirana! ✏️',
    USERNAME_REQUIRED: 'Molim vas unesite korisničko ime',
    TEXT_REQUIRED: 'Molim vas unesite tekst poruke'
};
