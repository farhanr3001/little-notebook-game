# Content coverage — v0.3

675 bilingual records, including 522 additions to the original 153-record pack. Arithmetic is generated separately at runtime.

Counts are records, not distinct lessons. Vocabulary appears across spelling, listening and matching; some original facts and logic sets recur across levels. Memory records share rules while layouts shuffle.

| Topic | Records |
| --- | ---: |
| english | 384 |
| general | 99 |
| puzzles | 192 |

| Format | Records |
| --- | ---: |
| image | 38 |
| spell | 99 |
| choice | 270 |
| order | 42 |
| listen | 99 |
| search | 90 |
| memory | 18 |
| pairs | 19 |

| Topic / category | Easy | Medium | Hard |
| --- | ---: | ---: | ---: |
| english / listening | 33 | 33 | 33 |
| english / sentences | 30 | 30 | 30 |
| english / spelling | 46 | 46 | 46 |
| english / vocabulary | 16 | 15 | 7 |
| english / word-pairs | 8 | 6 | 5 |
| general / everyday | 10 | 7 | 6 |
| general / nature | 8 | 7 | 7 |
| general / science | 10 | 13 | 14 |
| general / world | 5 | 6 | 6 |
| puzzles / logic | 6 | 6 | 6 |
| puzzles / memory | 6 | 6 | 6 |
| puzzles / patterns | 22 | 22 | 22 |
| puzzles / word-search | 30 | 30 | 30 |

## Adding content

Keep existing IDs stable. New authored records must specify Topic, Kind, Difficulty and Category; the Play page discovers available categories from these records for the selected topic and difficulty. Add an English/Arabic display label in MainWindow.CategoryName for a new category. A new interaction kind also needs a renderer, answer rules, study presentation, timer policy and native regression coverage. New procedural maths categories belong in QuestionCatalog and MathQuestion together.

Run `python tools/make_content.py`, then `python tools/audit_content.py`, rebuild, and run both native checks. Review answer ambiguity and Arabic wording with a fluent reader before considering the pack a finished curriculum.

## Source checks

The pack is locally authored, using original wording. Sample astronomy checks: [NASA Mercury facts](https://science.nasa.gov/mercury/facts/) and [NASA Mars facts](https://science.nasa.gov/mars/facts/). These sources were checked for the shortest-year and Red Planet facts; this is not a claim that every record has received external editorial review.
