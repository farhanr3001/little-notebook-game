# v0.3.3 follow-up — 21 October 2026

The focused native update suite passes 1,795 checks for borderless/windowed settings and F11 restoration, left avatar edit placement, all listening records using typed spelling in both languages with delayed single playback, replay/pause guards, and returning from Exit game without closing the application. The core suite, native smoke suite, content audit and 11,974-check full stress regression (1,350 bilingual flows) pass again. This extends the v0.3.2 reliability work below; logs and screenshots are in test-output. Tests use isolated profiles. The earlier v0.3.2 measurements below are retained as that release's evidence.

# v0.3.2 reliability and speech report — 21 October 2026

## Listening crash: reproduced and repaired

The old Windows speech completion callback ran on a background thread and changed WPF MediaPlayer volume directly. WPF rejected the cross-thread access and terminated the process. The captured original failure is in test-output/speech-before-fix.txt.

Audio callbacks now marshal to the owning Dispatcher. Completion tracks the current prompt so an older cancelled utterance cannot restore music volume during newer speech. Closing stops timers, cancels speech and disposes audio safely. Unexpected UI and fatal background exceptions are logged; this is diagnostic support, not a substitute for fixing errors.

Repeated neural playback also exposed MediaPlayer resource growth. The app now reuses one player, stops it between clips and closes it on disposal. The final complete stress run observed a peak of 204 MB private memory in the main game process. This figure excludes the optional neural synthesis worker, which loads models on demand and uses additional memory.

## Final checks

All suites use separate test profiles. They do not reset or spend the normal user's progress.

| Suite | Completed coverage | Result |
| --- | --- | --- |
| Core | 23,276 assertions: arithmetic, content, scoring, filters, endless play, hints, saves/recovery, shuffle | PASS |
| Native stress | 11,974 checks; all 675 authored records in both languages, 1,350 complete question flows | PASS, 1m44s |
| Native smoke | Actual screen/button handlers, automatic transitions, sidebar/layout, shop, profiles, notebook, category filters | PASS |
| Legacy speech regression | 131 completion/cancellation callbacks, rapid replay and shutdown | PASS |
| Neural listening | All 93 distinct clips reached actual MediaEnded with system speech disabled; 180 rapid replay/cancel operations | PASS |
| Question narration | 1,625 checks; all records' speech composition, 24 synthesized examples across languages/formats and generated maths | PASS |
| Music/effects | Three full tracks and five cues decoded; three actual track-end events advance shuffled radio | PASS |

Stress coverage includes every answer format (choices, spelling, sentence order, search, memory and pairs), correct/incorrect/timeout handling, duplicate submissions, hints, pausing during feedback, results and partial saves. It checks 120 game setup combinations; all characters/rewards/pet colours; theme/language/size/modal changes; and 25 notebook pages with dense curves, tools, shapes, stamps, traces, undo/redo and reset. Save lock fallback and stale delayed callbacks after navigation are included.

An early stress run hit an automatic-results assertion that did not recur on controlled reruns. The harness now provides round-state context on such a failure and runs minimized without stealing focus. Multiple complete runs subsequently passed; no separate cause for that one assertion has been established.

## Speech behaviour

Listening words use 93 bundled Kokoro af_heart clips. Question instructions use offline Kokoro af_heart for English and Piper Kareem for Arabic. Each question has a speaker button. Game settings has Auto-play question speech, off by default, with persistence. Arabic instructions and English exercise text are voiced in their respective languages. Hidden listening answers and matching solutions are excluded.

Tests cover manual reading, automatic reading, no repeat on redraw/hint/resume, language changes, rapid replacement, cancellation while preparing, answering during speech, saved preference and closing during synthesis/playback. Native English and Arabic question/settings renders were inspected, including maximum text size. During generation the question timer pauses; first use loads the model, then WAVs are cached. A private portable helper supplies speech without an account, network connection or separately installed Python.

## Practical limits and review

Passing tests cannot establish that every machine, audio device or DPI combination is fault-free. Long handwriting feel, subjective voice/music quality and fluent Arabic editorial review still need human review. Arabic voice is Jordanian Arabic, not Moroccan dialect. Microphone assessment and translation remain future work. The optional speech helper adds download size, startup latency on first narration and memory use while loaded.

Logs: test-output/tests.txt, smoke.txt, stress.txt, speech.txt, voice.txt, narration.txt and audio.txt. Normal diagnostic log: data/logs/errors.log beside the executable, with a temporary-directory fallback if needed. The test error log also retains earlier failed diagnostic runs; each successful suite checks its own new errors.

Release verification checks ZIP integrity, required voice models/runtime, exactly 93 listening WAVs and absence of personal/test saves. Use the complete v0.3.2 folder; older executable folders retain their previous behaviour.
