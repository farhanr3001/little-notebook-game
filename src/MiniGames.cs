using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LittleNotebook {public partial class MainWindow {
  MiniSession mini;MiniSurface miniSurface;ScrollViewer miniScroll;bool miniRendering;long miniStamp;
  TextBlock miniStatus,miniLives,miniCounter,miniHeading;double miniZoom=1,miniDashSaved;bool miniDashRecorded;
  Button miniReplay,miniRestart;
  ScrollViewer miniSelectionScroll;double miniSelectionOffset;
  readonly List<Key> miniHeldKeys=new List<Key>();
  string MiniName(string key){if(ExtraPuzzle.Keys.Contains(key))return ExtraTitle(key);switch(key){case "mousedash":return T("Ladybug Dodge","مراوغة الدعسوقة");case "arrows":return T("Arrow Escape","هروب الأسهم");case "signal":return T("Signal Path","مسار الإشارة");case "pulse":return T("Pulse Lock","قفل النبض");case "stack":return T("Paper Stack","برج الورق");case "tiles":return T("Picture Patches","قصاصات الصور");case "echo":return T("Echo Keys","مفاتيح الصدى");default:return T("Ladybug Trail","درب الدعسوقة");}}
  string MiniDescription(string key){if(ExtraPuzzle.Keys.Contains(key))return ExtraInstructions(key);switch(key){case "mousedash":return T("Guide your ladybug past the buzzing pests. How long can you last?","أمسك كرتك وراوغ النقاط المتحركة. كم يمكنك الصمود؟");case "arrows":return T("Clear winding arrows in the right order. Three hearts, piano notes and bigger boards.","أخرج الأسهم بالترتيب الصحيح. ثلاث محاولات ونغمات بيانو ولوحات أكبر.");case "signal":return T("Rotate the little pipes to carry a signal from IN to OUT.","دوّر الأنابيب لوصل الإشارة من المدخل إلى المخرج.");case "pulse":return T("Press E or click to begin, then keep tapping to open all three latches.","اضغط E أو انقر للبدء، ثم واصل الضغط لفتح الأقفال الثلاثة.");case "stack":return T("Time each landing. Trimmed edges fall away; how high can you stack?","اختر لحظة وضع كل قطعة. تسقط الحواف الزائدة؛ كم سيعلو برجك؟");case "tiles":return T("Piece together an animal photo. Hover over the eye for a peek.","رتّب البلاطات واترك الفراغ في أسفل اليمين.");case "echo":return T("Watch and listen, then play the little melody back.","شاهد واستمع، ثم أعد عزف النغمات بالترتيب.");default:return T("Guide the little ladybug through the paper maze to its leaf.","أرشد الدعسوقة الصغيرة عبر متاهة الورق إلى ورقتها.");}}
  void PlayTabs(bool games){
   var learning=Button(T("Learning","التعلّم"),()=>SwitchPlaySection(false),160,32,!games);learning.FontSize=14*Profile.Settings.TextScale;Put(body,learning,597,-7);
   var minigames=Button(T("Minigames","ألعاب مصغّرة"),()=>SwitchPlaySection(true),178,32,games);minigames.FontSize=14*Profile.Settings.TextScale;Put(body,minigames,765,-7);
  }
  void SwitchPlaySection(bool games){if(page=="minigame")StopMiniGame();Profile.Settings.PlaySection=games?"minigames":"learning";Navigate(games?"minigames":"home");}
  void MiniGamesView(){
   MiniCatalog.Normalize(Profile);Header(T("PLAY","اللعب"),T("A little break","استراحة صغيرة"),T("Pick a game. Progress and personal bests save automatically.","اختر لعبة. يُحفظ تقدّمك وأفضل أرقامك تلقائياً."));PlayTabs(true);
   string selected=Profile.Settings.SelectedMiniGame;var cards=new WrapPanel{Width=942,Orientation=Orientation.Horizontal};
   foreach(string key in MiniCatalog.Keys){string kind=key;bool active=selected==key;var progress=key=="stack"?new MiniProgress():Profile.MiniGames[key];var button=Button("",()=>{Profile.Settings.SelectedMiniGame=kind;Save();Build();},446,117,active);button.Margin=new Thickness(4,2,10,10);button.ToolTip=MiniDescription(kind);AutomationProperties.SetName(button,kind=="stack"?MiniName(kind)+T(" · high score "," · أعلى نتيجة ")+Profile.StackBest:MiniName(kind)+T(" · level "," · المستوى ")+progress.Level);
    var card=new Canvas{Width=418,Height=104};var artPalette=active?p.OnAccent():p;Put(card,new MiniGameBadge(kind,artPalette),Ar?365:9,12);
    var title=Label(card,MiniName(kind),Ar?12:62,5,21,334);title.Foreground=B(active?p.AccentInk:p.Ink);
    var description=Label(card,MiniDescription(kind),Ar?12:62,37,13,334);description.Foreground=B(active?p.AccentInk:p.Ink);description.MaxHeight=37;
    var level=Label(card,kind=="stack"?T("High score ","أعلى نتيجة ")+Profile.StackBest+T(" · fresh tower each run"," · برج جديد كل محاولة"):kind=="mousedash"?T("Best ","الأفضل ")+DashTime(progress.BestSeconds)+"  ·  "+progress.Completed+T(" runs"," محاولات"):T("Level ","المستوى ")+progress.Level+"  ·  "+T("Best ","الأفضل ")+progress.BestCompleted,Ar?12:62,78,12,334);level.Foreground=B(active?p.AccentInk:p.Muted);button.Content=card;cards.Children.Add(button);
   }
   var scroll=new ScrollViewer{Width=952,Height=496,Content=cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(body,scroll,8,133);miniSelectionScroll=scroll;scroll.ApplyTemplate();scroll.Measure(new Size(952,496));scroll.Arrange(new Rect(0,0,952,496));scroll.UpdateLayout();scroll.ScrollToVerticalOffset(miniSelectionOffset);scroll.UpdateLayout();
   Btn(body,selected=="stack"?T("Reset high score…","مسح أعلى نتيجة…"):T("Reset this game's progress…","إعادة تقدّم هذه اللعبة…"),17,656,ConfirmResetMiniProgress,285,42);
   Btn(body,T("Records","السجلات"),316,656,ShowMiniRecords,193,42);
   var start=Button(T("Start game  →","ابدأ اللعبة  ←"),()=>StartMiniGame(Profile.Settings.SelectedMiniGame),344,50,true);start.Background=B(Color.FromRgb(139,255,22));start.Foreground=B(Color.FromRgb(41,56,74));Put(body,start,533,651);
   Label(body,T("New boards grow with you. Take your time, or chase a new personal best.","تزداد الصعوبة مع التقدّم. خذ وقتك أو حقق رقماً أفضل."),17,715,12,900);
  }
  void StartMiniGame(string kind){
   MiniCatalog.Normalize(Profile);if(!MiniCatalog.Keys.Contains(kind))return;StopMiniGame(true);audio.PrepareMiniSounds(kind=="arrows"||kind=="stack");audio.StopSpeech();Profile.Settings.PlaySection="minigames";Profile.Settings.SelectedMiniGame=kind;
   var progress=kind=="stack"?new MiniProgress():Profile.MiniGames[kind];miniStackStartBest=Profile.StackBest;mini=new MiniSession(kind,progress.Level,kind=="mousedash"||kind=="stack"?unchecked(MiniCatalog.LevelSeed(progress)+Guid.NewGuid().GetHashCode()):MiniCatalog.LevelSeed(progress));miniDashSaved=0;miniDashRecorded=false;
   mini.Note+=PlayMiniInput;mini.Won+=MiniLevelWon;mini.Lost+=MiniLevelLost;mini.Next+=()=>StartMiniGame(kind);
   page="minigame";paused=false;miniZoom=0;extraLastKey=-1;Save();Build();
   miniStamp=System.Diagnostics.Stopwatch.GetTimestamp();if(!miniRendering){CompositionTarget.Rendering+=MiniTick;miniRendering=true;}FocusMiniSurface();miniScroll.UpdateLayout();UpdateMiniExitBounds();if(kind=="signal")KeepMiniCellVisible(mini.Signal.Source);
  }
  string DashTime(double seconds){return seconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+T("s"," ث");}
  void RememberDash(bool endRun){
   if(mini==null||mini.Dash==null||!mini.Dash.Started)return;var record=Profile.MiniGames["mousedash"];record.LastSeconds=mini.Dash.Seconds;record.BestSeconds=Math.Max(record.BestSeconds,mini.Dash.Seconds);
   int milestones=(int)(mini.Dash.Seconds/15);if(milestones>record.BestCompleted){Profile.Coins+=(milestones-record.BestCompleted)*2;record.BestCompleted=milestones;}
   if(endRun&&!miniDashRecorded){record.Completed++;miniDashRecorded=true;}Save();
  }
  void MiniDashDown(Point point){if(mini==null||paused||overlay.Children.Count>0)return;mini.HoldDash(point.X,point.Y);RefreshMiniHud();miniSurface.InvalidateVisual();}
  void MiniDashMove(Point point){if(mini==null||paused||overlay.Children.Count>0)return;mini.MoveDash(point.X,point.Y);RefreshMiniHud();if(miniSurface!=null)miniSurface.InvalidateVisual();}
  void MiniDashUp(){if(mini==null||mini.Dash==null)return;mini.ReleaseDash();RememberDash(false);RefreshMiniHud();if(miniSurface!=null)miniSurface.InvalidateVisual();}
  void StopMiniGame(bool nextRound=false){ClearMiniHeldInput();if(miniRendering){CompositionTarget.Rendering-=MiniTick;miniRendering=false;}if(mini!=null){RememberDash(true);RememberStack();if(miniSurface!=null){miniSurface.EndPan();miniSurface.CancelDashPointer();}mini.Note-=PlayMiniInput;mini.Won-=MiniLevelWon;mini.Lost-=MiniLevelLost;mini=null;}miniSurface=null;miniScroll=null;audio.StopMiniSounds(!nextRound);}
  void PlayMiniInput(int note){if(mini!=null&&mini.Kind=="arrows")audio.InterruptMiniNotes();audio.MiniNote(note);}
  void MiniLevelWon(){
   if(mini==null)return;var progress=Profile.MiniGames[mini.Kind];if(mini.Kind=="tiles"||mini.Extra!=null){progress.Results.Add(new MiniFinish{Level=mini.Level,Moves=mini.Moves,Seconds=mini.Elapsed,Date=DateTime.Now.ToString("o")});if(progress.Results.Count>200)progress.Results.RemoveAt(0);progress.LastSeconds=mini.Elapsed;progress.LastMoves=mini.Moves;if(progress.BestSeconds<=0||mini.Elapsed<progress.BestSeconds)progress.BestSeconds=mini.Elapsed;if(progress.BestMoves==0||mini.Moves<progress.BestMoves)progress.BestMoves=mini.Moves;}if(mini.Level>progress.BestCompleted){progress.BestCompleted=mini.Level;Profile.Coins+=2;}progress.Completed++;progress.Level=Math.Min(100000,mini.Level+1);Save();if(mini.Melody!=null){if(Profile.Settings.Sound)audio.MiniTune(mini.Melody);else mini.Wait=1.45;}else if(mini.Kind=="pulse")audio.MiniPulseWin();else audio.MiniWin();RefreshMiniHud();
  }
  void MiniLevelLost(){if(mini==null)return;if(mini.Extra!=null){Profile.MiniGames[mini.Kind].Failures++;Save();ExtraFailure();return;}if(mini.Stack!=null){RememberStack();Save();MiniFailure();return;}RememberDash(true);if(miniSurface!=null)miniSurface.CancelDashPointer();Profile.MiniGames[mini.Kind].Failures++;Save();MiniFailure();}
  void MiniTick(object sender,EventArgs args){
   long now=System.Diagnostics.Stopwatch.GetTimestamp();double rawElapsed=Math.Max(0,(now-miniStamp)/(double)System.Diagnostics.Stopwatch.Frequency);double elapsed=Math.Min(.1,rawElapsed);miniStamp=now;
   if(mini==null||page!="minigame"||paused||!IsActive)return;
   if((mini.Kind=="tiles"||mini.Extra!=null)&&mini.Phase=="playing")mini.Elapsed+=Math.Max(0,rawElapsed-elapsed);
   bool extraAnimating=mini.Extra!=null&&mini.Extra.Feedback>0;bool arrowAnimating=mini.Flights.Count>0;var before=mini;bool mazeAnimating=mini.Maze!=null&&mini.MoveTime<MiniSession.MazeStepSeconds;mini.Tick(elapsed);if(mini!=before)return;if(mini.Maze!=null)KeepMiniMazeVisible();if(mini.Dash!=null&&mini.Dash.Seconds-miniDashSaved>=5){miniDashSaved=mini.Dash.Seconds;RememberDash(false);}RefreshMiniHud();
   if(extraAnimating||arrowAnimating||mazeAnimating||mini.Phase!="playing"||mini.Dash!=null||mini.Kind=="pulse"||mini.Stack!=null||mini.TurnTime<.18||mini.MoveTime<.18||mini.FeedbackTime>0)miniSurface.InvalidateVisual();
  }
  void MiniGameView(){
   if(mini==null){page="minigames";MiniGamesView();return;}
   Header(T("MINIGAMES","الألعاب المصغّرة"),MiniName(mini.Kind),MiniDescription(mini.Kind));
   miniHeading=Label(body,T("Level ","المستوى ")+mini.Level,16,133,21,240);
   miniLives=Label(body,"",260,132,25,220,true);miniLives.Foreground=B(Color.FromRgb(187,91,91));miniLives.TextAlignment=TextAlignment.Center;
   miniCounter=Label(body,"",mini.Extra!=null?600:mini.Kind=="tiles"?660:490,139,15,mini.Extra!=null?170:240);miniCounter.TextAlignment=TextAlignment.Right;
   miniReplay=null;miniPreviewEye=null;miniStackPace=null;if(mini.Stack!=null){miniStackPace=Label(body,"",754,139,14,181);miniStackPace.TextAlignment=TextAlignment.Center;}else if(mini.Dash!=null){mini.ReleaseDash();Label(body,T("Hold to move","أمسك للتحريك"),754,139,14,181).TextAlignment=TextAlignment.Center;}else if(mini.Kind=="tiles"){var eye=NotebookIconButton("eye",T("Preview picture","معاينة الصورة"),()=>{},44,36);eye.ToolTip=T("Hover to view the complete picture","مرّر لرؤية الصورة كاملة");eye.MouseEnter+=(s,e)=>{miniSurface.ShowPicture=true;miniSurface.InvalidateVisual();};eye.MouseLeave+=(s,e)=>{miniSurface.ShowPicture=false;miniSurface.InvalidateVisual();};miniPreviewEye=eye;}else if(mini.Extra!=null){Btn(body,T("↶ Undo","↶ تراجع"),786,132,UndoExtra,148,34);}else{Btn(body,"−",754,132,()=>ZoomMini(.8),42,34);Btn(body,"+",803,132,()=>ZoomMini(1.25),42,34);Btn(body,T("Fit","ملاءمة"),852,132,()=>ZoomMini(0),83,34);}
   miniSurface=new MiniSurface(mini,p,Ar,Profile.Settings.ReducedMotion,Profile.Settings.TextScale);AutomationProperties.SetName(miniSurface,MiniName(mini.Kind)+". "+MiniDescription(mini.Kind));miniSurface.Press=MiniPress;miniSurface.DashDown=MiniDashDown;miniSurface.DashMove=MiniDashMove;miniSurface.DashUp=MiniDashUp;miniSurface.LostKeyboardFocus+=(s,e)=>ClearMiniHeldInput();
   if(miniZoom<=0)miniZoom=mini.Dash!=null?1:Math.Min(1.85,Math.Min(900/miniSurface.Width,408/miniSurface.Height));
   miniScroll=new ScrollViewer{Width=944,Height=446,Content=miniSurface,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center,CanContentScroll=false,ClipToBounds=true};StylePaperScroll(miniScroll);miniSurface.Viewport=miniScroll;miniSurface.LayoutTransform=new ScaleTransform(miniZoom,miniZoom);
   if(miniPreviewEye!=null){double boardLeft=8+(944-miniSurface.Width*miniZoom)/2;Put(body,miniPreviewEye,Math.Max(14,boardLeft-62),184+Math.Max(0,(446-36)/2));}
   Put(body,new Border{Width=946,Height=448,BorderBrush=B(p.Line),BorderThickness=new Thickness(1),Child=miniScroll},8,184);
   if(mini.Kind=="tiles"||mini.Extra!=null){miniClock=Label(body,"",280,133,21,360);miniClock.TextAlignment=TextAlignment.Center;}else miniClock=null;
   if(miniPreviewEye!=null)Panel.SetZIndex(miniPreviewEye,4);
   miniStatus=Label(body,"",14,639,17,928);miniStatus.TextAlignment=TextAlignment.Center;
   string help=mini.Extra!=null?ExtraControls(mini.Kind):mini.Stack!=null?T("Click the paper, or press E / Space / Enter · one press per piece","انقر الورق أو اضغط E / مسافة / Enter · ضغطة واحدة لكل قطعة"):mini.Dash!=null?T("Hold the ladybug to move · let go to pause · hold the ladybug again to continue","أمسك الكرة لتحريكها · اتركها للإيقاف · أمسكها مجدداً للمتابعة"):mini.Kind=="arrows"?T("Click an arrow · drag empty paper or middle-drag to pan · Enter releases","انقر سهماً · اسحب الورق الفارغ للتحريك · الأسهم للتحديد وEnter للإخراج"):mini.Kind=="signal"?T("Click a tile · arrow keys select · Space / Enter to use","انقر بلاطة · الأسهم للتحديد · مسافة أو Enter للاستخدام"):mini.Kind=="maze"?T("Hold arrows / W A S D to glide · tap or click a neighbour for one step","اضغط مطولاً الأسهم / W A S D للحركة · ضغطة أو نقرة مجاورة لخطوة واحدة"):mini.Kind=="tiles"?T("Click a neighbouring space or use arrow keys / W A S D","انقر خانة مجاورة أو استخدم الأسهم / W A S D"):mini.Kind=="echo"?T("Click the keys, or use their letters on your keyboard","انقر المفاتيح أو استخدم حروفها على لوحة المفاتيح"):T("Separate key presses or clicks count; holding a key does not","كل ضغطة أو نقرة مستقلة تُحسب؛ الضغط المستمر لا يُحسب");
   Label(body,help,14,671,12,930).TextAlignment=TextAlignment.Center;
   Btn(body,T("‹ Minigames","‹ الألعاب المصغّرة"),13,707,BackToMiniGames,230,39);miniRestart=Button(mini.Dash!=null||mini.Stack!=null?T("New run","محاولة جديدة"):T("Restart level","إعادة المستوى"),()=>{if(mini!=null&&mini.Phase=="complete")StartMiniGame(mini.Kind);else ConfirmRestartMini();},238,39);Put(body,miniRestart,327,707);Btn(body,T("Pause","إيقاف مؤقت"),708,707,PauseMiniGame,223,39);
   RefreshMiniHud();
  }
  void RefreshMiniHud(){
   if(miniReplay!=null&&mini!=null)miniReplay.IsEnabled=mini.Phase=="playing";
   if(miniRestart!=null&&mini!=null)miniRestart.Content=mini.Phase=="complete"?T("Next level  →","المستوى التالي  ←"):mini.Dash!=null||mini.Stack!=null?T("New run","محاولة جديدة"):T("Restart level","إعادة المستوى");
   if(mini==null||miniStatus==null)return;if(miniClock!=null)miniClock.Text=FormatPuzzleTime(mini.Elapsed);if(mini.Extra!=null){RefreshExtraHud();return;}miniLives.Text=mini.HasLives?new string('♥',Math.Max(0,mini.Lives))+new string('♡',3-Math.Max(0,mini.Lives)):"";
   if(mini.Dash!=null){
    miniHeading.Text=DashTime(mini.Dash.Seconds);miniLives.FontSize=17*Profile.Settings.TextScale;miniLives.Foreground=B(p.Ink);miniLives.Text=T("Best ","الأفضل ")+DashTime(Math.Max(Profile.MiniGames["mousedash"].BestSeconds,mini.Dash.Seconds));miniCounter.Text=T("Wave ","الموجة ")+mini.Dash.Wave+"  ·  "+mini.Dash.Obstacles.Count+T(" pests"," نقطة");
    miniStatus.Text=mini.Phase=="waiting"?T("Hold anywhere on the paper to begin. Avoid every flying pest.","أمسك أي موضع على الورق للبدء. تجنّب كل النقاط المتحركة."):mini.Phase=="holding"?T("Paused · hold your ball to continue.","متوقفة · أمسك كرتك للمتابعة."):mini.Phase=="failed"?T("You lasted ","صمدت ")+DashTime(mini.Dash.Seconds):T("Keep moving · new dots are outlined before they join in.","واصل الحركة · تظهر النقاط الجديدة بإطار قبل دخولها.");return;
   }
   if(mini.Stack!=null){RefreshStackHud();return;}
   miniCounter.Text=mini.Kind=="arrows"?mini.Arrows.Remaining+T(" arrows left"," أسهم متبقية"):mini.Kind=="pulse"?T("Latch ","القفل ")+Math.Min(3,mini.Latch+1)+" / 3":mini.Kind=="echo"?(mini.Phase=="showing"?Math.Max(0,Math.Min(mini.Pattern.Length,mini.ShowIndex+1)):Math.Min(mini.Pattern.Length,mini.PatternIndex))+" / "+mini.Pattern.Length:mini.Moves+T(" moves"," حركات");
   if(mini.Melody!=null&&mini.Phase=="complete"){miniStatus.Text=T("Board clear · ","اكتملت اللوحة · ")+(Ar?mini.Melody.Arabic:mini.Melody.Title)+(Profile.Settings.Sound?T(" · piano replay"," · إعادة عزف البيانو"):"");return;}
   if(mini.Kind=="tiles"&&mini.Phase=="complete"){miniStatus.Text=T("Picture complete!  ","اكتملت الصورة! ")+FormatPuzzleTime(mini.Elapsed)+" · "+mini.Moves+T(" moves"," حركات");return;}
   if(mini.Phase=="complete")miniStatus.Text=mini.Kind=="pulse"?T("Lock opened!","فُتح القفل!"):T("Level clear!  ✦  Next board…","اكتمل المستوى! ✦ اللوحة التالية…");
   else if(mini.Phase=="failed")miniStatus.Text=T("Your level is saved. Try the same board again.","مستواك محفوظ. حاول حل اللوحة نفسها مجدداً.");
   else if(mini.Kind=="echo")miniStatus.Text=mini.Phase=="showing"?T("Watch and listen…","شاهد واستمع…"):T("Your turn · repeat the melody","دورك · أعد النغمات");
   else if(mini.Kind=="pulse")miniStatus.Text=mini.Phase=="ready"?T("Press E or click the ring to start","اضغط E أو انقر الحلقة للبدء"):mini.Phase=="latch"?T("One latch open!","فُتح أحد الأقفال!"):T("Tap E or click · keep the ring filled","اضغط E أو انقر · أبقِ الحلقة ممتلئة");
   else if(mini.Kind=="arrows"&&mini.WrongFeedback&&mini.FeedbackTime>0)miniStatus.Text=T("That arrow is blocked. It returns to its place.","هذا السهم محجوب. سيعود إلى موضعه.");
   else miniStatus.Text=mini.Kind=="arrows"?T("Follow the arrowhead's exit lane before you click.","تتبّع طريق خروج رأس السهم قبل النقر."):mini.Kind=="signal"?T("Lit pipes carry the signal. Connect them all the way to OUT.","الأنابيب المضيئة تحمل الإشارة. صِلها حتى المخرج."):mini.Kind=="tiles"?T("Rebuild the photograph · leave the empty corner last.","أكمل الصورة واترك الزاوية الفارغة أخيراً."):T("Help the ladybug reach its leaf. Drag the paper to explore.","ساعد الدعسوقة في الوصول إلى الورقة. اسحب الورق للاستكشاف.");
  }
  void MiniPress(int index){
   if(mini!=null&&mini.Kind=="arrows"&&mini.Phase=="complete"){StartMiniGame("arrows");return;}
   if(mini==null||paused||overlay.Children.Count>0)return;bool used=false;
   UpdateMiniExitBounds();
   if(mini.Extra!=null){used=mini.ExtraInput(index);if(used)audio.Fx("click");RefreshMiniHud();miniSurface.InvalidateVisual();return;}
   switch(mini.Kind){case "stack":used=mini.StackTap();RememberStack();break;case "arrows":mini.SelectedCell=index;used=mini.ArrowTap(index);break;case "signal":mini.SelectedCell=index;used=mini.RotateSignal(index);break;case "tiles":used=mini.SlideTile(index);break;case "echo":used=mini.EchoTap(index);break;case "pulse":used=mini.PulseTap();break;case "maze":for(int d=0;d<4;d++)if(MiniGrid.Next(mini.Maze.Player,d,mini.Maze.Size,mini.Maze.Size)==index){used=mini.MoveMaze(d);break;}break;}
   if(used&&mini.Stack==null&&mini.Kind!="arrows"&&mini.Kind!="echo")audio.Fx("click");RefreshMiniHud();if(miniSurface!=null)miniSurface.InvalidateVisual();
  }
  void UpdateMiniExitBounds(){if(mini==null||mini.Arrows==null||miniScroll==null||miniSurface==null)return;Point a=miniScroll.TranslatePoint(new Point(0,0),miniSurface),b=miniScroll.TranslatePoint(new Point(miniScroll.ViewportWidth,miniScroll.ViewportHeight),miniSurface);Rect visible=new Rect(a,b);mini.ExitBounds[0]=visible.Top;mini.ExitBounds[1]=visible.Right;mini.ExitBounds[2]=visible.Bottom;mini.ExitBounds[3]=visible.Left;}
  void ZoomMini(double factor){if(miniSurface==null||miniScroll==null)return;double old=miniZoom;miniZoom=factor==0?Math.Max(.10,Math.Min(1,Math.Min(920/miniSurface.Width,424/miniSurface.Height))):Math.Max(.10,Math.Min(2.4,miniZoom*factor));double centerX=(miniScroll.HorizontalOffset+miniScroll.ViewportWidth/2)/old,centerY=(miniScroll.VerticalOffset+miniScroll.ViewportHeight/2)/old;miniSurface.LayoutTransform=new ScaleTransform(miniZoom,miniZoom);miniScroll.UpdateLayout();miniScroll.ScrollToHorizontalOffset(centerX*miniZoom-miniScroll.ViewportWidth/2);miniScroll.ScrollToVerticalOffset(centerY*miniZoom-miniScroll.ViewportHeight/2);FocusMiniSurface();}
  void FocusMiniSurface(){if(page=="minigame"&&miniSurface!=null&&overlay.Children.Count==0)miniSurface.Focus();}
  void BackToMiniGames(){StopMiniGame();paused=false;page="minigames";Save();Build();}
  void ConfirmRestartMini(){if(mini==null)return;if(mini.Stack!=null){ConfirmRestartStack();return;}if(mini.Dash!=null){Modal(T("Start a new dash?","بدء محاولة جديدة؟"),c=>{Label(c,T("Your personal best is kept. A new run starts with a fresh field of pests.","يُحفظ أفضل رقم لك. تبدأ المحاولة الجديدة بمجموعة نقاط جديدة."),97,112,21,595);Btn(c,T("Keep playing","متابعة اللعب"),103,270,CloseModal,266,49);Btn(c,T("New run","محاولة جديدة"),399,270,()=>StartMiniGame("mousedash"),293,49,true);},381);return;}string kind=mini.Kind;Modal(T("Restart this level?","إعادة هذا المستوى؟"),c=>{Label(c,T("Start this board again with fresh lives. Your saved level stays the same.","ابدأ هذه اللوحة مجدداً بمحاولات كاملة. يبقى مستواك المحفوظ كما هو."),97,112,21,595);Btn(c,T("Keep playing","متابعة اللعب"),103,270,CloseModal,266,49);Btn(c,T("Restart level","إعادة المستوى"),399,270,()=>StartMiniGame(kind),293,49,true);},381);}
  void MiniFailure(){if(mini==null)return;if(mini.Stack!=null){StackFailure();return;}if(mini.Dash!=null){Modal(T("One more dash?","محاولة أخرى؟"),c=>{Label(c,T("You lasted ","صمدت ")+DashTime(mini.Dash.Seconds)+"\n"+T("Personal best: ","أفضل رقم: ")+DashTime(Profile.MiniGames["mousedash"].BestSeconds),99,110,23,594);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),101,272,BackToMiniGames,281,50);Btn(c,T("Play again","العب مجدداً"),402,272,()=>StartMiniGame("mousedash"),287,50,true);},385);return;}string kind=mini.Kind;Modal(T("A fresh try?","محاولة جديدة؟"),c=>{Label(c,T("You're still on level ","ما زلت في المستوى ")+mini.Level+T(". Restart the full board with three lives, or come back later.",". أعد اللوحة كاملة بثلاث محاولات أو عد لاحقاً."),99,110,21,594);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),101,272,BackToMiniGames,281,50);Btn(c,T("Restart level","إعادة المستوى"),402,272,()=>StartMiniGame(kind),287,50,true);},385);}
  void PauseMiniGame(){if(mini==null||overlay.Children.Count>0)return;if(mini.Dash!=null){mini.ReleaseDash();miniSurface.CancelDashPointer();RememberDash(false);}Modal(T("A little pause","استراحة صغيرة"),c=>{Label(c,T("Everything is paused, including the timer and moving pieces.","كل شيء متوقف، بما في ذلك المؤقت والعناصر المتحركة."),99,109,20,590);Btn(c,T("Keep playing","متابعة اللعب"),214,191,CloseModal,365,54,true);Btn(c,mini.Dash!=null||mini.Stack!=null?T("New run","محاولة جديدة"):T("Restart level","إعادة المستوى"),214,263,()=>StartMiniGame(Profile.Settings.SelectedMiniGame),365,54);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),214,335,BackToMiniGames,365,54);},456);}
  void ConfirmResetMiniProgress(){string kind=Profile.Settings.SelectedMiniGame;if(kind=="stack"){ResetStackBest();return;}if(kind=="mousedash"){Modal(T("Reset Ladybug Dodge records?","مسح أرقام مراوغة الفأرة؟"),c=>{Label(c,T("Clear this game's survival times and run count. Your earned stars and other games stay as they are.","امسح أوقات الصمود وعدد المحاولات لهذه اللعبة. تبقى النجوم والألعاب الأخرى كما هي."),98,110,20,595);Btn(c,T("Cancel","إلغاء"),103,277,CloseModal,266,49);Put(c,DangerButton(T("Reset records","مسح الأرقام"),()=>{var record=Profile.MiniGames[kind];record.BestSeconds=record.LastSeconds=0;record.Completed=record.Failures=0;Save();page="minigames";paused=false;Build();},293,49),399,277);},388);return;}Modal(T("Reset ","إعادة ")+MiniName(kind)+"?",c=>{Label(c,T("Return this game to level 1. Your other games, best record, earned stars and notebook stay as they are.","أعد هذه اللعبة إلى المستوى ١. تبقى الألعاب الأخرى والرقم الأفضل والنجوم والدفتر كما هي."),98,110,20,595);Btn(c,T("Cancel","إلغاء"),103,277,CloseModal,266,49);Put(c,DangerButton(T("Reset to level 1","العودة للمستوى ١"),()=>{Profile.MiniGames[kind].Level=1;Save();page="minigames";paused=false;Build();},293,49),399,277);},388);}
  bool MiniKey(KeyEventArgs e){
   if(Keyboard.Modifiers!=ModifierKeys.None&&!(mini!=null&&mini.Extra!=null&&e.Key==Key.Z&&Keyboard.Modifiers==ModifierKeys.Control))return false;
   if(page!="minigame"||mini==null||paused||overlay.Children.Count>0)return false;
   if(mini.Extra!=null)return ExtraKey(e);
   if(mini.Dash!=null)return false;
   if(mini.Stack!=null){if(e.Key==Key.E||e.Key==Key.Space||e.Key==Key.Enter){if(!e.IsRepeat)MiniPress(0);return true;}return false;}
   if(mini.Kind=="pulse"){if(e.Key.ToString()==mini.PulseKey.ToString()){if(!e.IsRepeat)MiniPress(0);return true;}return false;}
   if(Keyboard.FocusedElement is Button||Keyboard.FocusedElement is TextBox||Keyboard.FocusedElement is Slider)return false;
   int direction=MiniDirection(e.Key);
   if(mini.Kind=="echo"){int index=Array.IndexOf(new[]{Key.A,Key.S,Key.D,Key.F,Key.J,Key.K,Key.L,Key.OemSemicolon},e.Key);if(index>=0&&index<mini.PadCount){if(!e.IsRepeat)MiniPress(index);return true;}if(e.Key==Key.R){mini.ReplayEcho();return true;}return false;}
   if(direction>=0){
    if(mini.Kind=="tiles"){int next=MiniGrid.Next(mini.Tiles.Blank,direction,mini.Tiles.Size,mini.Tiles.Size);if(next>=0)MiniPress(next);}
    else if(mini.Kind=="maze"){MazeKeyDown(e.Key,e.IsRepeat);RefreshMiniHud();}
    else if(mini.Kind=="arrows"){
     var board=mini.Arrows;var choices=Enumerable.Range(0,board.Arrows.Count).Where(i=>!board.Arrows[i].Removed).ToList();if(choices.Count>0){int current=Math.Max(0,Math.Min(board.Arrows.Count-1,mini.SelectedCell)),head=board.Arrows[current].Head;var ahead=choices.Where(i=>{int cell=board.Arrows[i].Head;return (cell%board.Size-head%board.Size)*MiniGrid.Dx[direction]+(cell/board.Size-head/board.Size)*MiniGrid.Dy[direction]>0;}).ToList();mini.SelectedCell=(ahead.Count>0?ahead:choices).OrderBy(i=>{int cell=board.Arrows[i].Head;return Math.Abs(cell%board.Size-head%board.Size)+Math.Abs(cell/board.Size-head/board.Size);}).First();KeepMiniCellVisible(board.Arrows[mini.SelectedCell].Head);}}
    else{int width=mini.Signal.Width,height=mini.Signal.Height,next=MiniGrid.Next(mini.SelectedCell,direction,width,height);if(next>=0){mini.SelectedCell=next;KeepMiniCellVisible(next);}}
    miniSurface.InvalidateVisual();return true;
   }
   if(e.Key==Key.Enter||e.Key==Key.Space){if(!e.IsRepeat)MiniPress(mini.SelectedCell);return true;}return false;
  }
  static int MiniDirection(Key key){return key==Key.Up||key==Key.W?0:key==Key.Right||key==Key.D?1:key==Key.Down||key==Key.S?2:key==Key.Left||key==Key.A?3:-1;}
  void MazeKeyDown(Key key,bool repeat){if(repeat||miniHeldKeys.Contains(key)||mini==null||mini.Maze==null)return;miniHeldKeys.Add(key);mini.HoldMaze(MiniDirection(key));}
  bool MiniKeyUp(Key key){if(!miniHeldKeys.Remove(key))return false;if(mini!=null&&mini.Maze!=null)mini.HoldMaze(miniHeldKeys.Count==0?-1:MiniDirection(miniHeldKeys.Last()));return true;}
  void ClearMiniHeldInput(){miniHeldKeys.Clear();if(miniSurface!=null&&mini!=null&&mini.Extra!=null&&mini.Extra.Kind=="untangle"&&miniSurface.IsMouseCaptured){mini.Extra.EndDrag();miniSurface.ReleaseMouseCapture();mini.CheckExtra();miniSurface.InvalidateVisual();}if(mini!=null&&mini.Maze!=null)mini.ReleaseMaze();}
  void KeepMiniMazeVisible(){if(miniSurface==null||miniScroll==null||mini==null||mini.Maze==null)return;double x=(28+(mini.MazeX+.5)*32)*miniZoom,y=(28+(mini.MazeY+.5)*32)*miniZoom,pad=28*miniZoom;
   if(x-pad<miniScroll.HorizontalOffset)miniScroll.ScrollToHorizontalOffset(x-pad);else if(x+pad>miniScroll.HorizontalOffset+miniScroll.ViewportWidth)miniScroll.ScrollToHorizontalOffset(x+pad-miniScroll.ViewportWidth);
   if(y-pad<miniScroll.VerticalOffset)miniScroll.ScrollToVerticalOffset(y-pad);else if(y+pad>miniScroll.VerticalOffset+miniScroll.ViewportHeight)miniScroll.ScrollToVerticalOffset(y+pad-miniScroll.ViewportHeight);
  }
  void KeepMiniCellVisible(int cell){if(miniSurface==null||miniScroll==null)return;Rect rect=miniSurface.CellRect(cell);double left=rect.X*miniZoom,top=rect.Y*miniZoom;if(left<miniScroll.HorizontalOffset)miniScroll.ScrollToHorizontalOffset(left);else if(left+rect.Width*miniZoom>miniScroll.HorizontalOffset+miniScroll.ViewportWidth)miniScroll.ScrollToHorizontalOffset(left+rect.Width*miniZoom-miniScroll.ViewportWidth);if(top<miniScroll.VerticalOffset)miniScroll.ScrollToVerticalOffset(top);else if(top+rect.Height*miniZoom>miniScroll.VerticalOffset+miniScroll.ViewportHeight)miniScroll.ScrollToVerticalOffset(top+rect.Height*miniZoom-miniScroll.ViewportHeight);}
 }}
