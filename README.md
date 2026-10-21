# Little Notebook — v0.4.0

A native Windows learning game and drawing notebook. C# / WPF / .NET Framework 4.8, with offline English and Arabic speech. No Electron, WebView, account or API key.

## Install and play

Download **LittleNotebook-Setup-0.4.0.exe** from the [GitHub release page](https://github.com/farhanr3001/little-notebook-game/releases/latest). The per-user installer creates a Start menu shortcut and offers a desktop shortcut. The game installs per user on 64-bit Windows 10/11. Setup detects .NET Framework 4.8 and the Microsoft Visual C++ x64 runtime; signed offline Microsoft installers are included and run only if missing. Windows may request administrator approval for those shared prerequisites and a restart if required. Python, offline voices and language files are bundled privately; no Python, browser engine, account or API key needs separate installation. Open the installed shortcut for ordinary Windows access to your Pictures and other folders; a development process launched inside a restricted coding sandbox inherits that sandbox's restrictions.

Choose English or Arabic in Game settings. Arabic moves the sidebar and radio to the right. Matcha is the default palette; Night uses charcoal accents, cream paper and green leaves. F11 switches between windowed and borderless fullscreen. Ctrl+B hides the sidebar; Esc opens the pause menu. Exit game in that menu returns to Play; closing the window exits the app.

## Learning and study

Choose one or several topics, difficulty, timer, lives, and 5/10/15/All questions before playing. All topics highlights all four topic cards; switching one off keeps the others selected. Each selected topic has its own question-type filters. Toggling a filter retains the list’s scroll position. All questions continues until lives run out. Hard starts with one life, and its 1 life switch can now be turned off for three lives. Select one or several question types. All highlights every type; switching one off excludes it while retaining the others. Finite rounds balance topics and selected types, with randomized remainder slots. Mixed balances Easy/Medium/Hard (15 questions gives five of each when the filters allow it). Filters take priority if a type only exists at one level. Questions do not repeat within their pool until it is exhausted; earlier correct answers have reduced, nonzero weight and unresolved wrong answers have increased weight. Maths generates new values. Medium/Hard word searches include diagonals and some multi-word boards. Correct letter selections register automatically; found words are crossed out. Board placement varies across the round.

Listening plays its word once after one active second and focuses the handwritten answer box. Type the word and press Enter, or use Check spelling. Replay remains available. Answers briefly show feedback and advance automatically. The question speaker reads supported instructions and visible question text; word-search boards and unavailable voices have no speaker button. Game settings → Auto-play question speech is off by default. English uses Kokoro af_heart; Arabic uses Piper Kareem. Both work offline. First narration can take a few seconds while a voice loads.

Study shows question setups for every format, including word tiles, matching columns and puzzle layouts. New mistakes save their actual question/layout; older mistakes reconstruct the question from the content bank where possible. My mistakes shows a count. Resolve mistake removes it from that review list without deleting statistics. Every study topic has a searchable View all questions list; mistakes have the same searchable list.

There are 675 bilingual authored records plus procedural maths. Concepts recur across formats and levels; see CONTENT.md. Pronunciation grading, translation and the next curriculum expansion remain separate future work.

## Notebook

The cursor tool is first. Pencil, pen, brush and highlighter show their actual tip shape; the eraser removes ink within its displayed size. Shapes and stamps use anchored picture menus; drag to preview and place them. Their idle cursor shows a short instruction, without a floating shape. The menus retain a More button for the larger picker. Bullet and dash tools place their shown mark. Fill colours an enclosed region; fills remain selectable objects. The colour wheel includes brightness, hex input and recent colours.

Drag a text box, type and press Enter to finish. Shift+Enter adds a line; double-click text to edit/append. Bold and size controls apply to selected text. Native handles move/resize text and ink. Area selection offers rectangle or ellipse. Group/Ungroup is available beside the selection and in its right-click menu, changing according to whether the whole selection is grouped. Moving selections shows alignment guides and snaps to the paper centre and other items; hold Alt to move without snapping.

Undo, Redo and Clear retain the selected input tool. Clear includes ink, text, fill and tracing and can be undone. Add paper length adds 600 design units; the adjacent minus removes 600 down to the original 414-unit length, where it is disabled. A shortening that crops content asks first. Each page can extend to 12,000 units and scroll independently. Undo/Redo retains 40 changes on the current page and clears when pages change/restart.

The paper icon opens a full-page preview with zoom. The camera icon opens a normal PNG/JPEG Save dialog and exports the entire page, excluding selection handles. Tracing has 18 outlines including full-body animals and larger scenery. The eye on the paper hides/shows the guide without deleting it. Import tracing converts a photo to grayscale edge outlines; adjust detail/width and edit with pen/eraser before placing. Photo quality and contrast affect the result. Longer guides extend the page automatically.

Game settings → Background paper lets you change colours, rules, margin, spacing, each binding icon (including the pink bow emoji), and imported icons. Draw & decorate opens the notebook tools for a separate background design; saving applies it across the game without replacing notebook pages.

## Characters, rewards and music

The edit pencil is left of the sidebar avatar. Edit character keeps progress and now includes owned pets, colour variants and No pet; Confirm commits, Cancel discards the draft. The shop distinguishes Equipped from a different owned colour's Equip button. Rewards use earned stars, with no real-money payments. Reset character and Reset notebook have separate warnings.

The radio shuffles and advances automatically, with history, mute and volume. Music library allows removing any track, including defaults. Removed music is a separate tab with restore controls. Imported tracks are copied into app data. Button/answer/typing feedback uses piano cues. Hover effects leave button hit areas fixed.

## Saves, updates and uninstall

Installed data lives in `%LOCALAPPDATA%\LittleNotebook`: profile, notebook pages, settings, purchases, music, narration cache and logs. Saves use atomic replacement and a backup. Portable preview copies may retain `data/profile.json` beside their executable. The installer supports importing an existing portable data folder with `/MIGRATEFROM="...\data"`, copying missing files while retaining existing installed saves.

The game checks GitHub releases on opening and offers a new version until it is installed. Game settings → Check for updates reports the result manually. Updates download only components containing changed files, verify SHA-256 hashes and replace/add only files whose contents differ. They do not run the installer again, and do not replace user data. Unchanged files keep their timestamps. The updater backs up replaced files and rolls back on failure. See UPDATES.md.

Uninstall through Windows Installed apps or the Start menu uninstall entry. The uninstaller removes the installed app, update-added managed files, profiles, drawings, purchases, imported music, caches, logs and app registry/shortcuts. Files you export to a chosen folder are ordinary user documents, separate from the installation.

## Build and release

Run `python tools/prepare_speech_runtime.py` once to assemble the private speech runtime, then `./build.ps1 -OutputDirectory 'Little Notebook v0.4.0'`. Existing runtime assets allow `-CodeOnly` for quick code iteration. Relevant native checks: `--revision-test` and `--notebook-test`. Installer lifecycle: `python tools/test_install_update.py` after building the installer. Test profiles are isolated and excluded from releases.

`python tools/build_release.py` makes the clean payload and component packs; compile `installer/LittleNotebook.iss` with Inno Setup 6.7+. Source, build scripts, and release workflow are in this repository. TESTING.md records checks and limits; ASSETS.md records illustration, music, emoji and speech provenance.

On closing the game, audio and timers stop, media handles close, pending speech is cancelled and the owned Python worker is terminated and waited for. Windows kill-on-close job ownership also prevents speech workers from surviving a game crash. Updates intentionally run a short-lived updater after the game closes and reopen the updated game.
