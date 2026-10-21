using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using LittleNotebook;

// Render the real application controls into README images using an isolated profile.
// This does not interact with the desktop or change the player's saved game.
static class ReadmeRenderer {
 static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
 static void Set(MainWindow window, string name, object value) { typeof(MainWindow).GetField(name, Private).SetValue(window, value); }
 static void Call(MainWindow window, string name, params object[] values) { typeof(MainWindow).GetMethod(name, Private).Invoke(window, values); }
 static void Scene(MainWindow window, string page, string image) { Set(window, "page", page); Call(window, "Build"); Call(window, "Capture", "public-" + image); }
 [STAThread] static int Main() {
  Diagnostics.Testing = true;
  var app = new Application();
  var window = new MainWindow(true, "readme-profile.json") { ShowActivated = false, WindowState = WindowState.Minimized };
  window.Loaded += (sender, args) => window.Dispatcher.BeginInvoke(new Action(() => {
   try {
    ((DispatcherTimer)typeof(MainWindow).GetField("timer", Private).GetValue(window)).Stop();
    window.Profile = new Profile { Name = "Maya", Character = 0 };
    window.Profile.Settings.Music = false; window.Profile.Settings.Sound = false; window.Profile.Settings.Volume = 0;
    Scene(window, "welcome", "setup");
    Set(window, "selectedTopic", "mixed"); window.Profile.Settings.Difficulty = 3;
    Scene(window, "home", "home");
    var bank = (QuestionBank)typeof(MainWindow).GetField("bank", Private).GetValue(window);
    var question = bank.Questions.First(q => q.Kind == "order" && q.Difficulty == 1);
    Set(window, "game", new GameSession(1) { Topic = "english", Questions = new List<Question> { question } });
    Set(window, "interactionId", ""); Scene(window, "game", "gameplay");
    Set(window, "game", null);
    window.Profile.Notebook = new List<NotebookPage> { new NotebookPage { Template = "rabbit-body" } };
    window.Profile.NotebookIndex = 0;
    Scene(window, "notebook", "notebook");
    Console.WriteLine("Rendered setup, Play, gameplay and notebook with Maya.");
   } catch(Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
   finally { Set(window, "game", null); Set(window, "closing", true); window.Close(); }
  }), DispatcherPriority.ApplicationIdle);
  app.Run(window); return Environment.ExitCode;
 }
}
