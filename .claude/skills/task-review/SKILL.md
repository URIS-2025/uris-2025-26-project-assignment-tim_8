---
name: task-review
description: >
  Use this skill when an agent needs to audit a completed task from a .coordination/tasks/
  directory — verifying that findings are accurate and code changes satisfy their acceptance
  criteria. This is the AUDITOR skill in a multi-agent parallel workflow. Trigger whenever
  the user says "task-review," "review task NNN," "audit the completed tasks," or when a
  review task has been set up by /coordinate. Also trigger when the user asks an agent to
  verify the output of a completed parallel task before merging. Read shared/procedures.md
  before starting.
---

# /task-review — the Auditor

The auditor reviews a completed task against the actual codebase. It never modifies
original findings — it only appends a `## Review` section. Its purpose is to catch the
failure mode that parallel agents are uniquely vulnerable to: each task is small enough
that hallucination or drift from ground truth has nowhere to be caught within the task itself.
The review pass is the catch.

Read `shared/procedures.md` first — it defines the filename convention, the task file
anatomy, and the coordination branch detection.

---

## Step-by-step

### 1. Detect the coordination branch and mode

```bash
COORD_BRANCH=$(cat .coordination/.branch)
MODE=$(cat .coordination/.mode)
```

In **remote mode**, pull first:
```bash
git pull origin $COORD_BRANCH
```

### 2. Identify the task to review

If a task number [N] was passed, review that specific task:
```bash
ls .coordination/tasks/ | grep "^NNN\."
```

If no number was passed, look for review tasks assigned to this agent (type = review in
PLAN.md, unclaimed). Claim one using the standard optimistic locking protocol from
`shared/procedures.md`.

The task file tells you which work task to audit (its dependency field points to it).

### 3. Read the task file

Open the completed task file. Understand:
- The **type** (research or code)
- The **acceptance criteria**
- The **Findings / Solution** section written by execute-task

### 4. Perform the review

The review strategy differs by task type:

#### Research tasks

Verify that every factual claim in the Findings section is accurate against the actual
codebase. Check each:

- **File paths**: do the referenced files actually exist?
  ```bash
  ls <claimed-path>
  ```
- **Line numbers**: are the cited line numbers accurate? Do they still point to what the
  finding says?
  ```bash
  sed -n '<line>p' <file>
  ```
- **Code snippets**: do quoted snippets match the actual file content?
- **Method/class names**: do they exist and match the described behavior?

Note any discrepancies precisely (e.g. "Line 142 was cited but the method moved to line 158
after a recent refactor").

#### Code tasks

Check out the task branch and review the diff:

```bash
BASE_BRANCH=$(grep "^Base branch:" .coordination/PLAN.md | cut -d' ' -f3)
TASK_BRANCH=$(grep "^## Branch" .coordination/tasks/NNN.completed.<slug>.md -A1 | tail -1 | xargs)

git fetch origin $TASK_BRANCH   # remote mode
git checkout $TASK_BRANCH
git diff $BASE_BRANCH...HEAD
```

Evaluate the diff against the acceptance criteria:
- Does the change actually accomplish what the task required?
- Are there obvious bugs, missing edge cases, or untested paths?
- Does the code style match the surrounding codebase?
- Are any files changed that shouldn't be (scope creep)?

### 5. Write the ## Review section

Append to the completed task file — **never edit the original Findings or Solution sections**.
Append only.

```markdown
## Review

**Reviewer:** <agent identifier or "task-review agent">
**Date:** <ISO date>
**Assessment:** accurate | minor-issues | needs-rework

### Findings

<Specific, factual observations. For research: which claims were verified, which were
inaccurate and how. For code: whether acceptance criteria are met, any issues found.>

### Verdict

- `accurate` — everything checks out, no action needed
- `minor-issues` — small inaccuracies or improvements noted; work product is still usable
- `needs-rework` — significant problems; recommend re-running execute-task on this task

<If needs-rework: describe exactly what needs to be fixed so execute-task can act on it
without further investigation.>
```

```bash
# After writing the review section:
git add .coordination/tasks/NNN.completed.<slug>.md
git commit -m "review task NNN: <assessment>"
# push in remote mode
```

### 6. Handle needs-rework

If the verdict is `needs-rework`, rename the task file back to `unclaimed` so it can be
re-executed:

```bash
git mv .coordination/tasks/NNN.completed.<slug>.md \
       .coordination/tasks/NNN.unclaimed.<slug>.md
git commit -m "reopen task NNN for rework"
# push in remote mode
```

Report the rework clearly so the user or next execute-task agent knows what to fix.

---

## Why this skill exists

Parallel agents each work in isolation. An agent's task is deliberately scoped small so
it can run independently — but that same isolation means there's no ambient context to
catch errors. An agent can:
- Cite a file path that doesn't exist
- Reference a method that was renamed
- Implement a fix that misses an edge case
- Drift from the acceptance criteria because it never saw the full picture

The review pass is a second agent with fresh eyes reading the actual repo. It's the
structural check that parallel work needs but single-agent work gets for free through
continuous context.
