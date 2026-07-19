// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });
self.addEventListener('push', event => {
    const data = event.data ? event.data.json() : {};
    event.waitUntil(self.registration.showNotification(data.title || 'FormaAI', {
        body: data.body || 'Masz nowe przypomnienie.',
        icon: '/icon-192.png', badge: '/icon-192.png', tag: data.tag || 'formaai-reminder',
        data: { url: data.url || '/' }
    }));
});
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windows => {
        const openWindow = windows.find(window => 'focus' in window);
        return openWindow ? openWindow.focus() : clients.openWindow(event.notification.data?.url || '/');
    }));
});
