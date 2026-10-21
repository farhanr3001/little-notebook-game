using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LittleNotebook {
 public static class SelfTests {
  static int count=0;
  static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
  public static int Run(){
   string dir=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"../test-output"));Directory.CreateDirectory(dir);
   try {
    var bank=new QuestionBank(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"content/questions.json"),42);
    Check(bank.Questions.Count>=100,"Content loaded");
    for(int level=0;level<3;level++)for(int n=0;n<2000;n++){
     var q=bank.MathQuestion(level);Check(q.Options.Length==4&&q.Options.Distinct().Count()==4&&q.Options.Contains(q.Answer),"Unique valid math choices");
     string s=q.Stem.Replace(" = ?","");var parts=s.Split(' ');int a=int.Parse(parts[0]),b=int.Parse(parts[2]),expected=0;
     switch(parts[1]){case "+":expected=a+b;break;case "−":expected=a-b;break;case "×":expected=a*b;break;case "÷":Check(a%b==0,"Exact division");expected=a/b;break;}
     if(parts.Length==5)expected=a+b*int.Parse(parts[4]);Check(q.Answer==expected.ToString(),"Correct arithmetic answer");
    }
    foreach(var topic in new[]{"english","math","puzzles","general","mixed"})for(int level=0;level<3;level++){
     var round=bank.Round(topic,level,15,new string[0]);Check(round.Count==15,"Round size");Check(round.Select(q=>q.Id).Distinct().Count()==15,"No duplicates in round");Check(round.All(q=>topic=="mixed"||q.Topic==topic),"Topic filtering");Check(round.All(q=>q.Difficulty==level),"Difficulty filtering");
    }
    Check(QuestionBank.Matches("  I   like TEA. ","I like tea"),"Answer normalization");Check(!QuestionBank.Matches("chair","book"),"Different answers rejected");
    var hard=new GameSession(2){Questions=bank.Round("math",2,5,new string[0])};Check(hard.Lives==1&&hard.MaxLives==1,"Hard starts with exactly one life");hard.Answer("incorrect",false);Check(hard.Lives==0&&hard.IsOver,"One incorrect answer ends Hard round");Check(new GameSession(0).Lives==3&&new GameSession(1).Lives==3,"Easy and Medium have three lives");
    var game=new GameSession{Questions=bank.Round("math",0,5,new string[0])};Check(game.Hint(),"Hint allowed");Check(!game.Hint(),"No double hint");var attempt=game.Answer(game.Current.Answer,false);Check(attempt.Correct&&game.Score==6&&game.Lives==3&&game.Earned==1,"Hint scoring");Check(game.Answer("wrong",false)==null,"No double submission");game.Next();Check(!game.Answered&&!game.UsedHint&&game.Index==1,"Next resets state");game.Answer(game.Current.Answer,true);Check(game.Lives==2&&game.Answer("",true)==null,"Timeout costs a life and duplicate is ignored");
    var path=Path.Combine(dir,"save-test.json");var store=new SaveStore(path);var profile=new Profile{Name="آية",Coins=17,Character=2};profile.Settings.Language="ar";profile.Attempts.Add(attempt);Check(store.Save(profile),"Save");var loaded=store.Load();Check(loaded.Name=="آية"&&loaded.Coins==17&&loaded.Attempts.Count==1,"Unicode save roundtrip");profile.Coins=18;Check(store.Save(profile),"Atomic replace");File.WriteAllText(path,"broken");loaded=store.Load();Check(loaded.Coins==17,"Backup recovery");
    foreach(int level in new[]{0,1}){var single=new GameSession(level,true){Questions=bank.Round("math",level,5,new string[0])};Check(single.MaxLives==1&&single.Lives==1,"Single-life override on Easy/Medium");single.Answer("wrong",false);Check(single.IsOver&&single.Lives==0,"Single-life override ends after one wrong answer");}
    foreach(string topic in new[]{"math","english","puzzles","general","mixed"})foreach(int level in new[]{0,1,2}){
     var stream=new QuestionStream(bank,topic,level,new string[0]);var endless=new GameSession(level,false,true){Questions=new List<Question>{stream.Next()}};
     for(int n=0;n<150;n++){Check(!endless.IsOver,"Endless has no question-count limit");Check(endless.Current.Difficulty==level&&(topic=="mixed"||endless.Current.Topic==topic),"Endless retains topic/difficulty through replenishment");endless.Answer(endless.Current.Answer,false);endless.Next(stream.Next());}
     Check(endless.Index==150&&endless.Correct==150,"Endless passes the former round limit");
     while(endless.Lives>0){endless.Answer("wrong",false);if(!endless.IsOver)endless.Next(stream.Next());}Check(endless.IsOver,"Endless stops when all lives are lost");
    }
    foreach(string topic in new[]{"english","puzzles","general"}){
     int poolCount=bank.Questions.Count(q=>q.Topic==topic&&q.Difficulty==0);var stream=new QuestionStream(bank,topic,0,new string[0]);var cycle=Enumerable.Range(0,poolCount).Select(i=>stream.Next()).ToList();Check(cycle.Select(q=>q.Id).Distinct().Count()==poolCount,"Authored pool exhausted before repeating");Check(stream.Next().Id!=cycle.Last().Id,"No adjacent repeat across shuffled cycles");
    }
    var sequences=Enumerable.Range(0,8).Select(seed=>new QuestionBank(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"content/questions.json"),seed).Round("mixed",0,10,new string[0])).ToList();Check(sequences.Select(q=>string.Join("|",q.Select(x=>x.Id))).Distinct().Count()==8,"Ten-question rounds are randomized");
    var original=bank.Questions.First(q=>q.Options!=null);var copy=new QuestionStream(bank,original.Topic,original.Difficulty,new string[0]).Next();if(copy.Options!=null){var source=bank.Questions.First(q=>q.Id==copy.Id);string before=source.Options[0];copy.Options[0]="test mutation";Check(source.Options[0]==before,"Play instances do not mutate study bank");}
    string blocked=Path.Combine(dir,"not-a-save-directory");File.WriteAllText(blocked,"This file deliberately prevents creation of a save directory.");string fallback=Path.Combine(dir,"portable-save-test.json");var recovery=new SaveStore(Path.Combine(blocked,"profile.json"),fallback);profile.Settings.RoundLength=0;profile.Settings.SingleLife=true;profile.Settings.CustomMusic.Add("data/music/test/song.mp3");Check(recovery.Save(profile)&&recovery.Path==fallback&&recovery.Warning=="","Save falls back after a denied/unusable primary location");var reopened=new SaveStore(Path.Combine(blocked,"profile.json"),fallback).Load();Check(reopened.Name==profile.Name&&reopened.Attempts.Count==profile.Attempts.Count&&reopened.Settings.RoundLength==0&&reopened.Settings.SingleLife&&reopened.Settings.CustomMusic.Count>0,"Portable fallback survives reopening with new settings and progress");
    foreach(int size in new[]{1,2,3,8})for(int seed=0;seed<12;seed++){
     var music=new ShufflePlaylist(size,0,seed);var first=new List<int>{music.Current};for(int n=1;n<size;n++)first.Add(music.Next());Check(first.Distinct().Count()==size,"Shuffle plays every track before repeating");
     int previous=music.Current;for(int cycle=0;cycle<12;cycle++){var played=new List<int>();for(int n=0;n<size;n++){int next=music.Next();Check(size==1||next!=previous,"Shuffle avoids adjacent repeats");played.Add(next);previous=next;}Check(played.Distinct().Count()==size,"Every shuffled cycle covers library once");}
     int before=music.Current;int after=music.Next();Check(music.Previous()==before&&music.Next()==after,"Previous/next follow listening history");music.Select(size-1);Check(music.Current==size-1,"Manual radio selection changes shuffle starting point");
    }
    foreach(string topic in new[]{"math","english","puzzles","general"})for(int level=0;level<3;level++)foreach(string category in QuestionCatalog.Categories(bank,topic,level).Where(x=>x!="all")){
     var stream=new QuestionStream(bank,topic,level,new string[0],category);for(int n=0;n<45;n++){var item=stream.Next();Check(item.Topic==topic&&item.Difficulty==level&&QuestionCatalog.Category(item)==category,"Question category never leaks another type");}
    }
    var injected=new Question{Id="future-category-test",Topic="english",Kind="choice",Category="future-reading",Difficulty=0};bank.Questions.Add(injected);Check(QuestionCatalog.Categories(bank,"english",0).Contains("future-reading"),"Future authored categories appear automatically");bank.Questions.Remove(injected);
    string legacyPath=Path.Combine(dir,"legacy-profile.json");File.WriteAllText(legacyPath,"{\"Version\":1,\"Name\":\"Aya\",\"Character\":1,\"Coins\":43,\"Settings\":{\"Language\":\"en\",\"Theme\":\"rose\"},\"Owned\":[\"leaf\",\"star\"],\"Attempts\":[],\"Sessions\":[]}");var migrated=new SaveStore(legacyPath).Load();Check(migrated.Name=="Aya"&&migrated.Character==1&&migrated.Coins==43&&migrated.Owned.Contains("star")&&migrated.Settings.Theme=="rose"&&migrated.Notebook.Count==1&&migrated.Pet==""&&migrated.Settings.QuestionCategories.Count==0,"Legacy profile gains feature defaults without losing progress");
    var promptOnly=bank.Questions.First(x=>x.Kind=="pairs");var promptGame=new GameSession{Questions=new List<Question>{promptOnly}};Check(!string.IsNullOrWhiteSpace(promptGame.Answer("wrong",false).Prompt),"Prompt-only activities keep readable attempt history");
    var rewardProfile=new Profile{Coins=150,Name="Aya"};Check(ProfileFeatures.Purchase(rewardProfile,"pet:cat")&&rewardProfile.Coins==30,"Pet purchase charges exactly its price");Check(!ProfileFeatures.Purchase(rewardProfile,"pet:cat")&&rewardProfile.Coins==30,"Repeat purchase does not charge twice");Check(!ProfileFeatures.Purchase(rewardProfile,"pet:dog")&&rewardProfile.Coins==30,"Unaffordable purchase is rejected");Check(ProfileFeatures.Equip(rewardProfile,"pet:cat","cream")&&rewardProfile.PetColor=="cream","Owned pet colour equips");Check(!ProfileFeatures.Equip(rewardProfile,"pet:dog"),"Unowned pet cannot equip");rewardProfile.Notebook[0].Ink="sample-data";rewardProfile.Notebook[0].Template="flower";rewardProfile.Notebook.Add(new NotebookPage());rewardProfile.NotebookIndex=1;rewardProfile.Settings.QuestionCategories["english"]="spelling";
    var featureSave=new SaveStore(Path.Combine(dir,"feature-save.json"));Check(featureSave.Save(rewardProfile),"Feature profile saves");var featureLoaded=featureSave.Load();Check(featureLoaded.Pet=="pet:cat"&&featureLoaded.PetColor=="cream"&&featureLoaded.Notebook.Count==2&&featureLoaded.Notebook[0].Template=="flower"&&featureLoaded.Notebook[0].Ink=="sample-data"&&featureLoaded.Settings.QuestionCategories["english"]=="spelling","New rewards, pages and category preferences survive reload");
    var fresh=ProfileFeatures.FreshCharacter(rewardProfile.Settings);Check(fresh.Name==""&&fresh.Coins==40&&fresh.Attempts.Count==0&&fresh.Sessions.Count==0&&fresh.Owned.SequenceEqual(new[]{"leaf"})&&fresh.Pet==""&&fresh.Notebook.Count==1&&fresh.Notebook[0].Ink==""&&object.ReferenceEquals(fresh.Settings,rewardProfile.Settings),"Character reset clears all progress while keeping app preferences");Check(featureSave.Reset(fresh),"Reset writes clean profile and backup");File.WriteAllText(featureSave.Path,"corrupt");Check(featureSave.Load().Name==""&&featureSave.Load().Pet=="","Reset backup cannot restore old character progress");
    File.WriteAllText(Path.Combine(dir,"tests.txt"),"PASS: "+count+" assertions. Arithmetic, content, endless play, life overrides, scoring, hints, timeouts, Unicode saves, backup/fallback recovery and radio shuffle/history.\n");return 0;
   }catch(Exception ex){File.WriteAllText(Path.Combine(dir,"tests.txt"),"FAIL after "+count+" checks: "+ex);return 1;}
  }
 }
 public partial class MainWindow {
  void Capture(string name){UpdateLayout();stage.Measure(new Size(1440,900));stage.Arrange(new Rect(0,0,1440,900));stage.UpdateLayout();var bmp=new RenderTargetBitmap(1440,900,96,96,PixelFormats.Pbgra32);bmp.Render(stage);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));string dir=Path.GetFullPath(Path.Combine(baseDir,"../test-output"));Directory.CreateDirectory(dir);using(var f=File.Create(Path.Combine(dir,name+".png")))encoder.Save(f);}
  public void RunSmoke(){
   try {
    if(Icon==null)throw new Exception("Book window icon was not loaded");Profile=new Profile();Profile.Settings.Music=false;Profile.Name="";Profile.Settings.Language="en";Profile.Settings.SidebarHidden=false;Profile.Settings.TextScale=.9;Profile.Settings.ButtonScale=.92;page="welcome";Build();Capture("01-welcome");Profile.Character=3;Build();Capture("01b-boy-selection");Profile.Character=8;Build();Capture("01c-added-girl");Profile.Character=0;Profile.Name="Aya";page="home";Build();Capture("02-home");QuestionCountSettings();Capture("02d-question-count");CloseModal();RadioSelection();Capture("02e-radio-selection");CloseModal();ToggleSidebar();Capture("02b-focus-mode");SizingSettings();Capture("02c-sizing-preview");CloseModal();ToggleSidebar();
    Profile.Settings.RoundLength=0;Profile.Settings.SingleLife=true;Build();Capture("02f-endless-setup");StartGame();Capture("02g-endless-game");for(int n=0;n<20;n++){Submit(game.Current.Answer,false);if(n==0){paused=true;AdvanceFeedback(4);if(game.Index!=0)throw new Exception("Feedback advanced while paused");paused=false;}AdvanceFeedback(1);if(game.Index!=n)throw new Exception("Feedback disappeared too soon");AdvanceFeedback(.36);if(page!="game"||game.Index!=n+1)throw new Exception("Endless UI failed after "+n);}Submit("incorrect",false);AdvanceFeedback(1.36);if(page!="results"||Profile.Sessions.Last().Total!=21||!Profile.Sessions.Last().Endless)throw new Exception("Endless results failed");game=null;page="home";Profile.Settings.RoundLength=10;Profile.Settings.SingleLife=false;Build();
    selectedTopic="math";interactionId="";Profile.Settings.Difficulty=2;StartGame();Capture("03-hard-one-life");game=null;Profile.Settings.Difficulty=0;StartGame();Capture("03-math");WorkingPaper();Capture("04-working-paper");CloseModal();
    foreach(string kind in new[]{"image","spell","order","listen","search","memory"}){game=new GameSession{Topic=kind=="search"||kind=="memory"?"puzzles":"english",Questions=new List<Question>{bank.Questions.First(q=>q.Kind==kind)}};interactionId="";Build();Capture("game-"+kind);}
    game=null;page="profile";Build();Capture("05-progress");page="shop";Build();Capture("06-shop");page="study";studyRevealed=true;Build();Capture("07-study");
    Profile.Settings.Language="ar";page="home";Build();Capture("08-arabic");ToggleSidebar();Capture("08b-arabic-hidden");ToggleSidebar();Profile.Settings.Theme="night";Profile.Settings.Language="en";Build();Capture("09a-night-home");SettingsView();Capture("09-night-settings");CloseModal();
    Profile.Settings.Language="en";Profile.Settings.Theme="matcha";Profile.Settings.TextScale=1.15;Profile.Settings.ButtonScale=1;page="home";Build();Capture("10-largest-text");
    Profile.Settings.TextScale=.85;Profile.Settings.ButtonScale=.75;Build();Capture("11-small-controls");
    Profile.Settings.TextScale=.9;Profile.Settings.ButtonScale=.92;
    foreach(string theme in new[]{"matcha","rose","sky","lavender","night"}){Profile.Settings.Theme=theme;Build();Capture("theme-"+theme);}
    Profile.Settings.Theme="matcha";
    foreach(string kind in new[]{"spell","order","memory","search"}){
     var q=bank.Questions.First(x=>x.Kind==kind);game=new GameSession{Topic=q.Topic,Questions=new List<Question>{q}};page="game";interactionId="";Build();
     if(kind=="order"){
      foreach(string word in q.Answer.Split(' ')){int index=orderTokens.FindIndex(w=>w==word);ordered.Add(index);}RenderInteraction();Submit(string.Join(" ",ordered.Select(i=>orderTokens[i])),false);
     }else if(kind=="memory"){
      foreach(int card in memoryCards.Distinct().ToArray()){var pair=Enumerable.Range(0,memoryCards.Length).Where(i=>memoryCards[i]==card).ToArray();FlipCard(pair[0]);FlipCard(pair[1]);}
     }else if(kind=="search"){
      bool found=false;for(int start=0;start<64&&!found;start++)foreach(int step in new[]{1,8}){var cells=Enumerable.Range(0,q.Answer.Length).Select(i=>start+i*step).ToArray();if(cells.Last()<64&&(step==8||cells[0]/8==cells.Last()/8)&&new string(cells.Select(i=>searchLetters[i]).ToArray())==q.Answer){foreach(int cell in cells)SelectSearch(cell);Submit(new string(searchSelected.Select(i=>searchLetters[i]).ToArray()),false);found=true;break;}}
     }else Submit(q.Answer,false);
     if(!game.Answered||game.Correct!=1)throw new Exception("UI interaction failed: "+kind);
     NextQuestion();if(page!="results"||!game.Recorded)throw new Exception("Results transition failed: "+kind);
    }
    Capture("12-results");Profile.Character=1;Build();Capture("12b-centered-hijab-results");page="home";Build();Capture("12c-centered-sidebar");RunFeatureSmoke();game=null;Profile=new Profile();Profile.Settings.Music=false;page="welcome";Build();Save();
    File.WriteAllText(Path.GetFullPath(Path.Combine(baseDir,"../test-output/smoke.txt")),"PASS: native screen renders, Arabic right sidebar and hidden layout, dark mode, sidebar recentering, Boy/Girl filter, sizing preview/extremes, question-count picker and radio selection. Endless game auto-advances after 1.35 seconds of active feedback, pauses feedback when paused, continues beyond 20 questions, then auto-shows results after one lost life and saves a 21-answer session. Spelling, sentence order, word search and memory matching transition to saved results. v0.3 feature suite passed character edit/cancel/confirm, purchase/equip, pet preview, scrollable shop, multi-page ink save/reload, undo/redo, clear/undo, tracing/stamp selection, notebook reset/cancel, Arabic notebook, word pairs at every difficulty, hard memory, category filtering and character reset.");closing=true;Close();
   }catch(Exception ex){File.WriteAllText(Path.GetFullPath(Path.Combine(baseDir,"../test-output/smoke.txt")),ex.ToString());Environment.ExitCode=1;closing=true;Close();}
  }
 }
}
