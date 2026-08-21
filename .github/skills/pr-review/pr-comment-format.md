# PR Comment Format Sub-skill

## Purpose

Use this sub-skill to convert findings into post-ready GitHub PR comment cards.

## Required chat signal

Print before rendering comment cards:

`Using sub-skill: pr-review/pr-comment-format`

## Card format

`[<findingId>] <severity> | <type>`
`Title: <short title>`
`File: <path>`
`Line: <line or unknown>`
`File Link: [<path>:<line>](<path>#L<line>)` (only when path and line are resolved)
`Issue: <problem statement>`
`Impact: <why this matters>`
`Suggested Change: <concise fix>`
`Proposed GitHub Comment:`
`<markdown comment body>`

Card field guardrails:

- Issue, Suggested Change, and markdown Current Code block must never be empty.
- If source finding has missing Code, use snippet-unavailable fallback text.
- If source finding has missing Suggested Change, use recommended-change-not-provided fallback text.
- If source finding has missing Issue, use issue-details-not-provided fallback text.

## Posting mapping

- content: markdown card body
- path: required for inline review comments
- line: required for inline review comments
- side: RIGHT for added-code comments
- start_line: optional for multi-line comments
- start_side: optional for multi-line comments
- CodeBlock: preferred source snippet (2-6 lines, exact indentation)
- AnchorBefore: nearest unchanged line before CodeBlock from diff hunk (or NONE)
- AnchorAfter: nearest unchanged line after CodeBlock from diff hunk (or NONE)

If file or line is missing, skip posting that finding and report unresolved-line.
If line cannot be resolved with unique anchor-consistent snippet match, skip posting that finding and report unresolved-line.
Never post a general PR comment for a selected finding unless user explicitly requests general-comment posting.
For inline posts, update Line shown in markdown body to resolved line.
