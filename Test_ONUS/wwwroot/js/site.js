// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Gestione Loading Screen
window.addEventListener('load', function () {
    var loader = document.getElementById('loading-overlay');

    // Aspetta un minimo di 500ms per evitare flash troppo veloci
    setTimeout(function () {
        if (loader) {
            loader.classList.add('loaded');

            // Rimuovi fisicamente l'elemento dopo la transizione CSS
            setTimeout(function () {
                loader.style.display = 'none';
            }, 500);
        }
    }, 500);
});