// PersonaNest — Add Media / external-API auto-fill (bonus: Consume an External API). Progressive
// enhancement only: the panel starts hidden and the manual fields below always work without it
// (§12) - this just saves typing by filling them from a search result at the matching category's
// external source (TMDB, RAWG, Jikan, Google Books, MusicBrainz).
(function () {
    'use strict';

    var categorySelect = document.getElementById('mediaCategorySelect');
    var panel = document.getElementById('externalSearchPanel');
    if (!categorySelect || !panel) {
        return;
    }

    // Seeded Category ids (CategoryConfiguration.cs) -> the provider searched for that category.
    var SOURCE_LABEL_BY_CATEGORY = {
        '1': 'RAWG', '2': 'TMDB', '3': 'TMDB', '4': 'Jikan (MyAnimeList)',
        '5': 'Jikan (MyAnimeList)', '6': 'Google Books', '7': 'MusicBrainz'
    };

    var queryInput = document.getElementById('externalQuery');
    var searchBtn = document.getElementById('externalSearchBtn');
    var resultsEl = document.getElementById('externalResults');
    var labelEl = document.getElementById('externalSearchLabel');

    var titleInput = document.getElementById('Form_Title');
    var yearInput = document.getElementById('Form_ReleaseYear');
    var descriptionInput = document.getElementById('Form_Description');
    var coverInput = document.getElementById('Form_OfficialCoverUrl');

    function currentSourceLabel() {
        return SOURCE_LABEL_BY_CATEGORY[categorySelect.value] || null;
    }

    function syncPanelVisibility() {
        var label = currentSourceLabel();
        panel.style.display = label ? '' : 'none';
        if (label && labelEl) {
            labelEl.textContent = 'Search ' + label;
        }
        resultsEl.innerHTML = '';
    }

    categorySelect.addEventListener('change', syncPanelVisibility);
    syncPanelVisibility();

    function renderResults(results) {
        resultsEl.innerHTML = '';

        if (results.length === 0) {
            var empty = document.createElement('div');
            empty.style.cssText = 'font-size:13px;color:var(--text-muted);padding:var(--sp-2)';
            empty.textContent = 'No matches. You can still fill the fields in manually below.';
            resultsEl.appendChild(empty);
            return;
        }

        results.forEach(function (result) {
            var row = document.createElement('button');
            row.type = 'button';
            row.className = 'flex gap-3';
            row.style.cssText =
                'width:100%;text-align:left;background:var(--bg-overlay);border:1px solid var(--border);' +
                'border-radius:var(--r-md);padding:var(--sp-2);cursor:pointer;align-items:center';

            var poster = document.createElement('div');
            poster.style.cssText =
                'width:36px;height:54px;flex-shrink:0;border-radius:4px;overflow:hidden;background:var(--bg-card)';
            if (result.imageUrl) {
                var img = document.createElement('img');
                img.src = result.imageUrl;
                img.alt = '';
                img.style.cssText = 'width:100%;height:100%;object-fit:cover';
                img.addEventListener('error', function () { poster.style.display = 'none'; });
                poster.appendChild(img);
            }

            var text = document.createElement('div');
            var titleLine = result.title + (result.releaseYear ? ' (' + result.releaseYear + ')' : '');
            text.innerHTML = '<div style="font-size:13px;color:var(--text-primary)">' +
                escapeHtml(titleLine) + '</div>';

            row.appendChild(poster);
            row.appendChild(text);

            row.addEventListener('click', function () {
                if (titleInput) titleInput.value = result.title || '';
                if (yearInput) yearInput.value = result.releaseYear || '';
                if (descriptionInput) descriptionInput.value = result.overview || '';
                if (coverInput) coverInput.value = result.imageUrl || '';
                resultsEl.innerHTML = '';
                queryInput.value = result.title || '';
            });

            resultsEl.appendChild(row);
        });
    }

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function runSearch() {
        var categoryId = categorySelect.value;
        var query = queryInput.value.trim();
        if (!currentSourceLabel() || !query) {
            return;
        }

        resultsEl.innerHTML = '<div style="font-size:13px;color:var(--text-muted);padding:var(--sp-2)">Searching…</div>';

        fetch('/Media/SearchExternal?query=' + encodeURIComponent(query) + '&categoryId=' + categoryId)
            .then(function (response) { return response.json(); })
            .then(renderResults)
            .catch(function () {
                resultsEl.innerHTML =
                    '<div style="font-size:13px;color:var(--danger)">Search failed - fill the fields in manually.</div>';
            });
    }

    if (searchBtn) {
        searchBtn.addEventListener('click', runSearch);
    }
    if (queryInput) {
        queryInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                runSearch();
            }
        });
    }
})();
