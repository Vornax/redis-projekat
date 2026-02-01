/**
 * Utility funkcije
 */


// HTML escaping za zaštitu od XSS
export function escapeHtml(unsafe) {
    return unsafe
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}


// Formatiranje vremena u relativnom formatu
export function formatTime(unixTimestamp) {
    const date = new Date(unixTimestamp * 1000);
    const now = new Date();
    const diff = (now - date) / 1000; // razlika u sekundama

    if (diff < 60) return 'Upravo sada';
    if (diff < 3600) return `Pre ${Math.floor(diff / 60)}m`;
    if (diff < 86400) return `Pre ${Math.floor(diff / 3600)}h`;
    if (diff < 604800) return `Pre ${Math.floor(diff / 86400)}d`;

    return date.toLocaleDateString('sr-RS');
}

// Ekstrakcija hashtag-a iz teksta
export function extractHashtags(text) {
    const regex = /#(\w+)/g;
    const tags = [];
    let match;

    while ((match = regex.exec(text)) !== null) {
        const tag = '#' + match[1].toLowerCase();
        if (!tags.includes(tag)) {
            tags.push(tag);
        }
    }

    return tags;
}

// Prikaz notifikacija (console fallback)
export function showNotification(message, type = 'info') {
    console.log(`[${type.toUpperCase()}] ${message}`);
    // Opciono: može se dodati toast notifikacija
}

// Debounce funkcija
export function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}
