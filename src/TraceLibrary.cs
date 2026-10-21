using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace LittleNotebook {
 public static class TraceLibrary {
  public static readonly string[] Keys={"cat","rabbit","rabbit-body","sleepy-cat","flower","butterfly","fish","bird","cup","eye","bear","plant","cake","rainbow","mountains","ocean","cozy-room","tea-set"};
  public static bool Large(string key){return new[]{"mountains","ocean","cozy-room","tea-set"}.Contains(key);}
  public static void Initialize(){if(Doodles.Paths.ContainsKey("mountains"))return;
   Doodles.Paths["math"]=new[]{"M20,40 L65,40 M42,17 L42,63 M98,24 L136,62 M136,24 L98,62 M20,114 L65,114 M98,114 L142,114 M120,91 A3,3 0 1 1 114,91 A3,3 0 1 1 120,91 M120,138 A3,3 0 1 1 114,138 A3,3 0 1 1 120,138"};
   Doodles.Paths["brain"]=new[]{"M80,28 C61,2 40,18 43,38 C15,32 13,65 28,75 C10,91 24,117 43,116 C33,141 66,156 80,137 C94,156 128,141 118,116 C142,117 150,91 132,75 C148,54 132,31 116,38 C119,18 99,2 80,28 Z","M80,28 L80,137 M43,38 Q62,37 58,60 M28,75 Q58,68 55,89 M43,116 Q62,106 68,126 M116,38 Q98,40 102,61 M132,75 Q104,69 107,91 M118,116 Q100,109 93,126"};
   Doodles.Paths["rabbit-body"]=new[]{"M58,54 C31,-2 61,-3 70,44 M88,44 C93,-4 123,1 103,55 M48,53 C28,95 128,100 114,57 Q82,33 48,53 Z","M55,94 C33,120 40,148 79,149 C122,150 126,121 105,94 M50,122 Q20,116 20,136 Q27,151 48,145 M110,121 Q141,117 140,137 Q133,151 111,145 M57,69 L58,73 M98,69 L99,73 M74,81 L85,81 L80,86 M80,86 Q68,95 64,87 M80,86 Q93,95 98,87 M65,111 L62,130 M95,111 L98,130"};
   Doodles.Paths["sleepy-cat"]=new[]{"M39,71 C14,113 27,145 91,144 C143,145 151,101 120,86 M38,73 L30,36 L59,54 Q77,41 97,54 L124,35 L118,76 C134,119 47,127 38,73 Z","M49,76 Q57,85 65,76 M86,76 Q94,85 102,76 M71,92 L81,92 M76,92 Q70,102 64,96 M76,92 Q83,102 90,96 M110,113 C76,94 56,117 78,132 C95,145 127,130 115,117 M13,26 L29,26 L13,44 L29,44 M39,8 L50,8 L39,21 L50,21"};
   Doodles.Paths["bear"]=new[]{"M40,46 C5,17 43,-3 58,26 M103,26 C125,-3 159,22 123,47 M37,45 C14,104 145,106 124,45 Q81,12 37,45 Z","M56,59 L57,64 M104,59 L105,64 M65,77 Q80,64 96,77 Q96,98 80,98 Q65,98 65,77 Z M74,77 L87,77 L80,83 M80,83 L80,91 M56,103 C32,126 42,150 80,149 C121,149 128,123 106,103 M59,125 L58,145 M102,125 L102,145"};
   Doodles.Paths["plant"]=new[]{"M37,100 L124,100 L113,150 L48,150 Z M31,91 L130,91 L130,101 L31,101 Z M80,91 L80,29","M80,70 C18,71 26,26 80,57 M80,52 C126,55 144,12 83,34 M80,30 C57,22 60,3 80,6 C100,3 103,21 80,30"};
   Doodles.Paths["cake"]=new[]{"M22,78 L138,78 L138,143 L22,143 Z M22,94 Q37,119 53,94 Q68,119 84,94 Q100,119 116,94 Q130,119 138,94 M14,143 L147,143 M33,78 Q48,42 81,45 Q118,43 129,78 Z","M75,45 L75,22 L86,22 L86,45 M80,19 C65,13 82,-1 84,1 C99,14 91,20 80,19"};
   Doodles.Paths["rainbow"]=new[]{"M17,123 C9,-8 152,-8 145,123 M34,122 C27,15 133,15 127,122 M50,122 C48,42 112,42 111,122 M66,123 C67,73 95,73 95,123","M17,121 C2,99 -9,124 6,138 L52,138 C67,126 52,111 40,121 M127,121 C113,102 100,124 113,139 L151,139 C173,125 151,107 143,122"};
   Doodles.Paths["mountains"]=new[]{"M1,95 L43,28 L87,96 M51,96 L101,12 L159,96 M28,53 L42,61 L53,48 M82,44 L100,57 L118,40","M31,20 A10,10 0 1 1 11,20 A10,10 0 1 1 31,20 M1,105 Q38,94 79,106 Q122,117 159,102 M1,130 Q39,118 80,130 Q118,143 159,126 M83,115 L83,151 M70,151 L98,151 M83,107 L64,130 L103,130 Z M135,86 L135,122 M120,122 L149,122 M135,75 L116,101 L154,101 Z M39,110 C58,119 29,138 52,158 M54,111 C70,122 45,138 67,158"};
   Doodles.Paths["ocean"]=new[]{"M1,87 Q16,78 31,87 T61,87 T91,87 T121,87 T159,87 M1,105 Q16,96 31,105 T61,105 T91,105 T121,105 T159,105 M1,126 Q16,117 31,126 T61,126 T91,126 T121,126 T159,126","M70,79 L108,79 L98,93 L80,93 Z M88,79 L88,27 L116,73 L88,73 M82,35 L62,74 L82,74 Z M29,27 A13,13 0 1 1 3,27 A13,13 0 1 1 29,27 M122,31 Q130,21 138,31 Q146,21 154,31 M17,145 L22,133 L27,145 L41,145 L30,151 L35,160 L22,154 L10,160 L14,151 L3,145 Z"};
   Doodles.Paths["cozy-room"]=new[]{"M7,8 L153,8 L153,151 L7,151 Z M7,112 L153,112 M7,151 L34,112 M153,151 L125,112 M27,22 L83,22 L83,78 L27,78 Z M55,22 L55,78 M27,50 L83,50","M99,82 L141,82 L141,113 L99,113 Z M102,114 L102,137 M138,114 L138,137 M98,81 L141,81 M119,81 L119,38 M103,38 L135,38 L130,24 L108,24 Z M37,102 L73,102 L73,117 L37,117 Z M41,117 L41,143 M69,117 L69,143 M41,102 L41,87 L69,87 L69,102 M16,129 L91,129 L91,143 L16,143"};
   Doodles.Paths["tea-set"]=new[]{"M17,137 Q81,153 146,137 M26,87 L66,87 L62,126 Q46,137 31,126 Z M66,94 C88,89 83,120 64,116 M97,70 L135,70 L133,110 Q116,119 101,110 Z M136,79 C154,72 155,100 134,102","M30,86 Q47,72 64,86 M101,68 Q117,57 133,68 M41,69 Q27,55 41,41 M55,66 Q44,52 58,38 M112,54 Q101,40 114,27 M14,136 L145,136 M77,24 C61,0 44,13 58,30 L77,47 L97,30 C111,13 94,0 77,24 Z"};
  }
 }
 public partial class MainWindow {
  string TraceName(string key){switch(key){case "rabbit-body":return T("Rabbit","أرنب");case "sleepy-cat":return T("Sleeping cat","قطة نائمة");case "mountains":return T("Mountain scenery","منظر جبلي");case "ocean":return T("Ocean scenery","منظر بحري");case "cozy-room":return T("Cozy room","غرفة هادئة");case "tea-set":return T("Tea time","وقت الشاي");default:return key.Replace('-',' ');}}
  void TracePicker(){TraceLibrary.Initialize();Modal(T("Tracing drawings","رسومات للتتبّع"),c=>{
   Label(c,T("Choose an outline, or import a photo to make your own.","اختر رسماً أو استورد صورة لإنشاء قالبك."),95,102,17,600);var content=new WrapPanel{Width=606};foreach(string key in TraceLibrary.Keys){string selected=key;var button=Button("",()=>{CloseModal();BeginNotebookChange();CurrentNotebookPage.Template=selected;CurrentNotebookPage.TraceInk="";CurrentNotebookPage.TraceWidth=TraceLibrary.Large(selected)?820:330;CurrentNotebookPage.TraceHeight=TraceLibrary.Large(selected)?650:330;CurrentNotebookPage.Height=Math.Max(CurrentNotebookPage.Height,CurrentNotebookPage.TraceHeight+70);CurrentNotebookPage.TraceVisible=true;ResizeNotebookSurface();RefreshNotebookGuide();CommitNotebookChange();},140,116);button.Margin=new Thickness(4);button.Content=new TraceArt(key,p,100,100){Opacity=.9};button.ToolTip=TraceName(key);System.Windows.Automation.AutomationProperties.SetName(button,key+" tracing");content.Children.Add(button);}var scroll=new ScrollViewer{Width=626,Height=445,Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};StylePaperScroll(scroll);Put(c,scroll,91,149);
   Btn(c,T("Remove outline","إزالة القالب"),99,632,()=>{CloseModal();BeginNotebookChange();CurrentNotebookPage.Template="none";CurrentNotebookPage.TraceInk="";RefreshNotebookGuide();CommitNotebookChange();},270,47);Btn(c,T("Import tracing…","استيراد قالب…"),399,632,ImportTracing,295,47,true);
  },737);}
 }
}
