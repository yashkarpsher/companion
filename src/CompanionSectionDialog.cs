using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

public partial class Companion {
 Grid sectionDialogLayer;
 Border sectionDialogSurface;
 TextBlock sectionDialogTitle;
 Button sectionDialogBack;
 TranslateTransform sectionDialogShift;
 bool sectionDialogOpen;
 int sectionDialogGeneration;
 void BuildSectionDialog(){
  sectionDialogLayer=new Grid{Visibility=Visibility.Collapsed,Background=Brushes.Transparent};Panel.SetZIndex(sectionDialogLayer,50);root.Children.Add(sectionDialogLayer);
  var shade=new Border{Background=Brush("#B3000000")};CompanionWindowDrag.SetReserved(shade,true);shade.MouseLeftButtonDown+=(s,e)=>{e.Handled=true;CloseSectionDialog();};sectionDialogLayer.Children.Add(shade);
  sectionDialogShift=new TranslateTransform();sectionDialogSurface=new Border{CornerRadius=new CornerRadius(18),Padding=new Thickness(18),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,RenderTransform=sectionDialogShift,ClipToBounds=true};Resource(sectionDialogSurface,Border.BackgroundProperty,"DockBg");Resource(sectionDialogSurface,Border.BorderBrushProperty,"Line");sectionDialogLayer.Children.Add(sectionDialogSurface);
  KeyboardNavigation.SetTabNavigation(sectionDialogSurface,KeyboardNavigationMode.Cycle);
  var body=new Grid();body.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});body.RowDefinitions.Add(new RowDefinition());sectionDialogSurface.Child=body;
  var header=new DockPanel{Margin=new Thickness(0,0,0,14)};sectionDialogBack=Btn("← Назад",()=>CloseSectionDialog());sectionDialogBack.MinHeight=40;sectionDialogBack.Margin=new Thickness(0,0,14,0);System.Windows.Automation.AutomationProperties.SetName(sectionDialogBack,"Назад в главное меню");DockPanel.SetDock(sectionDialogBack,Dock.Left);header.Children.Add(sectionDialogBack);
  sectionDialogTitle=Text("",18);sectionDialogTitle.VerticalAlignment=VerticalAlignment.Center;sectionDialogTitle.Margin=new Thickness(0);header.Children.Add(sectionDialogTitle);body.Children.Add(header);
  Grid.SetRow(tabs,1);body.Children.Add(tabs);
  sectionDialogLayer.PreviewMouseWheel+=ScrollMainWheel;root.SizeChanged+=(s,e)=>SizeSectionDialog();SizeSectionDialog();
  PreviewKeyDown+=(s,e)=>{if(sectionDialogOpen&&e.Key==Key.Escape){e.Handled=true;CloseSectionDialog();}};
  Deactivated+=(s,e)=>CloseSectionDialog(false);
  IsVisibleChanged+=(s,e)=>{if(!IsVisible)CloseSectionDialog(false,true);};
 }
 void SizeSectionDialog(){
  if(sectionDialogSurface==null)return;double width=root.ActualWidth>0?root.ActualWidth:Width,height=root.ActualHeight>0?root.ActualHeight:Height;
  sectionDialogSurface.Width=Math.Max(1,Math.Min(680,width-32));sectionDialogSurface.Height=Math.Max(1,Math.Min(760,height-32));
 }
 void OpenSectionDialog(int section){
  if(section<0||section>=tabs.Items.Count)return;CloseHelp();CloseArcade();if(compact)SetCompact(false);if(designing)ShowDesign();if(sectionReorderEditing)SetSectionReorder(false);
  tabs.SelectedIndex=section;tabs.Visibility=Visibility.Visible;sectionDialogTitle.Text=System.Windows.Automation.AutomationProperties.GetName(sectionButtons[section]);
  sectionDialogGeneration++;sectionDialogOpen=true;SizeSectionDialog();sectionDialogLayer.IsHitTestVisible=true;sectionDialogLayer.Visibility=Visibility.Visible;
  sectionDialogLayer.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(200)));
  sectionDialogShift.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(10,0,TimeSpan.FromMilliseconds(220)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
  sectionDialogBack.Focus();
 }
 void CloseSectionDialog(bool restoreFocus=true,bool immediate=false){
  if(sectionDialogLayer==null)return;if(!sectionDialogOpen&&!immediate)return;
  sectionDialogOpen=false;int generation=++sectionDialogGeneration;sectionDialogLayer.IsHitTestVisible=false;
  if(immediate){sectionDialogLayer.BeginAnimation(UIElement.OpacityProperty,null);sectionDialogLayer.Visibility=Visibility.Collapsed;return;}
  var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(150));fade.Completed+=(s,e)=>{if(generation!=sectionDialogGeneration||sectionDialogOpen)return;sectionDialogLayer.Visibility=Visibility.Collapsed;if(restoreFocus&&IsActive&&IsVisible&&tabs.SelectedIndex>=0)sectionButtons[tabs.SelectedIndex].Focus();};
  sectionDialogLayer.BeginAnimation(UIElement.OpacityProperty,fade);
 }
}
