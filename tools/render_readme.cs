using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LittleNotebook;

// Render the real application controls into README images using an isolated profile.
// This does not interact with the desktop or change the player's saved game.
static class ReadmeRenderer {
 static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
 static void Set(MainWindow window, string name, object value) { typeof(MainWindow).GetField(name, Private).SetValue(window, value); }
 static object Get(MainWindow window,string name) { return typeof(MainWindow).GetField(name,Private).GetValue(window); }
 static void Call(MainWindow window, string name, params object[] values) { typeof(MainWindow).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==values.Length).Invoke(window, values); }
 static IEnumerable<T> Children<T>(DependencyObject root) where T:DependencyObject { for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T)yield return (T)child;foreach(var nested in Children<T>(child))yield return nested;} }
 static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
 static void Click(Button button) { button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); }
 static Color ButtonColor(Button button) { var solid=button.Background as SolidColorBrush;if(solid!=null)return solid.Color;return ((SolidColorBrush)((GeometryDrawing)((DrawingGroup)((DrawingBrush)button.Background).Drawing).Children[0]).Brush).Color; }
 static void CheckHomeControls(MainWindow window) {
  foreach(string theme in new[]{"matcha","rose","sky","lavender","night"}) {
   window.Profile.Settings.Theme=theme;window.Profile.Settings.Difficulty=3;Call(window,"Build");
   var palette=new Palette(theme);Color expected=theme=="matcha"?Color.FromRgb(206,222,184):palette.Light;
   var groups=(Dictionary<string,Dictionary<string,Button>>)Get(window,"questionTypeButtons");var body=(Canvas)Get(window,"body");
   Check(groups.Values.SelectMany(x=>x.Values).All(b=>ButtonColor(b)==expected),"Question types do not follow theme: "+theme);
   Check(ButtonColor(Children<Button>(body).Single(b=>Convert.ToString(b.Content)=="Mixed"))==expected,"Mixed does not follow theme: "+theme);
   Check(ButtonColor(Children<Button>(body).Single(b=>Convert.ToString(b.Content)=="Start game  →"))==Color.FromRgb(139,255,22),"Start changed reference green");
   Call(window,"Capture","qa-home-"+theme);
  }
  window.Profile.Settings.Theme="matcha";Call(window,"Build");window.UpdateLayout();
  var scroll=(ScrollViewer)Get(window,"questionTypeScroll");scroll.ScrollToVerticalOffset(40);scroll.UpdateLayout();
  Check(scroll.VerticalOffset==40,"Scroll test fixture did not move");
  Action<Action> keep=action=>{action();var rebuilt=(ScrollViewer)Get(window,"questionTypeScroll");Check(Math.Abs(rebuilt.VerticalOffset-40)<.01,"Play control reset scroll before first render");window.Dispatcher.Invoke(new Action(()=>{}),DispatcherPriority.ApplicationIdle);Check(Math.Abs(rebuilt.VerticalOffset-40)<.01,"Play control changed scroll after layout");};
  foreach(string name in new[]{"Easy","Medium","Hard","Mixed"}){string label=name;keep(()=>Click(Children<Button>((Canvas)Get(window,"body")).Single(b=>Convert.ToString(b.Content)==label)));}
  keep(()=>Click(Children<Button>((Canvas)Get(window,"body")).Single(b=>AutomationProperties.GetName(b).StartsWith("Timer "))));
  keep(()=>Click(Children<Button>((Canvas)Get(window,"body")).Single(b=>AutomationProperties.GetName(b).StartsWith("1 life:"))));
  keep(()=>Call(window,"ToggleTopic","math"));keep(()=>Call(window,"ToggleTopic","mixed"));
  keep(()=>{Call(window,"QuestionCountSettings");Click(Children<Button>((Canvas)Get(window,"overlay")).Single(b=>Convert.ToString(b.Content)=="15 questions"));});
  keep(()=>Call(window,"ToggleSidebar"));keep(()=>Call(window,"ToggleSidebar"));
  keep(()=>{Call(window,"RadioSelection");Call(window,"CloseModal");});
  keep(()=>{Call(window,"Navigate","study");Call(window,"Navigate","home");});
  scroll=(ScrollViewer)Get(window,"questionTypeScroll");scroll.ScrollToVerticalOffset(0);scroll.UpdateLayout();
  window.Profile.Settings.Timer=true;window.Profile.Settings.SingleLife=false;window.Profile.Settings.HardSingleLife=null;window.Profile.Settings.RoundLength=10;
  Console.WriteLine("PASS: all five theme palettes and 13 Play interactions retain scroll synchronously and after layout; no delayed scroll restore.");
 }
 static void CheckNotebookControls(MainWindow window) {
  var body=(Canvas)Get(window,"body");var tools=(Dictionary<string,Button>)Get(window,"notebookTools");
  var area=Children<Button>(body).Single(b=>AutomationProperties.GetName(b)=="Area menu");
  Check(Canvas.GetLeft(tools["select"])<Canvas.GetLeft(area)&&Canvas.GetLeft(area)<Canvas.GetLeft(tools["pencil"]),"Area is not immediately after Select");
  Check(Canvas.GetLeft(tools["highlighter"])<Canvas.GetLeft(tools["fill"])&&Canvas.GetLeft(tools["fill"])<Canvas.GetLeft(tools["eraser"]),"Fill is not immediately before Eraser");
  Check(Canvas.GetTop(tools["fill"])==Canvas.GetTop(tools["pen"]),"Fill is outside drawing row");
  Call(window,"NotebookColorWheel");window.UpdateLayout();var overlay=(Canvas)Get(window,"overlay");
  var presets=Children<Button>(overlay).Where(b=>AutomationProperties.GetName(b).StartsWith("Preset colour: ")).ToList();
  Check(presets.Count==12,"Missing extra colour presets");Click(presets.Single(b=>AutomationProperties.GetName(b)=="Preset colour: Pink"));
  Check(Children<TextBox>(overlay).Single().Text=="#F4A8CB","Preset did not update colour controls");
  Click(Children<Button>(overlay).Single(b=>Convert.ToString(b.Content)=="Use colour"));
  Check(window.Profile.Settings.RecentColors.First()=="#FFF4A8CB","Preset did not enter recent colours");
  Call(window,"NotebookColorWheel");Call(window,"Capture","qa-extra-colours");Call(window,"CloseModal");
  var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){dc.DrawRectangle(Brushes.LightSkyBlue,null,new Rect(0,0,240,160));dc.DrawEllipse(Brushes.White,new Pen(Brushes.DarkSlateGray,3),new Point(120,80),65,50);}
  var bitmap=new RenderTargetBitmap(240,160,96,96,PixelFormats.Pbgra32);bitmap.Render(drawing);
  Call(window,"EditImportedTracing",bitmap);window.UpdateLayout();overlay=(Canvas)Get(window,"overlay");
  var photo=Children<Image>(overlay).Single();var ink=Children<SmoothInkCanvas>(overlay).Single();
  var eye=Children<Button>(overlay).Single(b=>AutomationProperties.GetName(b)=="Show or hide original photo");
  Check(photo.Visibility==Visibility.Collapsed,"Original photo should start hidden");int count=ink.Strokes.Count;Click(eye);
  var surface=(Grid)photo.Parent;Check(photo.Visibility==Visibility.Visible&&surface.Children.IndexOf(photo)<surface.Children.IndexOf(ink),"Original photo is not beneath tracing");
  Children<Slider>(overlay).Single(s=>s.Maximum==880).Value=480;
  Check(photo.Width==ink.Width&&photo.Height==ink.Height&&photo.Width==480&&photo.Height==320,"Photo/tracing lost alignment during resize");
  ink.Strokes.Add(new Stroke(new StylusPointCollection(new[]{new StylusPoint(10,10),new StylusPoint(30,30)})));
  Click(eye);Check(photo.Visibility==Visibility.Collapsed&&ink.Strokes.Count==count+1,"Eye toggle discarded tracing edits");Click(eye);Call(window,"Capture","qa-original-photo");Call(window,"CloseModal");
  Console.WriteLine("PASS: 10 focused notebook checks: tool ordering, colour presets/hex/recent colours, original-photo visibility, layer order, aligned resize and retained edits.");
 }
 static void Scene(MainWindow window, string page, string image) { Set(window, "page", page); Call(window, "Build"); Call(window, "Capture", "public-" + image); }
 [STAThread] static int Main(string[] args) {
  bool verify=Array.IndexOf(args,"--verify")>=0;
  Diagnostics.Testing = true;
  var app = new Application();
  var window = new MainWindow(true, "readme-profile.json") { ShowActivated = false, WindowState = WindowState.Minimized };
  window.Loaded += (sender, eventArgs) => window.Dispatcher.BeginInvoke(new Action(() => {
   try {
    ((DispatcherTimer)typeof(MainWindow).GetField("timer", Private).GetValue(window)).Stop();
    window.Profile = new Profile { Name = "Maya", Character = 0 };
    window.Profile.Settings.Music = false; window.Profile.Settings.Sound = false; window.Profile.Settings.Volume = 0;
    Scene(window, "welcome", "setup");
    Set(window, "selectedTopic", "mixed"); window.Profile.Settings.Difficulty = 3;
    Set(window,"page","home");Call(window,"Build");if(verify)CheckHomeControls(window);
    Scene(window, "home", "home");
    var bank = (QuestionBank)typeof(MainWindow).GetField("bank", Private).GetValue(window);
    var question = bank.Questions.First(q => q.Kind == "order" && q.Difficulty == 1);
    Set(window, "game", new GameSession(1) { Topic = "english", Questions = new List<Question> { question } });
    Set(window, "interactionId", ""); Scene(window, "game", "gameplay");
    Set(window, "game", null);
    window.Profile.Notebook = new List<NotebookPage> { new NotebookPage { Template = "rabbit-body" } };
    window.Profile.NotebookIndex = 0;
    Scene(window, "notebook", "notebook");
    if(verify)CheckNotebookControls(window);
    Console.WriteLine("Rendered setup, Play, gameplay and notebook with Maya.");
   } catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
   finally { Set(window, "game", null); Set(window, "closing", true); window.Close(); }
  }), DispatcherPriority.ApplicationIdle);
  app.Run(window); return Environment.ExitCode;
 }
}
