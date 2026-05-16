---
title: Thesis Planning Chat
category: sources
tags: [thesis, planning, kickoff]
source_path: raw/gdd/thesis-planning-chat.md
source_type: meeting
date: 2026-05-15
created: 2026-05-16
ingested: 2026-05-16
updated: 2026-05-16
---

# Thesis Planning Chat

## Abstract

Initial planning chat (Vietnamese) between developer and an AI planner before the autonomous overnight batch. Goes through current code-base analysis (Manager-Centric architecture, abuse of `Instantiate`/`Destroy` for enemies + projectiles, `GameObject.Find` in `Update` loops), proposes a 4-phase roadmap (Performance → Gameplay/Upgrades → Online/Shop → Polish/Report), and locks in three early decisions: **UGS for online backend**, **2D Survivor.io reference**, **no Google Mobile Ads**. Ends with a thesis outline (Chương 1 → 4) bound to the 4-phase roadmap.

## Key claims

- [[claims#c-20260516-02]] — Genre = 2D Survivor.io
- [[claims#c-20260516-03]] — No Google Mobile Ads
- [[claims#c-20260516-04]] — UGS chosen for Authentication + Cloud Save

## Pages updated from this source

- [[overview]]
- [[decisions/2d-survivor-genre]]
- [[decisions/ugs-cloud-backend]]
- [[decisions/no-google-ads]]
- [[decisions/object-pooling-priority]]

## Open questions raised

(None new — the planning chat *answered* open questions rather than raising them.)

## Notes

- Vietnamese-language source; key vocabulary: `tối ưu hóa` (optimization), `cộng dồn chỉ số` (stat accumulation), `nâng cấp nhân vật` (character upgrades), `đề cương báo cáo` (report outline).
- The 4-phase roadmap maps to thesis chapters: Performance → Ch.3 design, Online/Shop → Ch.4 implementation, Polish/Report → Ch.5 conclusion.
