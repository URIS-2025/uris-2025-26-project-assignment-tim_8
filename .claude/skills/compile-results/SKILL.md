---
name: compile-results
description: >
  Use this skill when all tasks in a .coordination/tasks/ directory are completed and
  the user wants a synthesis of everything the parallel agents produced. This is the
  SYNTHESIZER skill — it reads all completed task files, diffs all code branches, runs
  a coverage audit, and writes RESULTS.md with cross-cutting observations, a merge order,
  and conflict warnings. In local mode it also pushes all branches to remote. Trigger
  whenever the user says "compile-results," "compile the results," "wrap up the parallel
  work," "everything's done, now what?", or asks for a summary after a /coordinate session
  completes. Read shared/procedures.md before starting.
---

# /compile-results — the Synthesizer

Called once, after all tasks are completed. Its job is to read the parallel agents'
work and produce something that none of them could produce alone: cross-cutting observations
that emerge only from seeing all results together. If you're just concatenating per-task
findings, you're adding no value over a single sequential agent. The synthesizer's output
must earn its existence.

Read `shared/procedures.md` first — it defines the coordination branch layout, mode
detection, and task file anatomy.

---

## Step-by-step

### 1. Detect mode and pull (remote mode)

```bash
COORD_BRANCH=$(cat .coordination/.branch)
MODE=$(cat .coordination/.mode)
BASE_BRANCH=$(grep "^Base branch:" .coordination/PLAN.md | cut -d' ' -f3)
```

In **remote mode**:
```bash
git pull origin $COORD_BRANCH
```

### 2. Verify all tasks are done

```bash
ls .coordination/tasks/
```

Check for any tasks still in `unclaimed`, `in-progress`, or `blocked` state.

If any tasks are incomplete, report their names and statuses and ask the user whether to
wait, skip them, or proceed anyway with a partial compile.

Do not silently drop unfinished tasks — make the gap explicit.

### 3. Run the coverage audit

For workflows where the input set is enumerable (e.g. "process every comment on PR #1234,"
"audit all 20 files in /src/api"), run a coverage check **before** writing RESULTS.md:

1. Reconstruct the expected item list from PLAN.md or the user's original request.
2. Read each completed task file and extract what items it says it processed.
3. Cross-reference: anything missing? Anything processed twice?

```
Expected items: [file-a.py, file-b.py, file-c.py, ...]
Task 001 processed: [file-a.py, file-b.py]
Task 002 processed: [file-c.py, ...]
Missing: []
Duplicates: []
```

If anything is missing or duplicated: **abort**. Report it to the user and ask how to
proceed. Do not emit a RESULTS.md that silently drops items. Parallel agent workflows
introduce a new failure mode — "agent forgot item 47" — that a single agent would never
have. The coverage audit is the structural fix.

### 4. Read all completed task files

For each `NNN.completed.<slug>.md` in `tasks/`, read:
- The **type** (research / code / both)
- The **Findings / Solution** section
- The **Branch** field (for code tasks)
- The **## Review** section if present (verdict: accurate / minor-issues / needs-rework)

Note any `needs-rework` verdicts — flag these prominently in RESULTS.md.

### 5. Diff all code branches

For each task that produced a feature branch:

```bash
git fetch origin task/NNN-<slug>   # remote mode
git diff $BASE_BRANCH...task/NNN-<slug> --stat
git diff $BASE_BRANCH...task/NNN-<slug>
```

Note which files each task touches. Overlapping files are potential merge conflicts.

### 6. Write RESULTS.md

Write to the coordination branch root (or repo root — wherever is most visible):

```markdown
# Results: <task description>

**Coordination branch:** coordinate/<slug>
**Base branch:** <base-branch>
**Compiled:** <ISO timestamp>
**Mode:** local | remote

---

## Narrative summary

<2-4 paragraphs describing what was accomplished overall. Write as if the reader hasn't
seen any individual task files. What was the original goal? What did the agents collectively
find or build? What changed?>

---

## Key findings

<Bulleted list of the most important discoveries or changes, ordered by significance.
Include only findings that matter to the person reading this — not a mechanical list of
every task's output.>

---

## Changes per task

| # | Task | Type | Branch | Review verdict | Summary |
|---|---|---|---|---|---|
| 001 | review-api | research | — | accurate | Found N endpoints missing auth |
| 002 | fix-engine | code | task/002-fix-engine | accurate | Patched parse logic in engine.py |

---

## Cross-cutting observations

<THIS IS THE LOAD-BEARING SECTION. These are patterns, risks, or insights that no single
task could have surfaced — they only become visible when you see all results together.>

Examples of what belongs here:
- "Tasks 002, 004, and 007 all touched auth.py — these changes need a coordinated merge
  to avoid regressions."
- "Three independent tasks flagged the same missing null check in utils/format.py — this
  is a systemic pattern, not a one-off."
- "Research tasks 001 and 003 reached contradictory conclusions about the caching layer —
  this needs to be resolved before any code tasks are merged."
- "The combined diff adds 840 lines but removes only 12 — the scope may have expanded
  beyond the original goal."

If you cannot write anything in this section that wasn't already in an individual task
file, you haven't synthesized — you've just concatenated. Keep looking.

---

## Remaining work

<Tasks that weren't completed, items from the coverage audit that were skipped, review
verdicts of needs-rework, or follow-on work the agents identified.>

---

## Recommended merge order

<List task branches in the order they should be merged to minimize conflicts, with
reasoning. Flag any branches that touch the same files.>

| Merge order | Branch | Files touched | Conflict risk |
|---|---|---|---|
| 1 | task/001-review-api | — (research only) | none |
| 2 | task/002-fix-engine | engine.py, engine_test.py | low |
| 3 | task/003-update-tests | engine_test.py | **medium** — overlaps with 002 |

---

## Coverage audit

<Results of the coverage audit from step 3. If everything passed, say so. If anything
was missing or duplicated, explain it here even if the user chose to proceed.>
```

### 7. Commit RESULTS.md

```bash
git add RESULTS.md
git commit -m "compile-results: write synthesis for <slug>"
```

### 8. Push all branches (local mode)

In **local mode**, this is the moment everything gets pushed to remote. Local agents skipped
individual pushes to avoid overhead — now batch them all:

```bash
git push origin $COORD_BRANCH
for branch in $(git branch | grep "task/"); do
    git push origin $branch
done
```

In **remote mode**, all branches are already pushed — just push the final commit on the
coordination branch:
```bash
git push origin $COORD_BRANCH
```

### 9. Report to the user

Summarize what was written, the recommended merge order, any risks or conflicts, and
what remaining work was identified. Give them a clear next-action path.

---

## What the cross-cutting observations section must do

The synthesizer's unique value is that it sees all outputs simultaneously. Use that position:

- **Find contradictions** across task findings
- **Identify patterns** that appear in multiple independent tasks
- **Surface emergent risks** (file overlap, scope expansion, repeated red flags)
- **Quantify** where individual tasks gave qualitative descriptions (total files changed,
  total lines, combined test coverage delta)
- **Connect** findings that individual agents couldn't connect because each only saw their
  own task

If you're writing this section and realizing you're just repeating individual task summaries
with different formatting, stop. Re-read all task files looking specifically for what
*no single agent knew* — only the synthesizer, reading everything, can see it.
