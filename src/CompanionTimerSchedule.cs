using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

public partial class Companion {
 TextBox timerHours,timerDate;
 void BuildTimerSchedule(StackPanel form){
  var fields=new Grid();fields.ColumnDefinitions.Add(new ColumnDefinition());fields.ColumnDefinitions.Add(new ColumnDefinition());
  var hours=Stack();hours.Margin=new Thickness(0,0,6,0);hours.Children.Add(Text("Часы",13,"Muted"));timerHours=Input("Часы","0");PrepareTimerNumberInput(timerHours);hours.Children.Add(timerHours);fields.Children.Add(hours);
  var mins=Stack();mins.Margin=new Thickness(6,0,0,0);mins.Children.Add(Text("Минуты",13,"Muted"));minutes=Input("Минуты","30");PrepareTimerNumberInput(minutes);mins.Children.Add(minutes);Grid.SetColumn(mins,1);fields.Children.Add(mins);form.Children.Add(fields);
  form.Children.Add(Text("Дата",13,"Muted"));timerDate=Input("Дата напоминания · необязательно");timerDate.Tag="ДД.ММ.ГГГГ · необязательно";timerDate.MaxLength=10;form.Children.Add(timerDate);
  form.Children.Add(Text("Без даты - отсчёт на указанное количество часов и минут. С датой - напоминание в этот день в указанное время по часам компьютера.",11,"Muted"));
 }
 void PrepareTimerNumberInput(TextBox input){input.GotKeyboardFocus+=(s,e)=>{if(input.Text.Trim()=="0")input.Clear();};input.PreviewMouseLeftButtonDown+=(s,e)=>{if(input.Text.Trim()=="0")input.Clear();};input.LostKeyboardFocus+=(s,e)=>{if(String.IsNullOrWhiteSpace(input.Text))input.Text="0";};}
 public static long ResolveTimerInput(string date,string hours,string minutes,DateTimeOffset now,TimeZoneInfo zone){
  double h=TimerNumber(hours),m=TimerNumber(minutes);
  if(!String.IsNullOrWhiteSpace(date)){
   if(h>23||m>59||h!=Math.Floor(h)||m!=Math.Floor(m))throw new ArgumentException("Для даты укажи часы от 0 до 23 и минуты от 0 до 59, целыми числами.");
   return ResolveTimerDate(date,((int)h).ToString("00")+":"+((int)m).ToString("00"),now,zone);
  }
  double total=h*60+m;if(total<1.0/60||total>10080)throw new ArgumentException("Длительность: от 1 секунды до 7 дней. Введи часы и минуты.");
  return now.ToUnixTimeMilliseconds()+(long)(total*60000);
 }
 static double TimerNumber(string text){double result;if(String.IsNullOrWhiteSpace(text))return 0;if(!Double.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out result)||Double.IsNaN(result)||Double.IsInfinity(result)||result<0)throw new ArgumentException("В часах и минутах нужны неотрицательные числа.");return result;}
 public static long ResolveTimerDate(string date,string time,DateTimeOffset now,TimeZoneInfo zone){
  DateTime day;DateTime clock;
  if(!DateTime.TryParseExact(date.Trim(),new[]{"dd.MM.yyyy","d.M.yyyy","yyyy-MM-dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out day))throw new ArgumentException("Введи существующую дату в формате ДД.ММ.ГГГГ, например 31.10.2026.");
  if(!DateTime.TryParseExact(time.Trim(),new[]{"HH:mm","H:mm"},CultureInfo.InvariantCulture,DateTimeStyles.None,out clock))throw new ArgumentException("Введи время от 00:00 до 23:59 в формате ЧЧ:ММ.");
  var local=DateTime.SpecifyKind(day.Date.Add(clock.TimeOfDay),DateTimeKind.Unspecified);
  if(zone.IsInvalidTime(local))throw new ArgumentException("Такого времени нет из-за перевода часов. Выбери другое время.");
  if(zone.IsAmbiguousTime(local))throw new ArgumentException("Из-за перевода часов это время наступит дважды. Выбери другое время.");
  var end=new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local,zone));
  if(end<=now)throw new ArgumentException("Выбранные дата и время уже прошли. Укажи будущее время.");
  if(end-now>TimeSpan.FromDays(366))throw new ArgumentException("Напоминание можно поставить не больше чем на 366 дней вперёд.");
  return end.ToUnixTimeMilliseconds();
 }
}
