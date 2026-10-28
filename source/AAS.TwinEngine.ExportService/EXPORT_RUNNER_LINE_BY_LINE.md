# ExportRunner: Line-by-Line Explanation

This document explains the implementation in
[`ApplicationLogic/Services/Export/ExportRunner.cs`](ApplicationLogic/Services/Export/ExportRunner.cs).
The line numbers below refer to that source file as reviewed on 2026-10-27. Blank lines are
omitted because they provide visual spacing only; every declaration, executable statement,
branch, and meaningful expression is covered.

## 1. Imports, class, and phase order

| Source line | Explanation |
|---|---|
| 1 | Imports `SourceUnavailableException`, which receives special handling when a phase cannot safely read its source. |
| 2 | Imports `ExportServiceTracing` for the run-level activity. |
| 3 | Imports domain types such as `EntityKind`, `PhaseResult`, `ExportRunResult`, and `RunStatus`. |
| 4 | Imports the typed service configuration used to check which phases are enabled. |
| 6 | Imports `IOptions<T>` for access to the configured source settings. |
| 8 | Places the class in the Export Service application-layer namespace. |
| 10 | Declares the sealed `ExportRunner` class and its primary constructor. Dependency injection supplies the phase executor, configuration snapshot, and typed logger; the class implements `IExportRunner`. |
| 11 | Opens the class body. |
| 12 | Declares the fixed order used for forward processing. |
| 13 | Opens the collection expression that initializes the phase order. |
| 14 | Places concept descriptions first. |
| 15 | Places submodels after concept descriptions. |
| 16 | Places submodel descriptors after submodels. |
| 17 | Places shells after their submodels and descriptors. |
| 18 | Places shell descriptors last. |
| 19 | Closes the phase-order collection. Deletion processing later reverses this order. |
| 21 | Stores the injected phase executor in a private, read-only field. |
| 22 | Stores the configuration options in a private, read-only field. |
| 23 | Stores the typed logger in a private, read-only field. |

## 2. Running and assembling an export run

| Source line | Explanation |
|---|---|
| 25 | Declares the public asynchronous run method. Its cancellation token applies to both passes and their phase work. |
| 26 | Opens the method body. |
| 27 | Starts a run-level tracing activity and disposes it automatically when the method exits. |
| 29 | Captures the run's UTC start time. |
| 30 | Logs the run start with an ISO-8601 timestamp format. |
| 32 | Creates a dictionary that stores one accumulated `PhaseResult` per entity kind. Its initial capacity matches the number of configured kinds in `PhaseOrder`. |
| 33 | Creates a list of enabled kinds in forward order. This list later provides the reverse deletion order and preserves result ordering. |
| 35 | Executes the forward pass and awaits its status. `ConfigureAwait(false)` avoids requiring a captured synchronization context. |
| 37 | Allows deletions only when the forward pass did not abort. Partial failures do not by themselves prevent the deletion pass. |
| 38 | Opens the condition body. |
| 39 | Runs deletion phases using the enabled-kind list and current status, then awaits the result. |
| 40 | Closes the condition. |
| 42 | Calculates the final status from the pass status and the phase results collected so far. |
| 44 | Captures the run's UTC finish time. |
| 45 | Begins the final structured log message. |
| 46 | Defines log placeholders for the finish time, status, and elapsed seconds. |
| 47 | Supplies the finish time, status, and elapsed duration; `F1` formats the duration to one decimal place. |
| 49 | Returns an `ExportRunResult` with timestamps and status; its phase list contains completed enabled kinds in forward order. A phase that failed before returning a result is omitted. |
| 50 | Closes `RunAsync`. |

## 3. Forward pass

| Source line | Explanation |
|---|---|
| 52 | Declares the forward-pass method. It receives the result dictionary, enabled-kind list, and cancellation token, and returns the resulting run status. |
| 53 | Opens the method body. |
| 54 | Starts the forward pass as successful; later failures can escalate or abort this status. |
| 56 | Iterates over entity kinds in the fixed `PhaseOrder`. |
| 57 | Opens the loop body. |
| 58 | Checks the source-side configuration flag for this kind. |
| 59 | Opens the disabled-phase branch. |
| 60 | Logs that this kind is disabled and will be skipped. |
| 61 | Continues to the next kind without adding or executing this phase. |
| 62 | Closes the disabled-phase branch. |
| 64 | Records the kind as enabled before executing it. The list is later processed in reverse for deletions. |
| 65 | Throws immediately if the run token has already been canceled. |
| 67 | Starts exception handling around execution of this kind's phase. |
| 68 | Opens the `try` block. |
| 69 | Executes the forward phase and awaits its result. |
| 70 | Adds the completed result to the dictionary. `Add` also guards against duplicate phase insertion. |
| 71 | Escalates Success to PartialFailure if this result reports failed entities; other statuses are preserved. |
| 72 | Closes the `try` block. |
| 73 | Catches source-unavailability errors separately from other exceptions. |
| 74 | Opens the source-failure handler. |
| 75 | Logs the source failure as critical and explains why later phases are stopped. |
| 76 | Returns Aborted, preventing remaining forward kinds and the deletion pass. |
| 77 | Closes the source-failure handler. |
| 78 | Catches cancellation so it can add phase context before propagating it. |
| 79 | Opens the cancellation handler. |
| 80 | Rethrows cancellation with a phase-specific message while preserving the caught exception and cancellation token. |
| 81 | Closes the throw expression and cancellation handler. Cancellation is not converted into a normal run result. |
| 82 | Catches any other exception from phase execution. |
| 83 | Opens the general-failure handler. |
| 84 | Logs an unexpected phase failure as critical. |
| 85 | Returns Aborted so later forward phases and deletions do not run. |
| 86 | Closes the general-failure handler. |
| 87 | Closes the phase loop. |
| 89 | Returns Success or PartialFailure after every enabled forward phase completed without a phase-level abort. |
| 90 | Closes `ExecuteForwardPassAsync`. |

## 4. Reverse-order deletion pass

| Source line | Explanation |
|---|---|
| 92 | Declares the deletion-pass method. It accepts the existing status so forward failures can be preserved. |
| 93 | Receives the dictionary of completed forward results to update with deletion counters. |
| 94 | Receives only the kinds enabled during the forward pass. |
| 95 | Receives the current status. |
| 96 | Receives the run cancellation token. |
| 97 | Opens the method body. |
| 98 | Iterates over enabled kinds in reverse order, deleting dependent entities before the entities they depend on. |
| 99 | Opens the loop body. |
| 100 | Checks cancellation before starting each deletion phase. |
| 102 | Starts exception handling for one deletion phase. |
| 103 | Opens the `try` block. |
| 104 | Executes deletion processing for the current kind and awaits its result. |
| 105 | Merges the deletion counters with that kind's forward result and replaces its dictionary entry. |
| 106 | Escalates a successful status to PartialFailure if deletion work reports failed entities. |
| 107 | Closes the `try` block. |
| 108 | Catches source-unavailability errors separately. |
| 109 | Opens the source-failure handler. |
| 110 | Logs that source unavailability stops the remaining deletion phases. |
| 111 | Returns Aborted; already completed work is not rolled back. |
| 112 | Closes the source-failure handler. |
| 113 | Catches cancellation so it can add deletion-phase context before propagating it. |
| 114 | Opens the cancellation handler. |
| 115 | Rethrows cancellation with a deletion-phase message while preserving the caught exception and cancellation token. |
| 116 | Closes the throw expression and cancellation handler. |
| 117 | Catches any other exception from this deletion phase. |
| 118 | Opens the general-failure handler. |
| 119 | Logs an unexpected deletion-phase failure as critical. |
| 120 | Returns Aborted, stopping the remaining deletion phases. |
| 121 | Closes the general-failure handler. |
| 122 | Closes the reverse-order loop. |
| 124 | Returns the resulting status if all deletion phases completed without a phase-level abort. |
| 125 | Closes `ExecuteDeletionPassAsync`. |

## 5. Status, result merging, and enablement helpers

| Source line | Explanation |
|---|---|
| 127 | Defines a compact status helper: if a phase has failures while the run is still Success, return PartialFailure; otherwise preserve the existing status. |
| 129 | Declares the helper that computes the final status from the current status and phase results. |
| 130 | Opens the helper body. |
| 131 | Checks whether a pass already aborted. |
| 132 | Opens the Aborted branch. |
| 133 | Preserves Aborted without recomputing it from counters. |
| 134 | Closes the branch. |
| 136 | Materializes the result sequence so it can be summarized. |
| 137 | Sums failed entity counts across all completed phase results. |
| 138 | Checks whether any entity failed. |
| 139 | Opens the no-failures branch. |
| 140 | Returns Success when there are no failed entities, even if there were no enabled phases or no writes. |
| 141 | Closes the branch. |
| 143 | Sums successful writes: created, updated, and deleted. Skipped decisions do not count as writes. |
| 144 | Returns FullFailure if failures occurred and no writes succeeded; otherwise returns PartialFailure. |
| 145 | Closes `ResolveFinalStatus`. |
| 147 | Declares a compact helper that creates one phase result from forward and deletion results. |
| 148 | Preserves the entity kind. |
| 149 | Adds created counts. |
| 150 | Adds updated counts. |
| 151 | Adds deleted counts. |
| 152 | Adds skipped counts. |
| 153 | Adds failed counts and closes the constructed result. |
| 155 | Declares the helper that checks whether one entity kind is enabled. |
| 156 | Opens the helper body. |
| 157 | Reads the source endpoint settings. Source enablement controls whether the matching phase runs. |
| 159 | Starts a switch expression on the entity kind. |
| 160 | Opens the switch expression body. |
| 161 | Maps ConceptDescription to its plural configuration group and `Enabled` flag. |
| 162 | Maps Submodel to its configuration group's `Enabled` flag. |
| 163 | Maps SubmodelDescriptor to its configuration group's `Enabled` flag. |
| 164 | Maps Shell to its configuration group's `Enabled` flag. |
| 165 | Maps ShellDescriptor to its configuration group's `Enabled` flag. |
| 166 | Returns false for any unrecognized kind, so an unsupported value is not accidentally executed. |
| 167 | Closes the switch expression. |
| 168 | Closes `IsPhaseEnabled`. |
| 169 | Closes `ExportRunner`. |

## 6. Whole-run flow

1. `RunAsync` starts timing and tracing, then invokes the forward pass.
2. The forward pass visits enabled kinds in `PhaseOrder`; source failures and unexpected phase errors abort the run, while phase-reported entity failures produce PartialFailure and allow later kinds to run.
3. If the forward pass did not abort, enabled kinds are processed for deletion in reverse order.
4. Each deletion result is merged into its kind's existing forward result.
5. Final status distinguishes no failures, failures without successful writes, partial writes, and an aborted pass.
6. The returned result includes completed enabled phases in forward order. Cancellation observed by the runner or parallel loop can propagate; cancellation caught inside one `PhaseExecutor` decision may instead be counted as a per-entity failure.

These line references describe the implementation as it exists now. Re-check
`ExportRunner.cs` if the code changes.
