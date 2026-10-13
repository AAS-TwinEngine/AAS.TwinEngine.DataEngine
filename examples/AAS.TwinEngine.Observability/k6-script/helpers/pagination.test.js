import assert from 'node:assert/strict';
import test from 'node:test';

import {
    appendCursor,
    extractCursor,
    shouldFetchNextPage
} from './pagination.js';

test('extracts cursors from supported AAS pagination shapes', () => {
    assert.equal(extractCursor({ paging_metadata: { cursor: 'next-a' } }), 'next-a');
    assert.equal(extractCursor({ pagingMetadata: { cursor: 'next-b' } }), 'next-b');
    assert.equal(extractCursor({ cursor: 'next-c' }), 'next-c');
    assert.equal(extractCursor({ paging_metadata: {} }), null);
});

test('appends an encoded cursor without removing the configured page limit', () => {
    assert.equal(
        appendCursor('http://localhost:8080/shells?limit=250', 'next page'),
        'http://localhost:8080/shells?limit=250&cursor=next%20page'
    );
});

test('continues only while a cursor exists and the request limit is not reached', () => {
    assert.equal(shouldFetchNextPage('next-a', 1, 10), true);
    assert.equal(shouldFetchNextPage('next-b', 10, 10), false);
    assert.equal(shouldFetchNextPage(null, 1, 10), false);
});