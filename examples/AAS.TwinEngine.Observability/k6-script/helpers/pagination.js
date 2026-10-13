export function extractCursor(payload) {
    return (
        payload?.paging_metadata?.cursor ||
        payload?.pagingMetadata?.cursor ||
        payload?.cursor ||
        null
    );
}

export function shouldFetchNextPage(cursor, requestsMade, maxRequests) {
    return Boolean(cursor) && requestsMade < maxRequests;
}

export function appendCursor(url, cursor) {
    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}cursor=${encodeURIComponent(cursor)}`;
}