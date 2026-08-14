// PersonaNest — progressive enhancement only. Every page works without JavaScript;
// server-side validation always runs regardless of what happens here (§12).

(function () {
    'use strict';

    // Settings → Appearance: reflect the selected theme swatch, and keep the custom colour
    // input in sync with the accent preview.
    document.querySelectorAll('.swatch-row input[type="radio"]').forEach(function (radio) {
        var paint = function () {
            document.querySelectorAll('.swatch-row .swatch').forEach(function (s) {
                s.classList.remove('selected');
            });
            var swatch = radio.parentElement && radio.parentElement.querySelector('.swatch');
            if (radio.checked && swatch) {
                swatch.classList.add('selected');
            }
        };
        radio.addEventListener('change', paint);
        paint();
    });

    // Settings nav: mark the section the user clicked as active.
    document.querySelectorAll('.settings-nav a.settings-nav-item').forEach(function (link) {
        link.addEventListener('click', function () {
            document.querySelectorAll('.settings-nav .settings-nav-item').forEach(function (i) {
                i.classList.remove('active');
            });
            link.classList.add('active');
        });
    });
})();
