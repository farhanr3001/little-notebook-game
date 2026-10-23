using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Web.Script.Serialization;
using Path=System.IO.Path;

namespace LittleNotebook {
 public partial class MainWindow : Window {
  public Profile Profile;
  SaveStore store; QuestionBank bank; AudioSystem audio; Palette p;
  Canvas stage,body,overlay,radioWidget; Grid root;
  string page="welcome", selectedTopic="mixed", studyTopic="english";
  bool editingCharacter=false, paused=false, closing=false;
  int studyIndex=0; bool studyRevealed=false;
  GameSession game; QuestionStream questionStream;TextBlock clockText,scoreText,lifeText,feedbackText;double answerFeedbackElapsed=0;
  List<Button> answerButtons=new List<Button>();
  DispatcherTimer timer; DateTime lastTick;
  Dictionary<string,string> arabicOptions;
  Dictionary<UIElement,bool> modalEnabled=new Dictionary<UIElement,bool>();
  string baseDir=AppDomain.CurrentDomain.BaseDirectory;
  public bool Ar {get{return Profile.Settings.Language=="ar";}}
  string T(string en,string ar){return Ar?ar:en;}
  Brush B(Color c){return new SolidColorBrush(c);}
  string Topic(string s){switch(s){case "math":return T("Math","الرياضيات");case "english":return T("English","الإنجليزية");case "puzzles":return T("Puzzles","الألغاز");case "general":return T("General Knowledge","المعرفة العامة");default:return T("All topics","جميع المواضيع");}}
  string Difficulty(int d){return new[]{T("Easy","سهل"),T("Medium","متوسط"),T("Hard","صعب"),T("Mixed","مختلط")}[d];}
  public MainWindow(bool test=false,string testFile="ui-profile.json") {
   Title="Little Notebook · a little learning, every day";Width=1460;Height=960;MinWidth=1000;MinHeight=690;WindowStartupLocation=WindowStartupLocation.CenterScreen;
   string iconPath=Path.Combine(baseDir,"assets","LittleNotebook.ico");if(File.Exists(iconPath))Icon=System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath));
   AppPaths.Initialize(test);string savePath=Path.Combine(AppPaths.DataDirectory,"profile.json");
   if(test)savePath=Path.Combine(baseDir,"..","test-output",testFile);
   store=new SaveStore(savePath,test?null:Path.Combine(baseDir,"data","profile.json"));Profile=store.Load();AppPaths.FollowSave(store.Path);p=new Palette(Profile.Settings.Theme);
   TraceLibrary.Initialize();bank=new QuestionBank(Path.Combine(baseDir,"content/questions.json"));
   arabicOptions=new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(baseDir,"content/arabic-options.json")));
   audio=new AudioSystem(Profile.Settings);
   if(test){if(Profile.Settings.Music)audio.Toggle();Profile.Settings.Music=false;audio.ApplyVolume();}
   selectedTopic=Profile.Settings.Topics??"mixed";if(selectedTopic!="mixed"&&!selectedTopic.Split(',').Any(PlayTopics.Contains))selectedTopic="mixed";
   MiniCatalog.Normalize(Profile);page=string.IsNullOrWhiteSpace(Profile.Name)?"welcome":Profile.Settings.PlaySection=="minigames"?"minigames":"home";
   Build();
   audio.TrackChanged+=()=>{Save();if(radioWidget!=null)RefreshRadio(radioWidget);};
   timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)};lastTick=DateTime.UtcNow;timer.Tick+=Tick;timer.Start();
   Activated+=(s,e)=>{FocusAnswerInput();FocusMiniSurface();};
   Deactivated+=(s,e)=>{if(game!=null&&page=="game"&&overlay.Children.Count==0)PauseMenu();else if(page=="minigame"&&mini!=null&&overlay.Children.Count==0)PauseMiniGame();};
   Closing+=(s,e)=>{if(!closing && game!=null && !game.Recorded){e.Cancel=true;ConfirmLeave(true);}else {closing=true;CancelMusicDownload();timer.Stop();notebookSaveTimer.Stop();StopMiniGame();try{Save();}finally{audio.Dispose();}}};
   PreviewKeyDown+=(s,e)=>{if(e.Key==Key.F11){if(!e.IsRepeat)SetDisplayMode(!Profile.Settings.BorderlessFullscreen);e.Handled=true;}if(e.Key==Key.B&&Keyboard.Modifiers==ModifierKeys.Control){ToggleSidebar();e.Handled=true;}if(e.Key==Key.Escape){if(overlay.Children.Count>0)CloseModal();else if(page=="minigame")PauseMiniGame();else PauseMenu();e.Handled=true;}else if(MiniKey(e))e.Handled=true;};
   PreviewKeyUp+=(s,e)=>{if(MiniKeyUp(e.Key))e.Handled=true;};
   if(!test)Loaded+=(s,e)=>{ApplyDisplayMode();CheckForUpdates(true);};
   if(!string.IsNullOrEmpty(store.Warning))Loaded+=(s,e)=>Message(T("Save recovery","استعادة الحفظ"),store.Warning);
  }
  bool Save(){FlushNotebook();bool saved=store.Save(Profile);if(!Diagnostics.Testing)AppPaths.FollowSave(store.Path);Title=saved?"Little Notebook · a little learning, every day":"Little Notebook · "+store.Warning;return saved;}
  void Tick(object sender,EventArgs e){
   var now=DateTime.UtcNow;double elapsed=Math.Min(1,(now-lastTick).TotalSeconds);lastTick=now;
   if(game==null||page!="game"||paused||!IsActive||audio.IsPreparingQuestion)return;
   AdvanceListeningAutoplay(elapsed);
   game.Seconds+=elapsed;Profile.PlaySeconds+=elapsed;
   if(!game.Answered){game.QuestionSeconds+=elapsed;UpdateClock();if(Profile.Settings.Timer&&game.QuestionSeconds>=game.Limit)Submit("",true);}
   else AdvanceFeedback(elapsed);
  }
  void AdvanceFeedback(double elapsed){if(game==null||page!="game"||paused||!game.Answered)return;answerFeedbackElapsed+=elapsed;if(answerFeedbackElapsed>=((game.Current.Category=="spelling-repair"||game.Current.Category=="dictionary")?12:1.35))NextQuestion();}
  void UpdateClock(){if(clockText!=null&&game!=null){clockText.Text=Profile.Settings.Timer?"◷ "+Math.Max(0,(int)Math.Ceiling(game.Limit-game.QuestionSeconds))+" s":T("∞  Take your time","∞  خذ وقتك");clockText.Foreground=B(Profile.Settings.Timer&&game.Limit-game.QuestionSeconds<10?Color.FromRgb(184,90,69):p.Muted);}}
  void Put(Canvas c,UIElement e,double x,double y){var b=e as Button;if(b!=null&&b.Tag is Size){var slot=(Size)b.Tag;x+=(slot.Width-b.Width)/2;y+=(slot.Height-b.Height)/2;}Canvas.SetLeft(e,x);Canvas.SetTop(e,y);c.Children.Add(e);}
  TextBlock Text(string s,double size=22,double width=double.NaN, bool english=false){
   return new TextBlock{Text=s,FontFamily=new FontFamily(Ar&&!english?"Segoe UI":"Segoe Print"),FontSize=size*Profile.Settings.TextScale,Foreground=B(p.Ink),TextWrapping=TextWrapping.Wrap,Width=width,FlowDirection=Ar&&!english?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
  }
  TextBlock Label(Canvas c,string s,double x,double y,double size=22,double width=double.NaN,bool english=false){var t=Text(s,size,width,english);Put(c,t,x,y);return t;}
  Border Box(UIElement child,double width,double height,Color fill,double angle=0){return new Border{Width=width,Height=height,Background=Sketch.Textured(fill),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.4),CornerRadius=new CornerRadius(9,12,8,13),Child=child,Padding=new Thickness(15),RenderTransform=new RotateTransform(angle),RenderTransformOrigin=new Point(.5,.5)};}
  Button Button(string text,Action action,double width=160,double height=50,bool primary=false){
   var b=new Button{Content=text,Width=width,Height=height,FontFamily=new FontFamily(Ar||text.Any(ch=>ch>='\u0600'&&ch<='\u06FF')||text.Length<3?"Segoe UI":"Segoe Print"),FontSize=18,Foreground=B(primary?p.AccentInk:p.Ink),Background=Sketch.Textured(primary?p.Accent:p.Paper),BorderBrush=B(p.Ink),BorderThickness=new Thickness(1.5),Cursor=Cursors.Hand,Padding=new Thickness(12,4,12,5)};
   b.Tag=new Size(width,height);b.Width=width*Profile.Settings.ButtonScale;b.Height=height*Profile.Settings.ButtonScale;b.FontSize*=Profile.Settings.TextScale;
   var border=new FrameworkElementFactory(typeof(Border));border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetBinding(Border.BorderBrushProperty,new System.Windows.Data.Binding("BorderBrush"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});border.SetValue(Border.BorderThicknessProperty,new Thickness(1.5));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(9,12,8,12));
   var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);content.SetValue(ContentPresenter.MarginProperty,new Thickness(5,0,5,2));var fit=new FrameworkElementFactory(typeof(Viewbox));fit.SetValue(Viewbox.StretchProperty,Stretch.Uniform);fit.SetValue(Viewbox.StretchDirectionProperty,StretchDirection.DownOnly);fit.AppendChild(content);border.AppendChild(fit);
   b.Template=new ControlTemplate(typeof(Button)){VisualTree=border};
   b.MouseEnter+=(s,e)=>{if(b.IsEnabled){b.Opacity=.82;}};
   b.MouseLeave+=(s,e)=>{b.Opacity=b.IsEnabled?1:.55;b.RenderTransform=Transform.Identity;};
   b.IsEnabledChanged+=(s,e)=>b.Opacity=b.IsEnabled?1:.55;
   b.Click+=(s,e)=>{if(page!="minigame"&&page!="minigames")audio.Fx("click");action();};AutomationProperties.SetName(b,text);return b;
  }
  void Btn(Canvas c,string text,double x,double y,Action action,double w=160,double h=50,bool primary=false){Put(c,Button(text,action,w,h,primary),x,y);}
  void TypingFeedback(object sender,TextChangedEventArgs e){var input=e.OriginalSource as TextBox;if(input!=null&&input.IsKeyboardFocusWithin&&!input.IsReadOnly&&e.Changes.Any(change=>change.AddedLength>0||change.RemovedLength>0))audio.Typing();}
  Image Avatar(int n,double h,double w=double.NaN){return new Image{Source=CharacterArt.Get(n),Height=h,Width=w,Stretch=Stretch.Uniform};}
  void Build(){
   if(miniSelectionScroll!=null)miniSelectionOffset=miniSelectionScroll.VerticalOffset;miniSelectionScroll=null;
   if(page!="minigame"&&mini!=null)StopMiniGame();
   RememberQuestionTypeScroll();questionTypeScroll=null;
   if(notebookPopup!=null)notebookPopup.IsOpen=false;
   FlushNotebook();notebookInk=null;
   modalEnabled.Clear();
   p=new Palette(Profile.Settings.Theme);ApplyPaperDesign();Background=B(p.Desk);
   root=new Grid();root.AddHandler(TextBoxBase.TextChangedEvent,new TextChangedEventHandler(TypingFeedback));stage=new Canvas{Width=1440,Height=900,ClipToBounds=true};root.Children.Add(new Viewbox{Child=stage,Stretch=Stretch.Uniform});Content=root;
   Put(stage,new Sketch("desk",p,1440,900),0,0);
   double shift=Profile.Settings.SidebarHidden?-116.5:Ar?-233:0;
   Put(stage,new Sketch("paper",p,1145,841),264+shift,31);
   var sidebar=new Canvas{Width=250,Height=900,Visibility=Profile.Settings.SidebarHidden?Visibility.Collapsed:Visibility.Visible};Put(stage,sidebar,Ar?1190:0,0);
   Label(sidebar,"little\nnotebook",31,31,31,213,true).FontSize=28;
   Label(sidebar,T("a little learning, every day","كل يوم، نتعلّم شيئاً جديداً"),32,137,13,209);
   Put(sidebar,new Sketch("leaf",p,36,42),189,43);
   var nav=new[]{new[]{"home",T("⌂   Play","اللعب   ⌂")},new[]{"study",T("▤   Study","الدراسة   ▤")},new[]{"notebook",T("▧   Notebook","دفتر الرسم   ▧")},new[]{"profile",T("☆   Statistics","الإحصائيات   ☆")},new[]{"shop",T("✿   Shop","المتجر   ✿")},new[]{"settings",T("⚙   Game settings","إعدادات اللعبة   ⚙")}};
   for(int i=0;i<nav.Length;i++){string dest=nav[i][0];var b=Button(nav[i][1],()=>{if(dest=="settings")SettingsView();else Navigate(dest=="home"&&Profile.Settings.PlaySection=="minigames"?"minigames":dest);},210,44,page==dest||dest=="home"&&(page=="minigames"||page=="minigame"));b.IsEnabled=dest=="settings"||page!="welcome"||editingCharacter;Put(sidebar,b,28,218+i*50);}
   if(page!="welcome"){
    Put(sidebar,Avatar(Profile.Character,82,210),30,532);
    if(!string.IsNullOrEmpty(Profile.Pet))Put(sidebar,PetImage(Profile.Pet,Profile.PetColor,43,40),153,575);
    var edit=Button("✎",EditCharacter,29,28);edit.ToolTip=T("Edit character and name","تعديل الشخصية والاسم");AutomationProperties.SetName(edit,T("Edit character","تعديل الشخصية"));Put(sidebar,edit,68,527);
    Label(sidebar,Profile.Name,30,620,20,210).TextAlignment=TextAlignment.Center;
    Label(sidebar,"✦ "+Profile.Coins+T(" stars"," نجمة"),29,655,15,212).TextAlignment=TextAlignment.Center;
   } else {
    var note=new Canvas();Label(note,T("Learn.\nWonder.\nGrow.","تعلّم.\nاكتشف.\nتطوّر."),15,4,20,145);Put(note,new Sketch("star",p,35,35),145,72);Put(sidebar,Box(note,210,130,p.Light,-4),25,540);
   }
   BuildRadio();
   double toggleWidth=Profile.Settings.SidebarHidden?61:210;double toggleX=Profile.Settings.SidebarHidden?32:28;
   var toggle=Button("",ToggleSidebar,toggleWidth,37);var toggleContents=new StackPanel{Orientation=Orientation.Horizontal,FlowDirection=FlowDirection.LeftToRight};
   string arrow=Profile.Settings.SidebarHidden?(Ar?"‹":"›"):(Ar?"›":"‹");var arrowText=Text(arrow,19,20,true);arrowText.FontFamily=new FontFamily("Segoe UI");
   var toggleLabel=Text(Profile.Settings.SidebarHidden?"☰":T("Hide sidebar","إخفاء الشريط"),17);if(Ar){toggleContents.Children.Add(toggleLabel);toggleContents.Children.Add(arrowText);}else{toggleContents.Children.Add(arrowText);toggleContents.Children.Add(toggleLabel);}toggle.Content=toggleContents;
   toggle.ToolTip=T("Toggle sidebar · Ctrl+B","إظهار أو إخفاء الشريط · Ctrl+B");AutomationProperties.SetName(toggle,Profile.Settings.SidebarHidden?T("Show sidebar","إظهار الشريط"):T("Hide sidebar","إخفاء الشريط"));Put(stage,toggle,Ar?1440-toggleX-toggleWidth:toggleX,Profile.Settings.SidebarHidden?37:173);
   Label(stage,"v"+AppInfo.Version+T(" · made with care"," · صُنع بحب"),Ar?105+shift+233:1125+shift,820,12,202);
   body=new Canvas{Width=1000,Height=743};Put(stage,body,351+shift,72);
   Put(stage,new Sketch(Profile.Sticker,p,40,40),1320+shift,66);
   overlay=new Canvas{Width=1440,Height=900,Visibility=Visibility.Collapsed};Put(stage,overlay,0,0);
   if(page=="welcome")Welcome();else if(page=="home")Home();else if(page=="minigames")MiniGamesView();else if(page=="minigame")MiniGameView();else if(page=="game")GameView();else if(page=="study")Study();else if(page=="profile")Progress();else if(page=="notebook")NotebookView();else if(page=="shop")Shop();else if(page=="results")Results();
  }
  void ToggleSidebar(){if(overlay!=null&&overlay.Children.Count>0)return;Profile.Settings.SidebarHidden=!Profile.Settings.SidebarHidden;Save();Build();}
  void Navigate(string dest){Save();if(game!=null&&!game.Recorded){ConfirmLeave(false,dest);return;}page=dest;editingCharacter=false;Build();}
  void Header(string kicker,string title,string subtitle){Label(body,kicker.ToUpperInvariant(),10,0,13,920);Label(body,title,10,29,35,925);Label(body,subtitle,10,87,17,925);}
  void BuildRadio(){
   var radio=new Canvas{Width=224,Height=175};Put(radio,new Sketch("radio",p,224,175),0,0);
   var notes=Text("♪     ♫",19,200,true);notes.Foreground=B(p.Accent);Put(radio,notes,40,0);
   if(Profile.Settings.Music&&Profile.Settings.Volume>0&&!Profile.Settings.ReducedMotion){var trans=new TranslateTransform();notes.RenderTransform=trans;trans.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,-8,new Duration(TimeSpan.FromSeconds(3))){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});}
   RadioButton(radio,"◀",124,97,22,24,()=>audio.Track(-1),T("Previous track","المقطع السابق"));
   RadioButton(radio,Profile.Settings.Music?"Ⅱ":"▶",151,94,29,30,()=>{audio.Toggle();Save();RefreshRadio(radio);},Profile.Settings.Music?T("Pause music","إيقاف الموسيقى مؤقتاً"):T("Play music","تشغيل الموسيقى"),Profile.Settings.Theme=="night");
   RadioButton(radio,"▶",184,97,22,24,()=>audio.Track(1),T("Next track","المقطع التالي"));
   RadioButton(radio,Profile.Settings.Volume==0?"×":"♪",94,121,23,24,()=>{Profile.Settings.Volume=Profile.Settings.Volume==0?.3:0;audio.ApplyVolume();Save();RefreshRadio(radio);},T("Mute or unmute music","كتم الموسيقى أو تشغيلها"),Profile.Settings.Theme=="night");
   var slider=PaperSlider(0,1,Profile.Settings.Volume,79);slider.ValueChanged+=(s,e)=>{Profile.Settings.Volume=slider.Value;audio.ApplyVolume();};slider.PreviewMouseUp+=(s,e)=>Save();AutomationProperties.SetName(slider,T("Music volume","مستوى الموسيقى"));Put(radio,slider,123,119);
   var selection=Button(audio.CurrentTitle+"  ▾",RadioSelection,230,36);selection.Tag=null;selection.Width=230;selection.Height=36;selection.FontSize=11*Profile.Settings.TextScale;selection.Background=Brushes.Transparent;selection.BorderThickness=new Thickness(0);selection.ToolTip=T("Choose music","اختيار الموسيقى");AutomationProperties.SetName(selection,T("Choose music: ","اختيار الموسيقى: ")+audio.CurrentTitle);Put(radio,selection,-3,158);
   radioWidget=radio;if(Profile.Settings.SidebarHidden){radio.RenderTransform=new ScaleTransform(.68,.68);Put(stage,radio,Ar?1282:4,762);}else Put(stage,radio,Ar?1194:22,697);
  }
  void RadioButton(Canvas c,string label,double x,double y,double w,double h,Action action,string accessible,bool light=false){var b=Button(label,action,w,h);b.Tag=null;b.Width=w;b.Height=h;b.Background=Brushes.Transparent;b.Foreground=new SolidColorBrush(light?Color.FromRgb(255,248,233):Color.FromRgb(41,56,74));b.FontSize=12;b.FontFamily=new FontFamily("Segoe UI");var f=new FrameworkElementFactory(typeof(ContentPresenter));f.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);f.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);var hit=new FrameworkElementFactory(typeof(Border));hit.SetValue(Border.BackgroundProperty,Brushes.Transparent);hit.AppendChild(f);b.Template=new ControlTemplate(typeof(Button)){VisualTree=hit};AutomationProperties.SetName(b,accessible);b.ToolTip=accessible;Put(c,b,x,y);}
  void RefreshRadio(Canvas old){stage.Children.Remove(old);modalEnabled.Remove(old);BuildRadio();if(overlay.Children.Count>0){modalEnabled[radioWidget]=true;radioWidget.IsEnabled=false;}Panel.SetZIndex(overlay,10);}
  void Welcome(){
   Header(T("PROFILE SETUP","إعداد الملف الشخصي"),T("Choose your character","اختر شخصيتك"),T("Select a character and enter your name.","اختر شخصية وأدخل اسمك."));
   bool boy=new[]{3,5,6,7}.Contains(Profile.Character);int[] characters=boy?new[]{3,5,6,7}:new[]{0,1,2,4,8,9};
   Btn(body,T("Boy","ولد"),322,142,()=>{Profile.Character=3;Build();},161,46,boy);
   Btn(body,T("Girl","بنت"),483,142,()=>{Profile.Character=0;Build();},161,46,!boy);
   var av=Avatar(Profile.Character,281);av.Width=240;Put(body,av,361,212);
   Btn(body,"‹",252,320,()=>{Profile.Character=characters[(Array.IndexOf(characters,Profile.Character)+characters.Length-1)%characters.Length];Build();},64,66);
   Btn(body,"›",648,320,()=>{Profile.Character=characters[(Array.IndexOf(characters,Profile.Character)+1)%characters.Length];Build();},64,66);
   Put(body,new Sketch("star",p,40,40),698,225);Put(body,new Sketch("leaf",p,64,78),245,425);
   for(int i=0;i<characters.Length;i++){int n=characters[i];var b=Button("",()=>{Profile.Character=n;Build();},99,108,n==Profile.Character);b.Content=Avatar(n,94);AutomationProperties.SetName(b,T("Character ","الشخصية ")+(n+1));Put(body,b,484-characters.Length*56+i*112,505);}
   Label(body,T("Your name","اسمك"),187,643,21,160);
   var name=new TextBox{Text=Profile.Name,Width=358,Height=45,FontSize=24*Profile.Settings.TextScale,MaxLength=24,Padding=new Thickness(7,0,7,0),VerticalContentAlignment=VerticalAlignment.Center,FontFamily=new FontFamily(Ar?"Segoe UI":"Segoe Print"),Background=Brushes.Transparent,Foreground=B(p.Ink),BorderThickness=new Thickness(0),FlowDirection=Ar?FlowDirection.RightToLeft:FlowDirection.LeftToRight};AutomationProperties.SetName(name,T("Your name","اسمك"));var nameFrame=Box(name,390,52,p.Paper);nameFrame.Padding=new Thickness(8,1,8,1);Put(body,nameFrame,348,636);name.TextChanged+=(s,e)=>Profile.Name=name.Text;
   Btn(body,editingCharacter?T("Save my character  →","حفظ الشخصية  ←"):T("Let's begin  →","لنبدأ  ←"),348,699,()=>{if(string.IsNullOrWhiteSpace(name.Text)){name.BorderBrush=Brushes.IndianRed;name.Focus();return;}Profile.Name=name.Text.Trim();Save();editingCharacter=false;page="home";Build();},390,49,true);
   Btn(body,Ar?"English":"العربية",803,0,()=>{Profile.Settings.Language=Ar?"en":"ar";Build();},135,35);
  }
  string CategoryName(string key){switch(key){case "all":return T("All","الكل");case "vocabulary":return T("Vocabulary","المفردات");case "spelling":return T("Spelling","الإملاء");case "sentences":return T("Sentences","الجمل");case "listening":return T("Listening","الاستماع");case "dictionary":return T("Dictionary","القاموس");case "spelling-repair":return T("Sound to spelling","من الصوت إلى الإملاء");case "word-pairs":return T("Word pairs","أزواج الكلمات");case "word-search":return T("Word search","الكلمات المخفية");case "memory":return T("Memory","الذاكرة");case "patterns":return T("Patterns","الأنماط");case "logic":return T("Logic","المنطق");case "science":return T("Science","العلوم");case "nature":return T("Nature","الطبيعة");case "world":return T("World","العالم");case "everyday":return T("Everyday","الحياة اليومية");case "addition":return T("Addition","الجمع");case "subtraction":return T("Subtraction","الطرح");case "multiplication":return T("Multiplication","الضرب");case "division":return T("Division","القسمة");case "word-problems":return T("Word problems","مسائل لفظية");case "mixed-operations":return T("Mixed operations","عمليات مختلطة");default:return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('-',' '));}}
  ScrollViewer questionTypeScroll;double questionTypeScrollOffset;Dictionary<string,Dictionary<string,Button>> questionTypeButtons=new Dictionary<string,Dictionary<string,Button>>();
  Color SoftSelectionColor(){return Profile.Settings.Theme=="matcha"?Color.FromRgb(206,222,184):p.Light;}
  void StyleQuestionTypeButton(Button button,bool selected){button.Background=Sketch.Textured(selected?SoftSelectionColor():p.Paper);button.Foreground=B(p.Ink);button.FontWeight=selected?FontWeights.SemiBold:FontWeights.Normal;}
  void RefreshQuestionTypeHighlights(){foreach(var topic in questionTypeButtons)foreach(var entry in topic.Value)StyleQuestionTypeButton(entry.Value,TypeSelected(topic.Key,entry.Key));}
  Color DifficultyColor(int level){return new[]{Color.FromRgb(139,185,193),Color.FromRgb(224,188,112),Color.FromRgb(208,144,143),SoftSelectionColor()}[level];}
  Color PaleDifficulty(int level){var color=DifficultyColor(level);return Color.FromRgb((byte)(color.R*.25+p.Paper.R*.75),(byte)(color.G*.25+p.Paper.G*.75),(byte)(color.B*.25+p.Paper.B*.75));}
  void RememberQuestionTypeScroll(){if(questionTypeScroll!=null)questionTypeScrollOffset=questionTypeScroll.VerticalOffset;}
  static readonly string[] PlayTopics={"english","math","puzzles","general"};
  string[] SelectedTopics{get{return selectedTopic=="mixed"?PlayTopics:PlayTopics.Where(t=>selectedTopic.Split(',').Contains(t)).ToArray();}}
  string TopicCategory(string topic){string value;if(!Profile.Settings.QuestionCategories.TryGetValue(topic,out value)||value=="all")return "all";var available=QuestionCatalog.Categories(bank,topic,Profile.Settings.Difficulty);var selected=value.Split(',').Where(x=>x!="all"&&available.Contains(x)).Distinct().ToArray();return selected.Length==0?"all":string.Join(",",selected);}
  string SelectedCategory{get{return SelectedTopics.Length==1?TopicCategory(SelectedTopics[0]):"all";}}
  bool CategorySelected(string key){return TypeSelected(selectedTopic,key);}
  bool TypeSelected(string topic,string key){string value=TopicCategory(topic);return value=="all"||key!="all"&&value.Split(',').Contains(key);}
  void ToggleCategory(string category){ToggleTopicCategory(selectedTopic,category);}
  void ToggleTopicCategory(string topic,string category){RememberQuestionTypeScroll();if(category=="all")Profile.Settings.QuestionCategories[topic]="all";else{var all=QuestionCatalog.Categories(bank,topic,Profile.Settings.Difficulty).Where(x=>x!="all").ToList();var selected=TopicCategory(topic)=="all"?new HashSet<string>(all):new HashSet<string>(TopicCategory(topic).Split(','));if(selected.Contains(category)){if(selected.Count<=1)return;selected.Remove(category);}else selected.Add(category);Profile.Settings.QuestionCategories[topic]=selected.Count==all.Count?"all":string.Join(",",all.Where(selected.Contains));}Save();RefreshQuestionTypeHighlights();}
  void ToggleTopic(string topic){RememberQuestionTypeScroll();var selected=new HashSet<string>(SelectedTopics);if(topic=="mixed")selected=new HashSet<string>(PlayTopics);else if(selected.Contains(topic)){if(selected.Count<=1)return;selected.Remove(topic);}else selected.Add(topic);selectedTopic=selected.Count==4?"mixed":string.Join(",",PlayTopics.Where(selected.Contains));Profile.Settings.Topics=selectedTopic;Save();Build();}
  void Home(){
   questionTypeButtons.Clear();
   Header(T("PLAY","اللعب"),T("Hello, ","مرحباً، ")+Profile.Name,T("Choose topics, question types and difficulty.","اختر المواضيع وأنواع الأسئلة والمستوى."));
   PlayTabs(false);
   var allTopics=Button("",()=>ToggleTopic("mixed"),367,62,SelectedTopics.Length==4);var allCanvas=new Canvas{Width=335,Height=51};for(int k=0;k<4;k++)Put(allCanvas,new Sketch(new[]{"book","math","brain","sun"}[k],SelectedTopics.Length==4?p.OnAccent():p,25,25),8+k*29,12);Label(allCanvas,T("All topics","جميع المواضيع"),136,9,21,190);allTopics.Content=allCanvas;AutomationProperties.SetName(allTopics,Topic("mixed"));Put(body,allTopics,298,125);
   var icons=new[]{"book","math","brain","sun"};for(int i=0;i<4;i++){string topic=PlayTopics[i];bool selected=SelectedTopics.Contains(topic);var button=Button("",()=>ToggleTopic(topic),367,62,selected);var c=new Canvas{Width=333,Height=51};Put(c,new Sketch(icons[i],selected?p.OnAccent():p,39,39),Ar?282:6,4);Label(c,Topic(topic),Ar?6:62,9,21,265).Foreground=B(selected?p.AccentInk:p.Ink);button.Content=c;AutomationProperties.SetName(button,Topic(topic));Put(body,button,98+(Ar?1-i%2:i%2)*394,202+(i/2)*75);}
   Label(body,T("Question types","أنواع الأسئلة"),99,352,15,760);
   var groups=new StackPanel{Width=750};foreach(string item in SelectedTopics){string topic=item;var topicButtons=new Dictionary<string,Button>();questionTypeButtons[topic]=topicButtons;var row=new StackPanel{Width=742,Margin=new Thickness(0,0,0,8)};row.Children.Add(Text(Topic(topic),15,730));var types=new WrapPanel{Width=736,FlowDirection=Ar?FlowDirection.RightToLeft:FlowDirection.LeftToRight};foreach(string key in QuestionCatalog.Categories(bank,topic,Profile.Settings.Difficulty)){string category=key,label=CategoryName(key);var button=Button(label,()=>ToggleTopicCategory(topic,category),Math.Max(62,Math.Min(153,label.Length*8+24)),32,TypeSelected(topic,key));button.Margin=new Thickness(0,0,5,5);button.FontSize=14*Profile.Settings.TextScale;StyleQuestionTypeButton(button,TypeSelected(topic,key));topicButtons[key]=button;types.Children.Add(button);}row.Children.Add(types);groups.Children.Add(row);}
   double height=SelectedTopics.Length==1?100:153;var scroll=new ScrollViewer{Width=773,Height=height,Content=groups,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(body,scroll,99,381);questionTypeScroll=scroll;
   // Restore before the rebuilt screen can render, rather than jumping after Loaded.
   scroll.ApplyTemplate();scroll.Measure(new Size(773,height));scroll.Arrange(new Rect(0,0,773,height));scroll.UpdateLayout();scroll.ScrollToVerticalOffset(questionTypeScrollOffset);scroll.UpdateLayout();
   double controlsY=381+height+14;for(int i=0;i<4;i++){int level=i;bool selected=Profile.Settings.Difficulty==i;var difficulty=Button(Difficulty(i),()=>{RememberQuestionTypeScroll();Profile.Settings.Difficulty=level;Save();Build();},129,42);difficulty.Background=Sketch.Textured(selected?DifficultyColor(i):PaleDifficulty(i));difficulty.FontWeight=selected?FontWeights.Bold:FontWeights.Normal;Put(body,difficulty,Ar?625-i*139:196+i*139,controlsY);}
   Put(body,TimerToggle(),Ar?640:128,controlsY+57);Put(body,PaperToggle(T("1 life","حياة واحدة"),Profile.Settings.OneLife(),()=>{if(Profile.Settings.Difficulty==2)Profile.Settings.HardSingleLife=!Profile.Settings.OneLife();else Profile.Settings.SingleLife=!Profile.Settings.SingleLife;Save();Build();}),Ar?425:343,controlsY+57);Btn(body,QuestionCountText()+"  ▾",Ar?141:551,controlsY+57,QuestionCountSettings,266,43);
   Label(body,(Profile.Settings.OneLife()?T("1 life","حياة واحدة"):T("3 lives","3 محاولات"))+"  ·  "+QuestionCountText(),99,controlsY+106,14,760).TextAlignment=TextAlignment.Center;var start=Button(T("Start game  →","ابدأ اللعبة  ←"),StartGame,344,50,true);start.Background=new SolidColorBrush(Color.FromRgb(139,255,22));start.Foreground=B(Color.FromRgb(41,56,74));Put(body,start,307,controlsY+135);
  }
  void StartGame(){
   audio.StopSpeech();lastAutoReadKey="";
   interactionId="";
   answerFeedbackElapsed=0;
   var recent=Profile.Attempts.Skip(Math.Max(0,Profile.Attempts.Count-30)).Select(a=>a.Id);
   questionStream=new QuestionStream(bank,SelectedTopics,Profile.Settings.Difficulty,Profile.Attempts,new Dictionary<string,string>(Profile.Settings.QuestionCategories),Profile.Settings.RoundLength);
   searchFactory=new WordSearchFactory(bank.Random);
   bool endless=Profile.Settings.RoundLength==0;
   game=new GameSession(Profile.Settings.Difficulty,Profile.Settings.SingleLife,endless,Profile.Settings.OneLife()){Topic=selectedTopic,Category=SelectedCategory,Questions=Enumerable.Range(0,endless?1:Profile.Settings.RoundLength).Select(i=>PrepareQuestion(questionStream.Next())).ToList()};
   page="game";paused=false;lastTick=DateTime.UtcNow;Build();
  }
  Question PrepareQuestion(Question q){if(q.Kind=="listen"&&!audio.CanSpeak(q.Speech)){q.Kind="choice";q.PromptEn=string.IsNullOrEmpty(q.Translation)?"Choose the word meaning "+ListenMeaning(q.Answer,false):"Choose the English word for: "+q.Translation;q.PromptAr="اختر الكلمة الإنجليزية التي تعني "+(q.Translation??ListenMeaning(q.Answer,true));}return q;}
  string QuestionCountText(){return Profile.Settings.RoundLength==0?T("All questions","كل الأسئلة"):Profile.Settings.RoundLength+T(" questions"," أسئلة");}
  void QuestionCountSettings(){Modal(T("Number of questions","عدد الأسئلة"),c=>{
   Label(c,T("All questions: keep playing until you run out of lives.","كل الأسئلة: استمر في اللعب حتى تنتهي محاولاتك."),94,99,18,601);
   int[] counts={5,10,15,0};for(int i=0;i<counts.Length;i++){int count=counts[i];Btn(c,count==0?T("All questions  ∞","كل الأسئلة  ∞"):count+T(" questions"," أسئلة"),100+(i%2)*306,167+(i/2)*81,()=>{Profile.Settings.RoundLength=count;Save();Build();},284,57,Profile.Settings.RoundLength==count);}
  },370);}
  string ListenMeaning(string s,bool ar){if(s=="garden")return ar?"حديقة":"a place where flowers and plants grow";if(s=="morning")return ar?"صباح":"the early part of the day";return ar?"مكتبة":"a place where you borrow books";}
  void Modal(string title,Action<Canvas> content,double h=570){
   paused=true;audio.StopSpeech();ClearMiniHeldInput();if(mini!=null)audio.PauseMiniSounds();overlay.Children.Clear();overlay.Visibility=Visibility.Visible;
   foreach(UIElement element in stage.Children)if(element!=overlay){if(!modalEnabled.ContainsKey(element))modalEnabled[element]=element.IsEnabled;element.IsEnabled=false;}
   Panel.SetZIndex(overlay,10);
   overlay.Background=new SolidColorBrush(Color.FromArgb(145,24,31,27));
   var c=new Canvas{Width=790,Height=h};Put(c,new Sketch("paper",p,790,h),0,0);Label(c,title,88,35,29,600);Btn(c,"×",715,25,CloseModal,40,39);content(c);Put(overlay,c,325,(900-h)/2);
  }
  void CloseModal(){if(page=="minigame"&&mini!=null&&mini.Phase=="failed"){BackToMiniGames();return;}if(musicImporter!=null){CancelMusicDownload();RadioSelection();return;}overlay.Children.Clear();overlay.Visibility=Visibility.Collapsed;foreach(var entry in modalEnabled)entry.Key.IsEnabled=entry.Value;modalEnabled.Clear();paused=false;lastTick=DateTime.UtcNow;miniStamp=System.Diagnostics.Stopwatch.GetTimestamp();if(mini!=null)audio.ResumeMiniSounds();MaybeAutoReadQuestion();FocusAnswerInput();FocusMiniSurface();}
  void PauseMenu(){Modal(T("Game menu","قائمة اللعبة"),c=>{Label(c,T("The game is paused.","اللعبة متوقفة مؤقتاً."),105,111,20,580);Btn(c,T("Resume","متابعة"),214,180,CloseModal,365,60,true);Btn(c,T("Settings","الإعدادات"),214,263,SettingsView,365,60);Btn(c,T("Exit game","الخروج من اللعبة"),214,346,()=>{CloseModal();ConfirmLeave(false);},365,60);},477);}
  void Message(string title,string message){Modal(title,c=>{Label(c,message,100,114,22,585);Btn(c,T("Got it","حسناً"),248,302,CloseModal,280,55,true);},401);}
  void ConfirmLeave(bool exit,string dest="home"){
   if(game==null||game.Recorded){if(exit){closing=true;Close();}else{page=dest;Build();}return;}
   Modal(T("Leave this round?","هل تريد إغلاق هذه الصفحة؟"),c=>{Label(c,T("Your answers, time, and earned stars are saved. This unfinished round won't count as a completed session.","تُحفظ إجاباتك ووقتك والنجوم المكتسبة. لن تُحسب الجولة غير المكتملة كجلسة مكتملة."),99,110,21,575);Btn(c,T("Keep playing","متابعة اللعب"),107,288,CloseModal,268,57,true);Btn(c,exit?T("Save & exit","حفظ وخروج"):T("Leave","مغادرة"),402,288,()=>{RecordSession(false);game=null;audio.StopSpeech();Save();if(exit){closing=true;Close();}else{paused=false;page=dest;Build();}},278,57);var leaveButton=c.Children.OfType<Button>().Last();leaveButton.Background=B(Color.FromRgb(186,103,94));leaveButton.Foreground=B(Colors.White);},392);
  }
  void SettingsView(){
   Modal(T("Settings","الإعدادات"),c=>{
    Label(c,T("Language","اللغة"),94,105,20,180);Btn(c,"English",313,98,()=>ChangeSettings(()=>Profile.Settings.Language="en"),157,44,!Ar);Btn(c,"العربية",484,98,()=>ChangeSettings(()=>Profile.Settings.Language="ar"),157,44,Ar);
    Label(c,T("Notebook palette","ألوان الدفتر"),94,173,20,560);
    string[] keys={"matcha","rose","sky","lavender","night"};string[] names={T("Matcha","ماتشا"),T("Rose","وردي"),T("Sky","سماء"),T("Lavender","لافندر"),T("Night","ليلي")};
    for(int i=0;i<5;i++){string k=keys[i];var b=Button(names[i],()=>ChangeSettings(()=>Profile.Settings.Theme=k),107,46,Profile.Settings.Theme==k);b.Background=B(new Palette(k).Accent);b.Foreground=B(new Palette(k).AccentInk);Put(c,b,94+i*121,211);}
    Toggle(c,T("Question timer","مؤقت الأسئلة"),Profile.Settings.Timer,278,()=>Profile.Settings.Timer=!Profile.Settings.Timer);
    Toggle(c,T("Button & answer sounds","أصوات الأزرار والإجابات"),Profile.Settings.Sound,329,()=>Profile.Settings.Sound=!Profile.Settings.Sound);
    Toggle(c,T("Animations","الحركات"),!Profile.Settings.ReducedMotion,380,()=>Profile.Settings.ReducedMotion=!Profile.Settings.ReducedMotion);
    Toggle(c,T("Auto-play question speech","قراءة السؤال تلقائياً"),Profile.Settings.AutoReadQuestions,431,()=>{Profile.Settings.AutoReadQuestions=!Profile.Settings.AutoReadQuestions;lastAutoReadKey="";});
    Btn(c,T("Text & button sizes…","حجم النص والأزرار…"),94,498,SizingSettings,285,47);
    Btn(c,T("Done","تم"),395,498,CloseModal,303,47,true);
    Btn(c,T("Display: ","العرض: ")+(Profile.Settings.BorderlessFullscreen?T("Borderless","بلا حدود"):T("Windowed","نافذة"))+"  ▾",94,552,DisplaySettings,285,43);
    Btn(c,T("Background paper…","تصميم ورقة الخلفية…"),395,552,PaperDesignView,303,43);
    Btn(c,T("Check for updates","البحث عن تحديثات"),94,608,()=>CheckForUpdates(false),285,43);
    Label(c,"Version "+AppInfo.Version,410,620,13,285,true);
    Put(c,DangerButton(T("Reset character…","إعادة ضبط الشخصية…"),ConfirmResetCharacter,303,48),243,681);
    Label(c,T("Deletes this character and all its progress.","يحذف هذه الشخصية وكل تقدّمها."),94,743,13,599).TextAlignment=TextAlignment.Center;
   },800);
  }
  void Toggle(Canvas c,string title,bool value,double y,Action change){Label(c,title,94,y+7,19,400);Btn(c,value?T("On","نعم"):T("Off","لا"),547,y,()=>ChangeSettings(change),145,43,value);}
  void ChangeSettings(Action action){action();Save();audio.ApplyVolume();Build();SettingsView();}
 }
}
