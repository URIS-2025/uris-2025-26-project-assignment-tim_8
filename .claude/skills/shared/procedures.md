# Shared Coordination Procedures

All four coordination skills (coordinate, execute-task, task-review, compile-results) rely
on these shared rules. Read this file before implementing any of them.

---

## Repository layout

```
<repo-root>/
  .coordination/
    PLAN.md          ← static task plan, written once at coordinate-time, never edited again
    .branch          ← name of the coordination branch (e.g. "coordinate/add-auth")
    .mode            ← "local" or "remote"
    tasks/
      001.unclaimed.review-api.md
      002.in-progress.fix-engine.md
      003.completed.update-tests.md
      004.blocked.refactor-foo.md
```

---

## The golden rule: status lives in the filename

Task state is encoded in the filename, not inside the file content:

| Prefix segment | Meaning |
|---|---|
| `unclaimed`   | Available to be picked up |
| `in-progress` | Claimed by an agent currently working it |
| `completed`   | Done, findings/solution written into the file |
| `blocked`     | Cannot proceed until a dependency completes |

Filename format: `NNN.<status>.<slug>.md`
Example: `007.in-progress.refactor-auth.md`

**Why filenames?** A status field inside a file is a content-level conflict — two agents
editing the same line. A rename is a tree-level operation that git resolves at the directory
level without requiring a diff-merge.

---

## State transitions via git mv

To claim a task:
```bash
git mv .coordination/tasks/001.unclaimed.review-api.md \
       .coordination/tasks/001.in-progress.review-api.md
git commit -m "claim task 001"
git push    # remote mode only; skip in local mode
```

To complete a task:
```bash
git mv .coordination/tasks/001.in-progress.review-api.md \
       .coordination/tasks/001.completed.review-api.md
git commit -m "complete task 001"
git push    # remote mode only
```

---

## Optimistic locking (claim retry protocol)

Every agent assumes it will win the race, attempts the git mv + commit + push, and
handles failure by pulling and re-evaluating. Never use a lock file or a daemon.

```
max_retries = 5
for attempt in 1..max_retries:
    git mv <unclaimed> <in-progress>
    git commit -m "claim task NNN"
    result = git push
    if result == success:
        break
    else:                      # non-fast-forward: someone else claimed something
        git pull --rebase
        re-scan tasks/ directory
        if target task is now in-progress/completed by another agent:
            pick a different task
        else:
            continue retry loop
if max_retries exhausted:
    abort — report contention and exit; never spin infinitely
```

Two agents claiming **different** tasks → they touch different files → no conflict.
Two agents racing for the **same** task → one push fails → loser pulls, sees rename, picks another.

---

## Stale-claim detection

A task stays `in-progress` forever if its claiming agent crashed or the laptop slept.

Detection rule: if the **git commit timestamp** on the `in-progress` file is older than
**24 hours**, the claim is stale and any agent may reclaim it.

```bash
# Get the commit time of the file's last rename into in-progress
git log --follow --diff-filter=R --format="%ct" -- \
    .coordination/tasks/NNN.in-progress.<slug>.md | head -1
```

Compare that unix timestamp against `$(date +%s) - 86400`. If the file is older, treat
it as unclaimed: rename it back to `unclaimed` and proceed normally through the claim retry
protocol.

**Why git timestamps and not a `Started:` field inside the file?** The agent that owns the
task can edit fields inside the file — easy to falsify (by bug or intention). The git commit
time is a fact recorded in repo history that the agent cannot backdate.

---

## PLAN.md is immutable after creation

PLAN.md contains the overall task breakdown written at coordinate-time. It is **never
updated** to reflect progress. To read current status, list `tasks/` and inspect filenames.

If PLAN.md were mutable, every agent would have to fight a write-contention race to update it.
By keeping it static, you get a clean, conflict-free record of the original plan while status
lives entirely in the directory listing.

---

## Local vs. remote mode

Read `.coordination/.mode` at the start of every operation.

| Mode | Behavior |
|---|---|
| `remote` | Every claim and completion does `git commit + push`. Other machines `git pull` to see updates. Use when agents run on multiple physical machines or across teams. |
| `local`  | All transitions commit locally; **no pushes during task work**. `/compile-results` pushes everything at the end. Use when all agents share one filesystem via git worktrees — pushing every micro-transition is pure overhead when they share the same disk. |

---

## Detecting the coordination branch

Read `.coordination/.branch` to get the branch name. Do not hard-code it.

```bash
COORD_BRANCH=$(cat .coordination/.branch)
```

---

## Task file anatomy

Each task file contains:
- **Title / goal** (first heading)
- **Type**: research | code | both
- **Dependencies**: list of task numbers that must be completed first
- **Acceptance criteria**
- **Context**: file paths, method names, gotchas — everything an agent needs to start cold
- **Findings / Solution** (written by execute-task when completed)
- **Branch** (written by execute-task for code tasks — the feature branch name)
- **## Review** section (appended by task-review, never by execute-task)

The self-contained context requirement is load-bearing: an agent picking up a task must
be able to execute without reading any conversation history or prior context.
