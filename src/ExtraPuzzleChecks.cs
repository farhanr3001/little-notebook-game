using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
namespace LittleNotebook {public partial class MainWindow {
 public void RunExtraPuzzleChecks(){int count=0;Action<bool,string> check=(ok,message)=>{count++;if(!ok)throw new Exception(message);};string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/extra-puzzles.txt"));try{
 timer.Stop();Profile=new Profile{Name="Maya"};Profile.Settings.Sound=false;Profile.Settings.Music=false;audio.Dispose();audio=new AudioSystem(Profile.Settings);MiniCatalog.Normalize(Profile);
 foreach(string kind in ExtraPuzzle.Keys)for(int level=1;level<=100;level++){
  var b=new ExtraPuzzle(kind,level,level*7193+Array.IndexOf(ExtraPuzzle.Keys,kind));
  if(kind=="untangle")check(b.Crossings()>0,"Thread board starts untangled");if(kind=="crates")check(Enumerable.Range(0,b.Cells.Length).Any(i=>b.Goal[i]==1&&b.Cells[i]!=1),"Crates start solved");
  check(!b.Won&&!b.Over,"Board starts finished: "+kind+" "+level);
  if(kind=="lights")foreach(int i in b.Solution)check(b.Input(i),"Light move rejected");
  if(kind=="queens")foreach(int i in b.Solution)if(b.Cells[i]==0)check(b.Input(i),"Queen placement rejected");
  if(kind=="crates")foreach(int d in b.Solution){if(b.Won)break;check(b.Move(d),"Reverse-constructed crate solution failed at "+level);}
  if(kind=="pegs")foreach(int action in b.Solution){check(b.Input(action/1000),"Peg source rejected");check(b.Input(action%1000),"Peg jump rejected");}
  if(kind=="untangle"){b.BeginDrag(0);b.Drag(new Point(340,200));b.EndDrag();check(b.Undo(),"Drag undo missing");b.Points=b.Original.ToList();b.BeginDrag(0);b.EndDrag();}
  if(kind=="pour")foreach(int action in b.Solution){if(b.Won)break;check(b.Input(action/100),"Pour source rejected");check(b.Input(action%100),"Reverse-constructed pour failed at "+level);}
  if(kind=="code"){for(int i=0;i<b.W;i++){b.Input(i);b.Input(100+b.Goal[i]);}check(b.Input(99),"Code guess rejected");check(b.Scores[0][0]==b.W&&b.Scores[0][1]==0,"Repeated-colour feedback incorrect");}
  if(kind=="domino")foreach(int action in b.Solution){check(b.Input(action/1000),"Domino source rejected");check(b.Input(action%1000),"Domino pair rejected");}
  if(kind=="glide")foreach(int d in b.Solution)check(b.Move(d),"Glide BFS proof failed");
  if(kind=="flood")foreach(int c in b.Solution)check(b.Input(c),"Flood proof failed");
  check(b.Won,"Board solution did not win: "+kind+" "+level);if(kind!="untangle"){check(b.Undo(),"Undo missing: "+kind);check(!b.Won,"Undo retains win: "+kind);}
 }
 foreach(string theme in new[]{"matcha","sky","rose","lavender","night"})foreach(string lang in new[]{"en","ar"}){Profile.Settings.Language=lang;Profile.Settings.Theme=theme;p=new Palette(theme);foreach(string kind in ExtraPuzzle.Keys){StartMiniGame(kind);mini.Tick(.5);UpdateLayout();miniScroll.UpdateLayout();check(miniScroll.ScrollableWidth<1&&miniScroll.ScrollableHeight<1,"Extra game does not fit");Capture("extra-"+theme+"-"+lang+"-"+kind);PauseMiniGame();double elapsed=mini.Elapsed;MiniTick(null,EventArgs.Empty);check(mini.Elapsed==elapsed,"Pause advanced time");CloseModal();BackToMiniGames();}}
 Profile.Settings.Language="en";Profile.Settings.Theme="matcha";p=new Palette("matcha");page="minigames";Build();Capture("extra-menu");StartMiniGame("tiles");mini.Tick(.1);double before=mini.Elapsed;mini.Phase="complete";mini.Wait=4;mini.Tick(.1);check(mini.Elapsed==before,"Picture clock keeps running after completion");UpdateLayout();check(miniClock!=null&&miniPreviewEye!=null,"Picture timer/preview missing");check(Canvas.GetLeft(miniPreviewEye)<480,"Eye is not beside the puzzle");Capture("picture-timed");BackToMiniGames();StartMiniGame("tiles");foreach(int cell in mini.Tiles.Scramble.AsEnumerable().Reverse()){mini.SlideTile(cell);mini.Tick(.1);mini.Tick(.1);}check(Profile.MiniGames["tiles"].Results.Count>0,"Picture completion record missing");var result=Profile.MiniGames["tiles"].Results.Last();check(result.Moves>0&&result.Seconds>0,"Picture record lacks moves/time");check(store.Load().MiniGames["tiles"].Results.Last().Moves==result.Moves,"Picture record not persisted");BackToMiniGames();ShowMiniRecords();Capture("picture-records");CloseModal();
 check(Diagnostics.ErrorCount==0,"Unexpected native errors");
 foreach(string kind in ExtraPuzzle.Keys){StartMiniGame(kind);var b=mini.Extra;if(kind=="lights")MiniPress(0);if(kind=="queens")MiniPress(b.Solution.Last());if(kind=="crates"||kind=="glide"){foreach(int d in Enumerable.Range(0,4))if(b.Move(d))break;}if(kind=="pegs"||kind=="domino"){MiniPress(b.Solution[0]/1000);MiniPress(b.Solution[0]%1000);}if(kind=="untangle"){b.BeginDrag(0);b.Drag(new Point(325,210));b.EndDrag();}if(kind=="pour"){MiniPress(b.Solution[0]/100);MiniPress(b.Solution[0]%100);b.Feedback=.11;}if(kind=="code"){for(int i=0;i<b.W;i++){MiniPress(i);MiniPress(100+(b.Goal[i]+1)%b.Colours);}MiniPress(99);}if(kind=="flood")MiniPress(b.Solution[0]);mini.CheckExtra();RefreshMiniHud();miniSurface.InvalidateVisual();Capture("extra-action-"+kind);BackToMiniGames();}
 File.WriteAllText(report,"PASS: "+count+" checks; 100 levels per new game verified using legal solution proofs, input/undo, code scoring, native rendering in all five themes and both languages, action feedback captures, pause, fitting and Picture Patches clock/eye/record persistence. No desktop interaction.");
 }catch(Exception error){File.WriteAllText(report,"FAIL after "+count+": "+error);Environment.ExitCode=1;}finally{closing=true;Close();}}
}}
