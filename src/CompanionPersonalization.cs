using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

public partial class Companion {
 ScrollViewer mainScroll;
 TextBox accentHex;
 TextBlock accentValue;
 Border accentPreview;
 Slider accentHue;
 bool syncingPalette;
 public static bool TryAccent(string text,out Color color){
  color=Colors.Transparent;if(text==null)return false;string value=text.Trim();if(value.StartsWith("#"))value=value.Substring(1);
  if(value.Length==3)value=String.Concat(value.Select(c=>new string(c,2)));
  uint number;if(value.Length!=6||!UInt32.TryParse(value,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out number))return false;
  color=Color.FromRgb((byte)(number>>16),(byte)(number>>8),(byte)number);return true;
 }
 static string Hex(Color color){return "#"+color.R.ToString("X2")+color.G.ToString("X2")+color.B.ToString("X2");}
 static Color Mix(Color a,Color b,double amount){return Color.FromRgb((byte)Math.Round(a.R+(b.R-a.R)*amount),(byte)Math.Round(a.G+(b.G-a.G)*amount),(byte)Math.Round(a.B+(b.B-a.B)*amount));}
 static double Luminance(Color c){Func<byte,double> linear=v=>{double n=v/255.0;return n<=.04045?n/12.92:Math.Pow((n+.055)/1.055,2.4);};return .2126*linear(c.R)+.7152*linear(c.G)+.0722*linear(c.B);}
 static double Contrast(Color a,Color b){double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
 public static Color ReadableAccent(Color chosen,Color background){for(int i=0;i<=20;i++){Color candidate=Mix(chosen,Colors.White,i/20.0);if(Contrast(candidate,background)>=4.5)return candidate;}return Colors.White;}
 static SolidColorBrush WithAlpha(Color color,byte alpha){color.A=alpha;return new SolidColorBrush(color);}
 void ApplyCustomPalette(){
  Color seed;if(!TryAccent(data.AccentColor,out seed))return;
  bool cute=data.Theme=="cute";Color bg=Mix(seed,cute?Color.FromRgb(22,18,24):Color.FromRgb(16,19,24),.91);
  Color panel=Mix(bg,seed,.13),field=Mix(bg,Colors.White,.065),accent=ReadableAccent(seed,bg);
  byte alpha=(byte)Math.Round(data.Density*255);Resources["Bg"]=WithAlpha(bg,alpha);Resources["Panel"]=WithAlpha(panel,(byte)Math.Min(255,alpha+20));Resources["Field"]=WithAlpha(field,alpha);
  Resources["DockBg"]=WithAlpha(panel,data.Density>=1?(byte)255:(byte)242);Resources["Line"]=WithAlpha(Mix(accent,Colors.White,.12),90);
  Resources["Muted"]=new SolidColorBrush(Mix(bg,Colors.White,.65));Resources["Accent"]=new SolidColorBrush(accent);Resources["AccentLine"]=WithAlpha(accent,204);
  Resources["AccentText"]=new SolidColorBrush(Contrast(seed,Colors.Black)>=Contrast(seed,Colors.White)?Colors.Black:Colors.White);
  Resources["Selection"]=WithAlpha(seed,150);Resources["ScrollThumb"]=WithAlpha(accent,180);Resources["ScrollTrack"]=WithAlpha(accent,55);Resources["RingTrack"]=WithAlpha(accent,65);
  Resources["NavBg"]=WithAlpha(panel,180);Resources["SelectedTab"]=WithAlpha(Mix(panel,seed,.25),180);
  Resources["AccentFill"]=new LinearGradientBrush(Mix(seed,Colors.White,.2),seed,new Point(0,0),new Point(1,1));
 }
 void SetAccent(string value){Color color;if(!TryAccent(value,out color))throw new ArgumentException("Введите HEX-код: #RRGGBB, например #7C8CFF.");data.AccentColor=Hex(color);Theme();Save();SyncPaletteEditor();}
 void SyncPaletteEditor(){
  if(accentHex==null)return;Color chosen;if(!TryAccent(data.AccentColor,out chosen))TryAccent(data.Theme=="cute"?"#F99CDE":"#C7CDD4",out chosen);
  syncingPalette=true;accentHex.Text=Hex(chosen);accentValue.Text=Hex(chosen);accentPreview.Background=new SolidColorBrush(chosen);accentHue.Value=Hue(chosen);syncingPalette=false;
 }
 static double Hue(Color color){double r=color.R/255.0,g=color.G/255.0,b=color.B/255.0,max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),delta=max-min;if(delta==0)return 0;double h=max==r?(g-b)/delta+(g<b?6:0):max==g?(b-r)/delta+2:(r-g)/delta+4;return h*60;}
 static Color HueColor(double hue){double h=(hue%360)/60,x=1-Math.Abs(h%2-1);double r=0,g=0,b=0;if(h<1){r=1;g=x;}else if(h<2){r=x;g=1;}else if(h<3){g=1;b=x;}else if(h<4){g=x;b=1;}else if(h<5){r=x;b=1;}else{r=1;b=x;}return Color.FromRgb((byte)(55+200*r),(byte)(55+200*g),(byte)(55+200*b));}
 void PaletteSettings(StackPanel body){
  var panel=Stack();panel.Children.Add(Text("Своя палитра",17));panel.Children.Add(Text("Выбери цвет или вставь HEX-код. Он изменит акценты, фон, карточки и прокрутку.",11,"Muted"));
  var presets=new WrapPanel();foreach(string preset in new[]{"#C7CDD4","#9EC7FF","#F99CDE","#9D8BFF","#58D6AF","#FFC85C","#FF8B5E","#EE657A","#AAB4C3"}){string selected=preset;Color color;TryAccent(preset,out color);var swatch=Btn("",()=>SetAccent(selected));swatch.Width=32;swatch.Height=32;swatch.MinHeight=32;swatch.Padding=new Thickness(0);swatch.Background=new SolidColorBrush(color);swatch.ToolTip=preset;System.Windows.Automation.AutomationProperties.SetName(swatch,"Цвет "+preset);presets.Children.Add(swatch);}panel.Children.Add(presets);
  accentPreview=new Border{Width=30,Height=30,CornerRadius=new CornerRadius(9),Margin=new Thickness(0,0,10,0)};accentValue=Text("",14);accentValue.VerticalAlignment=VerticalAlignment.Center;accentValue.Margin=new Thickness(0);panel.Children.Add(Row(accentPreview,accentValue));
  panel.Children.Add(Text("Оттенок",11,"Muted"));accentHue=new Slider{Minimum=0,Maximum=360,Margin=new Thickness(0,8,0,12)};var rainbow=new LinearGradientBrush();string[] stops={"#FF3737","#FFFF37","#37FF37","#37FFFF","#3737FF","#FF37FF","#FF3737"};for(int i=0;i<stops.Length;i++)rainbow.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(stops[i]),i/6.0));panel.Children.Add(new Border{Height=5,CornerRadius=new CornerRadius(3),Background=rainbow});panel.Children.Add(accentHue);
  accentHue.ValueChanged+=(s,e)=>{if(syncingPalette)return;Color color=HueColor(e.NewValue);data.AccentColor=Hex(color);Theme();accentHex.Text=data.AccentColor;accentValue.Text=data.AccentColor;accentPreview.Background=new SolidColorBrush(color);};accentHue.LostMouseCapture+=(s,e)=>{if(ready)Save();};accentHue.KeyUp+=(s,e)=>{if(ready)Save();};
  panel.Children.Add(Text("HEX-код",11,"Muted"));accentHex=Input("HEX-код цвета");accentHex.MaxLength=7;accentHex.KeyDown+=(s,e)=>{if(e.Key==Key.Enter){Safe(()=>SetAccent(accentHex.Text));e.Handled=true;}};panel.Children.Add(accentHex);
  var buttons=new WrapPanel();buttons.Children.Add(Btn("Применить цвет",()=>Safe(()=>SetAccent(accentHex.Text)),true));buttons.Children.Add(Btn("Базовая палитра",()=>{data.Theme="strict";data.AccentColor=null;Theme();Save();SyncPaletteEditor();}));panel.Children.Add(buttons);panel.Children.Add(Text("Поддерживаются #RRGGBB и короткий #RGB.",10,"Muted"));body.Children.Add(Card(panel));SyncPaletteEditor();
 }
 void InstallMainScrolling(){
  normalLayout.Children.Remove(hero);normalLayout.Children.Remove(tabs);var body=Stack();body.Children.Add(hero);
  foreach(TabItem item in tabs.Items){var scroll=item.Content as ScrollViewer;if(scroll!=null)scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;}
  normalLayout.RowDefinitions[1].Height=new GridLength(1,GridUnitType.Star);normalLayout.RowDefinitions[2].Height=new GridLength(0);
  mainScroll=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(0,0,5,0)};Grid.SetRow(mainScroll,1);Grid.SetRowSpan(mainScroll,2);normalLayout.Children.Add(mainScroll);
  Grid.SetRow(settingsPage,1);Grid.SetRowSpan(settingsPage,2);feedback.MaxHeight=42;
  normalLayout.PreviewMouseWheel+=ScrollMainWheel;tabs.SelectionChanged+=(s,e)=>{if(e.Source==tabs&&mainScroll!=null)mainScroll.ScrollToTop();};SizeChanged+=(s,e)=>UpdateMainLayout();UpdateMainLayout();
 }
 void UpdateMainLayout(){if(mainScroll!=null)mainScroll.Visibility=designing?Visibility.Collapsed:Visibility.Visible;if(brand!=null)brand.Visibility=ActualWidth>0&&ActualWidth<540?Visibility.Collapsed:Visibility.Visible;UpdateDashboardLayout();}
 void ScrollMainWheel(object sender,MouseWheelEventArgs e){
  DependencyObject node=e.OriginalSource as DependencyObject;
  while(node!=null){var scroll=node as ScrollViewer;if(scroll!=null&&scroll.ScrollableHeight>0&&((e.Delta<0&&scroll.VerticalOffset<scroll.ScrollableHeight)||(e.Delta>0&&scroll.VerticalOffset>0))){scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta/120.0*Math.Max(1,SystemParameters.WheelScrollLines)*20);e.Handled=true;return;}
   node=node is Visual||node is System.Windows.Media.Media3D.Visual3D?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node);
  }
 }
 void RequestExit(){SetGameInput(false);Close();}
 void InstallSettingsPage(ScrollViewer scroll){
  var page=new Grid{Visibility=Visibility.Collapsed};page.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});page.RowDefinitions.Add(new RowDefinition());
  var back=Btn("← Назад в меню",()=>{if(designing)ShowDesign();},true);back.HorizontalAlignment=HorizontalAlignment.Left;back.MinHeight=42;back.FontSize=14;back.Padding=new Thickness(16,9,16,9);back.Margin=new Thickness(0,0,0,14);back.ToolTip="Вернуться в предыдущий раздел";System.Windows.Automation.AutomationProperties.SetName(back,"Назад в меню");
  page.Children.Add(back);Grid.SetRow(scroll,1);page.Children.Add(scroll);settingsPage=page;Grid.SetRow(page,2);normalLayout.Children.Add(page);
 }
 void ClosingApplication(object sender,CancelEventArgs e){
  if(!exiting){SetGameInput(false);if(data.Timers.Any(t=>t.End>0&&!t.Alerted)&&MessageBox.Show(this,"После полного выхода таймеры не смогут показывать уведомления. Закрыть помощник?","Активные таймеры",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes){e.Cancel=true;return;}exiting=true;}
  CommitArcadeScore();Save();
 }
}
