/**
 * UI logika - renderovanje i ažuriranje interfejsa
 */

import {
    postText, charCount, postsFeed, trendingList, trendingTitle,
    editBtn, rateLimitMsg, postBtn, periodType, periodValue
} from './dom.js';
import {
    MESSAGES, CONFIG
} from './config.js';
import {
    getPosts, getTrendingCurrent, getTrendingPeriod
} from './api.js';
import {
    extractHashtags, formatTime, escapeHtml, showNotification
} from './utils.js';
import { state, updateState } from './state.js';

// Ažuriranje brojača karaktera
export function updateCharCount() {
    const count = postText.value.length;
    charCount.textContent = `${count}/500`;
}

// Učitavanje i renderovanje poruka
export async function loadPosts() {
    try {
        const posts = await getPosts(20);
        renderPosts(posts);
    } catch (error) {
        postsFeed.innerHTML = `<p class="loading">${MESSAGES.ERROR_LOAD_POSTS}</p>`;
    }
}

// Renderovanje liste poruka
function renderPosts(posts) {
    if (posts.length === 0) {
        postsFeed.innerHTML = `<p class="loading">${MESSAGES.NO_POSTS}</p>`;
        return;
    }

    postsFeed.innerHTML = posts.map(post => {
        const time = formatTime(post.time);
        const hashtags = extractHashtags(post.text);
        const hashtagsHtml = hashtags.length > 0
            ? `<div class="post-hashtags">${hashtags.map(tag => `<span class="hashtag">${tag}</span>`).join('')}</div>`
            : '';

        const isUserPost = post.username === state.username && state.username !== '';
        const editIndicator = isUserPost && post.id === state.lastPostId ? ' (ažurirana)' : '';
        
        const deleteButtonHtml = isUserPost && post.text !== '[Poruka je obrisana]'
            ? `<button class="post-delete-btn" data-post-id="${post.id}" data-username="${post.username}" title="Obriši poruku">🗑️</button>`
            : '';

        return `
            <div class="post-item">
                <div class="post-header">
                    <span class="post-username">${escapeHtml(post.username)}${editIndicator}</span>
                    <span class="post-time">${time}</span>
                </div>
                <p class="post-text">${escapeHtml(post.text)}</p>
                <div class="post-footer">
                    ${hashtagsHtml}
                    ${deleteButtonHtml}
                </div>
            </div>
        `;
    }).join('');

    // Dodaj event listenere za delete dugmad
    document.querySelectorAll('.post-delete-btn').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            const postId = e.target.dataset.postId;
            const username = e.target.dataset.username;
            if (confirm('Da li ste sigurni da želite obrisati ovu poruku?')) {
                const { handleDeletePost } = await import('./handlers.js');
                handleDeletePost(postId, username);
            }
        });
    });
}

// Učitavanje trending topika - inicijalno (sa loading indikatorom)
export async function loadTrending() {
    const type = periodType.value;
    const value = parseInt(periodValue.value) || 1;

    trendingTitle.textContent = `Trending topici (zadnjih ${value} ${type === 'hours' ? 'sati' : 'dana'})`;
    trendingList.innerHTML = '<p class="loading">Učitavanje trending topika...</p>';

    try {
        let trending;
        if (type === 'hours') {
            trending = await getTrendingCurrent(value, 20);
        } else {
            trending = await getTrendingPeriod(value, 20, state.username);
        }
        renderTrending(trending);
    } catch (error) {
        trendingList.innerHTML = `<p class="loading">${MESSAGES.ERROR_LOAD_TRENDING}</p>`;
    }
}

// Osvežavanje trending topika - tiho (bez loading indika
export async function refreshTrendingQuietly() {
    try {
        const type = periodType.value;
        const value = parseInt(periodValue.value) || 1;

        let trending;
        if (type === 'hours') {
            trending = await getTrendingCurrent(value, 20);
        } else {
            trending = await getTrendingPeriod(value, 20, state.username);
        }
        
        // Samo ažurira podatke bez flashing-a
        renderTrendingQuietly(trending);
    } catch (error) {
        // Tiho ne prikazuj grešku, nastavi sa starim podacima
        console.error('Quiet trending refresh error:', error);
    }
}

// Renderovanje trending topika
function renderTrending(trendingItems) {
    if (trendingItems.length === 0) {
        trendingList.innerHTML = `<p class="loading">${MESSAGES.NO_TRENDING}</p>`;
        return;
    }

    trendingList.innerHTML = trendingItems.map((item, index) => `
        <div class="trending-item">
            <div class="trending-rank">#${index + 1}</div>
            <div class="trending-hashtag">${escapeHtml(item.hashtag)}</div>
            <div class="trending-score">
                <span class="trending-label">Engagement:</span>
                <span class="trending-value">${item.score}</span>
            </div>
        </div>
    `).join('');
}

// Tiho osvežavanje trending topika - ažurira samo ako se promenilo
function renderTrendingQuietly(newItems) {
    if (newItems.length === 0) {
        // Ako nema stavki, prikaži praznu listu
        if (trendingList.innerHTML !== `<p class="loading">${MESSAGES.NO_TRENDING}</p>`) {
            trendingList.innerHTML = `<p class="loading">${MESSAGES.NO_TRENDING}</p>`;
        }
        return;
    }

    // Kreiraj novi HTML
    const newHTML = newItems.map((item, index) => `
        <div class="trending-item">
            <div class="trending-rank">#${index + 1}</div>
            <div class="trending-hashtag">${escapeHtml(item.hashtag)}</div>
            <div class="trending-score">
                <span class="trending-label">Engagement:</span>
                <span class="trending-value">${item.score}</span>
            </div>
        </div>
    `).join('');

    // Ažurira samo ako se sadržaj promenio
    if (trendingList.innerHTML !== newHTML) {
        trendingList.innerHTML = newHTML;
    }
}

// Prikaz rate limit poruke
export function showRateLimitMessage() {
    rateLimitMsg.style.display = 'inline';
    rateLimitMsg.textContent = MESSAGES.RATE_LIMIT;
    setTimeout(() => {
        rateLimitMsg.style.display = 'none';
    }, 3000);
}


// Disablovanje/Enableovanje dugmadi tokom slanja
export function setPostButtonState(disabled) {
    postBtn.disabled = disabled;
}

// Čišćenje forme nakon slanja
export function clearPostForm() {
    postText.value = '';
    charCount.textContent = '0/500';
    editBtn.style.display = 'inline-block';
}
