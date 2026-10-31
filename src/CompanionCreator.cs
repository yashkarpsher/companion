using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

public partial class Companion {
 const string CreatorTikTokUrl="https://www.tiktok.com/@rpsher_yasha";
 const string CreatorTelegramUrl="https://t.me/yashkarp";
 Button CreatorLink(string name,string handle,string url){
  var body=new Grid();body.ColumnDefinitions.Add(new ColumnDefinition());body.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var labels=Stack();var title=Text(name,19);title.Margin=new Thickness(0,0,0,5);labels.Children.Add(title);var account=Text(handle,14,"Muted");account.Margin=new Thickness(0);labels.Children.Add(account);body.Children.Add(labels);var arrow=Text("↗",25,"Accent");arrow.Margin=new Thickness(20,0,0,0);arrow.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(arrow,1);body.Children.Add(arrow);
  var button=Btn("",()=>{try{Process.Start(new ProcessStartInfo{FileName=url,UseShellExecute=true});}catch(Exception){MessageBox.Show(this,"Не удалось открыть ссылку. Адрес: "+url,"Companion",MessageBoxButton.OK,MessageBoxImage.Information);}});button.Content=body;button.Tag=url;button.ToolTip="Открыть "+name+" в браузере: "+url;button.Padding=new Thickness(18);button.Margin=new Thickness(0,0,0,12);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;AutomationProperties.SetName(button,name+" · "+handle);return button;
 }
 void OpenCreator(){
  helpTutorial=false;helpHeading.Text="О создателе";helpStep.Text="COMPANION · АВТОР И СОЦСЕТИ";helpBody.Children.Clear();helpBody.Children.Add(Text("rpsher_yasha",28,"Accent"));helpBody.Children.Add(Text("Создатель Companion",14,"Muted"));helpBody.Children.Add(Text("Следи за новостями и оставайся на связи.",15));helpBody.Children.Add(CreatorLink("TikTok","@rpsher_yasha",CreatorTikTokUrl));helpBody.Children.Add(CreatorLink("Telegram","@yashkarp",CreatorTelegramUrl));helpBody.Children.Add(CreatorLink("GitHub","yashkarpsher/companion","https://github.com/"+CompanionUpdateCore.Repository));helpBody.Children.Add(Text("Ссылки открываются в твоём обычном браузере.",12,"Muted"));helpBack.Visibility=helpNext.Visibility=Visibility.Collapsed;helpSkip.Content="← Назад";helpScroll.ScrollToTop();ShowHelp();helpSkip.Focus();
 }
}
