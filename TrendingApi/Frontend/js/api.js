/**
 * API komunikacija
 */

import { ROUTES, CONFIG } from './config.js';

// Slanje nove poruke
export async function createPost(username, text) {

    const response = await fetch(ROUTES.POSTS_CREATE, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': CONFIG.API_KEY
        },
        body: JSON.stringify({ username, text })
    });

    if (!response.ok) {
        const error = await response.json();
        return {
            success: false,
            status: response.status,
            message: error.message || 'Greška pri slanju poruke'
        };
    }

    return {
        success: true,
        data: await response.json()
    };
}

// Ažuriranje poslednje poruke
export async function updateLastPost(username, text) {
    const response = await fetch(ROUTES.POSTS_EDIT, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': CONFIG.API_KEY
        },
        body: JSON.stringify({ username, text })
    });

    if (!response.ok) {
        const error = await response.json();
        return {
            success: false,
            message: error.message || 'Greška pri ažuriranju poruke'
        };
    }

    return {
        success: true,
        data: await response.json()
    };
}

// Preuzimanje poruka
export async function getPosts(count = 20) {
    try {
        const response = await fetch(`${ROUTES.POSTS_GET}?count=${count}`);
        if (!response.ok) throw new Error('Failed to load posts');

        return await response.json();
    } catch (error) {
        console.error('Load posts error:', error);
        throw error;
    }
}

// Preuzimanje trending po danima
export async function getTrendingPeriod(days = 7, top = 20, username = '') {
    try {
        const response = await fetch(`${ROUTES.TRENDING_PERIOD}?days=${days}&top=${top}&username=${username}`);
        if (!response.ok) throw new Error('Failed to load trending');

        return await response.json();
    } catch (error) {
        console.error('Load trending error:', error);
        throw error;
    }
}

// Preuzimanje svih korisnika
export async function getAllUsers() {
    try {
        const response = await fetch(ROUTES.USERS_ALL);
        if (!response.ok) throw new Error('Failed to load users');

        const result = await response.json();
        return result.data || [];
    } catch (error) {
        console.error('Load users error:', error);
        return [];
    }
}

// Brisanje poruke
export async function deletePost(postId, username) {
    const response = await fetch(`${ROUTES.POSTS_GET}/${postId}?username=${username}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': CONFIG.API_KEY
        }
    });

    if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        return {
            success: false,
            status: response.status,
            message: error.message || 'Greška pri brisanju poruke'
        };
    }

    return {
        success: true,
        data: await response.json()
    };
}
