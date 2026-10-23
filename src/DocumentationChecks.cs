using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
namespace LittleNotebook {public partial class MainWindow {
 void CaptureDocumentation(){
  StopMiniGame();game=null;Profile=new Profile{Name="Maya",Coins=42};Profile.Settings.Sound=Profile.Settings.Music=Profile.Settings.AutoReadQuestions=false;Profile.Settings.Theme="matcha";Profile.Settings.Paper=PaperDesign.ForTheme("matcha");Profile.Settings.PlaySection="learning";MiniCatalog.Normalize(Profile);selectedTopic="mixed";
  page="welcome";Build();Capture("readme-setup");page="home";Build();Capture("readme-home");
  var q=bank.Questions.First(x=>x.Topic=="english"&&x.Kind=="choice"&&x.Difficulty==0);game=new GameSession(0){Topic="english",Questions=new List<Question>{q}};page="game";interactionId="";Build();Capture("readme-gameplay");game=null;
  StartMiniGame("arrows");Capture("readme-arrow-escape");BackToMiniGames();StartMiniGame("tiles");Capture("readme-picture-patches");BackToMiniGames();StartMiniGame("stack");mini.StackTap();for(int i=0;i<7;i++){PaperStackChecks.Land(mini.Stack,0);mini.Stack.Tick(.1);mini.Stack.Tick(.1);mini.Stack.Tick(.1);}miniSurface.InvalidateVisual();RefreshMiniHud();Capture("readme-paper-stack");BackToMiniGames();
  StartMiniGame("mousedash");mini.HoldDash(450,204);mini.Tick(.1);mini.MoveDash(485,222);miniSurface.InvalidateVisual();RefreshMiniHud();Capture("readme-mouse-dash");BackToMiniGames();
  page="notebook";CurrentNotebookPage.Template="rabbit-body";CurrentNotebookPage.TraceWidth=CurrentNotebookPage.TraceHeight=330;CurrentNotebookPage.Texts.Add(new NotebookText{Text="A little time\nto create…",X=60,Y=85,Width=230,Height=130,FontSize=25,Color="#698D67"});Build();ConfigureNotebookTool("pencil");FitNotebook();Capture("readme-notebook");
 }
}}
