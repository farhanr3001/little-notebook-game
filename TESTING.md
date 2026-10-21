# v0.4.0 — focused verification

3,248 changed-feature checks passed in test-output/revision.txt. They include finite topic/type/difficulty quota simulations, weighted history, multi-word/diagonal search generation and auto-completion. They also cover multi-category sampling (English and maths), default/overridden Hard lives, question/layout snapshots and study list/search/resolve, removal/restoration of every music track including an empty library, pet draft/colour labels, retained tools after Undo/Clear, incremental page lengths/default disable, brush pointers, closed-region fill/undo, mixed object selection/grouping, snap guides, custom colour history, isolated background-paper editing/bows, editable photo outlines and update path guards.

The updated notebook-only suite passed 130 checks in test-output/notebook.txt. It covers the drawing/text/selection/page/export features from v0.3.4 with the revised idle cursor behaviour. Native English and Arabic renders were inspected for study tiles/pairs, categories, paper customizations, tracing, settings and notebook.

25 actual installer/update/uninstaller lifecycle checks passed in test-output/install-update.txt, including fresh-install cleanup, byte-identical player data, unchanged asset timestamps, changed/new files, rollback and hash rejection. This test uses a separate product identity and data root under test-output; it must not touch the real profile. Live GitHub update and normal Windows export verification are recorded in the release report after delivery checks.

Offline English/Arabic synthesis completed under the owned speech worker without audio playback. Normal-token notebook PNG export to Windows Pictures was verified and its unique test image removed. Two actual process tests verified worker termination after graceful cleanup and forced owner exit. Microsoft prerequisite downloads passed Authenticode validation; setup detects installed prerequisites before invoking them.

11 shutdown health checks also passed: English/Arabic pending speech cancellation, owned-worker termination, repeated cleanup, and media-file unlock.

No unchanged full question/game stress suite was repeated for this update. Earlier results below are retained as historical evidence, not presented as newly run checks. Imported photo quality and handwritten feel still benefit from owner review; not every hardware/DPI/audio configuration is covered.

# v0.3.3 focused update — 21 October 2026

**1,795 native checks passed** for saved borderless/windowed mode, F11 and restored window size, settings buttons, avatar edit placement, and every listening record in English and Arabic. Listening checks use the actual typed-input and Check spelling button; validate one-second delay, exactly one automatic play, pause/resume, redraw, early manual replay, normalization, wrong spelling/life loss and automatic results. Pause-menu Exit game, cancellation and partial-save return to Play keep the application open. Window close remains the application exit path.

23,276 core assertions and the 675-record structural content audit passed again. Native English/Arabic listening, display and game-settings renders were inspected. The full 1,350-flow stress regression passed 11,974 checks with typed listening. Outputs: test-output/update.txt, tests.txt and stress.txt. Foreground display-mode checks run first; gameplay checks then minimize their isolated test window to prevent the owner's focus changes from triggering the intentional deactivation pause.

# v0.3.2 verification — 21 October 2026

The listening crash was reproduced as a background speech callback accessing WPF audio on the wrong thread and repaired. Final validation passed 23,276 core assertions, 11,974 stress checks with 1,350 complete bilingual question flows, all 93 neural listening clips, 180 rapid replay/cancellations, 131 legacy speech callbacks and 1,625 question-narration checks. Native smoke and radio probes also passed. See STRESS_REPORT.md for scope, evidence, memory findings and limits.

Question speaker controls and the saved auto-play option (off by default) were checked in English and Arabic. Smooth offline English/Arabic synthesis, mixed-language segments, cancellation, shutdown and hidden-answer exclusion passed. Screenshots are test-output/tts-01-english-question.png through tts-04-settings-arabic.png.

v0.3.1 layout update: the native smoke suite passed after hiding the question-type section for All topics and moving all following setup controls up 124 design pixels. Exported renders confirm the compact All topics screen and restored category controls for individual topics; Arabic layout remains supported.

Actual results: test-output/tests.txt, smoke.txt and audio.txt. Screenshots use the native WPF renderer. Test profiles are separate from normal progress and excluded from the release.

## Automated checks

**23,276 core assertions passed.** These cover 6,000 seeded arithmetic examples independently recalculated; choices and exact division; topic/difficulty filtering; shuffled rounds; endless runs beyond 150 questions; life overrides; scoring/hints/timeouts; duplicate-answer prevention; Unicode saves; backups and portable fallback.

Every available question category is tested at every difficulty over 45 draws, including replenishing small pools. An injected future category appears automatically. Reward checks reject insufficient stars, repeat charges and unowned pets. Pet colour, notebook pages and category preferences survive reload. Character reset clears progress and replaces its backup.

The native screen/interaction suite passed:

- Setup, Boy/Girl filters, size extremes, hidden/recentred paper and right-side Arabic navigation.
- Automatic answer/results feedback after 1.35 active seconds, pauses, 21-answer endless sessions.
- Spelling, sentence tokens, search and memory completion into saved results.
- Edit popup draft, cancel and confirm without losing progress.
- Pet purchase/equip, feet placement, preview and scrollable shop sections.
- Native ink serialization/reload, page turning, blank pages, clear/undo, undo/redo, all six shapes and nine stamp outlines, highlighter/eraser configuration, tracing persistence, notebook reset/cancel with retained rewards.
- English/Arabic notebook, Arabic shop at maximum sizes, charcoal accents and green leaves.
- Word pairs at all difficulties, wrong-match life loss, automatic results and study answers; sixteen-card Hard memory.
- StartGame category filtering, reset warnings and confirmed character reset to clean setup.

The content audit passed for **675 records**: unique IDs, bilingual fields/categories, choice membership, Arabic general/logic options, search-grid bounds, ordered-token multisets, pair counts and unique meanings. This is structural validation, not independent human editorial review of every answer.

The Windows audio probe decodes three full tracks and five piano cues, then triggers three real MediaEnded events with muted playback to check automatic shuffle. Playlist tests cover libraries of 1/2/3/8 tracks, repeated cycles and listening history.

## Interactive check

Using the Computer Use skill, the actual v0.3 review app was opened. A mouse drag created a visible smooth pencil stroke. A new blank page was created; returning retained the stroke and showed saved status. The isolated preview closed normally. This verifies input/page retention, not the subjective feel of long handwriting sessions.

The packaged app then loaded the previous portable profile. Name, character, stars, attempt count and session count were all preserved; one blank notebook page was added by migration. Dragging the real shop scrollbar reached the pet/stamp sections. The app was left open on Play for owner review.

Native renders were visually inspected for Play/categories, editing, pets, shop, drawing tools, tracing, Arabic notebook at maximum sizes, word pairs/study, settings and Hard memory. The five radio palettes were checked in the earlier update and remain preserved.

## Owner review

Try category-only and All sessions; fixed/endless lengths; edit/cancel/confirm; both reset warnings; pet purchase/colours/equipping; writing curves with your own mouse; tracing/colouring, shapes, undo/redo and several saved pages. Reopen to check progress. Also try Arabic, palettes, text sizes and radio shuffle.

## Current limits

- Concepts recur across formats/levels. Logic sets repeat across levels; memory layouts shuffle. See CONTENT.md. Fluent Arabic editorial review and a broader curriculum remain later work.
- Erasers remove whole strokes. Notebook Undo/Redo retains 40 changes while working on a page and clears on page changes/restart. Maths Working paper has simpler undo without redo/erase history.
- Tracing uses an outline overlay; colouring uses brush/highlighter. No bucket fill or object moving yet.
- One local profile; no unfinished-round restore, microphone grading or translation model.
- The 1440×900 design scales with the window. Not every DPI, mouse, voice or audio device has been tested. Extended handwriting and subjective audio balance need owner review.
