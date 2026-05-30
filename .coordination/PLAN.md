# Plan: Bootstrap knowledge base (.claude/rules/ + .claude/guides/ + CLAUDE.md)

Base branch: dev
Mode: local
Created: 2026-05-30T15:29:01Z

## Goal

Bootstrap a dense, scannable, codebase-specific knowledge base that future agent
sessions load on every run:
- `.claude/rules/*.md` — "how to write code" — WRONG/CORRECT pairs, table-heavy, <500 lines each
- `.claude/guides/*.md` — "how the system works" — architecture, entities, data flow, gotchas
- `CLAUDE.md` at repo root — index of both

This is an ASP.NET Core 8 microservices backend (11 services) behind an Nginx gateway,
with a React 19 SPA frontend. Scope of rules/guides is the **backend**; frontend is
already covered by the existing root CLAUDE.md and is out of scope except where the
gateway contract touches it.

## Tasks

| #   | Name                          | Type     | Dependencies      |
|-----|-------------------------------|----------|-------------------|
| 001 | inventory-scan-report         | research | —                 |
| 002 | rule-controllers-and-errors   | code     | 001               |
| 003 | rule-repositories-and-dtos    | code     | 001               |
| 004 | rule-program-bootstrap        | code     | 001               |
| 005 | rule-inter-service-calls      | code     | 001               |
| 006 | rule-auth-jwt                 | code     | 001               |
| 007 | rule-gateway-routing          | code     | 001               |
| 008 | guide-architecture-overview   | both     | 001               |
| 009 | guide-request-and-auth-flow   | both     | 001               |
| 010 | guide-service-catalog         | both     | 001               |
| 011 | guide-cross-service-contracts | both     | 001               |
| 012 | index-claude-md               | code     | 002,003,004,005,006,007,008,009,010,011 |

001 is the **human-review gate**: it produces `.coordination/SCAN_REPORT.md`, which a
human reviews before tasks 002–011 fan out.

## Acceptance criteria

- [ ] `.coordination/SCAN_REPORT.md` exists and covers every section listed in task 001.
- [ ] 6 rule files exist under `.claude/rules/`, each <500 lines, table/WRONG-CORRECT heavy,
      each backed by 3+ real grep occurrences cited in the task's Findings.
- [ ] 4 guide files exist under `.claude/guides/`, each listing the files it actually read.
- [ ] `CLAUDE.md` indexes all rules + guides with one-line scopes, an architecture table,
      build/run commands, and a hard-constraints section.
- [ ] No invented patterns. Every rule traces to real `file:line` evidence in THIS repo.
- [ ] Repo-relative paths only; real class/method/file names, no generic placeholders.

## Hard constraints (apply to every task)

- DO NOT INVENT rules — 3+ real occurrences required, grep evidence mandatory in Findings.
- Describe only what EXISTS, never what the system "should" do.
- Skip `obj/`, `bin/`, `node_modules/`, migrations `*.Designer.cs`/`*ModelSnapshot.cs`,
  lockfiles, binaries.
- Local mode — NO pushes during task work.
- The existing root `CLAUDE.md` is the starting point for task 012; extend/replace it,
  don't duplicate the frontend detail it already has.
