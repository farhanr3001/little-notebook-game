using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
namespace LittleNotebook {public partial class MainWindow {
 public async void RunPlayPolishChecks(){int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/play-polish.txt"));try{
 timer.Stop();Profile=new Profile{Name="Maya"};Profile.Settings.Sound=false;Profile.Settings.Music=false;audio.Dispose();audio=new AudioSystem(Profile.Settings);
 check(MiniCatalog.Keys.Length==15&&!MiniCatalog.Keys.Contains("signal")&&!MiniCatalog.Keys.Contains("echo")&&!MiniCatalog.Keys.Contains("pulse"),"Removed games remain selectable");
 check(MiniMelodies.All.Length>=100&&MiniMelodies.All.Select(t=>string.Join(",",t.Notes)).Distinct().Count()>=100,"Fewer than 100 distinct tunes");
 for(int level=1;level<=100;level++){
  var board=ArrowBoard.Generate(level,level*7127);var solution=board.Solution();check(solution!=null&&solution.Count==board.Arrows.Count,"Unsolvable arrow level "+level);
  var occupied=new System.Collections.Generic.HashSet<int>();foreach(var arrow in board.Arrows)foreach(int c in arrow.Cells)check(occupied.Add(c),"Overlapping arrows");
  var session=new MiniSession("arrows",level,level*7127);int played=0;session.Note+=n=>played++;foreach(int i in solution)check(session.ArrowTap(i),"Rapid arrow input rejected");check(played==solution.Count,"Rapid sound feedback missing");MiniModelChecks.Advance(session,30);check(session.Phase=="complete","Rapid clear did not finish");
  var tiles=SlidingBoard.Generate(level,level*41);foreach(int cell in tiles.Scramble.AsEnumerable().Reverse())check(tiles.Move(cell),"Illegal reverse scramble");check(tiles.Solved,"Photo board not solvable");
 }
 foreach(string theme in new[]{"matcha","sky","rose","lavender","night"})foreach(string language in new[]{"en","ar"}){
  Profile.Settings.Theme=theme;Profile.Settings.Language=language;p=new Palette(theme);
  foreach(string kind in MiniCatalog.Keys){StartMiniGame(kind);UpdateLayout();miniScroll.UpdateLayout();check(miniScroll.ScrollableWidth<1&&miniScroll.ScrollableHeight<1,"Board not fitted: "+kind);miniSurface.InvalidateVisual();if(kind=="tiles"){miniSurface.ShowPicture=true;miniSurface.UpdateLayout();miniSurface.InvalidateVisual();}Capture("polish-"+theme+"-"+language+"-"+kind);BackToMiniGames();}
 }
 Profile.Settings.Language="en";Profile.Settings.Theme="matcha";p=new Palette("matcha");page="minigames";Build();Capture("polish-menu");StartMiniGame("tiles");miniSurface.ShowPicture=false;Capture("polish-photo-puzzle");miniSurface.ShowPicture=true;Capture("polish-photo-preview");BackToMiniGames();
 StartMiniGame("arrows");ZoomMini(2.4);miniScroll.UpdateLayout();check(miniSurface.BeginPan(new Point(30,30),-1),"Pan cannot begin");miniSurface.MovePan(new Point(15,15));miniSurface.EndPan();check(!miniSurface.Panning,"Pan did not end");StartMiniGame("arrows");miniScroll.UpdateLayout();check(miniScroll.ScrollableHeight<1,"Next round retained zoom");BackToMiniGames();
 page="home";Build();Capture("polish-home");StartMiniGame("mousedash");MiniDashDown(new Point(450,204));mini.Tick(.05);Capture("polish-dodge-playing");BackToMiniGames();
 page="notebook";Build();UpdateLayout();ConfigureNotebookTool("pen");BeginNotebookChange();var ink=new System.Windows.Ink.Stroke(new System.Windows.Input.StylusPointCollection(new[]{new System.Windows.Input.StylusPoint(20,20)}));ink.DrawingAttributes.FitToCurve=false;notebookInk.Strokes.Clear();notebookInk.Strokes.Add(ink);var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(SmoothInkCanvas).GetField("mouseStroke",flags).SetValue(notebookInk,ink);var append=typeof(SmoothInkCanvas).GetMethod("Append",flags);var clock=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<20000;i++)append.Invoke(notebookInk,new object[]{new Point(50+i%800,50+(i*7)%300),false});clock.Stop();check(notebookInk.Strokes.All(s=>s.StylusPoints.Count<=96),"Long mouse stroke is unbounded");check(clock.Elapsed.TotalSeconds<10,"Long stroke hot path too slow");CommitNotebookChange();check(notebookUndo.Count==1,"Scribble split the undo gesture");NotebookHistory(false);check(notebookInk.Strokes.Count==0,"Undo did not remove the complete scribble");
 check(Diagnostics.ErrorCount==0,"Unexpected native rendering exception");await Task.Delay(50);File.WriteAllText(report,"PASS: "+checks+" checks; all 100 arrow levels and photo scrambles solvable, immediate rapid arrow input, 100 distinct melodies, fifteen games rendered in all five themes and both languages, preview, round fit and panning. 20,000 scribble samples processed in "+clock.ElapsedMilliseconds+"ms with bounded segments and one undo gesture. No desktop interaction.");
 }catch(Exception error){File.WriteAllText(report,"FAIL after "+checks+": "+error);Environment.ExitCode=1;}finally{closing=true;Close();}}
}}
