using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

public sealed class CompanionBrowserEntry {
 public string Id {get;set;} public string Url {get;set;} public string Title {get;set;} public string VisitedUtc {get;set;}
}
public sealed class CompanionBrowserLibraryData {
 public bool RecordHistory {get;set;}
 public bool EngineHistoryClearPending {get;set;}
 public List<CompanionBrowserEntry> History {get;set;}
 public List<CompanionBrowserEntry> Bookmarks {get;set;}
 public CompanionBrowserLibraryData(){RecordHistory=true;History=new List<CompanionBrowserEntry>();Bookmarks=new List<CompanionBrowserEntry>();}
}
public sealed class CompanionBrowserLibrary {
 public const int HistoryLimit=200,BookmarkLimit=500;
 readonly string file;
 string savedState;
 readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=8388608};
 public CompanionBrowserLibraryData Data {get;private set;}
 public string Warning {get;private set;}
 public CompanionBrowserLibrary(string folder){
  file=Path.Combine(folder,"companion-library.json");Data=Read(file);
  if(Data==null){Data=Read(file+".bak");if(Data!=null)Warning="История и закладки восстановлены из резервной копии.";else {Data=new CompanionBrowserLibraryData();if(File.Exists(file))Warning="Не удалось прочитать историю и закладки. Исходный файл не изменён.";}}
  Data.History=Clean(Data.History,HistoryLimit,true);Data.Bookmarks=Clean(Data.Bookmarks,BookmarkLimit,false);
  savedState=json.Serialize(Data);
 }
 CompanionBrowserLibraryData Read(string name){try{if(!File.Exists(name)||new FileInfo(name).Length>8388608)return null;return json.Deserialize<CompanionBrowserLibraryData>(File.ReadAllText(name,Encoding.UTF8));}catch{return null;}}
 public static bool TryUrl(string value,out string normalized){
  normalized=null;Uri uri;if(String.IsNullOrWhiteSpace(value)||value.Length>8192||!Uri.TryCreate(value,UriKind.Absolute,out uri)||(uri.Scheme!="http"&&uri.Scheme!="https")||String.IsNullOrEmpty(uri.Host)||!String.IsNullOrEmpty(uri.UserInfo)||uri.AbsoluteUri.Length>8192)return false;normalized=uri.AbsoluteUri;return true;
 }
 static string Title(string value,string url){value=(value??"").Trim();if(value.Length==0)value=new Uri(url).Host;return value.Length>300?value.Substring(0,300):value;}
 static List<CompanionBrowserEntry> Clean(List<CompanionBrowserEntry> input,int limit,bool history){
  var result=new List<CompanionBrowserEntry>();var urls=new HashSet<string>(StringComparer.Ordinal);var ids=new HashSet<string>(StringComparer.Ordinal);
  foreach(var item in input??new List<CompanionBrowserEntry>()){
   string url;DateTime date;if(item==null||!TryUrl(item.Url,out url)||(!history&&!urls.Add(url))||(history&&!DateTime.TryParse(item.VisitedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out date)))continue;
   string id=item.Id;if(String.IsNullOrEmpty(id)||id.Length>100||!ids.Add(id)){id=Guid.NewGuid().ToString("N");ids.Add(id);}
   result.Add(new CompanionBrowserEntry{Id=id,Url=url,Title=Title(item.Title,url),VisitedUtc=item.VisitedUtc});if(result.Count>=limit)break;
  }return result;
 }
 public bool IsBookmarked(string url){string normalized;return TryUrl(url,out normalized)&&Data.Bookmarks.Any(x=>x.Url==normalized);}
 public void SetRecording(bool enabled){Data.RecordHistory=enabled;if(!enabled)Data.EngineHistoryClearPending=true;Save(false);}
 public void Visit(string url,string title){
  string normalized;if(!Data.RecordHistory||!TryUrl(url,out normalized))return;
  CompanionBrowserEntry entry=Data.History.FirstOrDefault();if(entry==null||entry.Url!=normalized){entry=new CompanionBrowserEntry{Id=Guid.NewGuid().ToString("N"),Url=normalized};Data.History.Insert(0,entry);}
  entry.Title=Title(title,normalized);entry.VisitedUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);if(Data.History.Count>HistoryLimit)Data.History.RemoveRange(HistoryLimit,Data.History.Count-HistoryLimit);Save(false);
 }
 public void UpdateTitle(string url,string title){
  string normalized;if(!Data.RecordHistory||String.IsNullOrWhiteSpace(title)||!TryUrl(url,out normalized))return;var entry=Data.History.FirstOrDefault(x=>x.Url==normalized);if(entry==null)return;string text=Title(title,normalized);if(entry.Title==text)return;entry.Title=text;Save(false);
 }
 public void ToggleBookmark(string url,string title){
  string normalized;if(!TryUrl(url,out normalized))throw new ArgumentException("Открой веб-страницу, чтобы добавить её в закладки.");var entry=Data.Bookmarks.FirstOrDefault(x=>x.Url==normalized);
  if(entry!=null)Data.Bookmarks.Remove(entry);else {if(Data.Bookmarks.Count>=BookmarkLimit)throw new InvalidOperationException("В закладках уже 500 страниц. Удали ненужные, чтобы добавить новую.");Data.Bookmarks.Insert(0,new CompanionBrowserEntry{Id=Guid.NewGuid().ToString("N"),Url=normalized,Title=Title(title,normalized),VisitedUtc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture)});}Save(true);
 }
 public void RenameBookmark(string id,string title){var entry=Data.Bookmarks.FirstOrDefault(x=>x.Id==id);if(entry==null)return;entry.Title=Title(title,entry.Url);Save(false);}
 public void Remove(string id,bool history){(history?Data.History:Data.Bookmarks).RemoveAll(x=>x.Id==id);Save(true);}
 public void ClearHistory(){Data.History.Clear();Data.EngineHistoryClearPending=true;Save(true);}
 public void EngineHistoryCleared(){if(!Data.EngineHistoryClearPending)return;Data.EngineHistoryClearPending=false;Save(false);}
 public IEnumerable<CompanionBrowserEntry> Find(bool history,string query){string text=(query??"").Trim();return(history?Data.History:Data.Bookmarks).Where(x=>text.Length==0||x.Title.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0||x.Url.IndexOf(text,StringComparison.OrdinalIgnoreCase)>=0).ToList();}
 void Save(bool scrubBackup){
  string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{Directory.CreateDirectory(Path.GetDirectoryName(file));string text=json.Serialize(Data);File.WriteAllText(temp,text,new UTF8Encoding(false));RetryIo(()=>{if(File.Exists(file))File.Replace(temp,file,file+".bak",true);else File.Move(temp,file);});
   savedState=text;
   // Clearing history also clears it from the recovery copy.
   if(scrubBackup)RetryIo(()=>File.WriteAllText(file+".bak",text,new UTF8Encoding(false)));
  }catch{Data=json.Deserialize<CompanionBrowserLibraryData>(savedState);throw;}
  finally{try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 static void RetryIo(Action action){for(int attempt=0;;attempt++)try{action();return;}catch(IOException){if(attempt>=2)throw;System.Threading.Thread.Sleep(attempt==0?25:75);}}
}
