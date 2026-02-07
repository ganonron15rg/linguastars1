# LinguaStars V1 Master Spec (English-Only)

## Product Goals
- Deliver an English-only literacy game focused on foundational reading and writing skills.
- Operate offline-first with an on-device content bundle and progress cache.
- Sync securely to the server with a best-effort queue when connectivity is available.
- Support parents and administrators through dedicated web dashboards.
- Monetize only through cosmetic shop items; no gameplay advantages.

## Offline-First Architecture
- Unity client ships with a full English content bundle and a local SQLite cache.
- All gameplay writes are stored locally first and placed into a sync queue.
- Sync queue attempts upload on app launch, at session end, and every 10 minutes while online.
- Conflict resolution: server accepts client writes if server timestamp is older; otherwise client pulls and merges progress (higher mastery wins).
- Queue never blocks gameplay; failures are retried with exponential backoff and capped retries per item.

## Core Modules
1. Player Onboarding & Profiles
2. Placement & Diagnostic
3. Lesson Runtime (phonics, decoding, vocabulary, writing)
4. Mastery & Review Engine
5. Bridge Challenges (inter-world transitions)
6. Shop & Inventory
7. Parent Dashboard
8. Admin Dashboard
9. Sync & Telemetry

## Screen List (Unity Client)
- Splash / Loading
- Profile Select / Create
- Parent Gate (simple math prompt)
- Placement Test
- World Map (Worlds 1–5)
- Stage Select
- Lesson Screen (core activity)
- Writing Practice (guided tracing + free write)
- Review Session
- Bridge Challenge
- Rewards Summary
- Shop
- Inventory / Dressing Room
- Settings / Accessibility
- Offline Sync Status

## Game Loop
1. Player selects world and stage.
2. Completes 3–5 micro-activities per level.
3. Earns stars and coins.
4. Mastery and review scheduler update locally.
5. Rewards screen shows coins, stars, streaks, and new items.
6. Optional shop visit or return to map.
7. Sync queue posts session data when online.

## Mastery Model
- Each skill has mastery score 0–100.
- Initial diagnostic sets baseline for core phonemes and graphemes.
- Lesson completion increases mastery based on accuracy and speed.
- Review session triggers when mastery decay threshold crossed or after 5 lessons.
- Mastery decay rate: 2 points per day without practice, minimum floor of 40.

## Review System
- Spaced review by skill, not by level.
- Review session pulls 5–8 items across last 3 skills practiced plus 2 weak skills.
- Reviews can occur after every 2 levels or when idle time exceeds 48 hours.

## Bridge Challenges
- End of World 1–4 includes a bridge challenge covering all skills from that world.
- Requires 2 out of 3 stars to unlock the next world.
- Bridge failures trigger focused review path and retry.

## Shop & Economy (Cosmetic Only)
- Coins earned through gameplay and streaks.
- Items include avatar outfits, room decor, and cosmetic effects.
- No power-ups, boosters, or progress gating in the shop.

## Parent Dashboard
- Child profile overview, usage time, mastery by skill, streaks, coin spend, and inventory view.
- Ability to toggle playtime limits and notification preferences.

## Admin Dashboard
- Content authoring overview, world/stage/lesson health metrics, and sync monitoring.
- Ability to manage roles, content versions, and feature flags.

## Hebrew Fallback Fields
- All content objects include `he_title` and `he_prompt` fallback fields for future localization.
- Client uses English fields by default; Hebrew fields are stored but never shown in V1.
