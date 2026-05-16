---
title: Online backend — Unity Gaming Services
category: decisions
tags: [backend, online, ugs]
sources: [raw/gdd/thesis-planning-chat.md]
created: 2026-05-16
updated: 2026-05-16
---

# Online backend — Unity Gaming Services (UGS)

## Decision
**Date**: 2026-05-15
**Decided by**: developer
**Status**: active (not yet integrated in code)

### Context

Thesis spec calls for "tích hợp lưu trữ dữ liệu người chơi trực tuyến và hệ thống cửa hàng" (online player-data storage + shop system). The chapter outline (Chương 2.1) wants a database/data-layer discussion. Three candidate stacks:

1. **Unity Gaming Services (UGS)** — Authentication + Cloud Save SDKs, first-party Unity, anonymous sign-in flow.
2. **Firebase** — Auth + Firestore/Realtime DB, broader feature set, more setup.
3. **Custom backend** — full control, but solo + thesis timeline = too much.

### Options considered

1. **UGS** — pros: pre-wired Unity packages, anonymous Auth covers thesis demo, dashboard already in Unity hub. Cons: Cloud Save quota limits, less material to talk about in Chapter 2 (less "data modeling").
2. **Firebase** — pros: richer database story for the report; cons: extra plugin install, GoogleService-Info.plist setup, account friction.
3. **Custom** — rejected (timeline).

### Decision

**UGS for Authentication + Cloud Save.** Trade richer report content for lower integration cost — the report can still cover NoSQL schema decisions and offline-first sync patterns.

### Consequences

- Need to add `com.unity.services.authentication` + `com.unity.services.cloudsave` packages when integration phase starts.
- Shop currency / equipment inventory will live in UGS Cloud Save (key-value JSON blobs).
- No Firebase plugin in `Packages/` — keep package list lean.
- Anonymous auth is fine for thesis defense; account-linking can be a "future work" item in Chapter 5.

> [!info] Design intent
> Online layer is currently **not wired** — the overnight batch did NOT touch UGS. Game runs fully offline today. UGS integration is a deferred milestone.

---
## Backlinks
- [[overview]]
- [[claims#c-20260516-04]]
- [[sources/thesis-planning-chat]]
