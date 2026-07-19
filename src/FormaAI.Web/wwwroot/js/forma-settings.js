window.formaSettings = (() => {
    const reminderTimers = [];

    function applyTheme(theme) {
        localStorage.setItem('formaai-theme', theme);
        if (theme === 'light' || theme === 'dark') document.documentElement.dataset.theme = theme;
        else delete document.documentElement.dataset.theme;
    }

    async function requestNotifications() {
        if (!('Notification' in window)) return 'unsupported';
        const permission = await Notification.requestPermission();
        if (permission === 'granted') {
            const registration = await navigator.serviceWorker.ready;
            await registration.showNotification('FormaAI jest gotowa', {
                body: 'Przypomnienia o posiłkach mogą już pojawiać się na tym urządzeniu.',
                icon: '/icon-192.png',
                badge: '/icon-192.png',
                tag: 'formaai-test'
            });
        }
        return permission;
    }

    async function scheduleMealReminders(meals, minutesBefore) {
        reminderTimers.splice(0).forEach(clearTimeout);
        if (Notification.permission !== 'granted') return;

        for (const meal of meals.filter(x => x.enabled)) {
            const [hours, minutes] = String(meal.time).split(':').map(Number);
            const alertAt = new Date();
            alertAt.setHours(hours, minutes - minutesBefore, 0, 0);
            const delay = alertAt.getTime() - Date.now();
            if (delay <= 0 || delay > 86400000) continue;
            reminderTimers.push(setTimeout(async () => {
                const registration = await navigator.serviceWorker.ready;
                await registration.showNotification(`Pora na ${meal.name.toLowerCase()}`, {
                    body: 'Otwórz FormaAI i zapisz posiłek w kilku krokach.',
                    icon: '/icon-192.png',
                    badge: '/icon-192.png',
                    tag: `formaai-meal-${meal.name}`
                });
            }, delay));
        }
    }

    return { applyTheme, requestNotifications, scheduleMealReminders };
})();
