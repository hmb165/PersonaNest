// PersonaNest — Home hero rotating imagery. Progressive enhancement only: the first slide is
// already marked "active" server-side, so without this script the hero just shows one static
// image instead of rotating (§12).
(function () {
    'use strict';

    var slides = document.querySelectorAll('.home-hero-visual-slide');
    if (slides.length < 2) {
        return;
    }

    var index = 0;

    window.setInterval(function () {
        slides[index].classList.remove('active');
        index = (index + 1) % slides.length;
        slides[index].classList.add('active');
    }, 3000);
})();
