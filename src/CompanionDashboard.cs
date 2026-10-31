using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

public partial class Companion {
 UniformGrid sectionTiles;
 readonly List<Button> sectionButtons=new List<Button>();
 readonly List<Ellipse> sectionIndicators=new List<Ellipse>();
 Path DashboardIcon(string name,double size=26){
  string drawing;
  switch(name){
   case "clock":drawing="M 12,2 A 10,10 0 1 1 11.99,2 M 12,6 L 12,12 L 17,14";break;
   case "check":drawing="M 5,3 L 19,3 Q 21,3 21,5 L 21,19 Q 21,21 19,21 L 5,21 Q 3,21 3,19 L 3,5 Q 3,3 5,3 M 7,12 L 10,15 L 17,8";break;
   case "notes":drawing="M 6,2 L 18,2 Q 21,2 21,5 L 21,19 Q 21,22 18,22 L 6,22 Q 3,22 3,19 L 3,5 Q 3,2 6,2 M 7,8 L 17,8 M 7,12 L 17,12 M 7,16 L 13,16";break;
   case "finance":drawing="M 4,21 L 4,14 M 12,21 L 12,4 M 20,21 L 20,9";break;
   case "calculator":drawing="M 7,2 L 17,2 Q 20,2 20,5 L 20,19 Q 20,22 17,22 L 7,22 Q 4,22 4,19 L 4,5 Q 4,2 7,2 M 4,9 L 20,9 M 8,13 L 8.1,13 M 15,13 L 15.1,13 M 8,18 L 8.1,18 M 15,18 L 15.1,18";break;
   case "globe":drawing="M 12,2 A 10,10 0 1 1 11.99,2 M 12,2 C 5,7 5,17 12,22 M 12,2 C 19,7 19,17 12,22 M 3,8 L 21,8 M 3,16 L 21,16";break;
   case "coins":drawing="M 3,5 C 3,1 21,1 21,5 C 21,9 3,9 3,5 M 3,10 C 3,14 21,14 21,10 M 3,15 C 3,19 21,19 21,15 M 3,20 C 3,24 21,24 21,20";break;
   case "bell":drawing="M 5,16 L 6,14 L 6,9 C 6,2 18,2 18,9 L 18,14 L 20,17 L 4,17 M 9,21 Q 12,24 15,21 M 12,2 L 12,1";break;
   case "exit":drawing="M 13,4 L 4,4 L 4,20 L 13,20 M 10,12 L 22,12 M 17,7 L 22,12 L 17,17";break;
   default:drawing="M 3,12 L 21,12";break;
  }
  var icon=new Path{Data=Geometry.Parse(drawing),Width=size,Height=size,Stretch=Stretch.Uniform,StrokeThickness=1.9,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,VerticalAlignment=VerticalAlignment.Center};Resource(icon,Shape.StrokeProperty,"Text");return icon;
 }
 void BuildDashboard(){
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CompanionTheme"))Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(stream));
  Width=Math.Min(800,SystemParameters.WorkArea.Width-40);Height=Math.Min(1030,SystemParameters.WorkArea.Height-40);normalWidth=Width;normalHeight=Height;
  frame=new Border{CornerRadius=new CornerRadius(18),BorderThickness=new Thickness(1.5),ClipToBounds=true};Resource(frame,Border.BackgroundProperty,"Bg");Resource(frame,Border.BorderBrushProperty,"Line");Content=frame;frame.Child=root;root.Children.Add(wallpaper);
  normalLayout=new Grid{Margin=new Thickness(26,16,26,14)};root.Children.Add(normalLayout);for(int i=0;i<4;i++)normalLayout.RowDefinitions.Add(new RowDefinition{Height=i==2?new GridLength(1,GridUnitType.Star):GridLength.Auto});
  var header=new Grid{MinHeight=48};header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
  var design=Btn("",ShowDesign);design.ToolTip="Дизайн";System.Windows.Automation.AutomationProperties.SetName(design,"Дизайн");
  var cog=new StreamGeometry();using(var outline=cog.Open()){for(int i=0;i<32;i++){double angle=-Math.PI/2+i*Math.PI/16,r=(i%4==1||i%4==2)?10:8;var point=new Point(12+r*Math.Cos(angle),12+r*Math.Sin(angle));if(i==0)outline.BeginFigure(point,false,true);else outline.LineTo(point,true,false);}}
  var gear=new GeometryGroup();gear.Children.Add(cog);gear.Children.Add(new EllipseGeometry(new Point(12,12),3.2,3.2));var gearIcon=new Path{Data=gear,Width=24,Height=24,Stretch=Stretch.None,StrokeThickness=1.9,StrokeLineJoin=PenLineJoin.Round};Resource(gearIcon,Shape.StrokeProperty,"Text");design.Content=gearIcon;
  var exit=Btn("",RequestExit);exit.Content=DashboardIcon("exit",24);exit.ToolTip="Полностью выйти из приложения";System.Windows.Automation.AutomationProperties.SetName(exit,"Полностью выйти из приложения");
  var mini=Btn("Мини",()=>SetCompact(true));var minimize=Btn("−",()=>Hide());minimize.ToolTip="Свернуть в трей — таймеры продолжаются";
  var controls=Row(design,exit,mini,minimize);foreach(Button button in controls.Children){button.Width=46;button.Height=48;button.MinHeight=48;button.Margin=new Thickness(8,0,0,0);button.Padding=new Thickness(0);button.FontSize=16;}mini.Width=72;Grid.SetColumn(controls,1);header.Children.Add(controls);
  var logo=Row();var orbit=new Path{Data=Geometry.Parse("M 4,24 L 27,1 M 24,5 A 12,12 0 1 1 5,24 A 12,12 0 0 1 24,5"),Width=33,Height=33,Stretch=Stretch.Uniform,StrokeThickness=2.3,Margin=new Thickness(0,0,12,0),VerticalAlignment=VerticalAlignment.Center};Resource(orbit,Shape.StrokeProperty,"Accent");logo.Children.Add(orbit);brand=Text("COMPANION",25);brand.Margin=new Thickness(0);brand.VerticalAlignment=VerticalAlignment.Center;logo.Children.Add(brand);header.Children.Add(new Viewbox{Child=logo,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,MaxHeight=46,Margin=new Thickness(0,0,12,0)});
  var headerLine=new Border{Child=header,BorderThickness=new Thickness(0,0,0,1),Padding=new Thickness(0,0,0,13),Margin=new Thickness(0,0,0,15)};Resource(headerLine,Border.BorderBrushProperty,"Line");normalLayout.Children.Add(headerLine);
  hero=Stack();hero.Children.Add(Text("ПАНЕЛЬ ИГРОКА",12,"Muted"));
  var badges=new WrapPanel{Margin=new Thickness(0,0,0,3)};dashboardTasks=Text("",14);dashboardIncome=Text("",14);int index=0;foreach(var text in new[]{dashboardTasks,dashboardIncome}){text.Margin=new Thickness(9,0,0,0);text.VerticalAlignment=VerticalAlignment.Center;var pill=new Border{CornerRadius=new CornerRadius(12),Padding=new Thickness(14,12,14,12),Margin=new Thickness(0,0,12,14),BorderThickness=new Thickness(1),Child=Row(DashboardIcon(index++==0?"check":"coins",18),text)};Resource(pill,Border.BackgroundProperty,"Field");Resource(pill,Border.BorderBrushProperty,"Line");badges.Children.Add(pill);}hero.Children.Add(badges);summary=Text("");Grid.SetRow(hero,1);normalLayout.Children.Add(hero);
  tabs=new TabControl{Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0)};Resource(tabs,Control.ForegroundProperty,"Text");tabs.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='TabControl'><ContentPresenter x:Name='PART_SelectedContentHost' Content='{TemplateBinding SelectedContent}' ContentTemplate='{TemplateBinding SelectedContentTemplate}' ContentTemplateSelector='{TemplateBinding SelectedContentTemplateSelector}'/></ControlTemplate>");Grid.SetRow(tabs,2);normalLayout.Children.Add(tabs);
  TimerTab();TasksTab();NotesTab();FinanceTab();CalculatorTab();BrowserTab();DesignTab();BuildSectionTiles();tabs.SelectedIndex=0;
  tabs.SelectionChanged+=(s,e)=>{if(e.Source!=tabs)return;UpdateSectionTiles();if(!designing)hero.Visibility=Visibility.Visible;var item=tabs.SelectedItem as TabItem;var body=item==null?null:item.Content as UIElement;if(body!=null){body.Opacity=.6;Animate(body,1,180);}};
  feedback=Text("Готов к работе",12,"Muted");feedback.Margin=new Thickness(9,0,0,0);feedback.VerticalAlignment=VerticalAlignment.Center;var ready=new Ellipse{Width=10,Height=10,Fill=Brush("#36D17A"),VerticalAlignment=VerticalAlignment.Center};var footer=new Border{Child=Row(ready,feedback),BorderThickness=new Thickness(0,1,0,0),Padding=new Thickness(0,12,0,0),Margin=new Thickness(0,12,0,0)};Resource(footer,Border.BorderBrushProperty,"Line");Grid.SetRow(footer,3);normalLayout.Children.Add(footer);
  BuildMini();InstallMainScrolling();BuildSectionDialog();BuildArcade();BuildHelp();UpdateSectionTiles();CompanionWindowDrag.Attach(this,()=>gameInputLocked);
 }
 void BuildSectionTiles(){
  BuildSectionOrderControls();sectionTiles=new UniformGrid{Columns=3,Margin=new Thickness(0,0,-12,12)};hero.Children.Add(sectionTiles);
  string[] titles={"Таймеры","Дела","Заметки","Финансы","Калькулятор","Браузер"},descriptions={"Всё вовремя","Планы на день","Важное под рукой","Доходы и расходы","Быстрый расчёт","Нужные сайты"},icons={"clock","check","notes","finance","calculator","globe"};
  for(int i=0;i<titles.Length;i++){
   int selected=i;var content=Stack();var iconBox=new Border{Width=48,Height=48,CornerRadius=new CornerRadius(11),BorderThickness=new Thickness(1),Child=DashboardIcon(icons[i],27),HorizontalAlignment=HorizontalAlignment.Left,Padding=new Thickness(10),Margin=new Thickness(0,0,0,9)};Resource(iconBox,Border.BackgroundProperty,"Field");Resource(iconBox,Border.BorderBrushProperty,"Line");content.Children.Add(iconBox);
   var title=Text(titles[i],21);title.Margin=new Thickness(0,0,0,3);content.Children.Add(title);var description=Text(descriptions[i],14,"Muted");description.Margin=new Thickness(0);content.Children.Add(description);
   var tileBody=new Grid();tileBody.Children.Add(content);var indicator=new Ellipse{Width=10,Height=10,Fill=Brush("#36D17A"),VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Right};tileBody.Children.Add(indicator);sectionIndicators.Add(indicator);
   var tile=new Button{Content=tileBody,Padding=new Thickness(16),Margin=new Thickness(0,0,12,12),Height=154,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch,Cursor=System.Windows.Input.Cursors.Hand,BorderThickness=new Thickness(1)};System.Windows.Automation.AutomationProperties.SetName(tile,titles[i]);tile.Template=(ControlTemplate)XamlReader.Parse(@"<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Button'><Border x:Name='card' Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='14' Padding='{TemplateBinding Padding}'><ContentPresenter HorizontalAlignment='Stretch' VerticalAlignment='Stretch'/></Border><ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='card' Property='BorderBrush' Value='{DynamicResource Accent}'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='card' Property='BorderBrush' Value='{DynamicResource Accent}'/></Trigger><Trigger Property='IsPressed' Value='True'><Setter TargetName='card' Property='Opacity' Value='.8'/></Trigger></ControlTemplate.Triggers></ControlTemplate>");tile.Click+=(s,e)=>{if(!sectionReorderEditing)OpenSectionDialog(selected);};AttachSectionReorder(tile,SectionKeys[i]);MotionButton(tile);sectionButtons.Add(tile);sectionTiles.Children.Add(tile);
  }
  ApplySectionOrder();sectionTiles.SizeChanged+=(s,e)=>UpdateDashboardLayout();
 }
 void UpdateSectionTiles(){for(int i=0;i<sectionButtons.Count;i++){bool active=tabs.SelectedIndex==i;Resource(sectionButtons[i],Control.BackgroundProperty,active?"TileSelected":"TileFill");bool target=sectionReorderDragging&&sectionButtons[i]==sectionReorderTarget;Resource(sectionButtons[i],Control.BorderBrushProperty,active||target?"Accent":"Line");sectionButtons[i].BorderThickness=new Thickness(active||target?2:1);sectionIndicators[i].Visibility=active?Visibility.Visible:Visibility.Collapsed;}}
 void UpdateDashboardLayout(){if(sectionTiles==null)return;double width=normalLayout.ActualWidth>0?normalLayout.ActualWidth:Width-56;int columns=width>=590?3:width>=315?2:1;if(sectionTiles.Columns!=columns)sectionTiles.Columns=columns;if(arcadeTiles!=null)arcadeTiles.Columns=width>=440?2:1;}
 void DashboardColors(){Color panel=((SolidColorBrush)Resources["Panel"]).Color,accent=((SolidColorBrush)Resources["Accent"]).Color;Color top=Mix(panel,Colors.White,.025);top.A=panel.A;Color selected=Mix(panel,accent,.20);selected.A=panel.A;Resources["TileFill"]=new LinearGradientBrush(top,panel,new Point(0,0),new Point(1,1));Resources["TileSelected"]=new LinearGradientBrush(selected,panel,new Point(0,0),new Point(1,1));}
}
