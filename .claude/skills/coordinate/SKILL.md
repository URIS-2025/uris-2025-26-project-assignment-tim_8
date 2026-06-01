---
name: coordinate
description: >
  Use this skill when the user wants to break a large task into parallel subtasks that
  multiple agents can work on simultaneously — for example "audit these 20 files," "investigate
  these 6 modules," "review these 12 PR comments," or any task that naturally splits into
  independent pieces. This is the PLANNER skill — it sets up the coordination branch,
  directory structure, and task files so that parallel execute-task agents can claim and
  work them. Trigger whenever the user mentions coordinating parallel agents, splitting
  work across worktrees or machines, running agents in parallel on a codebase, or says
  "coordinate this task." Also trigger when the user asks how to start a multi-agent
  workflow. Read shared/procedures.md before starting.
---

# /coordinate — the Planner

The planner is called **once** at the start of a multi-agent workflow. Its job is to break
a task into N self-contained subtasks, write them as files in a `.coordination/tasks/`
directory, and push the coordination branch so parallel agents can claim work.

Read `shared/procedures.md` first — it defines the filename convention, git mv protocol,
local/remote mode, and PLAN.md immutability rules that all four skills share.

---

## Step-by-step

### 1. Verify clean working tree

```bash
git status --short
```

If there are uncommitted changes, stop and tell the user. A dirty tree means the
coordination branch will carry unintended changes.

### 2. Identify base branch

```bash
git branch --show-current
```

Record this as the **base branch**. It goes into PLAN.md. Code-task agents will branch
off this, not off the coordination branch — so each task branch is independently mergeable.

### 3. Read the codebase and propose a breakdown

Investigate whatever the user described (read files, list structure, check existing tests).
Then propose a **task breakdown table** before creating any files:

| # | Name (slug) | Type | Dependencies |
|---|---|---|---|
| 001 | review-api | research | — |
| 002 | fix-engine | code | 001 |
| 003 | update-tests | code | 002 |

**Type** is one of:
- `research` — investigate, read, report findings into the task file
- `code` — make changes on a feature branch
- `both` — research first, then implement

**Dependencies** list the task numbers that must be `completed` before this task can start.
A task with no deps is immediately claimable.

Present the table to the user and get confirmation before writing anything to disk.

### 4. Optionally generate review tasks

If the user wants built-in quality checks, generate one review task per work task:

| # | Name | Type | Dependencies |
|---|---|---|---|
| 004 | review-fix-engine | research | 002 |
| 005 | review-update-tests | research | 003 |

Review tasks are always type `research` and depend on the task they audit.

### 5. Create the coordination branch

```bash
SLUG=$(echo "<task-description>" | tr ' ' '-' | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9-]//g')
git checkout -b coordinate/$SLUG
```

### 6. Write the .coordination/ directory

```bash
mkdir -p .coordination/tasks
```

**PLAN.md** — write this once; never update it again:
```markdown
# Plan: <task description>

Base branch: <base-branch>
Mode: <local|remote>
Created: <ISO timestamp>

## Tasks

| # | Name | Type | Dependencies |
|---|---|---|---|
| 001 | review-api | research | — |
...

## Acceptance criteria
<paste from user or derive from the task>
```

**.branch** — one line, the coordination branch name:
```
coordinate/<slug>
```

**.mode** — one line, `local` or `remote`:
```
remote
```

Ask the user which mode if not stated. Default to `remote` when unsure.

**tasks/** — one file per task, all starting as `unclaimed`:

Filename: `NNN.unclaimed.<slug>.md`

Contents of each task file:
```markdown
# Task NNN: <Name>

**Type:** research | code | both
**Dependencies:** NNN, NNN (or "none")

## Goal
<One-paragraph description of what this task accomplishes.>

## Acceptance criteria
- [ ] <verifiable criterion>
- [ ] <verifiable criterion>

## Context
<Everything an agent needs to start cold: file paths, method names, line numbers,
known gotchas, relevant prior art. Be specific. An agent picking this up must not
need to re-read any conversation history.>
```

### 7. Commit and push (remote mode)

```bash
git add .coordination/
git commit -m "coordinate: set up <slug> with N tasks"
git push -u origin coordinate/$SLUG   # remote mode only
```

In **local mode**, omit the push — local agents share the filesystem and will see
the new directory immediately through their worktrees.

### 8. Report to the user

After writing everything, tell the user:
- The coordination branch name
- How many tasks were created and their slugs
- The mode (local/remote)
- How to start agents: `run /execute-task in each terminal / worktree`
- When all tasks are complete, run `/compile-results`

---

## What makes a good task description

The self-contained context requirement is the hardest part. When writing task files,
ask yourself: *could an agent with zero conversation history execute this?* If not, add more:

- Spell out file paths (`src/api/routes.py:142`)
- Name the methods or classes involved
- Note known edge cases or gotchas
- Include acceptance criteria that are verifiable without ambient context

Vague tasks produce vague results. The overhead of writing good task files pays for itself
immediately when parallel agents don't have to ask clarifying questions.

---

## Anti-patterns

- **Don't** create dependencies between tasks unless they genuinely block each other — unnecessary deps serialize work that could run in parallel.
- **Don't** make PLAN.md mutable — it's the historical record; progress lives in the filename.
- **Don't** use the coordination branch as the base for code-task feature branches — task branches must be off the real base branch so they're independently mergeable.
