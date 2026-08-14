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

    // Create/Edit Entry: the star widget is a visual enhancement over the real, always-present
    // number input (§7) - one star = 2.0 points, and each star's left/right half is a click zone,
    // giving whole-number values (always valid 0.5-multiples). Typing a finer value like 8.5 into
    // the number input still works; the stars just repaint to match.
    document.querySelectorAll('[data-star-rating]').forEach(function (widget) {
        var container = widget.parentElement;
        var input = container && container.querySelector('[data-rating-input]');
        var readout = widget.querySelector('[data-rating-readout]');
        var clearLink = widget.querySelector('[data-rating-clear]');
        var stars = Array.prototype.slice.call(widget.querySelectorAll('.star'));
        if (!input) {
            return;
        }

        function paint(value) {
            stars.forEach(function (star, index) {
                var starMax = (index + 1) * 2;
                star.classList.remove('filled', 'half');
                if (value === null) {
                    return;
                }
                if (value >= starMax) {
                    star.classList.add('filled');
                } else if (value >= starMax - 1) {
                    star.classList.add('half');
                }
            });
            readout.textContent = value === null ? 'Unrated' : value.toFixed(1) + ' / 10';
        }

        function setValue(value) {
            input.value = value === null ? '' : value.toFixed(1);
            paint(value);
        }

        stars.forEach(function (star, index) {
            star.addEventListener('click', function (event) {
                var rect = star.getBoundingClientRect();
                var clickedLeftHalf = (event.clientX - rect.left) < rect.width / 2;
                var starNumber = index + 1;
                setValue(clickedLeftHalf ? starNumber * 2 - 1 : starNumber * 2);
            });
        });

        if (clearLink) {
            clearLink.addEventListener('click', function () {
                setValue(null);
            });
        }

        input.addEventListener('input', function () {
            var parsed = parseFloat(input.value);
            paint(isNaN(parsed) ? null : parsed);
        });

        var initial = parseFloat(input.value);
        paint(isNaN(initial) ? null : initial);
    });

    // Create/Edit Entry: tags are checkboxes under the hood (so the form works without JS) but
    // rendered as pills; toggle the "active" look on click.
    document.querySelectorAll('[data-tag-toggle]').forEach(function (checkbox) {
        var pill = checkbox.parentElement && checkbox.parentElement.querySelector('.tag');
        if (!pill) {
            return;
        }
        checkbox.addEventListener('change', function () {
            pill.classList.toggle('active', checkbox.checked);
        });
    });

    // Create/Edit Entry: live-preview the personal cover URL as it's typed.
    document.querySelectorAll('[data-cover-input]').forEach(function (input) {
        var preview = document.getElementById('cover-preview');
        if (!preview) {
            return;
        }
        input.addEventListener('input', function () {
            var url = input.value.trim();
            var img = preview.querySelector('[data-cover-image]');
            if (!url) {
                if (img) {
                    img.remove();
                }
                return;
            }
            if (!img) {
                img = document.createElement('img');
                img.setAttribute('data-cover-image', '');
                img.style.width = '100%';
                img.style.height = '100%';
                img.style.objectFit = 'cover';
                img.alt = 'Personal cover';
                preview.appendChild(img);
            }
            img.src = url;
        });
    });
})();
