using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

public partial class Companion {
 CompanionBrowserWindow browserWindow;
 bool CompactHeroTab { get { return false; } }
 void BrowserTab(){
  var body=Stack();body.Children.Add(Text("Браузер поверх игры",23));
  body.Children.Add(Text("Видео, музыка и поиск - в отдельном окне, размер которого выбираешь ты.",13,"Muted"));
  var card=Stack();card.Children.Add(Text("Стартовая страница",15));
  var google=Btn("Google",()=>{});var yandex=Btn("Яндекс",()=>{});
  Action refresh=()=>{bool useYandex=data.BrowserHome=="yandex";Resource(google,Control.BackgroundProperty,useYandex?"Field":"AccentFill");Resource(google,Control.ForegroundProperty,useYandex?"Text":"AccentText");Resource(yandex,Control.BackgroundProperty,useYandex?"AccentFill":"Field");Resource(yandex,Control.ForegroundProperty,useYandex?"AccentText":"Text");System.Windows.Automation.AutomationProperties.SetName(google,useYandex?"Google":"Google - выбрано");System.Windows.Automation.AutomationProperties.SetName(yandex,useYandex?"Яндекс - выбрано":"Яндекс");};
  Action<string> choose=provider=>{data.BrowserHome=provider;Save();if(browserWindow!=null)browserWindow.HomeProvider=provider;refresh();};
  google.Click+=(s,e)=>choose("google");yandex.Click+=(s,e)=>choose("yandex");refresh();
  card.Children.Add(Row(google,yandex));card.Children.Add(Btn("Открыть браузер",()=>OpenBrowser(null),true));
  card.Children.Add(Btn("Открыть YouTube",()=>OpenBrowser("https://www.youtube.com/")));
  card.Children.Add(Btn("Fletcher Wiki",()=>OpenBrowser("https://fletcher-wiki.com/")));
  body.Children.Add(Card(card));body.Children.Add(Text("Вкладки, история и закладки доступны в меню с тремя точками в окне браузера.",12,"Muted"));body.Children.Add(Text("Закрепи окно поверх игры. Режим просмотра убирает адресную строку; Esc возвращает панели. Для игры используй режим «без рамки».",12,"Muted"));
  var backgroundVideo=new CheckBox{Content="Продолжать видео, когда активна игра",IsChecked=data.BackgroundVideo??true,Margin=new Thickness(0,0,0,12)};Resource(backgroundVideo,Control.ForegroundProperty,"Text");backgroundVideo.Click+=(s,e)=>{data.BackgroundVideo=backgroundVideo.IsChecked==true;Save();if(browserWindow!=null)browserWindow.BackgroundPlayback=data.BackgroundVideo.Value;};body.Children.Add(backgroundVideo);
  body.Children.Add(Text("При потере фокуса страница остаётся активной для воспроизведения. Фокус клавиатуры остаётся игре. Режим не отменяет ограничения сайта или видеодрайвера.",11,"Muted"));
  body.Children.Add(Text("Сайты используют интернет. Их cookies и настройки хранятся отдельно от профиля помощника. Некоторые видеосервисы могут ограничивать просмотр во встроенном браузере.",11,"Muted"));
  Tab("Браузер",body);
 }
 void OpenBrowser(string destination){
  if(browserWindow==null){browserWindow=new CompanionBrowserWindow(Resources,Path.Combine(Folder,"browser"),data.BrowserHome,data.BrowserVolume??100);browserWindow.BackgroundPlayback=data.BackgroundVideo??true;browserWindow.VolumeChanged+=value=>{data.BrowserVolume=value;Save();};browserWindow.RequestGameMode+=()=>Safe(()=>SetGameInput(true));browserWindow.Closed+=(s,e)=>browserWindow=null;}
  browserWindow.Show();browserWindow.WindowState=WindowState.Normal;browserWindow.Activate();if(destination!=null)browserWindow.Go(destination);
 }
}

