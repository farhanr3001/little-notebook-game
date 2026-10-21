using System.Windows.Media;
namespace LittleNotebook {public partial class MainWindow {
 void AddReviewMistakes(double x,double y,double width,double height){var button=Button(T("Review mistakes","مراجعة الأخطاء")+" ("+UnresolvedMistakes().Count+")",()=>{game=null;page="study";studyTopic="mistakes";studyIndex=0;Build();},width,height);button.Background=B(Color.FromRgb(231,181,115));Put(body,button,x,y);}
}}
