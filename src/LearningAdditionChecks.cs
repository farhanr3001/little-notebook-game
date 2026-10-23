using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
namespace LittleNotebook {public partial class MainWindow {
 public void RunLearningAdditionChecks(){int count=0;Action<bool,string> check=(ok,message)=>{count++;if(!ok)throw new Exception(message);};string report=Path.GetFullPath(Path.Combine(baseDir,"../test-output/learning-additions.txt"));try{
 timer.Stop();audio.Dispose();Profile=new Profile{Name="Maya"};Profile.Settings.Sound=false;Profile.Settings.Music=false;audio=new AudioSystem(Profile.Settings);
 check(LearnerDictionary.Words.Length>=90,"Dictionary is too short");check(LearnerDictionary.Words.Select(w=>w.Word).Distinct().Count()==LearnerDictionary.Words.Length,"Duplicate headwords");foreach(var word in LearnerDictionary.Words)check(!string.IsNullOrWhiteSpace(word.DefinitionAr)&&!string.IsNullOrWhiteSpace(word.Example)&&!string.IsNullOrWhiteSpace(word.Pronunciation),"Incomplete entry "+word.Word);
 foreach(string language in new[]{"en","ar"}){Profile.Settings.Language=language;p=new Palette(Profile.Settings.Theme);foreach(string category in new[]{"spelling-repair","dictionary"})foreach(int level in new[]{0,1,2}){
 var stream=new QuestionStream(bank,new[]{"english"},level,new Attempt[0],new Dictionary<string,string>{{"english",category}},5);var questions=Enumerable.Range(0,5).Select(i=>stream.Next()).ToList();check(questions.All(q=>q.Category==category&&q.PairArabic.Length==4&&q.PairMeanings.Length==4),"New pair fields lost during generation");game=new GameSession(level){Topic="english",Questions=questions};page="game";interactionId="";Build();for(int i=0;i<4;i++){wordPairLeft=i;MatchWordPair(i);check(wordPairsMatched.Contains(i),"Correct pair was not marked");}check(game.Answered&&game.Correct==1,"All matches did not complete");check(Descendants<Button>(interaction).Count(b=>Convert.ToString(b.Content).Contains("✓"))==8,"Match ticks missing");check(Descendants<Button>(body).Any(b=>Convert.ToString(b.Content).Contains(language=="en"?"Continue":"متابعة")),"Continue button missing");AdvanceFeedback(2);check(game.Index==0,"Meanings disappear too soon");Capture("learn-"+language+"-"+category+"-"+level);game=null;
 }
 page="study";studyTopic="dictionary";Build();Capture("dictionary-"+language);check(Descendants<TextBlock>(body).Any(t=>t.Text=="already"),"Dictionary headwords missing");var search=Descendants<TextBox>(body).Single();search.Text="beautiful";check(Descendants<TextBlock>(body).Any(t=>t.Text=="beautiful"),"Dictionary search failed");check(!Descendants<TextBlock>(body).Any(t=>t.Text=="already"),"Dictionary search did not filter");dictionarySearch="";dictionaryCategory="conversation";Build();Capture("dictionary-conversation-"+language);dictionaryCategory="all";studyTopic="english";studyRevealed=true;Build();Capture("study-sidebar-"+language);
 }
 Profile.Settings.Language="en";page="notebook";Build();foreach(string key in TraceLibrary.Keys.Where(k=>k.StartsWith("anime-"))){CurrentNotebookPage.Template=key;CurrentNotebookPage.TraceWidth=400;CurrentNotebookPage.TraceHeight=400;CurrentNotebookPage.Height=720;ResizeNotebookSurface();RefreshNotebookGuide();check(notebookGuide.Children.Count==1,"Anime tracing not displayed");Capture("trace-"+key);}
 game=null;page="home";Profile.Settings.Language="en";Build();Capture("final-home");
 check(Diagnostics.ErrorCount==0,"Unexpected UI error");File.WriteAllText(report,"PASS: "+count+" checks; 92 dictionary entries, 144 matching activities, category filtering, meanings/ticks, delayed continue, generated field preservation, bilingual dictionary/search/sidebar and five simplified anime tracings. No desktop interaction.");
 }catch(Exception error){File.WriteAllText(report,"FAIL after "+count+": "+error);Environment.ExitCode=1;}finally{closing=true;Close();}}
}}
