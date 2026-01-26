const searchParams = new URLSearchParams();

// default pagination values
searchParams.set('skip', '0');
searchParams.set('take', '5');

search = () => {
    fetch(`/api/books?${searchParams.toString()}`, {method: 'GET'})
        .then(response => {
            if (!response.ok) {
                throw new Error(`Could not fetch data - response status was: ${response.status}`);
            }
            return response.json();
        })
        .then(data => {
            render(data);
        })
        .catch((error) => {
            console.error('Error:', error);
        })
};

render = (searchResult) => {
    document.getElementById('searchResults').innerHTML = `
        ${renderSearchResults(searchResult)}
        ${renderPagination(searchResult)}
    `;

    document.getElementById('searchFilters').innerHTML = `
        ${renderQuery()}
        ${renderRangeFacet(searchResult, 'publishYear', 'Century')}
        ${renderExactFacet(searchResult, 'length', 'Length')}
        ${renderExactFacet(searchResult, 'authorNationality', 'Nationality')}
        ${renderSorting()}
    `;
};

renderSearchResults = (searchResult) => `
    <fieldset>
        <legend>Search results</legend>
        <h2>Found ${searchResult.total} ${searchResult.total === 1 ? 'book' : 'books'}${searchParams.get('query') ? ` for "${searchParams.get('query')}"` : ''}</h2>
        ${searchResult.documents.map(document => `
            <h3 style="display: inline">${document.name}</h3> by ${document.properties.author}
            <br/>
            <small>
                Published: <strong>${document.properties.publishYear}</strong> |
                Length: <strong>${document.properties.length}</strong> |
                Nationality: <strong>${document.properties.authorNationality[0]}</strong>
            </small>
            <p>${document.properties.summary}</p>
        `).join('')}
    </fieldset>
`;

renderPagination = (searchResult) => {
    const skip = parseInt(searchParams.get('skip'));
    const take = parseInt(searchParams.get('take'));
    const totalPages = Math.floor(searchResult.total / take) + (searchResult.total % take === 0 ? 0 : 1);

    let pages = [];
    for (let i = 1; i <= totalPages; i++){
        pages.push({
            pageNumber: i,
            isCurrent: skip === (i - 1) * take
        });
    }

    return `
    <fieldset>
        <legend>Pagination</legend>
        ${pages.map(page => `
        <span ${page.isCurrent ? 'style="font-weight: bold;"' : ''}><a href onclick="paginate(event, ${take * (page.pageNumber - 1)})">${page.pageNumber}</a></span>
        `).join(' - ')}
    </fieldset>
`};

renderQuery = () => `
    <fieldset>
        <legend>Query</legend>
        <input type="text" id="query" placeholder="Enter query" value="${searchParams.get('query') ?? ''}" onkeyup="queryChange(event)">
    </fieldset>
`;

renderRangeFacet = (searchResult, fieldName, label) => {
    const facet = searchResult.facets.find(f => f.fieldName === fieldName);
    if (!facet){
        return '';
    }

    return `
    <fieldset>
        <legend>${label}</legend>
        ${facet.values.filter(value => value.count > 0).map(value => {
            const facetValue = `${value.min},${value.max}`;
            return `
        <div>
            <input type="checkbox" id="${fieldName}:${value.key}" name="${fieldName}" value="${facetValue}" ${searchParams.has(fieldName, facetValue) ? 'checked' : ''} onclick="toggleFacet(event)">
            <label for="${fieldName}:${value.key}">${value.key} (${value.count})</label>
        </div>`
        }).join('')}
    </fieldset>
`};

renderExactFacet = (searchResult, fieldName, label) => {
    const facet = searchResult.facets.find(f => f.fieldName === fieldName);
    if (!facet){
        return '';
    }

    let facetValues = [];

    // let's present the "Length" facet in a meaningful way, by explicitly ordering the facet values from "very short" to "very long"
    if (fieldName === 'length') {
        const facetValueOrder = ['very short', 'short', 'medium', 'long', 'very long'];
        facetValues = facet.values.sort((a, b) => facetValueOrder.indexOf(a.key.toLowerCase()) - facetValueOrder.indexOf(b.key.toLowerCase()));
    }
    // ...and order the facet values alphabetically for all other facets 
    else {
        facetValues = facet.values.sort((a, b) => a.key.localeCompare(b.key));
    }

    return `
    <fieldset>
        <legend>${label}</legend>
        ${facetValues.filter(value => value.count > 0).map(value => `
        <div>
            <input type="checkbox" id="${fieldName}:${value.key}" value="${value.key}" name="${fieldName}" ${searchParams.has(fieldName, value.key) ? 'checked' : ''} onclick="toggleFacet(event)">
            <label for="${fieldName}:${value.key}">${value.key} (${value.count})</label>
        </div>`
        ).join('')}
    </fieldset>
`};

renderSorting = () => {
    const sortByOptions = [
        { label: 'Relevance', value: 'relevance' },
        { label: 'Title', value: 'title' },
        { label: 'Publish year', value: 'publishYear' }
    ];

    const sortDirectionOptions = [
        { label: 'Descending', value: 'desc' },
        { label: 'Ascending', value: 'asc' }
    ];
    
    const sortBy = searchParams.get('sortBy') ?? sortByOptions[0].value;
    const sortDirection = searchParams.get('sortDirection') ?? sortDirectionOptions[0].value;

    return `
    <fieldset>
        <legend>Sorting</legend>
        <select onchange="sort(event)">
            ${sortByOptions.map(option => `
                <option value="${option.value}" ${sortBy === option.value ? 'selected' : ''}>${option.label}</option>
            `)}
        </select>
        <select onchange="sortDirection(event)">
            ${sortDirectionOptions.map(option => `
                <option value="${option.value}" ${sortDirection === option.value ? 'selected' : ''}>${option.label}</option>
            `)}
        </select>
    </fieldset>
`};

queryChange = (event) => {
    event.preventDefault();

    if (event.keyCode === 13) {
        skipTo(0);
        search();
        return;
    }

    searchParams.set('query', event.target.value);
}

toggleFacet = (event) => {
    const fieldName = event.target.name;
    const value = event.target.value

    if (searchParams.has(fieldName, value)) {
        searchParams.delete(fieldName, value);
    }
    else {
        searchParams.append(fieldName, value);
    }

    skipTo(0);
    search();
}

paginate = (event, skip) => {
    event.preventDefault();

    skipTo(skip);
    search();
}

sort = (event) => {
    event.preventDefault();

    searchParams.set('sortBy', event.target.value);
    skipTo(0);
    search();
}

sortDirection = (event) => {
    event.preventDefault();

    searchParams.set('sortDirection', event.target.value);
    skipTo(0);
    search();
}

skipTo = (skip) => searchParams.set('skip', skip);

window.addEventListener('load', () => search());
