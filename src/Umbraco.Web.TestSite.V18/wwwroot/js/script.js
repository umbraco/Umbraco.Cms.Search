/**
 * Books Search Interface
 * Provides client-side search functionality for the Umbraco Books demo.
 */
(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        apiEndpoint: '/api/books',
        defaultPageSize: 5,
        lengthFacetOrder: ['very short', 'short', 'medium', 'long', 'very long'],
        sortOptions: [
            { label: 'Relevance', value: 'relevance' },
            { label: 'Title', value: 'title' },
            { label: 'Publish year', value: 'publishYear' }
        ],
        sortDirectionOptions: [
            { label: 'Descending', value: 'desc' },
            { label: 'Ascending', value: 'asc' }
        ]
    };

    // State management
    const searchParams = new URLSearchParams();
    searchParams.set('skip', '0');
    searchParams.set('take', String(CONFIG.defaultPageSize));

    // Set culture from the server-rendered page
    if (window.__CULTURE__) {
        searchParams.set('culture', window.__CULTURE__);
    }

    // DOM element references
    const elements = {
        searchResults: null,
        searchFilters: null
    };

    /**
     * Executes a search request and updates the UI with results
     */
    function search() {
        showLoadingState();

        fetch(`${CONFIG.apiEndpoint}?${searchParams.toString()}`, { method: 'GET' })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                hideLoadingState();
                render(data);
            })
            .catch(error => {
                hideLoadingState();
                showError(`Failed to load search results: ${error.message}`);
                console.error('Search error:', error);
            });
    }

    /**
     * Renders the complete search interface with results and filters
     * @param {Object} searchResult - The search result data from the API
     */
    function render(searchResult) {
        if (!elements.searchResults || !elements.searchFilters) {
            console.error('Required DOM elements not found');
            return;
        }

        elements.searchResults.innerHTML = `
            ${renderSearchResults(searchResult)}
            ${renderPagination(searchResult)}
        `;

        elements.searchFilters.innerHTML = `
            ${renderQuery()}
            ${renderRangeFacet(searchResult, 'publishYear', 'Century')}
            ${renderExactFacet(searchResult, 'length', 'Length')}
            ${renderExactFacet(searchResult, 'authorNationality', 'Nationality')}
            ${renderSorting()}
        `;
    }

    /**
     * Renders the search results list
     * @param {Object} searchResult - The search result data
     * @returns {string} HTML string for search results
     */
    function renderSearchResults(searchResult) {
        const query = searchParams.get('query');
        const resultCount = searchResult.total;
        const pluralizedBooks = resultCount === 1 ? 'book' : 'books';
        const queryText = query ? ` for "${escapeHtml(query)}"` : '';

        if (resultCount === 0) {
            return `
                <fieldset>
                    <legend>Search results</legend>
                    <h2>No books found${queryText}</h2>
                    <p>Try adjusting your search terms or filters.</p>
                </fieldset>
            `;
        }

        return `
            <fieldset>
                <legend>Search results</legend>
                <h2>Found ${resultCount} ${pluralizedBooks}${queryText}</h2>
                ${searchResult.documents.map(document => `
                    <article class="book-result">
                        <h3>${escapeHtml(document.name)}</h3>
                        <p class="book-meta">
                            by ${escapeHtml(document.properties.author)}
                        </p>
                        <p class="book-details">
                            <small>
                                Published: <strong>${document.properties.publishYear}</strong> |
                                Length: <strong>${escapeHtml(document.properties.length)}</strong> |
                                Nationality: <strong>${escapeHtml(document.properties.authorNationality[0])}</strong>
                            </small>
                        </p>
                        <p class="book-summary">${escapeHtml(document.properties.summary)}</p>
                    </article>
                `).join('')}
            </fieldset>
        `;
    }

    /**
     * Renders pagination controls
     * @param {Object} searchResult - The search result data
     * @returns {string} HTML string for pagination
     */
    function renderPagination(searchResult) {
        const skip = parseInt(searchParams.get('skip'), 10);
        const take = parseInt(searchParams.get('take'), 10);
        const totalPages = Math.ceil(searchResult.total / take);

        if (totalPages <= 1) {
            return '';
        }

        const pages = Array.from({ length: totalPages }, (_, i) => ({
            pageNumber: i + 1,
            isCurrent: skip === i * take,
            skipValue: i * take
        }));

        return `
            <fieldset>
                <legend>Pagination</legend>
                <nav class="pagination" aria-label="Search results pagination">
                    ${pages.map(page => `
                        <button
                            type="button"
                            class="pagination-button${page.isCurrent ? ' current' : ''}"
                            data-skip="${page.skipValue}"
                            onclick="BooksSearch.paginate(event, ${page.skipValue})"
                            ${page.isCurrent ? 'aria-current="page"' : ''}>
                            ${page.pageNumber}
                        </button>
                    `).join(' ')}
                </nav>
            </fieldset>
        `;
    }

    /**
     * Renders the search query input
     * @returns {string} HTML string for query input
     */
    function renderQuery() {
        const query = searchParams.get('query') || '';
        return `
            <fieldset>
                <legend>Search</legend>
                <label for="query" class="visually-hidden">Search query</label>
                <input
                    type="search"
                    id="query"
                    placeholder="Search books..."
                    value="${escapeHtml(query)}"
                    onkeydown="BooksSearch.handleQueryKeydown(event)"
                    aria-label="Search books">
            </fieldset>
        `;
    }

    /**
     * Renders a range facet (e.g., date ranges)
     * @param {Object} searchResult - The search result data
     * @param {string} fieldName - The field name for the facet
     * @param {string} label - Display label for the facet
     * @returns {string} HTML string for range facet
     */
    function renderRangeFacet(searchResult, fieldName, label) {
        const facet = searchResult.facets.find(f => f.fieldName === fieldName);
        if (!facet) {
            return '';
        }

        const hasActiveFilters = facet.values.some(value => {
            const facetValue = `${value.min},${value.max}`;
            return searchParams.has(fieldName, facetValue);
        });

        return `
            <fieldset>
                <legend>${escapeHtml(label)}</legend>
                ${facet.values
                    .filter(value => value.count > 0)
                    .map(value => {
                        const facetValue = `${value.min},${value.max}`;
                        const inputId = `${fieldName}:${sanitizeId(value.key)}`;
                        const isChecked = searchParams.has(fieldName, facetValue);

                        return `
                            <div class="facet-option">
                                <input
                                    type="checkbox"
                                    id="${inputId}"
                                    name="${fieldName}"
                                    value="${escapeHtml(facetValue)}"
                                    ${isChecked ? 'checked' : ''}
                                    onchange="BooksSearch.toggleFacet(event)">
                                <label for="${inputId}">
                                    ${escapeHtml(value.key)} (${value.count})
                                </label>
                            </div>
                        `;
                    })
                    .join('')}
            </fieldset>
        `;
    }

    /**
     * Renders an exact value facet (e.g., tags, categories)
     * @param {Object} searchResult - The search result data
     * @param {string} fieldName - The field name for the facet
     * @param {string} label - Display label for the facet
     * @returns {string} HTML string for exact facet
     */
    function renderExactFacet(searchResult, fieldName, label) {
        const facet = searchResult.facets.find(f => f.fieldName === fieldName);
        if (!facet) {
            return '';
        }

        // Sort facet values
        let facetValues;
        if (fieldName === 'length') {
            // Custom ordering for length facet
            facetValues = facet.values.sort((a, b) => {
                const aIndex = CONFIG.lengthFacetOrder.indexOf(a.key.toLowerCase());
                const bIndex = CONFIG.lengthFacetOrder.indexOf(b.key.toLowerCase());
                return aIndex - bIndex;
            });
        } else {
            // Alphabetical ordering for other facets
            facetValues = facet.values.sort((a, b) => a.key.localeCompare(b.key));
        }

        return `
            <fieldset>
                <legend>${escapeHtml(label)}</legend>
                ${facetValues
                    .filter(value => value.count > 0)
                    .map(value => {
                        const inputId = `${fieldName}:${sanitizeId(value.key)}`;
                        const isChecked = searchParams.has(fieldName, value.key);

                        return `
                            <div class="facet-option">
                                <input
                                    type="checkbox"
                                    id="${inputId}"
                                    name="${fieldName}"
                                    value="${escapeHtml(value.key)}"
                                    ${isChecked ? 'checked' : ''}
                                    onchange="BooksSearch.toggleFacet(event)">
                                <label for="${inputId}">
                                    ${escapeHtml(value.key)} (${value.count})
                                </label>
                            </div>
                        `;
                    })
                    .join('')}
            </fieldset>
        `;
    }

    /**
     * Renders sorting controls
     * @returns {string} HTML string for sorting controls
     */
    function renderSorting() {
        const sortBy = searchParams.get('sortBy') || CONFIG.sortOptions[0].value;
        const sortDirection = searchParams.get('sortDirection') || CONFIG.sortDirectionOptions[0].value;

        return `
            <fieldset>
                <legend>Sorting</legend>
                <div class="sort-controls">
                    <label for="sortBy">Sort by:</label>
                    <select id="sortBy" onchange="BooksSearch.sort(event)">
                        ${CONFIG.sortOptions.map(option => `
                            <option
                                value="${escapeHtml(option.value)}"
                                ${sortBy === option.value ? 'selected' : ''}>
                                ${escapeHtml(option.label)}
                            </option>
                        `).join('')}
                    </select>

                    <label for="sortDirection">Direction:</label>
                    <select id="sortDirection" onchange="BooksSearch.sortDirection(event)">
                        ${CONFIG.sortDirectionOptions.map(option => `
                            <option
                                value="${escapeHtml(option.value)}"
                                ${sortDirection === option.value ? 'selected' : ''}>
                                ${escapeHtml(option.label)}
                            </option>
                        `).join('')}
                    </select>
                </div>
            </fieldset>
        `;
    }

    /**
     * Handles query input keydown events
     * @param {KeyboardEvent} event - The keyboard event
     */
    function handleQueryKeydown(event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            searchParams.set('query', event.target.value);
            skipTo(0);
            search();
        } else {
            // Update search params as user types, but don't trigger search
            searchParams.set('query', event.target.value);
        }
    }

    /**
     * Toggles a facet filter
     * @param {Event} event - The change event from the checkbox
     */
    function toggleFacet(event) {
        const fieldName = event.target.name;
        const value = event.target.value;

        if (searchParams.has(fieldName, value)) {
            searchParams.delete(fieldName, value);
        } else {
            searchParams.append(fieldName, value);
        }

        skipTo(0);
        search();
    }

    /**
     * Handles pagination
     * @param {Event} event - The click event
     * @param {number} skip - The number of results to skip
     */
    function paginate(event, skip) {
        event.preventDefault();
        skipTo(skip);
        search();
    }

    /**
     * Handles sort field change
     * @param {Event} event - The change event
     */
    function sort(event) {
        searchParams.set('sortBy', event.target.value);
        skipTo(0);
        search();
    }

    /**
     * Handles sort direction change
     * @param {Event} event - The change event
     */
    function sortDirection(event) {
        searchParams.set('sortDirection', event.target.value);
        skipTo(0);
        search();
    }

    /**
     * Updates the skip parameter for pagination
     * @param {number} skip - The number of results to skip
     */
    function skipTo(skip) {
        searchParams.set('skip', String(skip));
    }

    /**
     * Shows loading state in the UI
     */
    function showLoadingState() {
        if (elements.searchResults) {
            elements.searchResults.setAttribute('aria-busy', 'true');
            elements.searchResults.style.opacity = '0.6';
        }
    }

    /**
     * Hides loading state in the UI
     */
    function hideLoadingState() {
        if (elements.searchResults) {
            elements.searchResults.removeAttribute('aria-busy');
            elements.searchResults.style.opacity = '1';
        }
    }

    /**
     * Displays an error message to the user
     * @param {string} message - The error message to display
     */
    function showError(message) {
        if (elements.searchResults) {
            elements.searchResults.innerHTML = `
                <fieldset>
                    <legend>Error</legend>
                    <p class="error-message">${escapeHtml(message)}</p>
                    <button type="button" onclick="BooksSearch.search()">Try Again</button>
                </fieldset>
            `;
        }
    }

    /**
     * Escapes HTML special characters to prevent XSS
     * @param {string} str - The string to escape
     * @returns {string} The escaped string
     */
    function escapeHtml(str) {
        if (str == null) {
            return '';
        }
        const div = document.createElement('div');
        div.textContent = String(str);
        return div.innerHTML;
    }

    /**
     * Sanitizes a string for use as an HTML ID
     * @param {string} str - The string to sanitize
     * @returns {string} The sanitized string
     */
    function sanitizeId(str) {
        return String(str).replace(/[^a-zA-Z0-9-_]/g, '_');
    }

    /**
     * Initializes the search interface
     */
    function init() {
        elements.searchResults = document.getElementById('searchResults');
        elements.searchFilters = document.getElementById('searchFilters');

        if (!elements.searchResults || !elements.searchFilters) {
            console.error('Required DOM elements (#searchResults, #searchFilters) not found');
            return;
        }

        search();
    }

    // Public API
    window.BooksSearch = {
        search,
        handleQueryKeydown,
        toggleFacet,
        paginate,
        sort,
        sortDirection
    };

    // Initialize on page load
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

})();
