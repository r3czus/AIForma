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

    function decodeKey(value) {
        const padding = '='.repeat((4 - value.length % 4) % 4);
        const raw = atob((value + padding).replace(/-/g, '+').replace(/_/g, '/'));
        return Uint8Array.from([...raw].map(char => char.charCodeAt(0)));
    }

    async function enablePushNotifications(publicKey) {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) return null;
        if (await Notification.requestPermission() !== 'granted') return null;
        const registration = await navigator.serviceWorker.ready;
        let subscription = await registration.pushManager.getSubscription();
        if (!subscription) subscription = await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: decodeKey(publicKey) });
        const json = subscription.toJSON();
        return { endpoint: json.endpoint, p256Dh: json.keys.p256dh, auth: json.keys.auth };
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

    return { applyTheme, requestNotifications, enablePushNotifications, scheduleMealReminders };
})();
