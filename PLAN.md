# Little Notebook — execution and expansion plan

## Product direction

A calm personal learning game for an Arabic-speaking Moroccan adult learning English. It should feel like opening a handwritten notebook on a cosy desk. Learning is the purpose; hearts, points, timers, stickers, and puzzles add variety without creating pressure. Start with a polished reviewable vertical slice, then expand the curriculum after the owner's review.

Accepted UI direction:

- Warm cream textured ruled paper; soft matcha green by default; navy ink; restrained gold doodles; no default pink.
- Native Windows desktop, no Electron or web runtime.
- English first for the owner's testing, seamless Arabic selection retained. Use clear Modern Standard Arabic. Mirror the shell for Arabic, with navigation and radio on the right and directional hide/show controls.
- Quiet home page: compact topic choices and one clear Play action. Avoid large information panels and dashboard clutter.
- Hide/show navigation and recenter the notebook. Keep radio available at a smaller corner size when navigation is hidden.
- Boy/Girl character filter, full-body characters, browse arrows, thumbnails, name entry with the same English handwriting style.
- Adjustable text and button sizes, shown in a live preview before applying. Clamped ranges prevent overlap.
- A textured radio matching each chosen palette, centred playback symbols, slim drawn volume track with a gold knob, mute action, gentle floating notes. Fixed artwork proportions protect alignment as other controls change size.
- Use direct interface wording: Math, English, Puzzles, General Knowledge, Easy, Medium, Hard. Keep the visual theme out of functional names. No decorative green tape swatches. Use a drawn timer switch, not a system checkbox.
- Hard has exactly one life; Easy and Medium have three unless the pre-game 1 life toggle is enabled. Put the timer, life override and question-count picker together on the Play screen, with a live summary.
- Retain the original youthful illustration style for all characters. Add skin-tone and hairstyle variety without switching to older or heavier adult designs. The final roster has six female and four male characters. Future outfits should use varied colours independent of the matcha UI theme; do not dress everyone in green.

## Native foundation and architecture

The review build uses WPF and .NET Framework 4.8, available on the target Windows machine. WPF provides native text layout, Arabic shaping, ink input, media playback, accessible control roles, and resolution-independent graphics. No downloaded runtime was needed for this build.

Implementation boundaries:

| File / directory | Responsibility |
| --- | --- |
| `src/Core.cs` | Profile/settings models; atomic JSON save and backup recovery; question bank; generated arithmetic; round state, score, hearts, hints |
| `src/MainWindow.cs` | Desktop shell, notebook navigation, character setup, topic page, radio controls, settings, pause/exit |
| `src/GameView.cs` | Question renderers, answer interactions, feedback, working paper, results |
| `src/NotebookPages.cs` | Study, latest-mistake review, progress statistics |
| `src/DrawingNotebook.cs`, `src/Doodles.cs` | Smooth native ink, saved pages, undo/redo, stamps, shapes and tracing |
| `src/CharacterEditor.cs`, `src/ProfileFeatures.cs` | Character edit/reset, reward catalogue and profile migrations |
| `src/RewardsShop.cs`, `src/PaperControls.cs` | Categorized scrollable shop, pet previews and palette scrollbars |
| `src/QuestionCatalog.cs`, `src/WordPairs.cs` | Content-driven filters and bilingual matching activity |
| `src/Appearance.cs` | Native drawn sliders and live size preview |
| `src/Art.cs` | Theme palette, paper texture, desk/doodles, atlas character sources |
| `src/Audio.cs` | Independent music, feedback, and installed English speech voice |
| `content/` | Bilingual authored question records and translated general-answer options |
| `assets/` | Generated illustrations, artist-provided lo-fi tracks, and sampled piano cues |
| `tools/` | Reproducible question-pack and audio creation scripts |

The current screen implementation uses partial classes and a common question model. Before adding several more game families, replace the `Kind` rendering switch with an `IQuestionActivity` registry and separate screen presenters. Keep score/answer rules in the core and layout/audio in presentation. This is the next refactor, not an existing plugin system.

Proposed future activity interface: `CreateView(context)`, `RestoreState(state)`, `Pause()`, `GetAnswer()`, `GetHint(level)`, `Explain(result)`, `Dispose()`. Each question provider should return validated, versioned question data with a stable content identity separate from the generated instance identity.

Microsoft references used for the native choices: [WPF bidirectional text](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/bidirectional-features-in-wpf-overview), [InkCanvas](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.inkcanvas?view=netframework-4.7.2).

## Phase 1 — reviewable game (implemented)

### Theme and shell

Native resizable window; paper/desk layout; matcha, rose, sky, lavender and dark palettes; ten full-body characters; Boy/Girl filter; handwritten English name field; collapsible navigation mirrored to the right for Arabic; Game settings below Shop; pause/resume/exit; independent text/button size preview. Night is an optional charcoal/black accent palette. Like Rose and the other selections, it preserves the cream paper; selected buttons have light text and the surrounding desk uses warm neutral grey. Botanical leaves stay natural green in Night. The original Matcha radio is preserved. Separate recoloured radio artwork follows Rose, Sky, Lavender and charcoal Night, keeping cream speaker faces, green botanical details in Night, and the control geometry.

### Play loop

Choose one of four subjects or mixed play. Three difficulty selections. Choose 5, 10, 15, or All questions beside the timer on the Play screen. Every round has newly shuffled questions and answer choices. All questions continues generating/selecting questions until lives run out; authored question pools reshuffle after exhaustion, while arithmetic is procedural. Easy and Medium begin with three lives or the chosen one-life override; Hard always begins with one life. All levels begin with three hints. Each incorrect/expired question costs one heart; losing all lives ends the round. Each question can be submitted once. All answers show selection/correctness feedback for 1.35 seconds, then automatically advance to the next question or final results. There is no manual Next question button. Pauses and inactive windows suspend the feedback countdown; detailed explanations remain available in study/review.

Ordinary timers: 55 / 40 / 30 seconds by difficulty. Search, memory, and sentence activities: 100 / 80 / 60 seconds. No-timer play is available. Pause menus, losing window focus, and opening working paper suspend the clock. There is no speed bonus, so relaxed play can earn the same base rewards.

Scoring: correct answer earns `10 × (difficulty + 1)`, or `6 × (difficulty + 1)` if a hint was used; streak bonus adds up to 10 points. Correct answers award three stars, or one with a hint. Incorrect answers award no points/stars. Stars purchase stickers, pets and notebook stamps. Repeated submissions cannot duplicate rewards.

### Activity coverage

| Family | Playable now | Expansion |
| --- | --- | --- |
| Arithmetic | Add, subtract, multiply, exact divide, word problems; negative results and operation order at Hard | Fractions, decimals, percentages, time, money, estimation |
| Vocabulary | Identify drawn objects, English answers from Arabic prompts | Illustrated scene vocabulary, actions, adjectives, categories, opposites |
| Spelling | Type an English word; choose correct spelling | Listen-and-type, missing letters, syllables, word building |
| Grammar | Fill a sentence blank; order sentence tokens | Articles, prepositions, tense progression, error correction, dialogue turns |
| Listening | Replay an English word and select it | Short sentences, minimal pairs, dictation, voice variety |
| Puzzles | Memory pairs, word search, number patterns, odd-one-out logic | Logic grids, hidden objects, paths, sorting, spatial puzzles |
| General knowledge | Science, nature, world and everyday facts, with Arabic options | Curated source-reviewed packs and deeper difficulty progression |
| Word pairs | Match 4/5/6 English words to Arabic meanings; study answers and wrong-match feedback | Picture matching, definitions and sentence matching |

### Learning and persistence

Study mode reveals answers without lives or timers; maths has six explanatory mini-lessons. Incorrect latest attempts appear in a review notebook. Profile shows attempted questions, accuracy, active play time, completed rounds, best answer streak, average response time, hints, study reveals, topic accuracy, and format-level results. Individual attempts and session records preserve further analysis potential.

One local profile, offline runtime. Settings, character, stars, purchases and answers persist. Saves fall back from LocalAppData to data/profile.json beside the executable when the primary location cannot be written. Existing portable saves are loaded on restart. An interrupted session records the answered portion when explicitly left. The current unfinished round is not restorable after restarting; this is documented rather than pretending autosave includes in-progress activity state.

### Content limits at this review stage

The v0.3 pack contains 675 bilingual records, 522 more than the original 153. See CONTENT.md for counts by format/category/difficulty. Maths changes ranges and complexity. Added English vocabulary, spelling and grammar are differentiated by level; new general facts also progress in complexity. Original items and logic sets still recur across levels, and vocabulary recurs across formats. Do not present record counts as distinct lessons. Fluent Arabic editorial review and concept-level repeat control remain unfinished.

Microphone pronunciation assessment is not in this build. Spoken examples/listening are implemented through the Windows voice. The bank replaces listening play prompts with a written vocabulary form when a suitable voice is unavailable.

## Further learning expansion — discuss after v0.3 review

1. **Apply review feedback first.** Review atmosphere, spacing, radio, characters, hint usefulness, timer comfort, settings accessibility and mouse/keyboard behaviour. Preserve the user's specific corrections in this document.
2. **Refactor for expansion.** Introduce activity/provider registries and localised string keys. Add explicit schema version and canonical content IDs, retaining save migration and old records. Avoid repeatedly copying the same fact into each level.
3. **Build a reviewed curriculum.** Aim initially for 250–400 genuinely distinct English items, 80–120 general items, and procedural maths/puzzle generators. Quantity is subordinate to correctness and useful explanations. Separate authored facts, reusable generators, and activity variants in reporting.
4. **English progression.** Pre-A1/A1 everyday nouns and short phrases; A1/A2 articles/plurals/present and past; later A2/B1 comparison, tense contrast, short reading and dialogues. Include Morocco-relevant everyday contexts without assuming religious, cultural, or dialect preferences.
5. **Mathematics progression.** Easy: number bonds, small totals, positive subtraction, basic times tables. Medium: two-digit work, division, missing operands, practical money/time. Hard: negative numbers, order of operations, fractions/decimals/percentages and multistep reasoning. Validate generators independently, with exact expected values and plausible nonduplicate distractors.
6. **Better puzzles.** First add a visual hidden-object desk scene, a simple path/maze activity, letter tiles, and categorical sorting. Next consider nonograms, Sudoku variants and constraint logic. Store puzzle solutions and verify unique solutions where required. All puzzles need keyboard access, hints and a study explanation.
7. **Spaced review.** Track mastery per concept independently of score. Revisit recent mistakes sooner; introduce new material slowly; space correct recalls over days. Prevent same-concept repetition in a short session and use a clear fallback when a pool is exhausted. A wrong response should produce a teaching moment, not simply harder work.
8. **Stats that guide learning.** Add a per-day activity history, mastery per skill, recent trend, hinted/unhinted accuracy, timeout rate, and previous-session comparisons. Keep the default profile calm; put detailed statistics behind secondary pages. Daily goals should be optional and forgiving.
9. **Bilingual editorial review.** Review every prompt, hint, explanation and mixed Arabic/English sentence with a fluent reader. Verify Arabic accessibility names, punctuation, numbers, focus sequence, and maximum text size across all screens.
10. **Regression and delivery.** Validate content schemas, duplicates, answer ambiguity, no unsolvable generated puzzles, scoring/persistence, pause timing, audio focus, themes, minimum window size and size extremes. Deliver an updated portable Windows build and a short change log.

Phase 2 acceptance: truly distinct difficulty content, reviewed answers/explanations, repeat-control by concept, at least two additional activity families, a working review schedule, migrated saves, and successful English/Arabic regression checks.

## Phase 3 — richer learning and personalisation

- Multiple local profiles and optional export/import, with recoverable save migrations.
- Resumable activities and sessions; bounded history storage with retained aggregates.
- More full-body characters, clothing/accessories, notebook covers, music collections and decorative stickers. Equal visual care for Boy and Girl choices.
- Better working paper: adjustable pencil width, redo, selective object move, grid/dot paper, page expansion, and optional per-question retained notes. Current eraser removes whole strokes; decide whether pixel erasing is worth its added complexity.
- Mini-lessons with worked examples before practice; a guided review queue instead of treating all study cards equally.
- Reduced-motion, screen-reader and keyboard review of every custom activity. Independent narration/music/effect volumes if the owner wants finer audio control.
- A signed installer and release pipeline if sharing beyond the two intended users. Consider upgrading to supported modern .NET after confirming runtime distribution requirements; keep the WPF native UI.

## Phase 4 — pronunciation and translation (separate decisions)

Pronunciation: start with listen/repeat plus optional local recording and playback. Only add grading after selecting an assessment approach suitable for beginner English, Moroccan accents, and the target machine. Do not treat transcription similarity as reliable pronunciation scoring. Recording must be opt-in with a visible indicator and clear deletion behaviour.

Translation: define an `ITranslationProvider` with local phrasebook fallback and cancellation/timeouts. Evaluate an optional on-device model against machine capacity and actual translation quality. A network provider, if later wanted, should be separately enabled with clear data handling and costs. Keep validated educational answers authored; model output should never silently replace the answer key.

These features are not prerequisites for the cosy offline learning loop, and remain deferred until the owner requests them.

## Review update — 21 October 2026

The v0.2.1 update established endless play, pre-game controls, Arabic navigation/radio, sidebar settings, real music, piano feedback, shuffle/history, portable saves, book icon and centred avatars. These remain in v0.3. Future outfit colours remain independent of the UI palette.

## v0.3 handoff — implemented for this review

The owner authorized character editing/reset, pet rewards, a persistent drawing notebook, more questions and category filters. This work is complete for testing; it does not imply that the entire future curriculum, adaptive schedule or pronunciation roadmap is complete.

- Edit is a draft modal with current name/avatar, Boy/Girl choices, Confirm and Cancel. It keeps progress. Reset is a separate red settings action with an explicit deletion warning; it returns to setup and replaces both save and backup. App preferences remain.
- Reward shop keeps paper cards, adds vertical scrolling and section titles, six stickers, four pet species (three cat colours), pet/avatar preview and three stamp packs. The equipped pet sits at the sidebar avatar's feet. All items use earned stars; no real-money purchases.
- Notebook is below Study. Native ISF ink is serialized per page. Pencil/pen/brush/highlighter use curve fitting; mouse input is resampled with light filtering. Six colours, size slider, stroke eraser, six shapes, nine stamp outlines, undo/redo and eight tracing templates are available. Star/heart stamps are free; other packs come from the shop. Reset notebook warns and clears every page while retaining game progress/rewards, including replacement of the save backup.
- Notebook autosaves after a short 550 ms idle period and flushes before navigation/exit. Undo/redo keeps up to 40 changes while working on a page, clearing when pages change. Tracing is a pale overlay; colouring is manual. Bucket fill, moving objects and export are future options.
- All topics now sits centred above the four mode cards with multiple icons at the same height. Each topic's question-type controls come from Category metadata at the selected level, with All selected initially and preferences saved per topic. Endless replenishment keeps the filter. New authored categories appear automatically; add localized labels when introducing them. Procedural maths categories are registered centrally with their generator.
- Added 522 records, native Word pairs matching, more pictures/search words/grammar/spelling/listening, number patterns, odd-one-out logic, memory families and general facts; maths adds word problems. Picture prompts say “What do you see?” The original IDs stay stable.

Validation and remaining subjective review are in TESTING.md. Before expanding again, collect feedback on density, drawing feel, pet scale/prices, content quality and category selection. Then refactor activity renderers before adding several more families, and choose the next curriculum scope together.

## Suggested next instruction

v0.3.1 layout correction: selecting All topics removes the entire question-type section and shifts the difficulty, timer, lives, question count, summary and Play controls up together. Individual topics restore the section and its saved filter. This applies to English and Arabic.

v0.3.2 reliability/speech update: the listening completion thread crash was reproduced and fixed; audio ownership, cancellation, shutdown and MediaPlayer reuse were hardened. Broad native stress coverage now completes every authored record in both languages. Question speech adds an explicit speaker button and saved auto-play preference, disabled by default. Offline Kokoro English and Piper Arabic voices run in a private portable helper with cached output. See STRESS_REPORT.md. Use the v0.3.2 folder/ZIP for this review. Pronunciation grading and translation remain deferred.

“Continue Little Notebook from the v0.3.2 handoff in PLAN.md and STRESS_REPORT.md. First fix this feedback: [my notes]. Keep the approved native design, characters/pets, drawing notebook, category filters and offline English/Arabic speech. Then plan the next learning expansion with me before implementing it. Keep pronunciation assessment and model-based translation as separate later milestones.”


## v0.3.3 review update

Added saved windowed/borderless fullscreen settings and F11 switching with window restoration. Moved the avatar edit pencil to the left. Listening now uses typed answers, centred instructions and one automatic playback after an active second, retaining replay and pause protection. Pause-menu Exit game returns to Play after preserving the partial round; OS window close still exits the application. New listening records must use the write-the-word prompt in both languages, retain Speech and Answer, and receive a bundled clip. The shared listening renderer and delay apply automatically. Use the v0.3.3 folder/ZIP for review.

## v0.4.0 delivery

Notebook interaction/cursor/fill/snap/colour and length controls, expanded/imported tracing, full paper design, study snapshots/search/resolve, music disable/restore, selectable Hard lives, multi-category filters, listening focus, pets and fixed hover areas are implemented. The native per-user installer and differential updater introduce the stable distribution. Future releases must preserve product ID/data paths and run relevant checks plus update lifecycle checks when modifying persistence/distribution. Source and releases live at farhanr3001/little-notebook-game. See UPDATES.md and TESTING.md. Curriculum expansion and pronunciation assessment remain separate work.
