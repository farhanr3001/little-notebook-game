using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LittleNotebook {
 public sealed partial class MiniSurface {
  void StackPiece(DrawingContext dc,double x,double y,double width,int row,bool perfect){
   Color color=Blend(palette.Accent,.45+.38*(.5+.5*Math.Sin(row*.67)));if(width<70)color=Mix(color,Color.FromRgb(211,133,93),.38);if(perfect)color=Mix(color,palette.Gold,.20);
   dc.DrawRoundedRectangle(Brush(Blend(palette.Ink,.10)),null,new Rect(x+2,y+9,width,17),3,3);
   dc.DrawRoundedRectangle(Brush(color),Pen(Blend(palette.Ink,.75),1),new Rect(x,y+5,width,16),2,2);
   var top=new StreamGeometry();using(var c=top.Open()){c.BeginFigure(new Point(x,y+5),true,true);c.LineTo(new Point(x+7,y),true,false);c.LineTo(new Point(x+width+7,y),true,false);c.LineTo(new Point(x+width,y+5),true,false);}dc.DrawGeometry(Brush(Mix(color,palette.Paper,.35)),Pen(Blend(palette.Ink,.55),.9),top);
   dc.DrawLine(Pen(Blend(palette.Paper,.55),.8),new Point(x+2,y+9),new Point(x+Math.Max(2,width-2),y+9));
  }
  void DrawStack(DrawingContext dc){
   var board=Session.Stack;double camera=reduced?Math.Max(0,(board.Score-7)*PaperStackBoard.LayerHeight):board.Camera;
   if(PaperStackBoard.Floor+camera<Height)dc.DrawLine(Pen(Blend(palette.Ink,.3),1.5),new Point(250,377+camera),new Point(657,377+camera));
   foreach(var layer in board.Layers){double y=PaperStackBoard.Floor-layer.Row*PaperStackBoard.LayerHeight+camera;if(y<-35||y>Height)continue;double bounce=layer==board.Top&&board.LandingAge<.20&&!reduced?Math.Sin(board.LandingAge/.20*Math.PI)*3:0;StackPiece(dc,layer.X,y+bounce,layer.Width,layer.Row,layer.Perfect);}
   if(board.Started&&board.Phase=="moving"||!board.Started){double y=PaperStackBoard.Floor-(board.Score+1)*PaperStackBoard.LayerHeight+camera;
    var guide=Pen(Blend(palette.Accent,.45),1);guide.DashStyle=DashStyles.Dot;dc.DrawLine(guide,new Point(board.Top.X,y-9),new Point(board.Top.X,y+42));dc.DrawLine(guide,new Point(board.Top.X+board.Top.Width,y-9),new Point(board.Top.X+board.Top.Width,y+42));
    if(!board.Started)dc.PushOpacity(.5);StackPiece(dc,board.MovingX,y,board.PieceWidth,board.Score+1,false);if(!board.Started)dc.Pop();
   }
   foreach(var chip in board.Chips){if(reduced)continue;dc.PushOpacity(Math.Max(0,1-chip.Age/1.6));dc.PushTransform(new RotateTransform(chip.Angle,chip.X+chip.Width/2,chip.Y+camera+10));StackPiece(dc,chip.X,chip.Y+camera,chip.Width,chip.Row,false);dc.Pop();dc.Pop();}
   if(board.LandingAge<.65&&board.Score>0){double age=board.LandingAge,y=PaperStackBoard.Floor-board.Score*PaperStackBoard.LayerHeight+camera;dc.PushOpacity(Math.Max(0,1-age/.65));
    Label(dc,board.Perfect?(ar?(board.Grew?"مثالي! + عرض":"مثالي!"):(board.Grew?"Perfect! + width":"Perfect!")):(ar?"أحسنت!":"Nice!"),new Point(board.Top.X+board.Top.Width/2,y-29-age*14),board.Perfect?22:17,board.Perfect?palette.Ink:palette.Muted);
    if(board.Score%5==0)Label(dc,ar?"أسرع!":"Faster!",new Point(board.Top.X+board.Top.Width/2,y-65-age*12),18,palette.Muted);
    if(board.Perfect){Rect halo=new Rect(board.Top.X-4-age*8,y-4-age*8,board.Top.Width+8+age*16,29+age*16);dc.DrawRoundedRectangle(null,Pen(palette.Gold,2),halo,5,5);if(!reduced)for(int i=0;i<10;i++){double angle=i*Math.PI/5,radius=16+age*65;Point sparkle=new Point(board.Top.X+board.Top.Width/2+Math.Cos(angle)*radius,y+9+Math.Sin(angle)*radius*.6);dc.DrawLine(Pen(palette.Gold,2),sparkle,sparkle+new Vector(Math.Cos(angle)*4,Math.Sin(angle)*4));}}
    dc.Pop();
   }
   if(!board.Started){Label(dc,ar?"قطعة فوق قطعة…":"One careful landing at a time",new Point(450,103),25);Label(dc,ar?"انقر أو اضغط E / مسافة للبدء":"Click or press E / Space to begin",new Point(450,144),16);}
  }
 }
 public partial class MainWindow {
  int miniStackStartBest;
  TextBlock miniStackPace;
  void RememberStack(){if(mini!=null&&mini.Stack!=null)Profile.StackBest=Math.Max(Profile.StackBest,mini.Stack.Score);}
  void RefreshStackHud(){var board=mini.Stack;miniHeading.Text=T("Score ","النتيجة ")+board.Score;miniLives.Text="";miniCounter.Text=T("High score ","أعلى نتيجة ")+Profile.StackBest;if(miniStackPace!=null)miniStackPace.Text=board.Score>=35?T("Intense","مكثف"):board.Score>=20?T("Rapid","خاطف"):board.Score>=10?T("Fast","سريع"):board.Score>=5?T("Quicker","أسرع"):T("Steady","هادئ");
   miniStatus.Text=!board.Started?T("Click or press E / Space to start your tower.","انقر أو اضغط E / مسافة لبدء برجك."):board.Phase=="miss"||board.Over?T("That piece missed. One more tower?","فاتت القطعة مكانها. برج آخر؟"):board.Grew&&board.LandingAge<1?T("Perfect streak! A little width restored.","سلسلة مثالية! استعدت قليلاً من العرض."):board.Perfect&&board.LandingAge<1?T("Perfect!", "مثالي!")+(board.Streak>1?" ×"+board.Streak:""):board.Score>miniStackStartBest?T("New high score! Keep your tower growing.","أعلى نتيجة جديدة! واصل بناء برجك."):T("Line up the front edges. Overhangs fall away.","حاذِ الحواف الأمامية. تسقط الأجزاء الزائدة.");
  }
  void ConfirmRestartStack(){Modal(T("Start a fresh tower?","بدء برج جديد؟"),c=>{Label(c,T("Every run starts from the base. Your high score is kept.","تبدأ كل محاولة من القاعدة. تُحفظ أعلى نتيجة لك."),97,112,21,595);Btn(c,T("Keep playing","متابعة اللعب"),103,270,CloseModal,266,49);Btn(c,T("New run","محاولة جديدة"),399,270,()=>StartMiniGame("stack"),293,49,true);},381);}
  void StackFailure(){Modal(T("A little higher next time?","أعلى قليلاً في المرة القادمة؟"),c=>{Label(c,T("Your tower: ","ارتفاع برجك: ")+mini.Stack.Score+"\n"+T("High score: ","أعلى نتيجة: ")+Profile.StackBest,99,110,25,594);Btn(c,T("Back to minigames","العودة للألعاب المصغّرة"),101,272,BackToMiniGames,281,50);Btn(c,T("Play again","العب مجدداً"),402,272,()=>StartMiniGame("stack"),287,50,true);},385);}
  void ResetStackBest(){Modal(T("Reset Paper Stack high score?","مسح أعلى نتيجة لبرج الورق؟"),c=>{Label(c,T("Clear only this game's high score. Your other games and earned stars are kept.","امسح أعلى نتيجة لهذه اللعبة فقط. تبقى الألعاب الأخرى والنجوم المكتسبة."),98,110,20,595);Btn(c,T("Cancel","إلغاء"),103,277,CloseModal,266,49);Put(c,DangerButton(T("Reset high score","مسح أعلى نتيجة"),()=>{Profile.StackBest=0;Save();page="minigames";paused=false;Build();},293,49),399,277);},388);}
 }
}
