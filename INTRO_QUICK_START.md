# Polished Intro Scene - Quick Start (5 Min Setup)

## 5 New Scripts Created

- IntroSequenceManager
- IntroNarrativeController
- ImprovedLanternController
- NoteInteraction
- LetterPanel

All in: `Assets/Scripts/Systems/`

---

## 30-Second Overview

**What happens:**

1. Player wakes up, frozen
2. Opening narrative with typewriter
3. Lantern flickers to life
4. Player unfrozen, picks up lantern
5. Light follows player's facing direction
6. Finds note, reads inherited farm letter
7. Transitions to FarmScene

---

## Scene Setup (Quick)

### Objects to create in Intro.unity:

1. **IntroSequenceManager** (empty)
   - Add: IntroSequenceManager component

2. **Player** (already exists)
   - Keep as is, verify CharacterController2D exists

3. **Lantern** (new)
   - Sprite, BoxCollider2D (trigger), Light2D (disabled), ImprovedLanternController

4. **Note** (new)
   - Sprite, BoxCollider2D (trigger), NoteInteraction

5. **NarrativeCanvas**
   - Add: IntroNarrativeController, TextMeshPro text children

6. **LetterUIDocument**
   - Add: LetterPanel component

### Positions (example):

- Player: (0, -1, 0)
- Lantern: (2, 0.5, 0)
- Note: (1.5, -2, 0)

---

## Inspector Wiring (5 minutes)

**IntroSequenceManager:**

- Player Controller → Player
- Lantern → Lantern
- Note → Note
- Narrative Controller → NarrativeCanvas

**ImprovedLanternController (on Lantern):**

- Lantern Light → Lantern's Light2D
- Lantern Sprite → Lantern's SpriteRenderer

**NoteInteraction (on Note):**

- Letter Panel → LetterUIDocument
- Note Sprite → Note's SpriteRenderer
- Note Collider → Note's BoxCollider2D

**IntroNarrativeController (on NarrativeCanvas):**

- Narrative Text → NarrativeText TMPro
- Hint Text → HintText TMPro

---

## Key Settings

**Lantern Light2D:**

- Type: Spot
- Intensity: 1.8
- Range: 6
- Color: (1, 0.8, 0.4) warm orange
- **Enabled: UNCHECKED** ← Must be OFF to start

**Global Lighting:**

- Window → Rendering → Lighting
- Global Light 2D Intensity: 0.1-0.15 ← Makes lantern critical

**Typewriter:**

- Speed: 0.05 (lower = slower)

---

## Test Checklist

- [ ] Play intro, player is frozen
- [ ] Opening text types out
- [ ] Lantern flickers 3× then stays on
- [ ] Player can move after lantern
- [ ] Lantern light rotates with player direction
- [ ] Note glows when player approaches
- [ ] Mouse click opens letter
- [ ] Letter looks rustic/parchment
- [ ] Can close letter
- [ ] Scene transitions to FarmScene

---

## Gameplay Flow

```
Start Intro
    ↓
NarrativeManager
    ├─ Show opening (frozen)
    ├─ Lantern activation
    ├─ Unfreeze player
    ├─ Show "Pick up lantern" hint
    ├─ Player picks up lantern
    ├─ Discovery: Note nearby
    ├─ Player reads note (letter UI)
    └─ Back to normal control
    ↓
FarmScene
```

---

## Customize

**Story text:** IntroNarrativeController.cs → openingLines array

**Letter content:** LetterPanel Inspector or LetterPanel.cs → letterContent

**Lantern color:** ImprovedLanternController Inspector → Light color setting

**Hint text:** IntroSequenceManager → StateMovementUnlock() ShowHint call

---

## Troubleshooting Quick Fixes

| Problem                   | Fix                                                    |
| ------------------------- | ------------------------------------------------------ |
| Player walks during intro | Check Player assigned in IntroSequenceManager          |
| No lantern light          | Light2D must be DISABLED to start, check Intensity > 1 |
| Can't pick up lantern     | BoxCollider2D needs "Is Trigger" checked               |
| Letter won't open         | Check LetterPanel assigned in NoteInteraction          |
| Scene too dark            | Increase Global Light 2D Intensity                     |

---

## Files Modified

**GameManager.cs** → NewGame() now loads Intro instead of FarmScene (already done)

**CharacterController.cs** → No changes needed, disable/enable handles freeze

---

## Build Settings

Build Settings must have:

- [0] Intro.unity
- [1] FarmScene.unity

---

**Done!** Follow POLISHED_INTRO_SETUP_GUIDE.md for detailed walkthrough.
