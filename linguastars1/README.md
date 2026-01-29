# LinguaStars v1 — Find & Tap (PRE-K Colors)

This repo contains a **single playable** PyGame scene designed for kids age **4–5**:
**Find & Tap (Colors)** — portrait **9:16** (720×1280) with safe margins.

## Quick start (Windows / macOS / Linux)

1) Install Python 3.10+  
2) Install dependencies:
```bash
pip install -r requirements.txt
```

3) Run:
```bash
python main.py
```

## What you should see
- A mobile-like portrait window (720×1280)
- LinguaGuide (a cute-quirky illustrated guide) at the bottom
- 3 rounds × 3 objects
- Voice-first UX (this v1 uses on-screen “speech bubble” text as a placeholder for audio)
- Gentle correction (no harsh fail)
- Idle hint after ~4.5s (pulse + gentle repeat)

## Project structure
- `main.py` — app entry
- `src/scene_find_tap.py` — explicit state machine (INTRO → ... → END)
- `src/tutor_agent.py` — adaptive round planner (window memory)
- `src/validator.py` — strict curriculum validation (no hallucinated ids/text)
- `curriculum/colors_v1.json` — single source of truth for content
- `style/tokens.json` — “Room and a Half” illustrated style tokens
- `src/telemetry.py` — round results + SceneSummary (printed at the end)

## Notes
- Audio is stubbed for now to keep the game **instant and stable**. Replace bubble text with TTS/audio later.
- Webhook stub exists in `src/webhook.py` (disabled by default).

