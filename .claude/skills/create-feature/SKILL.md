---
name: create-feature
description: >
  A structured 5-phase pipeline for implementing features with hard gates between phases.
  Use this skill whenever a user asks to "build", "implement", "add", or "create" a feature,
  endpoint, component, or piece of functionality — especially in an existing codebase.
  Also use when the user says things like "let's work on X", "I need X built", or "write the
  code for X". The skill enforces test-first discipline, written requirements, and tiered
  self-review to prevent implementation drift.
---

# /create-feature

Imposes a five-phase pipeline with **hard gates between phases**. Each gate requires a written
artifact or passing tests before the next phase begins. Gates are non-negotiable — the moment
they become advisory, discipline collapses.

## Phase 0: Branch Setup

- Optional: run `/question-me` to pressure-test the idea before committing
- Branch off agreed base; claim ticket (if any)
- Confirm scope verbally with user before proceeding

**Gate: user confirms scope → proceed to Phase 1**

---

## Phase 1: Requirements & Design

1. Capture requirements — entities? endpoints? frontend? scope boundaries?
2. Write them to `feature-requirements.md` (single source of truth)
3. Load relevant rules/guides from `.claude/rules/` and `.claude/guides/` based on what's being built:

| Building | Load |
|----------|------|
| Backend API endpoint | caching rules, tracing rules, error-shape rules |
| Frontend component | component rules, state management rules, styling rules |
| Full-stack feature | all of the above; backend track must go green first |
| Domain/study logic | engine guide, domain entity guide |

4. Produce a written plan: entities, endpoints, files to create/modify, tests to write

**Gate: `feature-requirements.md` exists and is confirmed → proceed to Phase 2**

---

## Phase 2: Spec-Driven Scaffolding (optional)

- Use codegen tooling if the artifact will be long-lived and the project has it
- Skip when refactoring, experimenting, or one-off scripts
- Output: compilable stubs only — no logic yet

---

## Phase 3: Implementation — test-first, red-green-refactor

**Hard rule: production code is never written before a failing test covers it.**

**Hard rule (fullstack): backend endpoint must reach GREEN before the frontend track for that same endpoint begins.** Frontend test specs lock onto the live API contract; if the contract isn't real yet, the specs encode hopes instead of facts.

### Per-endpoint cycle (repeat for each endpoint/scenario):

**3.1 Scaffolding** — create minimum structure (entity, DTO, repository, API class, REST resource / React component, service, hook). Every method body throws `UnsupportedOperationException("TODO")` or equivalent. Compile/lint must pass.

**3.2 RED** — write ONE test. Run it. Confirm it fails for the *right reason* (stub error), not the wrong reason (compile error, missing setup, NPE/undefined). If the failure mode is wrong, the test itself is buggy.

**3.3 GREEN** — fill stub bodies layer by layer:
- Backend: repository → service → API caller → REST resource
- Frontend: service → store/state → container → template/component

Re-run the test after each layer. Stop as soon as it passes.

**3.4 REFACTOR** — clean up with the test still green. Add logging, tighten caching, scan for reuse opportunities.

**3.5 Per-layer checklist** — verify each architectural layer against its rules (annotations, logging level, caching pattern, test wrapper). Use tables from the relevant rule files, not memory.

**3.6 Repeat** — one red-green-refactor cycle per endpoint or scenario.

> ⚠️ Writing all tests up front then implementing in one big pass IS NOT the same thing. That defeats the feedback loop. One test, one cycle.

**Gate: all tests green → proceed to Phase 4**

---

## Phase 4: Tiered Self-Review

Measure the diff (files changed + lines changed), then pick a tier:

| Tier | Trigger | Approach |
|------|---------|----------|
| Small | ≤20 files AND ≤500 lines | Run all 7 review skills sequentially in this conversation |
| Medium | up to 50 files OR ≤1500 lines | Spawn each review skill as a parallel sub-agent (fresh context), max 3 concurrent |
| Large | beyond medium | Use `/coordinate` flow: one review-skill-per-slice as a task |

### The 7 review dimensions (same in all tiers):

1. **Rule violations** — check against loaded rules from `.claude/rules/`
2. **Requirements alignment** — read `feature-requirements.md`, verify every requirement is met
3. **Test correctness** — tests are testing the right things, not just passing
4. **Completeness** — no unimplemented stubs, no TODOs left behind
5. **Security** — auth checks, input validation, injection risks
6. **Performance** — N+1 queries, missing caching, unbounded loops
7. **Duplication** — code that already exists elsewhere in the codebase

**Verdict aggregation:** APPROVE / REQUEST_CHANGES / COMMENT with severity counts.
- Fix loop runs until APPROVE or a cap is hit
- Cosmetic findings (whitespace, formatting, taste-without-a-rule) are silently dropped, not reported as LOW

**Gate: verdict = APPROVE → proceed to Phase 5**

---

## Phase 5: Finalize

- Run `/capture-learnings` (optional) — every captured learning tightens the next feature
- Hand off to ship → post-PR → cleanup
- Delete `feature-requirements.md` or archive it

---

## Context loading

Pull only the slice that matches the task. Do not dump the entire knowledge base into context — less context = sharper agent.

```
.claude/rules/   ← "how to write code" — load the relevant ones
.claude/guides/  ← "how the system works" — load the relevant ones
```

If CLAUDE.md exists at repo root, read it first for the build/test/lint commands and architecture table.

---

## Anti-patterns

| Anti-pattern | Why it fails |
|---|---|
| Writing all tests then implementing in one pass | Defeats the feedback loop; same drift you were trying to prevent |
| Starting frontend before backend is green | Frontend specs encode hopes, not facts |
| Skipping `feature-requirements.md` | Halfway through a long session, the agent silently reinterprets requirements; a file is a thing you can grep |
| Letting gates become advisory | The entire discipline collapses; you're back to the default failure mode |
| Implementing across multiple endpoints before running any tests | Batching is the enemy of the feedback loop |