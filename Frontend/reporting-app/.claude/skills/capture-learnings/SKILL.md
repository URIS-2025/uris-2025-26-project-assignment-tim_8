---
name: capture-learnings
description: >
  Use this skill at the end of ANY coding session, bug fix, or feature work to mine
  what was learned and propose additions to the long-lived knowledge base in
  .claude/rules/ and .claude/guides/. Trigger whenever the user says "capture learnings",
  "update the knowledge base", "save what we learned", "add this to the rules", or
  "don't forget this for next time". Also trigger automatically when a task is finishing
  and the session contained user corrections, new patterns, architectural decisions, or
  bug root-cause discoveries. Accepts an optional --from-flow=feature|bug|tests flag
  for targeted extraction. Never auto-commits — always proposes changes and waits for
  explicit user approval before writing anything.
---

# /capture-learnings

Every coding session produces two streams of knowledge that almost always evaporate:

1. **Patterns the agent figured out from the code** — a recurring idiom, an anti-pattern
   it had to rewrite, a non-obvious API, a workaround discovered.
2. **Corrections the user typed into chat** — "no, do it this way", "stop suggesting X",
   "the reason this works is…"

This skill mines both streams, deduplicates against what is already documented, and
proposes additions to the knowledge base. The user approves each change. Nothing is
written without approval.

---

## Knowledge base locations

| Location | Purpose | Format constraint |
|----------|---------|-------------------|
| `.claude/rules/*.md` | How to write code — WRONG/CORRECT patterns | Tables + code blocks, <500 lines |
| `.claude/guides/*.md` | How the system works — architecture, data flow, gotchas | Longer OK when justified |
| `CLAUDE.md` | Index of all rules and guides — loaded every session | Tables only; update when new files are created |

---

## Step 1 — Gather session context

Run these before doing anything else:

```bash
# Files touched this session (uncommitted changes)
git diff --name-only

# Recent commits made during this session
git log --oneline -10

# Current branch
git branch --show-current
```

Categorize touched files by area: `production-code | tests | frontend | config | docs`.
Note which project each file belongs to: `main/` (legacy CakePHP) or `USFX-Monorepo/`
(new Next.js stack).

Do not write anything yet. This step is orientation only.

---

## Step 2 — Analyze user messages for signals

Walk every user message in this session. Match against this signal table and list every
hit — do not discard anything yet, classification happens in Step 4.

| Signal type | What it looks like | What to extract |
|-------------|-------------------|-----------------|
| **Correction** | "no, use X" / "don't do that" / "that's the wrong approach" | A WRONG/CORRECT pattern pair |
| **Preference** | "always do X first" / "I prefer Y" / "we do it this way" | A DO/DON'T rule |
| **Complaint** | "this is dead code" / "that's deprecated" / "this never works" | An anti-pattern to document |
| **Decision** | "let's go with A" / "the reason is..." / "we decided on B" | An architectural note for a guide |
| **Rejection** | User denied a tool call / said "stop doing X" | A workflow constraint |
| **Teaching** | "the way this works is…" / "this exists because…" | A system-knowledge fragment |

**Core rule: every correction is a potential learning.** If the user had to correct the
agent, the knowledge base is missing something that would have prevented it.

---

## Step 3 — Extract learnings from code

For each file touched in Step 1, look for:

| What to find | How to find it | Threshold to promote |
|-------------|---------------|----------------------|
| Recurring pattern | Used 3+ times in changed or surrounding code | 3+ grep hits required |
| Anti-pattern fixed | Something was rewritten — what was wrong and why | Document the WRONG/CORRECT pair |
| New API or module surfaced | First time this path/class/function appeared | Document entry point + purpose |
| Workaround discovered | Non-obvious fix for a framework or legacy quirk | Document the gotcha |
| Test failure insight | Failure that revealed a missing test pattern | Document the gap |

For every candidate pattern, confirm with grep before promoting:

```bash
# Legacy project — PHP patterns
grep -rn "the_pattern" main/ --include="*.php" | head -20

# New project — TypeScript/React patterns
grep -rn "the_pattern" USFX-Monorepo/src/ --include="*.ts" --include="*.tsx" | head -20
```

- **3+ hits** → eligible for promotion to a rule
- **1–2 hits** → mark WEAK, note the locations, do not promote yet
- **0 hits** → discard entirely

**Never invent a rule. Every rule must trace to real file:line evidence in this codebase.**

---

## Step 4 — Classify strategic vs. tactical

Take every item from Steps 2 and 3. Classify each one:

| Classification | Definition | Destination |
|----------------|-----------|-------------|
| **Strategic** | Reusable across future sessions — a pattern, rule, or architectural fact | → Propose to knowledge base |
| **Tactical** | One-off detail specific to this task only | → Belongs in a code comment or commit message; drop from this flow |

**Tactical — always discard:**
- "Fixed null check on line 142 of Order.php"
- "Temporarily disabled the rate import cron"
- "TODO: refactor this after the migration"

**Strategic — always promote:**
- "CakePHP Order model validates payment method in `beforeSave` — never validate in controller"
- "Exchange rate margin is applied inside `ExchangeRate::getRetailRate()`, not at display time"
- "Next.js API routes in USFX-Monorepo follow: validate → transform → respond; never put business logic in the route handler"

---

## Step 5 — Deduplicate against the existing knowledge base

For every strategic candidate, grep the existing knowledge base before proposing:

```bash
# Search rules for the concept
grep -ril "concept_keyword" .claude/rules/

# Search guides for the concept
grep -ril "concept_keyword" .claude/guides/

# If a match is found, read that file
cat .claude/rules/the-matching-file.md
```

Classify each candidate:

| Result | Action |
|--------|--------|
| **New** — nothing close exists | Propose as addition |
| **Update** — exists but incomplete or outdated | Propose a targeted edit only |
| **Documented** — already fully covered | Skip silently |
| **Contradicts** — conflicts with an existing rule | Flag for the user to resolve; never resolve unilaterally |

**Skip aggressively.** If removing a candidate would not change how an agent writes code,
discard it. Bloat kills the knowledge base faster than gaps do.

---

## Step 6 — Propose changes and wait for approval

Show the summary table first — one row per proposed change:

```
## Proposed knowledge base changes

| # | Action | Target file | One-line summary |
|---|--------|-------------|-----------------|
| 1 | CREATE | .claude/rules/order-validation.md | CakePHP beforeSave owns all order validation — never validate in controller |
| 2 | UPDATE | .claude/guides/purchase-flow.md | Add: retail rate is calculated in ExchangeRate::getRetailRate() |
| 3 | FLAG   | .claude/rules/error-handling.md | Contradicts existing rule on line 34 — user must decide |
```

Then show each proposal in full, one at a time, and wait for a response before continuing:

```
### Proposal 1 — CREATE .claude/rules/order-validation.md

[full proposed file content here]

---
Approve? (yes / no / edit)
```

**Apply only what the user approves.** For each approved CREATE:
1. Write the file to `.claude/rules/` or `.claude/guides/`
2. Add a row to the relevant table in `CLAUDE.md` so the file is discoverable next session

For each approved UPDATE:
1. Show a before/after diff of the specific section being changed
2. Apply only that section — do not rewrite the whole file

For flagged contradictions: show both versions side by side and ask the user which is correct.

---

## Optional: --from-flow targeted extraction

When passed, run an extra pass after Step 3 with these targeted prompts:

### `--from-flow=feature`
- Did this feature introduce a new pattern other areas of the codebase should follow?
- Was a missing rule discovered that would have made this feature faster to build?
- Is any part of this workflow worth extracting as a reusable Claude Code skill?

### `--from-flow=bug`
- What class of bug was this? (N+1, missing cache invalidation, validation gap, session
  leak, unguarded null, etc.)
- Would a test have caught this before production? What would that test look like?
- Does the same root cause appear elsewhere in the codebase?
  ```bash
  grep -rn "the_root_cause_pattern" main/ --include="*.php"
  ```

### `--from-flow=tests`
- What assertion gap did this session reveal?
- Was there a repeated failure mode with a single underlying cause?
- Is there a test setup/teardown/fixture pattern worth documenting?

---

## What never to capture

| Do not capture | Reason |
|----------------|--------|
| One-off bug fix details | Not reusable — belongs in the commit message |
| Session state or TODO items | Ephemeral |
| Unverified speculation | May be wrong; grep first |
| Anything already in the knowledge base | Cross-reference instead |
| Verbose prose | Knowledge base is read by an LLM — every token costs context |
| Patterns with fewer than 3 grep hits | Anecdotal, not a rule |
| Generic best practices not seen in this codebase | Invented; must trace to real code |

---

## Output format for new rule files

```markdown
# Short Title

## Pattern name (verb phrase)

| Pattern | Evidence (file:line) |
|---------|-----------------------|
| description | main/app/Controller/Example.php:142 |

```php
// WRONG
badPattern();

// CORRECT
goodPattern(); // one-line reason why
```
```

## Output format for new guide sections (appended to existing guide)

```markdown
## New Section Title

<what exists, entry points, how it connects — specific identifiers only>

| Entity | File | Purpose |
|--------|------|---------|
| ClassName | main/app/Model/Order.php | ... |
```

---

## When to run

| Workflow | Run after |
|---------|-----------|
| Feature shipped | Final commit, before closing the session |
| Bug fixed | Fix confirmed working |
| Knowledge base bootstrap | After `/compile-results` finishes |
| Any session with agent corrections | Always — corrections = missing rules |
| Research / exploration session | If architectural decisions were made |