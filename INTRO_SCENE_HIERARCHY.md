# Intro Scene Hierarchy - Copy This Structure

## Complete Hierarchy for Intro.unity

```
Intro
├─ Main Camera
│  └─ Position: (0, 0, -10)
│  └─ Background Color: Black
│
├─ ShedBackground (Sprite)
│  ├─ Position: (0, 1, 1)
│  ├─ Scale: (20, 15, 1)
│  ├─ Order in Layer: -10
│  ├─ SpriteRenderer
│  │  └─ Sprite: Your shed interior image
│  │  └─ Color: (0.1, 0.1, 0.1, 1) or brown
│  └─ [No physics components needed]
│
├─ Player [Your character from farm]
│  ├─ Position: (0, -1, 0)
│  ├─ CharacterController2D ✓
│  ├─ Rigidbody2D ✓
│  ├─ SpriteRenderer ✓ (your character sprite)
│  ├─ BoxCollider2D ✓
│  └─ Animator ✓
│
├─ Lantern (Interactive)
│  ├─ Position: (2, 0.5, 0)
│  ├─ SpriteRenderer
│  │  └─ Sprite: Lantern image
│  │  └─ Color: (0.6, 0.6, 0.6, 1)
│  ├─ BoxCollider2D (Is Trigger ✓)
│  ├─ Light 2D (Spot type, DISABLED ✓)
│  │  └─ Intensity: 1.8
│  │  └─ Outer Radius: 6
│  │  └─ Color: (1, 0.8, 0.4)
│  ├─ ImprovedLanternController ✓
│  └─ LightAnchor (auto-created by script)
│     └─ Light 2D (child, attached by script)
│
├─ Note (Interactable)
│  ├─ Position: (1.5, -2, 0)
│  ├─ SpriteRenderer
│  │  └─ Sprite: Note/letter image
│  │  └─ Color: (1, 1, 1, 0.3)
│  ├─ BoxCollider2D (Is Trigger ✓)
│  └─ NoteInteraction ✓
│     └─ Letter Panel ref: LetterUIDocument
│
├─ EventSystem (auto-created if missing)
│
├─ NarrativeCanvas (UI Panel)
│  ├─ UIDocument component
│  ├─ CanvasGroup (for fade control)
│  ├─ IntroNarrativeController ✓
│  ├─ Child: NarrativeText (TextMeshProUGUI)
│  │  └─ Position: Top center
│  │  └─ Alignment: Center
│  │  └─ Font Size: 36
│  └─ Child: HintText (TextMeshProUGUI)
│     └─ Position: Bottom center
│     └─ Alignment: Center
│     └─ Font Size: 24
│
├─ LetterUIDocument (UI Panel)
│  ├─ UIDocument component
│  └─ LetterPanel ✓
│     └─ Auto-creates letter UI at runtime
│
└─ IntroSequenceManager (Master Controller)
   ├─ Position: (0, 0, 0)
   ├─ IntroSequenceManager ✓
   ├─ Player Controller ref: Player
   ├─ Lantern ref: Lantern
   ├─ Note ref: Note
   └─ Narrative Controller ref: NarrativeCanvas
```

---

## Quick Creation Checklist

**Root Level Objects (in order):**

- [ ] Main Camera (already exists, configure)
- [ ] ShedBackground (sprite for visual)
- [ ] Player (use existing from farm)
- [ ] Lantern (interactive story object)
- [ ] Note (readable letter)
- [ ] NarrativeCanvas (UI for text)
- [ ] LetterUIDocument (UI for parchment letter)
- [ ] IntroSequenceManager (controller)

**Key: Components Must Be Added**

- [ ] Player: CharacterController2D, Rigidbody2D
- [ ] Lantern: SpriteRenderer, BoxCollider2D (trigger), Light 2D, ImprovedLanternController
- [ ] Note: SpriteRenderer, BoxCollider2D (trigger), NoteInteraction
- [ ] NarrativeCanvas: IntroNarrativeController, CanvasGroup
- [ ] LetterUIDocument: LetterPanel
- [ ] IntroSequenceManager: IntroSequenceManager

---

## Component Reference Diagram

```
IntroSequenceManager
    ├── references --> Player (CharacterController2D)
    ├── references --> Lantern (ImprovedLanternController)
    ├── references --> Note (NoteInteraction)
    └── references --> NarrativeCanvas (IntroNarrativeController)

Lantern
    ├── ImprovedLanternController
    │   ├── references --> Light 2D
    │   └── references --> SpriteRenderer
    └── creates --> LightAnchor
        └── parent --> Light 2D

Note
    ├── NoteInteraction
    │   └── references --> LetterUIDocument/LetterPanel
    └── SpriteRenderer
        └── pulses when player nearby

NarrativeCanvas
    ├── IntroNarrativeController
    ├── NarrativeText (TextMeshPro)
    └── HintText (TextMeshPro)

LetterUIDocument
    └── LetterPanel
        └── creates UI at runtime (parchment letter)
```

---

## Sorting & Z-Order

| Object         | Sorting Layer | Order in Layer | Z Position |
| -------------- | ------------- | -------------- | ---------- |
| ShedBackground | Default       | -10            | 1          |
| Player         | Default       | 0              | 0          |
| Lantern        | Default       | 0              | 0          |
| Note           | Default       | 0              | 0          |

---

## Essential Inspector Values at a Glance

### Player

```
Position: (0, -1, 0)
Scale: (1, 1, 1)
CharacterController2D: enabled
Rigidbody2D: Body Type: Dynamic, Gravity: 0, Freeze Rotation Z
```

### Lantern

```
Position: (2, 0.5, 0)
SpriteRenderer Color: (0.6, 0.6, 0.6, 1)
Light 2D: DISABLED (script enables it)
ImprovedLanternController:
  - Lantern Light: [Light 2D from this GameObject]
  - Lantern Sprite: [SpriteRenderer from this GameObject]
  - Pickup Distance: 2
  - Directional Light Arc Angle: 120
```

### Note

```
Position: (1.5, -2, 0)
SpriteRenderer Color: (1, 1, 1, 0.3)
BoxCollider2D: Is Trigger ✓
NoteInteraction:
  - Discovery Distance: 3
  - Letter Panel: [LetterUIDocument]
```

### NarrativeCanvas

```
IntroNarrativeController:
  - Narrative Text: [NarrativeText child]
  - Hint Text: [HintText child]
  - Typewriter Speed: 0.05
  - Fade In Duration: 0.5
```

### IntroSequenceManager

```
Player Controller: [Player GameObject]
Lantern: [Lantern GameObject]
Note: [Note GameObject]
Narrative Controller: [NarrativeCanvas GameObject]
```

---

## Prefab Note

If you create reusable prefabs:

- **LanternPrefab** (Lantern + all components)
- **NotePrefab** (Note + all components)
- Can be dragged into scene and auto-configured

---

This hierarchy is the exact structure you need. Follow it step-by-step for quickest setup!
