using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LittleNotebook {
 public partial class MainWindow {
  string interactionId="",typedAnswer="";
  bool listeningAutoPending;double listeningDelay;int listeningAutoCount;
  List<int> ordered=new List<int>(), searchSelected=new List<int>(), memoryOpen=new List<int>();
  List<string> orderTokens=new List<string>();
  WordSearchFactory searchFactory;WordSearchBoard searchBoard;HashSet<string> searchFound=new HashSet<string>();List<int[]> searchFoundPaths=new List<int[]>();
  char[] searchLetters;int[] memoryCards;string[] memoryIcons;HashSet<int> memoryMatched=new HashSet<int>();
  Canvas interaction;string responseNote="";
  StrokeCollection workingStrokes=new StrokeCollection();
  void InitInteraction(){
   string id=game.Index+":"+game.Current.Id;
   if(interactionId==id)return;
   interactionId=id;typedAnswer="";listeningAutoPending=game.Current.Kind=="listen";listeningDelay=0;ordered.Clear();searchSelected.Clear();memoryOpen.Clear();memoryMatched.Clear();responseNote="";workingStrokes=new StrokeCollection();
   orderTokens=(game.Current.Options??new string[0]).OrderBy(x=>bank.Random.Next()).ToList();
   searchBoard=null;searchFound.Clear();searchFoundPaths.Clear();searchLetters=new char[64];
   if(game.Current.Kind=="search"){if(searchFactory==null)searchFactory=new WordSearchFactory(bank.Random);searchBoard=searchFactory.Create(game.Current.Answer,bank.Questions.Where(q=>q.Kind=="search"&&q.Difficulty==game.Level).Select(q=>q.Answer),game.Level);searchLetters=searchBoard.Letters;}
   wordPairLeft=-1;wordPairWrong=-1;wordPairsMatched.Clear();wordPairRight=game.Current.Pairs==null?new int[0]:Enumerable.Range(0,game.Current.Pairs.Length).OrderBy(x=>bank.Random.Next()).ToArray();
   memoryIcons=game.Current.Image=="animals"?new[]{"cat","dog","rabbit","bird","fish","butterfly","paw","leaf"}:game.Current.Image=="nature"?new[]{"leaf","sun","moon","star","flower","cloud","tree","apple"}:new[]{"book","chair","water","cup","key","clock","pencil","umbrella"};
   int pairs=game.Level==0?4:game.Level==1?6:8;memoryCards=Enumerable.Range(0,pairs).Concat(Enumerable.Range(0,pairs)).OrderBy(x=>bank.Random.Next()).ToArray();
  }
  string DisplayAnswer(string option){return Ar&&(game.Current.Topic=="general"||QuestionCatalog.Category(game.Current)=="logic")&&arabicOptions.ContainsKey(option)?arabicOptions[option]:option;}
  void GameView(){
   InitInteraction();var q=game.Current;answerButtons.Clear();
   Label(body,Topic(game.Current.Topic)+"  /  "+Difficulty(game.Current.Difficulty),9,0,15,780);
   Label(body,q.Category=="dictionary"||q.Category=="spelling-repair"?CategoryName(q.Category):KindName(q.Kind),9,31,27,840);
   Label(body,T("Question ","سؤال ")+(game.Index+1)+(game.Endless?"":" / "+game.Questions.Count),10,91,18,230);
   if(game.Endless)Label(body,T("∞  All questions","∞  كل الأسئلة"),264,92,18,365);
   else for(int i=0;i<game.Questions.Count;i++){var fill=i<game.Index?p.Accent:i==game.Index?p.Gold:p.Paper;Put(body,new Ellipse{Width=16,Height=16,Fill=B(fill),Stroke=B(p.Ink),StrokeThickness=1.3},245+i*25,102);}
   scoreText=Label(body,T("Score: ","النقاط: ")+game.Score,739,86,22,201);
   lifeText=Label(body,new string('♥',game.Lives)+new string('♡',game.MaxLives-game.Lives),741,119,22,200);lifeText.Foreground=B(p.Accent);
   clockText=Label(body,"",490,127,16,229);UpdateClock();
   var note=new Canvas();Label(note,q.Kind=="search"&&searchBoard.Words.Length>1?T("Find the hidden words","اعثر على الكلمات المخفية"):q.Prompt(Ar),55,9,24,800).TextAlignment=TextAlignment.Center;AddQuestionSpeaker(note);
   if(q.Kind=="image")Put(note,new Sketch(q.Image,p,114,114),385,55);
   else if(q.Kind=="listen"){
    Btn(note,T("♫  Listen again","♫  استمع مرة أخرى"),285,74,PlayListeningWord,340,63,true);
    Label(note,T("Listen closely. Replay whenever you like.","استمع جيداً. يمكنك إعادة الاستماع متى شئت."),130,147,14,650).TextAlignment=TextAlignment.Center;
   }
   else if(q.Kind=="pairs")Label(note,q.Category=="spelling-repair"?T("Match each sound spelling to its correct English spelling.","صِل الكتابة حسب الصوت بالإملاء الإنجليزي الصحيح."):T("Match each word to its meaning. A wrong match uses a life.","صِل كل كلمة بمعناها. المطابقة الخاطئة تستهلك محاولة."),73,75,21,750).TextAlignment=TextAlignment.Center;
   else if(q.Kind=="memory")Label(note,T("Turn two cards. Find all the matching pairs.","اقلب بطاقتين واعثر على كل الأزواج المتطابقة."),92,78,21,700).TextAlignment=TextAlignment.Center;
   else if(q.Kind=="order")Label(note,T("Tap the words to build your sentence below.","اضغط على الكلمات لبناء الجملة أدناه."),73,75,21,750).TextAlignment=TextAlignment.Center;
   else Label(note,q.Kind=="search"?string.Join(" · ",searchBoard.Words):q.Stem,54,68,q.Topic=="math"?39:28,784,true).TextAlignment=TextAlignment.Center;
   Put(body,Box(note,940,194,p.Paper,-.25),8,178);
   interaction=new Canvas{Width=942,Height=257};Put(body,interaction,8,393);RenderInteraction();
   feedbackText=Label(body,"",19,652,16,905);RenderFeedback();
   var leave=Button(T("Leave round","مغادرة الجولة"),()=>ConfirmLeave(false),186,43);var leaveContent=new StackPanel{Orientation=Orientation.Horizontal};leaveContent.Children.Add(new System.Windows.Shapes.Path{Data=Geometry.Parse("M 2,3 L 14,3 L 14,21 M 2,3 L 10,1 L 10,23 L 2,21 Z M 7,12 L 8,12"),Width=18,Height=24,Stroke=B(p.Ink),StrokeThickness=1.6,Margin=new Thickness(0,0,8,0)});leaveContent.Children.Add(Text(T("Leave round","مغادرة الجولة"),17));leave.Content=leaveContent;Put(body,leave,10,706);
   var hint=Button(T("✧ Hint (","✧ تلميح (")+game.Hints+")",UseHint,177,43);hint.IsEnabled=!game.Answered&&!game.UsedHint&&game.Hints>0;Put(body,hint,211,706);
   if(q.Topic=="math")Btn(body,T("✎ Working paper","✎ ورقة الحل"),401,706,WorkingPaper,223,43);
   if(game.Answered&&(q.Category=="spelling-repair"||q.Category=="dictionary"))Btn(body,T("Continue →","متابعة ←"),688,706,NextQuestion,250,43,true);else if(game.Answered)Label(body,game.IsOver?T("Showing your results…","جارٍ عرض النتائج…"):T("Next question…","السؤال التالي…"),688,715,16,250).TextAlignment=TextAlignment.Center;
   MaybeAutoReadQuestion();
  }
  void RenderInteraction(){
   interaction.Children.Clear();var q=game.Current;
   if(q.Kind=="spell"||q.Kind=="listen"){
    Label(interaction,T("Write your answer in English:","اكتب إجابتك بالإنجليزية:"),131,10,19,680).TextAlignment=TextAlignment.Center;
    var input=new TextBox{Text=typedAnswer,Width=576,Height=67,FontSize=30*Profile.Settings.TextScale,FontFamily=new FontFamily("Segoe Print"),Padding=new Thickness(15,5,15,5),Background=B(p.Paper),Foreground=B(p.Ink),BorderBrush=B(p.Ink),IsReadOnly=game.Answered,MaxLength=80,FlowDirection=FlowDirection.LeftToRight,TextAlignment=TextAlignment.Center};
    activeAnswerInput=input;input.TextChanged+=(s,e)=>typedAnswer=input.Text;input.KeyDown+=(s,e)=>{if(e.Key==Key.Enter&&!game.Answered)Submit(input.Text,false);};Put(interaction,input,183,52);
    var b=Button(T("Check spelling","تحقق من الكتابة"),()=>Submit(input.Text,false),320,55,true);b.IsEnabled=!game.Answered;Put(interaction,b,311,146);if(q.Kind=="listen")FocusAnswerInput();
   }else if(q.Kind=="order")RenderOrder();
   else if(q.Kind=="search")RenderSearch();
   else if(q.Kind=="memory")RenderMemory();
   else if(q.Kind=="pairs")RenderWordPairs();
   else {
    var options=q.Options.ToArray();
    for(int i=0;i<options.Length;i++){
     string option=options[i];var b=Button(DisplayAnswer(option),()=>Submit(option,false),455,104);b.FontSize=(q.Topic=="math"?32:25)*Profile.Settings.TextScale;b.FlowDirection=Ar&&(q.Topic=="general"||QuestionCatalog.Category(q)=="logic")?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
     if(game.Answered){b.IsEnabled=false;if(option==q.Answer){b.Background=B(p.Accent);b.Foreground=B(p.AccentInk);}else if(Profile.Attempts.LastOrDefault()!=null&&Profile.Attempts.Last().Given==option)b.Background=B(Color.FromRgb(218,160,136));}
     answerButtons.Add(b);Put(interaction,b,(i%2)*480,(i/2)*119);
    }
   }
  }
  void PlayListeningWord(){
   if(game==null||game.Answered||page!="game"||paused||game.Current.Kind!="listen")return;
   listeningAutoPending=false;
   if(!audio.Speak(game.Current.Speech))Message(T("Listening unavailable","الاستماع غير متاح"),T("The voice files are unavailable. Keep the assets folder beside the app.","ملفات الصوت غير متاحة. احتفظ بمجلد assets بجانب التطبيق."));
  }
  void AdvanceListeningAutoplay(double elapsed){
   if(!listeningAutoPending||game==null||game.Answered||page!="game"||paused||game.Current.Kind!="listen")return;
   listeningDelay+=elapsed;if(listeningDelay<1||audio.IsSpeaking||audio.IsPreparingQuestion)return;
   listeningAutoCount++;PlayListeningWord();
  }
  void RenderOrder(){
   string sentence=string.Join(" ",ordered.Select(i=>orderTokens[i]));var label=Text(sentence.Length>0?sentence:T("Your sentence…","ستظهر جملتك هنا…"),22,871,true);Put(interaction,Box(label,937,77,p.Light),0,0);
   var wrap=new WrapPanel{Width=930,Orientation=Orientation.Horizontal,FlowDirection=FlowDirection.LeftToRight};
   for(int i=0;i<orderTokens.Count;i++){int index=i;var b=Button(orderTokens[i],()=>{ordered.Add(index);RenderInteraction();},Math.Max(68,orderTokens[i].Length*13+22),47);b.Margin=new Thickness(0,0,9,8);b.IsEnabled=!ordered.Contains(i)&&!game.Answered;wrap.Children.Add(b);}Put(interaction,wrap,0,93);
   var undo=Button(T("↶ Undo","↶ تراجع"),()=>{if(ordered.Count>0)ordered.RemoveAt(ordered.Count-1);RenderInteraction();},155,43);undo.IsEnabled=!game.Answered;Put(interaction,undo,0,199);
   var check=Button(T("Check sentence","تحقق من الجملة"),()=>Submit(sentence,false),267,49,true);check.IsEnabled=ordered.Count==orderTokens.Count&&!game.Answered;Put(interaction,check,670,195);
  }
  void RenderSearch(){
   for(int i=0;i<searchBoard.Words.Length;i++){string word=searchBoard.Words[i];var label=Label(interaction,word,0,i*38,23,280,true);if(searchFound.Contains(word)){label.TextDecorations=TextDecorations.Strikethrough;label.Foreground=B(p.Muted);}}
   Label(interaction,game.Level==0?T("Click letters along a straight line.\nWords are checked automatically.","اضغط على الحروف في خط مستقيم.\nيتم التحقق تلقائياً."):T("Find each word in a straight line,\nincluding diagonals. Either direction.","اعثر على الكلمات في خط مستقيم،\nبما فيه القطري. في أي اتجاه."),0,128,15,292);
   for(int i=0;i<64;i++){int cell=i;bool found=searchFoundPaths.Any(path=>path.Contains(cell));var button=Button(searchLetters[i].ToString(),()=>SelectSearch(cell),31,28,searchSelected.Contains(i)||found);button.FontFamily=new FontFamily("Segoe Print");button.FontSize=16;button.IsEnabled=!game.Answered;Put(interaction,button,323+(i%8)*35,(i/8)*31);}
   foreach(var path in searchFoundPaths){var line=new Line{X1=338.5+(path.First()%8)*35,Y1=14+(path.First()/8)*31,X2=338.5+(path.Last()%8)*35,Y2=14+(path.Last()/8)*31,Stroke=B(p.Ink),StrokeThickness=3,Opacity=.5,IsHitTestVisible=false};interaction.Children.Add(line);}
   var clear=Button(T("Clear selection","مسح التحديد"),()=>{searchSelected.Clear();RenderInteraction();},220,45);clear.IsEnabled=!game.Answered;Put(interaction,clear,693,95);
   Label(interaction,searchFound.Count+" / "+searchBoard.Words.Length+T(" found"," تم العثور عليها"),693,153,19,225);
  }
  void SelectSearch(int cell){
   if(game.Answered||cell<0||cell>=64)return;
   if(searchSelected.Contains(cell))searchSelected.Clear();
   if(searchSelected.Count>0){int last=searchSelected.Last(),dr=cell/8-last/8,dc=cell%8-last%8;bool adjacent=Math.Abs(dr)<=1&&Math.Abs(dc)<=1&&(dr!=0||dc!=0);if(!adjacent||(searchSelected.Count>1&&(dr!=searchSelected[1]/8-searchSelected[0]/8||dc!=searchSelected[1]%8-searchSelected[0]%8)))searchSelected.Clear();}
   searchSelected.Add(cell);string value=new string(searchSelected.Select(i=>searchLetters[i]).ToArray());string reverse=new string(value.Reverse().ToArray());var word=searchBoard.Words.FirstOrDefault(w=>!searchFound.Contains(w)&&(w==value||w==reverse));
   if(word!=null){searchFound.Add(word);searchFoundPaths.Add(searchSelected.ToArray());searchSelected.Clear();if(searchFound.Count==searchBoard.Words.Length){Submit(game.Current.Answer,false);return;}audio.Fx("correct");}
   else if(!searchBoard.Words.Where(w=>!searchFound.Contains(w)).Any(w=>w.StartsWith(value)||new string(w.Reverse().ToArray()).StartsWith(value))){searchSelected.Clear();searchSelected.Add(cell);}
   RenderInteraction();
  }
  void RenderMemory(){
   string[] icons=memoryIcons;int cols=memoryCards.Length/2;
   double width=cols==4?154:cols==6?130:104,step=cols==4?179:cols==6?152:114,start=cols==4?120:12;
   for(int i=0;i<memoryCards.Length;i++){int n=i;bool face=memoryOpen.Contains(i)||memoryMatched.Contains(i)||game.Answered;var b=Button("",()=>FlipCard(n),width,109,face);if(face){b.Background=Sketch.Textured(p.Light);b.Content=new Sketch(icons[memoryCards[i]],p,80,80);}else{b.Content="✧";b.FontSize=36;}b.IsEnabled=!game.Answered&&!memoryMatched.Contains(i)&&!memoryOpen.Contains(i)&&memoryOpen.Count<2;System.Windows.Automation.AutomationProperties.SetName(b,T("Memory card ","بطاقة الذاكرة ")+(i+1)+(face?" "+icons[memoryCards[i]]:""));Put(interaction,b,start+(i%cols)*step,(i/cols)*123);}
  }
  void FlipCard(int n){
   if(memoryOpen.Count>=2||game.Answered)return;memoryOpen.Add(n);audio.Fx("click");RenderInteraction();
   if(memoryOpen.Count!=2)return;
   if(memoryCards[memoryOpen[0]]==memoryCards[memoryOpen[1]]){foreach(int m in memoryOpen)memoryMatched.Add(m);memoryOpen.Clear();audio.Fx("correct");if(memoryMatched.Count==memoryCards.Length)Submit("matched",false);else RenderInteraction();}
   else {var currentGame=game;string key=interactionId;var delay=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(850)};delay.Tick+=(s,e)=>{delay.Stop();if(currentGame==game&&key==interactionId){memoryOpen.Clear();if(page=="game"&&!game.Answered)RenderInteraction();}};delay.Start();}
  }
  void UseHint(){if(!game.Hint())return;responseNote=T("Hint: ","تلميح: ")+game.Current.Hint(Ar);Build();}
  void RenderFeedback(){
   if(game.Answered){var a=Profile.Attempts.Last();feedbackText.Text=(a.Correct?T("✓ Correct.  ","✓ إجابة صحيحة.  "):a.TimedOut?T("◷ Time's up.  ","◷ انتهى الوقت.  "):T("Incorrect.  ","إجابة غير صحيحة.  "))+((game.Current.Category=="spelling-repair"||game.Current.Category=="dictionary")?T("Review the matched words above, then continue.","راجع الكلمات المتطابقة أعلاه ثم تابع."):game.Current.Explain(Ar));feedbackText.Foreground=B(a.Correct?p.Muted:p.Ink);}
   else feedbackText.Text=responseNote;
  }
  void Submit(string given,bool timeout){
   if(game==null||game.Answered)return;
   answerFeedbackElapsed=0;int oldEarned=game.Earned;var a=game.Answer(given,timeout);CaptureAttemptSetup(a);Profile.Attempts.Add(a);Profile.Coins+=game.Earned-oldEarned;Profile.BestStreak=Math.Max(Profile.BestStreak,game.Streak);Profile.LastPlayed=DateTime.Now.ToString("yyyy-MM-dd");audio.StopSpeech();audio.Fx(a.Correct?"correct":"wrong");Save();Build();
  }
  void NextQuestion(){if(!game.Answered)return;answerFeedbackElapsed=0;if(game.IsOver){RecordSession(true);page="results";audio.Fx("finish");Build();}else{game.Next(game.Endless?PrepareQuestion(questionStream.Next()):null);Build();}}
  void RecordSession(bool completed){if(game==null||game.Recorded)return;game.Recorded=true;Profile.Sessions.Add(new SessionRecord{Date=DateTime.Now.ToString("o"),Topic=game.Topic,Category=game.Category,Difficulty=game.Difficulty,Correct=game.Correct,Total=game.Index+(game.Answered?1:0),Score=game.Score,Seconds=game.Seconds,Completed=completed,Endless=game.Endless});Save();}
  void Results(){
   Header(T("RESULTS","النتائج"),T("Round finished","انتهت الجولة"),T("Review your results or start another round.","راجع نتائجك أو ابدأ جولة أخرى."));
   Put(body,Avatar(Profile.Character,242,240),359,158);Put(body,new Sketch("star",p,75,75),246,195);Put(body,new Sketch("leaf",p,65,80),638,268);
   Label(body,game.Score+T(" points"," نقطة"),169,428,40,620,true).TextAlignment=TextAlignment.Center;
   Label(body,game.Correct+" / "+(game.Index+1)+T(" correct   ·   +"," صحيحة   ·   +")+game.Earned+T(" stars   ·   "," نجمة   ·   ")+Math.Round(game.Seconds)+T(" seconds"," ثانية"),83,493,21,792).TextAlignment=TextAlignment.Center;
   Label(body,game.Lives<=0?T("No lives remaining. Your progress has been saved.","انتهت محاولاتك. تم حفظ تقدّمك."):T("Your progress has been saved.","تم حفظ تقدّمك."),104,552,18,750).TextAlignment=TextAlignment.Center;
   AddReviewMistakes(17,645,282,57);
   Btn(body,T("Main menu","العودة إلى مكتبي"),326,645,()=>{game=null;page="home";Build();},282,57);
   Btn(body,T("Play again  →","جولة أخرى  ←"),636,645,()=>{interactionId="";StartGame();},306,57,true);
  }
  void WorkingPaper(){
   Modal(T("Working paper","ورقة الحل"),c=>{
    Label(c,game.Current.Stem,93,88,26,596,true).TextAlignment=TextAlignment.Center;
    Label(c,T("The timer pauses while you work. Your paper stays for this question.","يتوقف المؤقت أثناء الحل. تُحفظ الورقة لهذا السؤال."),91,129,13,604);
    var ink=new InkCanvas{Width=620,Height=345,Background=B(p.Paper),Strokes=workingStrokes,ClipToBounds=true,DefaultDrawingAttributes=new DrawingAttributes{Color=p.Ink,Width=3,Height=3,FitToCurve=true}};
    var grid=new Canvas{Width=620,Height=345,IsHitTestVisible=false};for(int y=25;y<345;y+=25)Put(grid,new Rectangle{Width=620,Height=1,Fill=B(p.Line)},0,y);
    // Lines sit beneath the transparent ink surface.
    var panel=new Grid{Width=620,Height=345,Background=B(p.Paper)};ink.Background=Brushes.Transparent;panel.Children.Add(grid);panel.Children.Add(ink);Put(c,panel,90,232);
    string tool="pencil";Point start=new Point();bool drawing=false;
    Action<string> setTool=s=>{tool=s;ink.EditingMode=s=="eraser"?InkCanvasEditingMode.EraseByStroke:s=="pencil"?InkCanvasEditingMode.Ink:InkCanvasEditingMode.None;};
    string[] labels={T("✎ Pencil","✎ قلم"),T("▱ Eraser","▱ ممحاة"),T("― Line","― خط"),T("□ Box","□ مربع"),T("○ Circle","○ دائرة")};string[] tools={"pencil","eraser","line","box","circle"};
    for(int i=0;i<5;i++){string t=tools[i];Btn(c,labels[i],90+i*125,177,()=>setTool(t),115,39);}
    ink.PreviewMouseLeftButtonDown+=(s,e)=>{if(tool=="pencil"||tool=="eraser")return;start=e.GetPosition(ink);drawing=true;ink.CaptureMouse();e.Handled=true;};
    ink.PreviewMouseLeftButtonUp+=(s,e)=>{if(!drawing)return;drawing=false;Point end=e.GetPosition(ink);ink.ReleaseMouseCapture();var pts=new StylusPointCollection();
     if(tool=="line"){pts.Add(new StylusPoint(start.X,start.Y));pts.Add(new StylusPoint(end.X,end.Y));}
     else if(tool=="box"){pts.Add(new StylusPoint(start.X,start.Y));pts.Add(new StylusPoint(end.X,start.Y));pts.Add(new StylusPoint(end.X,end.Y));pts.Add(new StylusPoint(start.X,end.Y));pts.Add(new StylusPoint(start.X,start.Y));}
     else {double cx=(start.X+end.X)/2,cy=(start.Y+end.Y)/2,rx=Math.Abs(start.X-end.X)/2,ry=Math.Abs(start.Y-end.Y)/2;for(int a=0;a<=64;a++)pts.Add(new StylusPoint(cx+rx*Math.Cos(a*Math.PI/32),cy+ry*Math.Sin(a*Math.PI/32)));}
     if(pts.Count>1)ink.Strokes.Add(new Stroke(pts,ink.DefaultDrawingAttributes.Clone()));e.Handled=true;
    };
    Btn(c,T("↶ Undo","↶ تراجع"),90,598,()=>{if(ink.Strokes.Count>0)ink.Strokes.RemoveAt(ink.Strokes.Count-1);},119,42);
    Btn(c,T("Clear paper","مسح الورقة"),222,598,()=>ink.Strokes.Clear(),155,42);
    var colors=new[]{p.Ink,Color.FromRgb(94,140,89),Color.FromRgb(66,126,168),Color.FromRgb(188,105,89)};
    for(int i=0;i<colors.Length;i++){Color color=colors[i];var b=Button("●",()=>ink.DefaultDrawingAttributes.Color=color,36,37);b.Foreground=B(color);Put(c,b,399+i*43,601);}
    Btn(c,T("Done","تم"),590,595,CloseModal,120,48,true);
   },690);
  }
 }
}
