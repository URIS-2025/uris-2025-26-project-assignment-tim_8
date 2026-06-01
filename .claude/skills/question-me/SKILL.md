---
name: question-me
description: >
  A Socratic pre-flight check that pressure-tests a rough idea before any code is written.
  Use this skill when a user has a fuzzy plan and wants to sharpen it, when they say things
  like "I'm thinking of...", "what do you think about...", "should I...", "help me think
  through...", or when /create-feature's Phase 0 prompts "want to pressure-test first?".
  Also use proactively when a user's stated approach has obvious blind spots — wrong scope,
  missing edge cases, root cause untouched, dependency unmodeled. Output is always a
  structured refined plan document, never code.
---

# /question-me

Forces a Socratic pass before implementation. The cheapest moment to catch a bad plan is
before any code is written. Agents (and humans) are biased toward action — this skill
exists to surface blind spots first.

**Output: a refined plan document. Zero code.**

The natural place to invoke this is right before `/create-feature` Phase 1 — drop the
output plan into Phase 1 and you're already most of the way through requirements.

---

## Phase 1: Capture

User states idea. Agent paraphrases back in 2–4 bullets.
No questions yet — just confirm the agent heard correctly.

---

## Phase 2: Self-Research (mandatory — never skip)

Resolve every FACT from the codebase **before** asking the user anything.

| Question type | Resolution |
|---|---|
| "Does X exist?" | grep the codebase |
| "What's the current pattern?" | read the relevant guide |
| "How does subsystem Y work?" | read `.claude/guides/` |
| "What files would change?" | grep + read |

**User time is reserved exclusively for: intent, preferences, trade-offs, scope.**
Everything else is the agent's job.

Each question must be prefaced with what was researched:

```
**Researched**: Checked engine-performance.md and RepositoryCache.java:45 —
caching uses cacheBy*/getBy*. Mechanism is solved; open question is *what* to cache.

**Question**: ...
```

This lets the user correct false premises before answering, and proves the agent isn't
wasting their time with things a grep could answer.

---

## Phase 3: Iterative Challenge Loop

**ONE focused question per turn. Not a questionnaire.**

After each answer:
1. Update internal "Plan State" — what the agent now thinks will be built
2. Mark which dimension was covered
3. Pick next dimension with highest remaining value given what's still uncovered
4. Show the running plan state every 2–3 turns so the user can catch drift early

### Challenge dimension catalogue (~15 dimensions, pick by expected value):

| Dimension | Probe |
|---|---|
| Problem framing | Is the problem defined, or only the solution? |
| Necessity | What happens if we do nothing? |
| Root cause | Is this change at the source or a symptom/downstream? |
| Existing solutions | Has this been partially built somewhere in the codebase? |
| Alternatives | What's the simplest version that delivers value? |
| Scope | What's explicitly out of scope? What's deferred? |
| Edge cases | Empty/null/zero, concurrent callers, multi-tenant, pre-existing data, partial failure |
| Failure modes | What breaks if this goes wrong? Who notices? |
| Generality | Is this solving the specific case or the general pattern? |
| Performance | Any N+1, unbounded loops, or missing caching implications? |
| Security | Auth, input validation, data exposure risks |
| Maintainability | Who owns this going forward? How does it get tested? |
| Testing | What's the verification plan? What's hard to test? |
| Migration | What happens to existing data/state? Is rollback possible? |
| Dependencies | What does this change depend on? What depends on it? |

**Highest-value dimensions to probe early** (these tend to reframe rather than refine):
- Problem framing
- Necessity
- Root cause
- Existing solutions

If the plan survives those four, the remaining dimensions refine rather than reframe.

---

## Phase 4: Convergence

Stop when:
- User says "stop" or "wrap up"
- All high-value dimensions are covered
- Next questions would be low-value

**Never end silently.** When the agent thinks it's done:
> "I think we've covered the major dimensions. The remaining open thread is `<dimension>`.
> Want to keep going on that, or wrap up?"

The user can say "stop" at any point and get the refined plan from whatever state is current —
partial sections are fine.

---

## Phase 5: Refined Plan Output

Produce a single structured document:

```markdown
## Problem Statement
[What problem this actually solves, not just what will be built]

## Chosen Approach
[The selected approach and why]

## Alternatives Considered
[What was rejected and why]

## Scope
**In:** [what's included]
**Deferred:** [what's explicitly out of scope for now]

## Risks & Mitigations
[Each risk with its mitigation]

## Verification Plan
[How we'll know this works]

## Files Likely to Change
[repo-relative paths]
```

The transcript is throwaway. The plan is the deliverable.
Drop it into `/create-feature` Phase 1 to fast-track requirements.

---

## Anti-patterns

| Don't | Why |
|---|---|
| Ask the user something a grep could answer | Burns trust; trains them to ignore questions |
| Dump four questions in one turn | Kills iteration; agent loses the thread of which answer steered what |
| Lead the witness ("don't you think X is wrong?") | Pressure-test means probe, not push |
| End silently when you think you're done | Always offer to wrap up; user should never be guessing the state |
| Edit code in this skill | Output is the refined plan only — no implementation |
| Re-ask a dimension already covered | Track coverage; the catalogue exists so you don't loop |