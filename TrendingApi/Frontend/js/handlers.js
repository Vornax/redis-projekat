/**
 * Event handleri
 */

import { state, updateState } from './state.js';
import { loadPosts, loadTrending, refreshTrendingQuietly, clearPostForm, showRateLimitMessage, setPostButtonState, updateCharCount } from './ui.js';
import { startSSE, stopSSE } from './sse.js';
import { createPost, updateLastPost, getAllUsers, deletePost } from './api.js';
import { showNotification } from './utils.js';
import { 
    roleSelect, usernameInput, postText, postBtn, editBtn, filterBtn, periodType, periodValue
} from './dom.js';
import { MESSAGES, CONFIG } from './config.js';


// Inicijalizacija event listenera
export function initializeEventListeners() {
    roleSelect.addEventListener('change', handleRoleChange);
    postBtn.addEventListener('click', handleCreatePost);
    editBtn.addEventListener('click', handleEditPost);
    filterBtn.addEventListener('click', loadTrending);
    postText.addEventListener('input', updateCharCount);
    postText.addEventListener('input', updateEditButtonVisibility);
    usernameInput.addEventListener('input', updateEditButtonVisibility);
    usernameInput.addEventListener('change', handleUsernameChange);
    periodType.addEventListener('change', handlePeriodTypeChange);
    periodValue.addEventListener('input', validatePeriodValue); // Validacija unosa
    
    // Postavi inicijalne max vrednosti za period value
    periodValue.max = 24; 
    
    // Učitaj korisnike u select
    loadUsersIntoSelect();
}

// Globalna promenljiva za čuvanje liste korisnika
let allUsers = [];


// Učitaj sve korisnike iz baze i popuni select
async function loadUsersIntoSelect() {
    try {
        allUsers = await getAllUsers();
        
        // Čisti sve opcije osim first
        while (roleSelect.options.length > 0) {
            roleSelect.remove(0);
        }
        
        // Dodaj prvi option kao placeholder
        const placeholder = document.createElement('option');
        placeholder.value = '';
        placeholder.text = 'Odaberite korisnika...';
        placeholder.disabled = true;
        placeholder.selected = true;
        roleSelect.appendChild(placeholder);
        
        // Dodaj sve korisnike
        allUsers.forEach(user => {
            const option = document.createElement('option');
            option.value = user.username;
            option.text = `${user.username} (${user.role})`;
            roleSelect.appendChild(option);
        });
    } catch (error) {
        console.error('Greška pri učitavanju korisnika:', error);
    }
}

// Promena korisnika, učitaj korisničko ime i ulogu
function handleRoleChange(e) {
    const selectedUsername = e.target.value;
    
    if (!selectedUsername) {
        return; // Korisnik je kliknuo na placeholder
    }
    
    // Pronađi korisnika u listi
    const selectedUser = allUsers.find(u => u.username === selectedUsername);
    
    if (selectedUser) {
        // Postavi korisničko ime i ulogu
        usernameInput.value = selectedUser.username;
        localStorage.setItem('username', selectedUser.username);
        updateState({ 
            username: selectedUser.username,
            currentRole: selectedUser.role
        });
        
        // Ažuriranje vidljivosti view-a
        const userView = document.getElementById('userView');
        const adminView = document.getElementById('adminView');
        userView.classList.toggle('active', selectedUser.role === 'user');
        adminView.classList.toggle('active', selectedUser.role === 'admin');

        if (selectedUser.role === 'user') {
            loadPosts();
            startSSE();
            // Zaustavi trending refresh
            if (state.trendingRefreshInterval) {
                clearInterval(state.trendingRefreshInterval);
                updateState({ trendingRefreshInterval: null });
            }
        } else {
            stopSSE();
            loadTrending();
            // Pokreni auto-refresh trending ali tiho bez osvežavanja celog prikaza
            if (!state.trendingRefreshInterval) {
                const interval = setInterval(refreshTrendingQuietly, CONFIG.TRENDING_REFRESH_INTERVAL);
                updateState({ trendingRefreshInterval: interval });
            }
        }
    }
}

// Sačuvavanje korisničkog imena u localStorage
function handleUsernameChange(e) {
    localStorage.setItem('username', e.target.value);
    updateState({ username: e.target.value });
}

// Promena tipa perioda, ažurira defaultnu vrednost
function handlePeriodTypeChange(e) {
    if (e.target.value === 'hours') {
        periodValue.value = 24;
        periodValue.max = 24; // max 24 sata
    } else {
        periodValue.value = 7;
        periodValue.max = 30;
    }
}

// Validacija unosa vrednosti perioda - sprečava ručni unos preko limi
function validatePeriodValue(e) {
    const maxValue = parseInt(periodValue.max);
    const currentValue = parseInt(e.target.value) || 0;
    
    // Ako je uneta vrednost veća od max-a, postavi je na max
    if (currentValue > maxValue) {
        periodValue.value = maxValue;
    }
    
    // Sprečava unos 0 ili negativnih vrednosti
    if (currentValue < 1) {
        periodValue.value = 1;
    }
}

// Ažuriranje vidljivosti edit dugmadi - prikazuje ga samo ako ima poslane poruke
function updateEditButtonVisibility() {
    if (state.lastPostId && postText.value.trim()) {
        editBtn.style.display = 'inline-block';
    } else {
        editBtn.style.display = 'none';
    }
}

// Slanje nove poruke
async function handleCreatePost() {
    const username = usernameInput.value.trim();
    const text = postText.value.trim();

    // Validacija
    if (!username) {
        showNotification(MESSAGES.USERNAME_REQUIRED, 'error');
        return;
    }
    if (!text) {
        showNotification(MESSAGES.TEXT_REQUIRED, 'error');
        return;
    }

    // Provera rate limitera
    if (!checkRateLimit()) {
        showRateLimitMessage();
        return;
    }

    setPostButtonState(true);

    try {
        const result = await createPost(username, text);
        
        if (!result.success) {
            if (result.status === 429) {
                showRateLimitMessage();
            } else {
                showNotification(result.message, 'error');
            }
            return;
        }

        updateState({ 
            lastPostId: result.data.id,
            username: username
        });

        clearPostForm();
        showNotification(MESSAGES.SUCCESS_POST, 'success');
        await loadPosts();
    } catch (error) {
        showNotification(MESSAGES.ERROR_SERVER, 'error');
        console.error('Create post error:', error);
    } finally {
        setPostButtonState(false);
    }
}

// Ažuriranje poslednje poruke
async function handleEditPost() {
    const username = usernameInput.value.trim();
    const text = postText.value.trim();

    if (!username) {
        showNotification(MESSAGES.USERNAME_REQUIRED, 'error');
        return;
    }
    if (!text) {
        showNotification(MESSAGES.TEXT_REQUIRED, 'error');
        return;
    }

    editBtn.disabled = true;

    try {
        const result = await updateLastPost(username, text);

        if (!result.success) {
            showNotification(result.message, 'error');
            return;
        }

        updateState({ lastPostId: result.data.postId });
        clearPostForm();
        showNotification(MESSAGES.SUCCESS_EDIT, 'success');
        await loadPosts();
    } catch (error) {
        showNotification(MESSAGES.ERROR_SERVER, 'error');
        console.error('Edit post error:', error);
    } finally {
        editBtn.disabled = false;
    }
}

// Rate limiter provera
function checkRateLimit() {
    const now = Date.now();

    if (!state.lastPostTime) {
        updateState({ lastPostTime: now, postCount: 1 });
        return true;
    }

    // Ako je proslo vise od minuta, resetuj brojac
    if (now - state.lastPostTime > CONFIG.RATE_LIMIT_WINDOW) {
        updateState({ lastPostTime: now, postCount: 1 });
        return true;
    }

    // Ako je unutar minuta, proveri limit
    if (state.postCount < CONFIG.MAX_POSTS_PER_MINUTE) {
        updateState({ postCount: state.postCount + 1 });
        return true;
    }

    return false;
}

// Brisanje poruke
export async function handleDeletePost(postId, username) {
    try {
        const result = await deletePost(postId, username);

        if (!result.success) {
            showNotification(result.message, 'error');
            return;
        }

        showNotification('Poruka uspešno obrisana', 'success');
        await loadPosts();
    } catch (error) {
        showNotification(MESSAGES.ERROR_SERVER, 'error');
        console.error('Delete post error:', error);
    }
}
