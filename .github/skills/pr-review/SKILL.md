---
name: pr-review
description: Use for end-to-end PR review orchestration outputs: findings consolidation, comment-card formatting, user selection, and GitHub posting payload preparation.
argument-hint: Provide PR findings and target posting flow details.
---

# PR Review Skill

## Purpose

This is the canonical PR review skill for formatting and posting workflow.

## Required chat signal

When this skill is actually invoked, print this exact line before rendering findings:

`Using skill: pr-review`

When the sub-skill is actually invoked, print this exact line before rendering or posting comment bodies:

`Using sub-skill: pr-review/pr-comment-format`

Print the sub-skill line once per review run at first invocation; do not repeat it in later steps.

Do not print either line when the related skill or sub-skill was not invoked.

## Scope

1. Consolidate findings from all reviewer agents.
2. Render post-ready comment cards.
3. Build askQuestions options for selective posting.
4. Map selected findings to GitHub PR review comment payloads.
5. If linked issues exist, use them as contextual requirements input.
6. Do not block review execution when linked issues are absent.

## Distinctness And Depth Rules

1. Suppress redundant findings by semantic root cause, not only exact text equality.
2. If the same issue appears across multiple files, keep one representative card and mention additional files in Action Required.
3. If same file+line+code appears under multiple categories, keep one merged finding with highest severity and primary category precedence: Security > Architecture > Quality > Requirements > Other.
4. For non-trivial PRs (changed files >= 3 or added diff lines >= 80), enforce a minimum target of 18 distinct findings by requesting up to two depth passes for unreported lines.
5. Depth-pass findings may include actionable Medium, Low, or Info improvements, but must be evidence-based on added lines and non-duplicate.
6. Never invent findings; if fewer actionable non-duplicate items exist, continue with actual count and state the reason explicitly.

## Sub-skills

1. pr-review/pr-comment-format

- Use to transform findings into user-selectable comment cards and GitHub payloads.
- Reference file: pr-comment-format.md.

## Findings Display Format

Use this exact shape for every finding shown to the user in chat:

`🤖 Automated Code Review`

`Category: <Type> · Severity: <Severity> · Line: <Line>`
`File: <FilePath>`
`File Link: [<FilePath>:<Line>](<FilePath>#L<Line>)`

`Issue`
`<Issue>`

`Current Code`

```
<CodeSnippet>
```

`Recommended Change`

```
<SuggestedChange>
```

`Action Required`
`<Solution>`

Display field requirements:

1. Current Code and Recommended Change must never be empty.
2. If reviewer output is missing CodeSnippet, use fallback text describing that snippet was unavailable.
3. If reviewer output is missing SuggestedChange, use fallback guidance to apply Action Required.
4. If reviewer output is missing Issue, use fallback text to inspect the referenced diff block.
5. If reviewer output is missing Solution, use fallback text to provide concrete fix and validate with tests.

## Findings Totals In Display Window

After rendering all finding cards, always print totals:

1. By severity: Critical, High, Medium, Low, Info.
2. By category with fixed rows when trusted: Architecture, Security, Quality, Requirements, Other.

Totals consistency rules:

1. Normalize categories before counting.
2. Normalize severities before counting.
3. Use one shared findings list as single source for both summary tables.
4. Enforce invariant: total findings shown == severity table sum == category table sum.
5. If invariant fails, recompute both tables directly from displayed findings list before printing summaries.
6. If invariant still fails after recomputation, suppress category table and print mismatch warning.

## GitHub comment body template

Use this exact body for posted comments:

`🤖 Automated Code Review`

`Category: <Type> · Severity: <Severity> · Line: <Line>`

`Issue`
`<Issue>`

`Current Code`

```
<CodeSnippet>
```

`Recommended Change`

```
<SuggestedChange>
```

`Action Required`
`<Solution>`

Acceptance-number formatting rule:

1. In all posted comment fields, never prefix acceptance item numbers with #.
2. Normalize patterns like #10 acceptance to 10 acceptance before posting.
3. Apply this normalization before both display rendering and GitHub payload creation so chat and posted comments stay consistent.

## Selection UX contract

Order is mandatory:

1. Show all findings first using exact Findings Display Format.
2. Print END OF FINDINGS DISPLAY.
3. Only then ask for posting selection.
4. After the findings reference list is printed, next action must be vscode/askQuestions.
5. Do not ask additional permission prompts; continue automatically after each required user response.
6. Findings display is never permission-gated; askQuestions in this phase is only for GitHub posting selection.

Selection behavior:

1. Show multi-select choices where each option maps 1:1 to a finding id.
2. Keep options unambiguous: include id, severity, and short title.
3. Use up to 6 options: post all critical, high, medium, low, all, and Custom Selection as last option.
4. Parse freeform indices and ranges (for example: 1,3,5 or 2-7) and support post none or none.
5. If no valid selection is produced, re-open vscode/askQuestions instead of ending.

## Posting payload mapping

For each selected finding:

1. content: markdown comment body.
2. Re-validate line location before posting, even when numeric line already exists.
3. Prefer finding CodeBlock (2-6 lines) with AnchorBefore and AnchorAfter; if missing, reconstruct from diff and do not guess.
4. Use read_file on local workspace file and match full CodeBlock with anchors.
5. Accept only a unique anchor-consistent match and use full matched snippet span as posting line range.
6. Override stale reviewer line numbers with resolved span lines.
7. If no reliable location exists, skip posting that finding and record unresolved-line.
8. Do not post unanchored comments by default.
9. For inline posts, set displayed Line in comment body to resolved start line.
10. For posting operation, call GitHub PR review comment APIs, not Azure DevOps thread APIs.

## Final summary format

- Selected findings: <count>
- Posted successfully: <count>
- Failed to post: <count>
- Failed IDs: <comma-separated or NONE>
