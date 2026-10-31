using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

public partial class Companion {
 Grid helpLayer;Border helpSurface;TextBlock helpHeading,helpStep;StackPanel helpBody;ScrollViewer helpScroll;
 Button helpBack,helpNext,helpSkip;bool helpOpen,helpTutorial;int helpIndex,helpGeneration;
 const int TutorialSteps=7;
 void HelpSettings(StackPanel body){body.Children.Add(Text("Помощь",17));var links=new WrapPanel();links.Children.Add(Btn("О программе",OpenAbout));links.Children.Add(Btn("О создателе",OpenCreator));links.Children.Add(Btn("Пройти обучение",OpenTutorial));body.Children.Add(links);}
 void BuildHelp(){
  helpLayer=new Grid{Visibility=Visibility.Collapsed,Background=Brushes.Transparent};Panel.SetZIndex(helpLayer,90);root.Children.Add(helpLayer);
  var shade=new Border{Background=Brush("#C0000000")};CompanionWindowDrag.SetReserved(shade,true);shade.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;CloseHelp();};helpLayer.Children.Add(shade);
  helpSurface=new Border{CornerRadius=new CornerRadius(18),Padding=new Thickness(20),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};Resource(helpSurface,Border.BackgroundProperty,"DockBg");Resource(helpSurface,Border.BorderBrushProperty,"Line");helpLayer.Children.Add(helpSurface);KeyboardNavigation.SetTabNavigation(helpSurface,KeyboardNavigationMode.Cycle);
  var layout=new Grid();layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition());layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});helpSurface.Child=layout;
  helpStep=Text("",11,"Accent");layout.Children.Add(helpStep);helpHeading=Text("",24);Grid.SetRow(helpHeading,1);layout.Children.Add(helpHeading);
  helpBody=Stack();helpScroll=new ScrollViewer{Content=helpBody,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,4,8,8)};Grid.SetRow(helpScroll,2);layout.Children.Add(helpScroll);
  var actions=new WrapPanel{Margin=new Thickness(0,14,0,0)};helpBack=Btn("Назад",()=>ChangeHelpStep(-1));helpNext=Btn("Далее →",()=>{if(helpIndex==TutorialSteps-1)CloseHelp();else ChangeHelpStep(1);},true);helpSkip=Btn("Пропустить",()=>CloseHelp());actions.Children.Add(helpBack);actions.Children.Add(helpNext);actions.Children.Add(helpSkip);Grid.SetRow(actions,3);layout.Children.Add(actions);
  root.SizeChanged+=(s,e)=>SizeHelp();helpLayer.PreviewMouseWheel+=ScrollMainWheel;
  PreviewKeyDown+=(s,e)=>{if(helpOpen&&e.Key==Key.Escape){e.Handled=true;CloseHelp();}};
  Loaded+=(s,e)=>Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,(Action)(()=>{if(!data.TutorialSeen&&IsVisible&&!startupSplashPending)OpenTutorial();}));
 }
 void SizeHelp(){if(helpSurface==null)return;helpSurface.Width=Math.Max(1,Math.Min(740,(root.ActualWidth>0?root.ActualWidth:Width)-24));helpSurface.Height=Math.Max(1,Math.Min(helpTutorial?760:780,(root.ActualHeight>0?root.ActualHeight:Height)-24));}
 void ShowHelp(){CloseCardGames();
  if(compact)SetCompact(false);CloseSectionDialog(false,true);CloseArcade();helpOpen=true;helpGeneration++;SizeHelp();helpLayer.Visibility=Visibility.Visible;helpLayer.IsHitTestVisible=true;helpLayer.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));var shift=new TranslateTransform(0,8);helpSurface.RenderTransform=shift;shift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(0,TimeSpan.FromMilliseconds(180)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});helpNext.Focus();
 }
 void OpenTutorial(){helpTutorial=true;helpIndex=0;RenderHelpStep();ShowHelp();}
 void ChangeHelpStep(int direction){helpIndex=Math.Max(0,Math.Min(TutorialSteps-1,helpIndex+direction));RenderHelpStep();}
 void TutorialPicture(string name){
  using(var stream=GetType().Assembly.GetManifestResourceStream("Tutorial."+name+".png")){
   if(stream==null){helpBody.Children.Add(Text("Скриншот недоступен. Ориентируйся на названия кнопок ниже.",12,"Muted"));return;}
   var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.StreamSource=stream;bitmap.EndInit();bitmap.Freeze();if(name=="browser"){helpBody.Children.Add(CompanionBrowserGuide.Create(Resources,bitmap));return;}if(name=="header"){helpBody.Children.Add(HeaderTutorialPicture(bitmap));return;}var picture=new Image{Source=bitmap,Stretch=Stretch.Uniform,MaxWidth=680,Margin=new Thickness(0,6,0,12),HorizontalAlignment=HorizontalAlignment.Left};System.Windows.Automation.AutomationProperties.SetName(picture,"Скриншот: "+name+". Нужные кнопки отмечены цифрами.");helpBody.Children.Add(picture);
  }
 }
 void TutorialPoint(int number,string message){
  var row=new Grid{Margin=new Thickness(0,0,0,9)};row.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(30)});row.ColumnDefinitions.Add(new ColumnDefinition());var label=new TextBlock{Text=number.ToString(),Foreground=Brush("#101317"),FontSize=12,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};row.Children.Add(new Border{Width=22,Height=22,CornerRadius=new CornerRadius(11),Background=Brush("#70EABC"),Child=label,VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Left});var text=Text(message,14);text.Margin=new Thickness(0);Grid.SetColumn(text,1);row.Children.Add(text);helpBody.Children.Add(row);
 }
 void RenderHelpStep(){
  string[] headings={"Кнопки главного окна","Твой любимый цвет","Картинка и прозрачность","Все кнопки браузера","Твои горячие клавиши","Мини-режим и виджеты","Мышь - игре, помощник - рядом"};helpHeading.Text=headings[helpIndex];helpStep.Text="УДОБНЫЕ МЕЛОЧИ · "+(helpIndex+1)+" / "+TutorialSteps;helpBody.Children.Clear();
  switch(helpIndex){
   case 0:
    helpBody.Children.Add(Text("Companion подходит для разных игр, работы и повседневных дел. Разделы открываются в меню слева. В «Обзоре» остаются плитки, а мини-игры вынесены отдельным пунктом. Главные кнопки - в верхней панели.",15));TutorialPicture("header");helpBody.Children.Add(Card(Text("Чтобы помощник отображался поверх игры, включи в ней «Оконный» режим или «Окно без рамки».",13)));TutorialPoint(1,"Шестерёнка открывает настройки: цвет, фон и горячие клавиши.");TutorialPoint(2,"Дверца полностью закрывает программу. Уведомления после выхода не работают.");TutorialPoint(3,"«Мини» сворачивает главное окно до одной записи.");TutorialPoint(4,"«−» прячет программу в трей. Таймеры продолжают работать.");helpBody.Children.Add(Text("На скриншотах - базовый дизайн и стандартные клавиши. Твои настройки могут отличаться.",12,"Muted"));break;
   case 1:
    helpBody.Children.Add(Text("Шестерёнка → «Своя палитра».",15));TutorialPicture("palette");TutorialPoint(1,"Нажми на готовый цвет - он применится сразу.");TutorialPoint(2,"Или вставь свой HEX-код и нажми «Применить цвет».");break;
   case 2:
    helpBody.Children.Add(Text("В настройках прокрути до «Своя картинка на фоне».",15));TutorialPicture("wallpaper");TutorialPoint(1,"«Выбрать фон» - поставить свою картинку.");TutorialPoint(2,"Ползунок под кнопкой меняет яркость картинки.");helpBody.Children.Add(Text("Хочешь видеть игру сквозь окно? Выше есть прозрачность:",14));TutorialPicture("opacity");TutorialPoint(1,"Влево - прозрачнее. Вправо до конца - полностью непрозрачно.");break;
   case 3:
    helpBody.Children.Add(Text("В окне браузера звук можно настроить отдельно от игры.",15));TutorialPicture("browser");TutorialPoint(1,"Передвинь ползунок, чтобы изменить громкость. Кнопка с динамиком выключает и возвращает звук.");helpBody.Children.Add(Text("Выбранная громкость сохраняется.",12,"Muted"));TutorialPoint(2,"Булавка закрепляет окно поверх остальных окон.");TutorialPoint(3,"Значок окна убирает панели. Наведи на край - кнопки появятся. F11 возвращает панели.");TutorialPoint(4,"«В игру» пропускает мышь сквозь окно. Вернуть управление: "+InputBinding+".");TutorialPoint(5,"Звёздочка сохраняет страницу в закладки. Повторное нажатие удаляет её.");TutorialPoint(6,"Меню с тремя точками открывает историю и закладки. Там можно искать страницы, переименовывать закладки и отключить запись истории.");helpBody.Children.Add(Text("«+» открывает новую вкладку. Ctrl+H - история, Ctrl+D - сохранить страницу.",12,"Muted"));break;
   case 4:
    helpBody.Children.Add(Text("Шестерёнка → «Горячие клавиши». Выбери сочетания, удобные тебе.",15));TutorialPicture("hotkeys");TutorialPoint(1,"Нажми поле и зажми своё сочетание: Ctrl или Alt + буква, цифра или F-клавиша.");TutorialPoint(2,"Нажми «Сохранить клавиши».");helpBody.Children.Add(Text("У быстрой заметки - своё сочетание, в блоке выше:",14));TutorialPicture("quicknote");TutorialPoint(1,"Клавиши быстрой заметки сейчас: "+QuickBinding+". Здесь их тоже можно заменить.");TutorialPoint(2,"Для этого поля нажми «Сохранить сочетание».");break;
   case 5:
    helpBody.Children.Add(Text("У таймеров, дел и заметок есть две разные кнопки:",15));TutorialPicture("widgets");TutorialPoint(1,"«В мини» - свернуть главное окно до одной записи.");TutorialPoint(2,"«Открыть виджет» - показать отдельное окно. Таких окон может быть несколько.");helpBody.Children.Add(Text("Тяни фон для перемещения, край - для размера. Кнопки виджета появляются при наведении.",12,"Muted"));break;
   case 6:
    helpBody.Children.Add(Text("Нажми "+InputBinding+", чтобы мышь проходила сквозь помощник в игру. Нажми ещё раз - вернётся управление окнами.",15));helpBody.Children.Add(Card(Text(ShowBinding+" - показать или скрыть главное окно. Эти клавиши можно поменять в настройках.",13)));helpBody.Children.Add(Text("Если окно не реагирует на клики, верни управление тем же сочетанием. Программа должна оставаться запущенной.",14));break;
  }
  helpBack.Visibility=helpIndex==0?Visibility.Collapsed:Visibility.Visible;helpNext.Content=helpIndex==TutorialSteps-1?"Готово, к игре":"Далее →";helpNext.Visibility=helpSkip.Visibility=Visibility.Visible;helpSkip.Content="Пропустить";helpScroll.ScrollToTop();
 }
 string ReadProgramReadme(){
  try{var file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Прочитай меня.txt");if(File.Exists(file)&&new FileInfo(file).Length<=1000000)return File.ReadAllText(file,Encoding.UTF8);}catch(IOException){}catch(UnauthorizedAccessException){}
  using(var stream=GetType().Assembly.GetManifestResourceStream("CompanionReadme")){if(stream!=null)using(var reader=new StreamReader(stream,Encoding.UTF8))return reader.ReadToEnd();}
  return "Companion - таймеры, дела, заметки, финансы, калькулятор и браузер. Инструкция находится в файле «Прочитай меня.txt» рядом с программой.";
 }
 void OpenAbout(){
  helpTutorial=false;helpHeading.Text="О программе";helpStep.Text="COMPANION · ВЕРСИЯ "+GetType().Assembly.GetName().Version.ToString(3);helpBody.Children.Clear();helpBody.Children.Add(Text("Помощник для игр, работы и повседневных дел",15,"Accent"));helpBody.Children.Add(Text("Companion можно использовать с Majestic RP, другими играми и без запущенной игры.",14));helpBody.Children.Add(Card(Text("Для отображения поверх игры выбери в настройках самой игры «Оконный» режим или «Окно без рамки».",14)));
  helpBody.Children.Add(new WrapPanel{Children={Btn("Пройти обучение",OpenTutorial,true),Btn("О создателе",OpenCreator)}});helpBody.Children.Add(Text("Инструкция из файла «Прочитай меня.txt»",12,"Muted"));var instruction=Text(ReadProgramReadme(),13);instruction.FontWeight=FontWeights.Normal;helpBody.Children.Add(instruction);
  helpBack.Visibility=helpNext.Visibility=Visibility.Collapsed;helpSkip.Content="← Назад";helpScroll.ScrollToTop();ShowHelp();helpSkip.Focus();
 }
 void CloseHelp(){
  if(helpLayer==null||!helpOpen)return;if(helpTutorial){data.TutorialSeen=true;Save();}helpOpen=false;helpLayer.IsHitTestVisible=false;int generation=++helpGeneration;var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(140));fade.Completed+=(s,e)=>{if(generation==helpGeneration&&!helpOpen)helpLayer.Visibility=Visibility.Collapsed;};helpLayer.BeginAnimation(OpacityProperty,fade);
 }
}
