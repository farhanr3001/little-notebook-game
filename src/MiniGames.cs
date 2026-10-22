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
  MiniSession mini;MiniSurface miniSurface;ScrollViewer miniScroll;DispatcherTimer miniTimer;DateTime miniLast;
  TextBlock miniStatus,miniLives,miniCounter;double miniZoom=1;
  Button miniReplay;
  ScrollViewer miniSelectionScroll;double miniSelectionOffset;
  string MiniName(string key){switch(key){case "arrows":return T("Arrow Escape","هروب الأسهم");case "signal":return T("Signal Path","مسار الإشارة");case "pulse":return T("Pulse Lock","قفل النبض");case "lanterns":return T("Lanterns","الفوانيس");case "tiles":return T("Sliding Tiles","البلاطات المنزلقة");case "echo":return T("Echo Keys","مفاتيح الصدى");default:return T("Starlight Maze","متاهة النجوم");}}
  string MiniDescription(string key){switch(key){case "arrows":return T("Clear winding arrows in the right order. Three hearts, piano notes and bigger boards.","أخرج الأسهم بالترتيب الصحيح. ثلاث محاولات ونغمات بيانو ولوحات أكبر.");case "signal":return T("Rotate the little pipes to carry a signal from IN to OUT.","دوّر الأنابيب لوصل الإشارة من المدخل إلى المخرج.");case "pulse":return T("Tap the shown key or click the ring. Open three latches, then take a breath.","اضغط المفتاح الظاهر أو انقر الحلقة. افتح ثلاثة أقفال ثم استرح.");case "lanterns":return T("Switch off every lantern. Each tap also changes its neighbours.","أطفئ كل الفوانيس. كل ضغطة تغيّر الفوانيس المجاورة أيضاً.");case "tiles":return T("Slide tiles into order, leaving the empty space at the bottom right.","رتّب البلاطات واترك الفراغ في أسفل اليمين.");case "echo":return T("Watch and listen, then play the little melody back.","شاهد واستمع، ثم أعد عزف النغمات بالترتيب.");default:return T("Find a way through the winding paper maze to the golden star.","اعثر على طريق عبر متاهة الورق إلى النجمة الذهبية.");}}
  void PlayTabs(bool games){
   var learning=Button(T("Learning","التعلّم"),()=>SwitchPlaySection(false),160,32,!games);learning.FontSize=14*Profile.Settings.TextScale;Put(body,learning,597,-7);
   var minigames=Button(T("Minigames","ألعاب مصغّرة"),()=>SwitchPlaySection(true),178,32,games);minigames.FontSize=14*Profile.Settings.TextScale;Put(body,minigames,765,-7);
  }
  void SwitchPlaySection(bool games){if(page=="minigame")StopMiniGame();Profile.Settings.PlaySection=games?"minigames":"learning";Navigate(games?"minigames":"home");}
  void MiniGamesView(){
   MiniCatalog.Normalize(Profile);Header(T("PLAY","اللعب"),T("A little break","استراحة صغيرة"),T("Pick a game. Your level is saved separately in every minigame.","اختر لعبة. يُحفظ مستواك في كل لعبة مصغّرة بشكل مستقل."));PlayTabs(true);
   string selected=Profile.Settings.SelectedMiniGame;var cards=new WrapPanel{Width=942,Orientation=Orientation.Horizontal};
   foreach(string key in MiniCatalog.Keys){string kind=key;bool active=selected==key;var progress=Profile.MiniGames[key];var button=Button("",()=>{Profile.Settings.SelectedMiniGame=kind;Save();Build();},446,117,active);button.Margin=new Thickness(4,2,10,10);button.ToolTip=MiniDescription(kind);AutomationProperties.SetName(button,MiniName(kind)+T(" · level "," · المستوى ")+progress.Level);
    var card=new Canvas{Width=418,Height=104};var artPalette=active?p.OnAccent():p;Put(card,new MiniGameBadge(kind,artPalette),Ar?365:9,12);
    var title=Label(card,MiniName(kind),Ar?12:62,5,21,334);title.Foreground=B(active?p.AccentInk:p.Ink);
    var description=Label(card,MiniDescription(kind),Ar?12:62,37,13,334);description.Foreground=B(active?p.AccentInk:p.Ink);description.MaxHeight=37;
    var level=Label(card,T("Level ","المستوى ")+progress.Level+"  ·  "+T("Best ","الأفضل ")+progress.BestCompleted,Ar?12:62,78,12,334);level.Foreground=B(active?p.AccentInk:p.Muted);button.Content=card;cards.Children.Add(button);
   }
   var scroll=new ScrollViewer{Width=952,Height=496,Content=cards,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(body,scroll,8,133);miniSelectionScroll=scroll;scroll.ApplyTemplate();scroll.Measure(new Size(952,496));scroll.Arrange(new Rect(0,0,952,496));scroll.UpdateLayout();scroll.ScrollToVerticalOffset(miniSelectionOffset);scroll.UpdateLayout();
   Btn(body,T("Reset this game's progress…","إعادة تقدّم هذه اللعبة…"),17,656,ConfirmResetMiniProgress,285,42);
   var start=Button(T("Start game  →","ابدأ اللعبة  ←"),()=>StartMiniGame(Profile.Settings.SelectedMiniGame),344,50,true);start.Background=B(Color.FromRgb(139,255,22));start.Foreground=B(Color.FromRgb(41,56,74));Put(body,start,533,651);
   Label(body,T("New boards grow with you. Take your time, or chase a new personal best.","تزداد الصعوبة مع التقدّم. خذ وقتك أو حقق رقماً أفضل."),17,715,12,900);
  }
  void StartMiniGame(string kind){
   MiniCatalog.Normalize(Profile);if(!MiniCatalog.Keys.Contains(kind))return;audio.StopSpeech();StopMiniGame();Profile.Settings.PlaySection="minigames";Profile.Settings.SelectedMiniGame=kind;
   var progress=Profile.MiniGames[kind];mini=new MiniSession(kind,progress.Level,MiniCatalog.LevelSeed(progress));
   mini.Note+=audio.MiniNote;mini.Won+=MiniLevelWon;mini.Lost+=MiniLevelLost;mini.Next+=()=>StartMiniGame(kind);
   page="minigame";paused=false;miniZoom=1;Save();Build();
   miniLast=DateTime.UtcNow;if(miniTimer==null){miniTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(30)};miniTimer.Tick+=MiniTick;}miniTimer.Start();FocusMiniSurface();
  }
  void StopMiniGame(){if(miniTimer!=null)miniTimer.Stop();if(mini!=null){mini.Note-=audio.MiniNote;mini.Won-=MiniLevelWon;mini.Lost-=MiniLevelLost;mini=null;}miniSurface=null;miniScroll=null;audio.StopMiniSounds();}
  void MiniLevelWon(){
   if(mini==null)return;var progress=Profile.MiniGames[mini.Kind];if(mini.Level>progress.BestCompleted){progress.BestCompleted=mini.Level;Profile.Coins+=2;}progress.Completed++;progress.Level=Math.Min(100000,mini.Level+1);Save();audio.MiniWin();
  }
  void MiniLevelLost(){if(mini==null)return;Profile.MiniGames[mini.Kind].Failures++;Save();MiniFailure();}
  void MiniTick(object sender,EventArgs args){
   var now=DateTime.UtcNow;double elapsed=Math.Min(.1,(now-miniLast).TotalSeconds);miniLast=now;
   if(mini==null||page!="minigame"||paused||!IsActive)return;
   var before=mini;mini.Tick(elapsed);if(mini!=before)return;RefreshMiniHud();
   if(mini.Phase!="playing"||mini.Kind=="pulse"||mini.TurnTime<.18||mini.MoveTime<.18||mini.FeedbackTime>0)miniSurface.InvalidateVisual();
  }
  void MiniGameView(){
   if(mini==null){page="minigames";MiniGamesView();return;}
   Header(T("MINIGAMES","الألعاب المصغّرة"),MiniName(mini.Kind),MiniDescription(mini.Kind));
   Label(body,T("Level ","المستوى ")+mini.Level,16,133,21,240);
   miniLives=Label(body,"",260,132,25,220,true);miniLives.Foreground=B(Color.FromRgb(187,91,91));miniLives.TextAlignment=TextAlignment.Center;
   miniCounter=Label(body,"",490,139,15,240);miniCounter.TextAlignment=TextAlignment.Right;
   miniReplay=null;if(mini.Kind=="echo"){miniReplay=Button(T("Replay melody","إعادة اللحن"),()=>{mini.ReplayEcho();RefreshMiniHud();miniSurface.InvalidateVisual();FocusMiniSurface();},181,34);Put(body,miniReplay,754,132);}else{Btn(body,"−",754,132,()=>ZoomMini(.8),42,34);Btn(body,"+",803,132,()=>ZoomMini(1.25),42,34);Btn(body,T("Fit","ملاءمة"),852,132,()=>ZoomMini(0),83,34);}
   miniSurface=new MiniSurface(mini,p,Ar,Profile.Settings.ReducedMotion,Profile.Settings.TextScale);AutomationProperties.SetName(miniSurface,MiniName(mini.Kind)+". "+MiniDescription(mini.Kind));miniSurface.Press=MiniPress;
   miniScroll=new ScrollViewer{Width=944,Height=446,Content=miniSurface,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalContentAlignment=HorizontalAlignment.Center,VerticalContentAlignment=VerticalAlignment.Center,CanContentScroll=false};StylePaperScroll(miniScroll);miniSurface.Viewport=miniScroll;miniSurface.LayoutTransform=new ScaleTransform(miniZoom,miniZoom);
   Put(body,new Border{Width=946,Height=448,BorderBrush=B(p.Line),BorderThickness=new Thickness(1),Child=miniScroll},8,184);
   miniStatus=Label(body,"",14,639,17,928);miniStatus.TextAlignment=TextAlignment.Center;
   string help=mini.Kind=="arrows"?T("Click an arrow · drag empty paper to pan · arrow keys select, Enter releases","انقر سهماً · اسحب الورق الفارغ للتحريك · الأسهم للتحديد وEnter للإخراج"):mini.Kind=="signal"||mini.Kind=="lanterns"?T("Click a tile · arrow keys select · Space / Enter to use","انقر بلاطة · الأسهم للتحديد · مسافة أو Enter للاستخدام"):mini.Kind=="tiles"||mini.Kind=="maze"?T("Click a neighbouring space or use arrow keys / W A S D","انقر خانة مجاورة أو استخدم الأسهم / W A S D"):mini.Kind=="echo"?T("Click the keys, or use their letters on your keyboard","انقر المفاتيح أو استخدم حروفها على لوحة المفاتيح"):T("Separate key presses or clicks count; holding a key does not","كل ضغطة أو نقرة مستقلة تُحسب؛ الضغط المستمر لا يُحسب");
   Label(body,help,14,671,12,930).TextAlignment=TextAlignment.Center;
   Btn(body,T("‹ Minigames","‹ الألعاب المصغّرة"),13,707,BackToMiniGames,230,39);Btn(body,T("Restart level","إعادة المستوى"),327,707,ConfirmRestartMini,238,39);Btn(body,T("Pause","إيقاف مؤقت"),708,707,PauseMiniGame,223,39);
   RefreshMiniHud();
  }
  void RefreshMiniHud(){
   if(miniReplay!=null&&mini!=null)miniReplay.IsEnabled=mini.Phase=="playing";
   if(mini==null||miniStatus==null)return;miniLives.Text=mini.HasLives?new string('♥',Math.Max(0,mini.Lives))+new string('♡',3-Math.Max(0,mini.Lives)):"";
   miniCounter.Text=mini.Kind=="arrows"?mini.Arrows.Remaining+T(" arrows left"," أسهم متبقية"):mini.Kind=="pulse"?T("Latch ","القفل ")+Math.Min(3,mini.Latch+1)+" / 3":mini.Kind=="echo"?Math.Min(mini.Pattern.Length,mini.PatternIndex)+" / "+mini.Pattern.Length:mini.Moves+T(" moves"," حركات");
   if(mini.Phase=="complete")miniStatus.Text=mini.Kind=="pulse"?T("Lock opened · take a breath · next round in ","فُتح القفل · استرح · الجولة التالية خلال ")+Math.Ceiling(Math.Max(0,mini.Wait))+"s":T("Level clear!  ✦  Next board…","اكتمل المستوى! ✦ اللوحة التالية…");
   else if(mini.Phase=="failed")miniStatus.Text=T("Your level is saved. Try the same board again.","مستواك محفوظ. حاول حل اللوحة نفسها مجدداً.");
   else if(mini.Kind=="echo")miniStatus.Text=mini.Phase=="showing"?T("Watch and listen…","شاهد واستمع…"):T("Your turn · repeat the melody","دورك · أعد النغمات");
   else if(mini.Kind=="pulse")miniStatus.Text=mini.Phase=="ready"?T("Ready in ","استعد خلال ")+Math.Ceiling(Math.Max(0,mini.Wait))+"…":mini.Phase=="latch"?T("One latch open!","فُتح أحد الأقفال!"):T("Tap ","اضغط ")+mini.PulseKey+T(" or click · "," أو انقر · ")+Math.Ceiling(mini.TimeLeft)+"s";
   else if(mini.Kind=="arrows"&&mini.WrongFeedback&&mini.FeedbackTime>0)miniStatus.Text=T("That arrow is blocked. It returns to its place.","هذا السهم محجوب. سيعود إلى موضعه.");
   else miniStatus.Text=mini.Kind=="arrows"?T("Follow the arrowhead's exit lane before you click.","تتبّع طريق خروج رأس السهم قبل النقر."):mini.Kind=="signal"?T("Lit pipes carry the signal. Connect them all the way to OUT.","الأنابيب المضيئة تحمل الإشارة. صِلها حتى المخرج."):mini.Kind=="lanterns"?T("A tap changes a cross of lanterns. Turn every light off.","الضغطة تغيّر فانوساً وجيرانه. أطفئها جميعاً."):mini.Kind=="tiles"?T("1, 2, 3… in order, with the empty space last.","١، ٢، ٣… بالترتيب، والفراغ أخيراً."):T("Find the golden star. Drag the paper to explore larger mazes.","اعثر على النجمة الذهبية. اسحب الورق لاستكشاف المتاهات الكبيرة.");
  }
  void MiniPress(int index){
   if(mini==null||paused||overlay.Children.Count>0)return;bool used=false;
   switch(mini.Kind){case "arrows":mini.SelectedCell=index;used=mini.ArrowTap(index);break;case "signal":mini.SelectedCell=index;used=mini.RotateSignal(index);break;case "lanterns":mini.SelectedCell=index;used=mini.PressLantern(index);break;case "tiles":used=mini.SlideTile(index);break;case "echo":used=mini.EchoTap(index);break;case "pulse":used=mini.PulseTap();break;case "maze":for(int d=0;d<4;d++)if(MiniGrid.Next(mini.Maze.Player,d,mini.Maze.Size,mini.Maze.Size)==index){used=mini.MoveMaze(d);break;}break;}
   if(used&&mini.Kind!="arrows"&&mini.Kind!="echo")audio.Fx("click");RefreshMiniHud();if(miniSurface!=null)miniSurface.InvalidateVisual();
  }
  void ZoomMini(double factor){if(miniSurface==null||miniScroll==null)return;double old=miniZoom;miniZoom=factor==0?Math.Max(.10,Math.Min(1,Math.Min(920/miniSurface.Width,424/miniSurface.Height))):Math.Max(.10,Math.Min(1.7,miniZoom*factor));double centerX=(miniScroll.HorizontalOffset+miniScroll.ViewportWidth/2)/old,centerY=(miniScroll.VerticalOffset+miniScroll.ViewportHeight/2)/old;miniSurface.LayoutTransform=new ScaleTransform(miniZoom,miniZoom);miniScroll.UpdateLayout();miniScroll.ScrollToHorizontalOffset(centerX*miniZoom-miniScroll.ViewportWidth/2);miniScroll.ScrollToVerticalOffset(centerY*miniZoom-miniScroll.ViewportHeight/2);FocusMiniSurface();}
  void FocusMiniSurface(){if(page=="minigame"&&miniSurface!=null&&overlay.Children.Count==0)miniSurface.Focus();}
  void BackToMiniGames(){StopMiniGame();paused=false;page="minigames";Save();Build();}
  void ConfirmRestartMini(){if(mini==null)return;string kind=mini.Kind;Modal(T("Restart this level?","إعادة هذا المستوى؟"),c=>{Label(c,T("Start this board again with fresh lives. Your saved level stays the same.","ابدأ هذه اللوحة مجدداً بمحاولات كاملة. يبقى مستواك المحفوظ كما هو."),97,112,21,595);Btn(c,T("Keep playing","متابعة اللعب"),103,270,CloseModal,266,49);Btn(c,T("Restart level","إعادة المستوى"),399,270,()=>StartMiniGame(kind),293,49,true);},381);}
  void MiniFailure(){if(mini==null)return;string kind=mini.Kind;Modal(T("A fresh try?","محاولة جديدة؟"),c=>{Label(c,T("You're still on level ","ما زلت في المستوى ")+mini.Level+T(". Restart the full board with three lives, or come back later.",". أعد اللوحة كاملة بثلاث محاولات أو عد لاحقاً."),99,110,21,594);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),101,272,BackToMiniGames,281,50);Btn(c,T("Restart level","إعادة المستوى"),402,272,()=>StartMiniGame(kind),287,50,true);},385);}
  void PauseMiniGame(){if(mini==null||overlay.Children.Count>0)return;Modal(T("A little pause","استراحة صغيرة"),c=>{Label(c,T("Everything is paused, including the timer and any moving arrows.","كل شيء متوقف، بما في ذلك المؤقت والأسهم المتحركة."),99,109,20,590);Btn(c,T("Keep playing","متابعة اللعب"),214,191,CloseModal,365,54,true);Btn(c,T("Restart level","إعادة المستوى"),214,263,()=>StartMiniGame(Profile.Settings.SelectedMiniGame),365,54);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),214,335,BackToMiniGames,365,54);},456);}
  void ConfirmResetMiniProgress(){string kind=Profile.Settings.SelectedMiniGame;Modal(T("Reset ","إعادة ")+MiniName(kind)+"?",c=>{Label(c,T("Return this game to level 1. Your other games, best record, earned stars and notebook stay as they are.","أعد هذه اللعبة إلى المستوى ١. تبقى الألعاب الأخرى والرقم الأفضل والنجوم والدفتر كما هي."),98,110,20,595);Btn(c,T("Cancel","إلغاء"),103,277,CloseModal,266,49);Put(c,DangerButton(T("Reset to level 1","العودة للمستوى ١"),()=>{Profile.MiniGames[kind].Level=1;Save();page="minigames";paused=false;Build();},293,49),399,277);},388);}
  bool MiniKey(KeyEventArgs e){
   if(Keyboard.Modifiers!=ModifierKeys.None)return false;
   if(page!="minigame"||mini==null||paused||overlay.Children.Count>0)return false;
   if(mini.Kind=="pulse"){if(e.Key.ToString()==mini.PulseKey.ToString()){if(!e.IsRepeat)MiniPress(0);return true;}return false;}
   if(Keyboard.FocusedElement is Button||Keyboard.FocusedElement is TextBox||Keyboard.FocusedElement is Slider)return false;
   int direction=e.Key==Key.Up||e.Key==Key.W?0:e.Key==Key.Right||e.Key==Key.D?1:e.Key==Key.Down||e.Key==Key.S?2:e.Key==Key.Left||e.Key==Key.A?3:-1;
   if(mini.Kind=="echo"){int index=Array.IndexOf(new[]{Key.A,Key.S,Key.D,Key.F,Key.J,Key.K,Key.L,Key.OemSemicolon},e.Key);if(index>=0&&index<mini.PadCount){if(!e.IsRepeat)MiniPress(index);return true;}if(e.Key==Key.R){mini.ReplayEcho();return true;}return false;}
   if(direction>=0){
    if(mini.Kind=="tiles"){int next=MiniGrid.Next(mini.Tiles.Blank,direction,mini.Tiles.Size,mini.Tiles.Size);if(next>=0)MiniPress(next);}
    else if(mini.Kind=="maze"){if(mini.MoveMaze(direction)){audio.Fx("click");KeepMiniCellVisible(mini.Maze.Player);}RefreshMiniHud();}
    else if(mini.Kind=="arrows"){
     var board=mini.Arrows;var choices=Enumerable.Range(0,board.Arrows.Count).Where(i=>!board.Arrows[i].Removed).ToList();if(choices.Count>0){int current=Math.Max(0,Math.Min(board.Arrows.Count-1,mini.SelectedCell)),head=board.Arrows[current].Head;var ahead=choices.Where(i=>{int cell=board.Arrows[i].Head;return (cell%board.Size-head%board.Size)*MiniGrid.Dx[direction]+(cell/board.Size-head/board.Size)*MiniGrid.Dy[direction]>0;}).ToList();mini.SelectedCell=(ahead.Count>0?ahead:choices).OrderBy(i=>{int cell=board.Arrows[i].Head;return Math.Abs(cell%board.Size-head%board.Size)+Math.Abs(cell/board.Size-head/board.Size);}).First();KeepMiniCellVisible(board.Arrows[mini.SelectedCell].Head);}}
    else{int width=mini.Kind=="signal"?mini.Signal.Width:mini.Lanterns.Size,height=mini.Kind=="signal"?mini.Signal.Height:width,next=MiniGrid.Next(mini.SelectedCell,direction,width,height);if(next>=0){mini.SelectedCell=next;KeepMiniCellVisible(next);}}
    miniSurface.InvalidateVisual();return true;
   }
   if(e.Key==Key.Enter||e.Key==Key.Space){if(!e.IsRepeat)MiniPress(mini.SelectedCell);return true;}return false;
  }
  void KeepMiniCellVisible(int cell){if(miniSurface==null||miniScroll==null)return;Rect rect=miniSurface.CellRect(cell);double left=rect.X*miniZoom,top=rect.Y*miniZoom;if(left<miniScroll.HorizontalOffset)miniScroll.ScrollToHorizontalOffset(left);else if(left+rect.Width*miniZoom>miniScroll.HorizontalOffset+miniScroll.ViewportWidth)miniScroll.ScrollToHorizontalOffset(left+rect.Width*miniZoom-miniScroll.ViewportWidth);if(top<miniScroll.VerticalOffset)miniScroll.ScrollToVerticalOffset(top);else if(top+rect.Height*miniZoom>miniScroll.VerticalOffset+miniScroll.ViewportHeight)miniScroll.ScrollToVerticalOffset(top+rect.Height*miniZoom-miniScroll.ViewportHeight);}
 }}
