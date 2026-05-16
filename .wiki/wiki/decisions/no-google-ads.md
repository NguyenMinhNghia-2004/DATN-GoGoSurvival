---
title: Remove Google Mobile Ads
category: decisions
tags: [monetization, scope, cleanup]
sources: [raw/gdd/thesis-planning-chat.md]
created: 2026-05-16
updated: 2026-05-16
---

# Remove Google Mobile Ads

## Decision
**Date**: 2026-05-15
**Decided by**: developer
**Status**: active

### Context

Initial template included Gley `Mobile Ads` plugin + `GoogleMobileAds.Editor` integration. Planning chat asked whether developer had a Google AdMob account → answer was **no**. Carrying the plugin would add compile-time noise without working at runtime.

### Decision

Delete all Gley + GoogleMobileAds artefacts from the project. Confirmed by git commit `55223c1 add all refactor code` which records `D Assets/GleyPlugins/...` and `D Assets/ExternalDependencyManager/...` deletions in bulk.

### Consequences

- Smaller compile surface, fewer csproj entries.
- Thesis Chapter 4 can omit ads-integration boilerplate.
- If monetization becomes a future-work topic, re-introduce Unity LevelPlay or AdMob package fresh rather than restoring Gley.
- `GoogleMobileAds.Editor.csproj` still appears at project root (Unity-generated) but `Assets/` no longer contains GleyPlugins → csproj is stale; will regenerate on next Unity refresh.

---
## Backlinks
- [[overview]]
- [[claims#c-20260516-03]]
- [[sources/thesis-planning-chat]]
