"""Small, curated bilingual review pack. Run with Python 3 to regenerate JSON."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
questions = []
def add(topic, kind, level, prompt, ar, stem, answer, options=None, hint=None, hint_ar=None, explanation=None, explanation_ar=None, **extra):
    questions.append(dict(Id=f'{topic}-{len(questions)+1:03}', Topic=topic, Kind=kind, Difficulty=level,
        PromptEn=prompt, PromptAr=ar, Stem=stem, Answer=answer, Options=options,
        HintEn=hint or 'Look carefully and take it one step at a time.', HintAr=hint_ar or 'انظر جيداً وفكّر خطوة بخطوة.',
        ExplainEn=explanation or f'The answer is {answer}.', ExplainAr=explanation_ar or f'الإجابة هي: {answer}.', **extra))

vocab = [('book','كتاب','book'),('chair','كرسي','chair'),('water','ماء','water'),('apple','تفاحة','apple'),('sun','شمس','sun'),('house','منزل','house')]
for level in range(3):
    for word, arabic, icon in vocab:
        alternatives = [x[0].title() for x in vocab if x[0]!=word][:3]
        add('english','image',level,'What do you see?','ماذا ترى في الصورة؟','',word.title(),[word.title()]+alternatives,
            hint=f'It begins with {word[0].upper()}.',hint_ar=f'تبدأ الكلمة بالحرف {word[0].upper()}.',
            explanation=f'{word.title()} means {arabic}.',explanation_ar=f'{word.title()} تعني {arabic}.',Image=icon)
    for word,arabic in [('window','نافذة'),('friend','صديق'),('beautiful','جميل')]:
        add('english','spell',level,'Write the English word','اكتب الكلمة بالإنجليزية',arabic,word,
            hint=f'{len(word)} letters. Starts with “{word[:2]}”.',hint_ar=f'عدد الحروف {len(word)}. البداية: {word[:2]}.',
            explanation=f'{word} = {arabic}.',explanation_ar=f'{word} تعني {arabic}.')

sentences = [
 [('I ___ a student.','am',['is','are','be'],'Use “am” with “I”.','نستخدم am مع I.'),('She ___ a book.','has',['have','having','do'],'Use “has” with she, he, and it.','نستخدم has مع she و he و it.'),('There are two ___.','cats',['cat','cates','catss'],'Add -s to make the plural of cat.','نضيف s لجمع كلمة cat.'),('I drink ___ every day.','water',['chair','pencil','window'],'Water is something we drink.','الماء شيء نشربه.')],
 [('Yesterday, I ___ to school.','went',['go','goes','going'],'The past tense of go is went.','الماضي من go هو went.'),('She is ___ than me.','taller',['tall','tallest','more tall'],'Use taller to compare two people.','نستخدم taller للمقارنة بين شخصين.'),('We have lived here ___ 2020.','since',['for','at','during'],'Since introduces a starting point in time.','نستخدم since مع نقطة بداية زمنية.'),('There is ___ apple on the table.','an',['a','the two','some'],'Use an before a vowel sound.','نستخدم an قبل صوت متحرك.')],
 [('If I had time, I ___ travel.','would',['will','am','did'],'An imagined situation uses would + verb.','نستخدم would مع الفعل في الحالة الافتراضية.'),('The letter ___ yesterday.','was sent',['is send','sent','has sending'],'The passive past is was/were + past participle.','المبني للمجهول في الماضي: was أو were ثم التصريف الثالث.'),('I look forward to ___ you.','meeting',['meet','met','will meet'],'In this expression, to is followed by an -ing form.','في هذا التعبير يأتي الفعل بعد to بصيغة ing.'),('Neither answer ___ correct.','is',['are','be','were'],'Neither answer is singular.','تعبير neither answer مفرد، لذلك نستخدم is.')]
]
for level,rows in enumerate(sentences):
    for stem,answer,wrong,ex,exar in rows:
        add('english','choice',level,'Complete the sentence','أكمل الجملة',stem,answer,[answer]+wrong,hint=ex,hint_ar=exar,explanation=ex,explanation_ar=exar)

for level in range(3):
    for correct,wrong in [('because',['becuase','becaus','becouse']),('Wednesday',['Wensday','Wednsday','Wenesday']),('different',['diffrent','diferent','differant'])]:
        add('english','choice',level,'Which spelling is correct?','ما الكتابة الصحيحة؟','Find the carefully written word',correct,[correct]+wrong,
            hint='Read each letter slowly.',hint_ar='اقرأ كل حرف ببطء.',explanation='Remember this spelling: '+correct,explanation_ar='تذكّر هذه الكتابة: '+correct)
    ordered = [
        ['I like green tea','She reads a book'],
        ['We went to the park yesterday','My sister is learning English'],
        ['If it rains we will stay inside','She has been studying all morning']][level]
    for phrase in ordered:
        add('english','order',level,'Put the words in order','رتّب الكلمات لتكوين جملة', '',phrase,phrase.split(),
            hint='Start with “'+phrase.split()[0]+'”.',hint_ar='ابدأ بالكلمة: '+phrase.split()[0],explanation=phrase+'.',explanation_ar='الترتيب الصحيح: '+phrase+'.')
    for word in ['garden','morning','library']:
        add('english','listen',level,'Listen, then write the word','استمع ثم اكتب الكلمة','',word,[word,'mountain','kitchen','evening'],Speech=word,
            hint='Replay the word as often as you like.',hint_ar='يمكنك إعادة الاستماع عدة مرات.',explanation='You heard “'+word+'”.',explanation_ar='الكلمة التي سمعتها هي: '+word)

facts = [
 ('Which planet do we live on?','على أي كوكب نعيش؟','Earth',['Mars','Venus','Jupiter'],'Earth is our home planet.','الأرض هي الكوكب الذي نعيش عليه.'),
 ('What is the capital of Morocco?','ما عاصمة المغرب؟','Rabat',['Casablanca','Marrakesh','Fes'],'Rabat is the capital of Morocco.','الرباط هي عاصمة المغرب.'),
 ('How many days are in a week?','كم يوماً في الأسبوع؟','7',['5','6','8'],'A week has seven days.','الأسبوع يتكوّن من سبعة أيام.'),
 ('Which animal is a mammal?','أي حيوان من الثدييات؟','Dolphin',['Shark','Trout','Octopus'],'Dolphins breathe air and feed their young milk.','الدلافين تتنفس الهواء وترضع صغارها.'),
 ('Which part of a plant absorbs water?','أي جزء من النبات يمتص الماء؟','Roots',['Flowers','Fruit','Seeds'],'Roots absorb water from the soil.','تمتص الجذور الماء من التربة.'),
 ('Which direction does the sun rise from?','من أي جهة تشرق الشمس؟','East',['West','North','South'],'The sun rises in the east.','تشرق الشمس من الشرق.'),
 ('How many sides does a hexagon have?','كم ضلعاً للشكل السداسي؟','6',['5','7','8'],'Hexa means six. A hexagon has six sides.','للشكل السداسي ستة أضلاع.'),
 ('Which ocean lies west of Morocco?','أي محيط يقع غرب المغرب؟','Atlantic',['Pacific','Indian','Arctic'],'Morocco has a long Atlantic coastline.','للمغرب ساحل طويل على المحيط الأطلسي.'),
 ('What do bees collect from flowers?','ماذا يجمع النحل من الأزهار؟','Nectar',['Salt','Sand','Wool'],'Bees collect nectar, which they turn into honey.','يجمع النحل الرحيق ويحوّله إلى عسل.'),
 ('Which is a source of renewable energy?','ما مصدر الطاقة المتجددة؟','Sunlight',['Coal','Oil','Natural gas'],'Sunlight is continually replenished.','ضوء الشمس مصدر متجدد للطاقة.'),
 ('How many minutes are in two hours?','كم دقيقة في ساعتين؟','120',['60','90','100'],'Each hour contains 60 minutes: 2 × 60 = 120.','الساعة 60 دقيقة، إذن ساعتان تساويان 120 دقيقة.'),
 ('Which instrument measures temperature?','ما الأداة التي تقيس درجة الحرارة؟','Thermometer',['Compass','Ruler','Clock'],'A thermometer measures temperature.','يقيس مقياس الحرارة درجة الحرارة.'),
 ('Which state of water is ice?','ما الحالة الفيزيائية للجليد؟','Solid',['Liquid','Gas','Vapour'],'Ice is water in its solid state.','الجليد هو الماء في حالته الصلبة.'),
 ('Which organ pumps blood?','أي عضو يضخ الدم؟','Heart',['Lungs','Stomach','Brain'],'The heart pumps blood around the body.','يضخ القلب الدم في الجسم.'),
 ('Which colour comes from mixing blue and yellow paint?','ما اللون الناتج عن خلط الطلاء الأزرق والأصفر؟','Green',['Purple','Orange','Red'],'Blue and yellow paint mix to make green.','خلط الطلاء الأزرق والأصفر يعطي اللون الأخضر.')]
arabic_answers = {'Earth':'الأرض','Mars':'المريخ','Venus':'الزهرة','Jupiter':'المشتري','Rabat':'الرباط','Casablanca':'الدار البيضاء','Marrakesh':'مراكش','Fes':'فاس','Dolphin':'الدلفين','Shark':'القرش','Trout':'سمك السلمون المرقط','Octopus':'الأخطبوط','Roots':'الجذور','Flowers':'الأزهار','Fruit':'الثمار','Seeds':'البذور','East':'الشرق','West':'الغرب','North':'الشمال','South':'الجنوب','Atlantic':'الأطلسي','Pacific':'الهادئ','Indian':'الهندي','Arctic':'المتجمد الشمالي','Nectar':'الرحيق','Salt':'الملح','Sand':'الرمل','Wool':'الصوف','Sunlight':'ضوء الشمس','Coal':'الفحم','Oil':'النفط','Natural gas':'الغاز الطبيعي','Thermometer':'مقياس الحرارة','Compass':'البوصلة','Ruler':'المسطرة','Clock':'الساعة','Solid':'صلبة','Liquid':'سائلة','Gas':'غازية','Vapour':'بخار','Heart':'القلب','Lungs':'الرئتان','Stomach':'المعدة','Brain':'الدماغ','Green':'الأخضر','Purple':'البنفسجي','Orange':'البرتقالي','Red':'الأحمر'}
for level in range(3):
    for en,ar,answer,wrong,ex,exar in facts:
        add('general','choice',level,en,ar,'',answer,[answer]+wrong,hint=ex.split('.')[0]+'.',hint_ar=exar,explanation=ex,explanation_ar=exar)

for level in range(3):
    for word in ['BOOK','MOON','LEAF','STAR','HOME','TEA']:
        add('puzzles','search',level,'Find the hidden word','ابحث عن الكلمة المخفية',word,word,
            hint='The word runs left to right or top to bottom, in a straight line.',hint_ar='توجد الكلمة أفقياً من اليسار إلى اليمين أو عمودياً من الأعلى إلى الأسفل.',
            explanation='You found '+word+'! Scan one row or column at a time.',explanation_ar='وجدت الكلمة! ابحث في كل صف أو عمود على حدة.')
    for i in range(3):
        add('puzzles','memory',level,'Match the pairs','اعثر على الأزواج المتطابقة','', 'matched',
            hint='Notice positions. Try to remember one pair at a time.',hint_ar='تذكّر المواقع وحاول العثور على زوج واحد في كل مرة.',
            explanation='Every pair found! Remembering locations becomes easier with practice.',explanation_ar='وجدت كل الأزواج! تتحسن ذاكرة المواقع مع التدريب.')
    for start,step in [(2,2),(3,3),(5,5),(1,4),(10,10),(7,2)]:
        factor=level+1
        seq=[start+step*factor*i for i in range(5)]
        answer=str(seq[-1])
        add('puzzles','choice',level,'What comes next?','ما العدد التالي؟', '   ·   '.join(map(str,seq[:-1]))+'   ·   ?',answer,[answer,str(seq[-1]+1),str(seq[-1]-1),str(seq[-1]+step)],
            hint=f'Each step adds {step*factor}.',hint_ar=f'في كل خطوة نضيف {step*factor}.',explanation=f'Add {step*factor} each time. The next number is {answer}.',explanation_ar=f'نضيف {step*factor} كل مرة. العدد التالي هو {answer}.')

from content_expansion import extend
extend(questions, arabic_answers)

(root/'content').mkdir(exist_ok=True)
(root/'content/questions.json').write_text(json.dumps(questions,ensure_ascii=False,indent=2),encoding='utf-8')
(root/'content/arabic-options.json').write_text(json.dumps(arabic_answers,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Wrote {len(questions)} question records (some vocabulary and facts recur across levels).')
