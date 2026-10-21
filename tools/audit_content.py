"""Validate the shipped pack and write a factual coverage report."""
import json
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parent.parent
questions = json.loads((root / 'content/questions.json').read_text(encoding='utf-8'))
translations = json.loads((root / 'content/arabic-options.json').read_text(encoding='utf-8'))
assert len({q['Id'] for q in questions}) == len(questions)
for q in questions:
    assert all(q.get(key) for key in ['Id', 'Topic', 'Kind', 'Category', 'PromptEn', 'PromptAr', 'Answer', 'ExplainEn', 'ExplainAr']), q['Id']
    assert q['Difficulty'] in [0, 1, 2], q['Id']
    if q.get('Options') and q['Kind'] != 'order':
        assert len(set(q['Options'])) == len(q['Options']) and q['Answer'] in q['Options'], q['Id']
    if q['Topic'] == 'general' or q['Category'] == 'logic':
        assert all(option.isnumeric() or option in translations for option in q['Options']), q['Id']
    if q['Kind'] == 'search':
        assert 1 <= len(q['Answer']) <= 8 and q['Answer'].isascii() and q['Answer'].isalpha(), q['Id']
    if q['Kind'] == 'order':
        assert Counter(q['Options']) == Counter(q['Answer'].split()), q['Id']
    if q['Kind'] == 'pairs':
        pairs = [p.split('|') for p in q['Pairs']]
        assert len(pairs) == q['Difficulty'] + 4, q['Id']
        assert len({p[0] for p in pairs}) == len(pairs) == len({p[1] for p in pairs}), q['Id']
    if q['Kind'] == 'image':
        assert q['PromptEn'] == 'What do you see?', q['Id']

lines = ['# Content coverage — v0.3', '', f'{len(questions)} bilingual records, including {sum("-v3-" in q["Id"] for q in questions)} additions to the original 153-record pack. Arithmetic is generated separately at runtime.', '', 'Counts are records, not distinct lessons. Vocabulary appears across spelling, listening and matching; some original facts and logic sets recur across levels. Memory records share rules while layouts shuffle.', '', '| Topic | Records |', '| --- | ---: |']
for topic, count in Counter(q['Topic'] for q in questions).items():
    lines.append(f'| {topic} | {count} |')
lines += ['', '| Format | Records |', '| --- | ---: |']
for kind, count in Counter(q['Kind'] for q in questions).items():
    lines.append(f'| {kind} | {count} |')
lines += ['', '| Topic / category | Easy | Medium | Hard |', '| --- | ---: | ---: | ---: |']
for topic, category in sorted({(q['Topic'], q['Category']) for q in questions}):
    counts = [sum(q['Topic'] == topic and q['Category'] == category and q['Difficulty'] == level for q in questions) for level in range(3)]
    lines.append(f'| {topic} / {category} | ' + ' | '.join(map(str, counts)) + ' |')
lines += ['', '## Adding content', '', 'Keep existing IDs stable. New authored records must specify Topic, Kind, Difficulty and Category; the Play page discovers available categories from these records for the selected topic and difficulty. Add an English/Arabic display label in MainWindow.CategoryName for a new category. A new interaction kind also needs a renderer, answer rules, study presentation, timer policy and native regression coverage. New procedural maths categories belong in QuestionCatalog and MathQuestion together.', '', 'Run `python tools/make_content.py`, then `python tools/audit_content.py`, rebuild, and run both native checks. Review answer ambiguity and Arabic wording with a fluent reader before considering the pack a finished curriculum.', '', '## Source checks', '', 'The pack is locally authored, using original wording. Sample astronomy checks: [NASA Mercury facts](https://science.nasa.gov/mercury/facts/) and [NASA Mars facts](https://science.nasa.gov/mars/facts/). These sources were checked for the shortest-year and Red Planet facts; this is not a claim that every record has received external editorial review.', '']
(root / 'CONTENT.md').write_text('\n'.join(lines), encoding='utf-8')
print(f'PASS: {len(questions)} records; IDs, bilingual fields, categories, choices, Arabic options, search bounds, ordered-token multisets and word-pair uniqueness.')
