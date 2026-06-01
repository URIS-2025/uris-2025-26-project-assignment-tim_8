---
name: execute-task
description: >
  Use this skill when an agent needs to claim and work a task from a .coordination/tasks/
  directory — i.e. in a multi-agent parallel workflow that was set up by /coordinate.
  This is the WORKER skill — it picks the next available unclaimed (or stale) task,
  claims it via git mv, does the work (research or code), marks it completed, and offers
  the next task. Trigger whenever the user says "execute-task," "work the next task,"
  "claim a task," or when an agent is running in a worktree and needs to pick up work.
  Also trigger automatically when the user launches a parallel agent session after
  /coordinate has run. Read shared/procedures.md before starting.
---

# /execute-task — the Worker

Called by every parallel agent. Each agent runs this skill independently — they share
the `.coordination/tasks/` directory and coordinate through git.

Read `shared/procedures.md` first — it defines the filename convention, optimistic locking
protocol, stale-claim detection, local/remote mode, and task file anatomy.

---

## Step-by-step

### 1. Detect the coordination branch

```bash
COORD_BRANCH=$(cat .coordination/.branch)
MODE=$(cat .coordination/.mode)
```

If these files don't exist, the user hasn't run `/coordinate` yet — tell them to do that first.

In **remote mode**, pull first to get the latest task states:
```bash
git pull origin $COORD_BRANCH
```

In **local mode**, the worktree already shares the filesystem — no pull needed.

### 2. Pick the next eligible task

Eligible = `unclaimed` OR `stale` (see stale-claim detection in `shared/procedures.md`),
AND all dependencies are `completed`.

```bash
# List all task files, sort by number
ls .coordination/tasks/ | sort

# Check dependency completion: for task NNN, look for all dep numbers in completed files
ls .coordination/tasks/ | grep "^<dep_num>\.completed\."
```

**If an optional task number [N] was passed**, try to claim that specific task first;
fall back to lowest-numbered eligible if it's already claimed.

**Selection priority**: lowest-numbered eligible task that is ready (all deps completed).
This creates a natural work queue without a scheduler.

### 3. Claim the task (optimistic locking)

```bash
ORIGINAL="NNN.unclaimed.<slug>.md"
CLAIMED="NNN.in-progress.<slug>.md"

git mv .coordination/tasks/$ORIGINAL .coordination/tasks/$CLAIMED
git commit -m "claim task NNN: <slug>"
```

In **remote mode**:
```bash
git push origin $COORD_BRANCH
```

If push fails (non-fast-forward):
```bash
git pull --rebase origin $COORD_BRANCH
# Re-scan tasks/ — the target may now be claimed by another agent
# If still unclaimed, retry the git mv; if gone, pick a different task
```

Cap retries at **5**. If the claim still fails after 5 attempts, report high contention
and exit cleanly — never spin indefinitely.

### 4. Execute the task

Read the task file to understand what's needed. Task type determines the approach:

#### Research tasks

Investigate the codebase, external resources, or whatever the task requires. Write your
findings back into the task file under a `## Findings` section:

```markdown
## Findings

<Detailed findings. Be specific: file paths, line numbers, actual code snippets.
Another agent reading this must be able to act on it without re-investigating.>
```

Commit your findings to the coordination branch:
```bash
git add .coordination/tasks/$CLAIMED
git commit -m "task NNN: add findings"
# push only in remote mode
```

#### Code tasks

Create a feature branch off the **base branch** (from PLAN.md), not the coordination branch:

```bash
BASE_BRANCH=$(grep "^Base branch:" .coordination/PLAN.md | cut -d' ' -f3)
git checkout $BASE_BRANCH
git checkout -b task/NNN-<slug>
```

Make changes, commit them:
```bash
# ... implement the changes ...
git add <changed-files>
git commit -m "task NNN: <describe change>"
git push origin task/NNN-<slug>   # remote mode; local mode pushes at compile time
```

**Why branch off the base branch?** Task branches must be independently mergeable.
If they branched off the coordination branch, merging one would pull in all coordination
metadata. Keep them clean.

#### Both (research + code)

Investigate first and write findings. Then create the feature branch and implement.

### 5. Mark completed

After work is done, return to the coordination branch and rename the task file:

```bash
git checkout $COORD_BRANCH
COMPLETED="NNN.completed.<slug>.md"
git mv .coordination/tasks/$CLAIMED .coordination/tasks/$COMPLETED
```

Write the resolution fields into the task file before committing:

```markdown
## Solution

<What was done. For research: summary of findings and their implications.
For code: what was changed and why. Keep it terse but complete.>

## Branch

task/NNN-<slug>   ← omit for research-only tasks
```

```bash
git add .coordination/tasks/$COMPLETED
git commit -m "complete task NNN: <slug>"
# push in remote mode
```

### 6. Offer the next task

After marking complete, re-scan tasks/ and report what's now eligible. If there's an
obvious next task (newly unblocked by this completion), name it explicitly:

> "Task 003 update-tests is now unblocked — you can run `/execute-task 003` or I can
> pick it up now."

This keeps momentum in a single-agent session and makes it easy for a human orchestrator
to direct parallel agents.

---

## Handling blocked tasks

If a task is blocked (its dependencies aren't all `completed` yet), skip it and pick the
next eligible one. Report what's blocking it so the user understands the queue state.

Don't wait — pick something else or exit if nothing is currently eligible.

---

## Stale-claim reclamation

If you detect a stale in-progress file (git commit timestamp > 24 hours old):
1. Report it to the user before doing anything.
2. Rename it back to `unclaimed`:
   ```bash
   git mv .coordination/tasks/NNN.in-progress.<slug>.md \
          .coordination/tasks/NNN.unclaimed.<slug>.md
   git commit -m "reclaim stale task NNN"
   # push in remote mode
   ```
3. Proceed with the normal claim retry protocol.
