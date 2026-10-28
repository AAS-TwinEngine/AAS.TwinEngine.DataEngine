# CrudDecisionMaker: Line-by-Line Explanation

This document explains the implementation in
[`ApplicationLogic/Services/Crud/CrudDecisionMaker.cs`](ApplicationLogic/Services/Crud/CrudDecisionMaker.cs).
The line numbers below refer to that source file as reviewed on 2026-10-27. Blank lines are
omitted because they provide visual spacing only; every declaration, executable statement,
branch, and meaningful expression is covered.

## 1. Import, namespace, and class

| Source line | Explanation |
|---|---|
| 1 | Imports the domain model types used by the decision maker, including entity kinds, source entities, managed entities, operations, and decisions. |
| 3 | Places the class in the application-layer namespace for CRUD decision logic. |
| 5 | Declares `CrudDecisionMaker` as a sealed class implementing `ICrudDecisionMaker`. `sealed` prevents subclassing. |
| 6 | Opens the class body. |

## 2. Decide method and input validation

| Source line | Explanation |
|---|---|
| 7 | Declares `Decide`, which returns a read-only list of planned CRUD operations. |
| 8 | Receives the entity kind; it is copied into every decision produced by this call. |
| 9 | Receives the source entities currently present for this kind. |
| 10 | Receives the previously managed entities loaded from the ownership ledger. |
| 11 | Opens the method body. |
| 12 | Throws `ArgumentNullException` if the source collection is null. It validates the collection reference, not each element. |
| 13 | Throws `ArgumentNullException` if the managed-state collection is null. |

## 3. Indexing state and processing source entities

| Source line | Explanation |
|---|---|
| 15 | Builds a dictionary keyed by managed identifier for fast lookup. `StringComparer.Ordinal` makes IDs case-sensitive and culture-independent. Duplicate managed IDs cause `ToDictionary` to throw. |
| 16 | Creates a case-sensitive set for IDs seen in the source. This set is later used to identify managed entities absent from the source listing. |
| 17 | Creates the result list with capacity for at most one decision per source item plus one per managed item, reducing list growth. |
| 19 | Iterates through source entities in their input order. Each source item will produce one decision. Duplicate source IDs are not removed from this iteration. |
| 20 | Opens the source loop. |
| 21 | Records the source identifier as present. Repeated IDs remain a single entry in the set, but their source items are still individually processed. |
| 23 | Looks for a managed-state entry with the same identifier. If no entry exists, this source entity is new to the exporter. |
| 24 | Opens the new-entity branch. |
| 25 | Adds a Create decision carrying the kind, identifier, and full source entity. |
| 26 | Closes the Create branch. |
| 27 | If state exists, compares the stored hash with the hash computed from the source entity's raw JSON. The comparison is ordinal; a null or different hash does not match. |
| 28 | Opens the unchanged-entity branch. |
| 29 | Adds a Skip decision when hashes match. The decision retains the source entity, but downstream processing performs no target or ledger write for Skip. |
| 30 | Closes the Skip branch. |
| 31 | Handles the remaining case: a managed ID whose source hash differs from the stored hash. |
| 32 | Opens the changed-entity branch. |
| 33 | Adds an Update decision carrying the current source entity and its identifier. |
| 34 | Closes the Update branch. |
| 35 | Closes the source loop. |

## 4. Finding managed entities missing from the source

| Source line | Explanation |
|---|---|
| 37 | Iterates through managed entities whose identifiers were not found in the source-ID set. This identifies deletion candidates; it does not itself verify source absence or delete the target. |
| 38 | Opens the missing-managed-entity loop. |
| 39 | Adds a Delete decision with the kind and managed identifier. The source entity is `null` because no source payload exists for a missing entity. |
| 40 | Closes the loop. |

## 5. Returning the decisions

| Source line | Explanation |
|---|---|
| 42 | Returns the decisions in the order they were produced: source decisions first, followed by Delete candidates. |
| 43 | Closes `Decide`. |
| 44 | Closes `CrudDecisionMaker`. |

## 6. Decision table

| Source contains ID? | Managed state contains ID? | Hashes match? | Result |
|---|---|---|---|
| Yes | No | Not applicable | Create |
| Yes | Yes | Yes | Skip |
| Yes | Yes | No, including a null stored hash | Update |
| No | Yes | Not applicable | Delete candidate |
| No | No | Not applicable | No decision |

This class only plans decisions. It does not call the source, write to the target, update the
ownership ledger, or verify deletions. `PhaseExecutor` applies the decisions and performs the
extra source GET-by-ID safety check before executing a Delete.

The algorithm uses ordinal identifiers and raw-content hashes. Duplicate source IDs can yield
multiple decisions for that identifier; duplicate managed IDs fail during dictionary creation.
Inputs are expected to contain unique identifiers.

The line references describe the implementation as it exists now. Re-check
`CrudDecisionMaker.cs` if the code changes.