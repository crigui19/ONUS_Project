// Questo evento scatta quando il server C# manda una notifica
self.addEventListener('push', function (event) {
    let data = {};

    // Controlliamo se ci sono dati nel messaggio
    if (event.data) {
        data = event.data.json();
    }

    // Configura l'aspetto della notifica sul telefono
    const title = data.titolo || "ONUS Athletes";
    const options = {
        body: data.messaggio || "Hai una nuova notifica dal tuo allenatore.",
        icon: '/Img/icon-192.png', // L'icona della tua app
        badge: '/Img/icon-192.png', // L'iconcina piccola nella barra di stato
        vibrate: [200, 100, 200, 100, 200], // Vibrazione stile messaggio
        data: {
            url: data.url || '/' // Dove va se clicca la notifica
        }
    };

    // Mostra la notifica sullo schermo!
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

// Cosa succede quando l'atleta tocca la notifica col dito
self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});

// ==========================================
// RICEZIONE DELLA NOTIFICA PUSH
// ==========================================
self.addEventListener('push', function (event) {
    console.log('[Service Worker] Push Ricevuto!');

    // Testo di default se il server non manda nulla
    let testoNotifica = 'Ricordati di inserire i dati della sessione di oggi!';

    // Se il server C# ci ha mandato un messaggio personalizzato, usiamo quello
    if (event.data) {
        testoNotifica = event.data.text();
    }

    const title = 'ONUS Athletes 🏀';
    const options = {
        body: testoNotifica,
        icon: '/Img/icon-192.png', // L'icona che compare a fianco del messaggio
        badge: '/Img/icon-192.png', // L'icona piccola in alto sulla barra di stato (Android)
        vibrate: [200, 100, 200, 100, 200, 100, 200], // Fa vibrare il telefono!
        requireInteraction: true // Su PC, la notifica non scompare finché non la chiudi
    };

    // Mostra la notifica di sistema sul telefono
    event.waitUntil(self.registration.showNotification(title, options));
});

// ==========================================
// AZIONE QUANDO L'ATLETA CLICCA SULLA NOTIFICA
// ==========================================
self.addEventListener('notificationclick', function (event) {
    console.log('[Service Worker] L\'utente ha cliccato sulla notifica.');

    // Chiude la tendina della notifica
    event.notification.close();

    // Apre l'app direttamente sulla Dashboard
    event.waitUntil(
        clients.openWindow('https://onusathletes.it/Dashboard')
    );
});