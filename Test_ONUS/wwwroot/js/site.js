// Funzione di utilità richiesta da Google/Apple per decodificare la tua VAPID Key
function urlB64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

// La funzione principale che chiameremo
async function chiediPermessoNotifiche(tuaPublicKey) {
    // Controlliamo che il telefono supporti le notifiche push
    if ('serviceWorker' in navigator && 'PushManager' in window) {
        try {
            // 1. Registra il lavoratore invisibile
            const swReg = await navigator.serviceWorker.register('/service-worker.js');

            // 2. Fai comparire il popup: "Consenti Notifiche?"
            const permission = await Notification.requestPermission();

            if (permission !== 'granted') {
                console.log('Permesso negato dall\'atleta.');
                return; // Se dice no, ci fermiamo qui
            }

            // 3. Ottieni l'indirizzo segreto del telefono per spedirgli le notifiche
            const subscription = await swReg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlB64ToUint8Array(tuaPublicKey)
            });

            // 4. Manda questo indirizzo al nostro backend C# (PushController)
            await fetch('/api/push/subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(subscription)
            });

            console.log('Ottimo! Dispositivo registrato per le notifiche.');

        } catch (error) {
            console.error('Errore durante l\'iscrizione alle notifiche:', error);
        }
    } else {
        console.warn('Questo browser non supporta le notifiche Push.');
    }
}