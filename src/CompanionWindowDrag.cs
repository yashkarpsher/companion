using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

// A click still operates buttons. A deliberate drag moves the window instead.
// Editable text, selectors, sliders and resize handles keep their own gestures.
public static class CompanionWindowDrag {
 public static readonly DependencyProperty ReservedProperty=DependencyProperty.RegisterAttached("Reserved",typeof(bool),typeof(CompanionWindowDrag),new PropertyMetadata(false));
 public static void SetReserved(DependencyObject target,bool value){target.SetValue(ReservedProperty,value);}
 public static bool CanStartFrom(DependencyObject source,Window owner){
  for(var node=source;node!=null;node=Parent(node)){
   if((bool)node.GetValue(ReservedProperty))return false;
   if(node is TextBoxBase||node is PasswordBox||(node is Selector&&!(node is TabControl))||node is RangeBase||node is Thumb||node is MenuItem||node is Hyperlink)return false;
   var button=node as ButtonBase;if(button!=null&&button.ClickMode!=ClickMode.Release)return false;
   if(node==owner)return true;
  }
  return false;
 }
 static DependencyObject Parent(DependencyObject node){return(node is Visual||node is System.Windows.Media.Media3D.Visual3D?VisualTreeHelper.GetParent(node):null)??LogicalTreeHelper.GetParent(node);}
 public static bool PastThreshold(Vector delta){return Math.Abs(delta.X)>=SystemParameters.MinimumHorizontalDragDistance||Math.Abs(delta.Y)>=SystemParameters.MinimumVerticalDragDistance;}
 public static void Attach(Window window,Func<bool> blocked){
  bool pending=false,moving=false;Point start=new Point(),screenStart=new Point();double left=0,top=0;
  Action finish=()=>{pending=false;moving=false;if(Mouse.Captured==window)Mouse.Capture(null);};
  window.PreviewMouseLeftButtonDown+=(s,e)=>{
   pending=!blocked()&&e.ClickCount==1&&CanStartFrom(e.OriginalSource as DependencyObject,window);
   if(pending){start=e.GetPosition(window);screenStart=window.PointToScreen(start);left=window.Left;top=window.Top;}
  };
  window.PreviewMouseLeftButtonUp+=(s,e)=>{bool wasMoving=moving;finish();if(wasMoving)e.Handled=true;};
  window.Deactivated+=(s,e)=>finish();
  window.LostMouseCapture+=(s,e)=>{if(moving&&Mouse.Captured!=window)finish();};
  window.PreviewMouseMove+=(s,e)=>{
   if(!pending)return;if(e.LeftButton!=MouseButtonState.Pressed||blocked()){finish();return;}
   Point current=window.PointToScreen(e.GetPosition(window));
   if(!moving&&!PastThreshold(e.GetPosition(window)-start))return;
   if(!moving){Mouse.Capture(window,CaptureMode.SubTree);moving=true;}
   e.Handled=true;
   var source=PresentationSource.FromVisual(window);Matrix transform=source==null?Matrix.Identity:source.CompositionTarget.TransformFromDevice;
   Vector delta=transform.Transform(current-screenStart);
   if(window.WindowState!=WindowState.Normal)window.WindowState=WindowState.Normal;
   if(!Double.IsNaN(left))window.Left=left+delta.X;if(!Double.IsNaN(top))window.Top=top+delta.Y;
  };
 }
}
