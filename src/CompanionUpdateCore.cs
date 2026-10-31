using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

public class CompanionReleaseAsset {public string name{get;set;}public string browser_download_url{get;set;}public string digest{get;set;}public long size{get;set;}}
public class CompanionRelease {public string tag_name{get;set;}public string html_url{get;set;}public string body{get;set;}public bool draft{get;set;}public bool prerelease{get;set;}public List<CompanionReleaseAsset> assets{get;set;}}
public class CompanionUpdateJob {
 public int Protocol{get;set;}public int ParentPid{get;set;}public string TargetDirectory{get;set;}public string StageDirectory{get;set;}public string Version{get;set;}public Dictionary<string,string> Files{get;set;}
}
public static class CompanionUpdateCore {
 public const string Repository="yashkarpsher/companion";
 public const string MainExe="Majestic Companion.exe",UpdaterExe="Companion Updater.exe";
 public const long MaxDownload=150L*1024*1024;
 public static readonly string[] RequiredFiles={"Microsoft.Web.WebView2.Core.dll","Microsoft.Web.WebView2.Wpf.dll","WebView2Loader.dll",UpdaterExe,MainExe};
 public static readonly string[] OptionalFiles={"Прочитай меня.txt","SHA256SUMS.txt","GitHub-обновления.md"};
 public static Version ParseVersion(string tag){Version version;if(!Regex.IsMatch(tag??"",@"^v?\d+\.\d+\.\d+(\.\d+)?$",RegexOptions.IgnoreCase)||!Version.TryParse(tag.TrimStart('v','V'),out version))throw new InvalidDataException("У релиза неверный номер версии. Нужен тег v1.2.3.");return new Version(version.Major,version.Minor,Math.Max(0,version.Build),Math.Max(0,version.Revision));}
 public static CompanionRelease ReadRelease(string text){
  if(text==null||text.Length>4000000)throw new InvalidDataException("Некорректный ответ GitHub.");var release=new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<CompanionRelease>(text);if(release==null||release.draft||release.prerelease)throw new InvalidDataException("Нужен опубликованный стабильный релиз.");ParseVersion(release.tag_name);return release;
 }
 public static CompanionReleaseAsset SelectAsset(CompanionRelease release){
  ParseVersion(release.tag_name);string name="Majestic-Companion-"+release.tag_name.TrimStart('v','V')+".zip";var matches=(release.assets??new List<CompanionReleaseAsset>()).Where(a=>a!=null&&a.name==name).ToList();if(matches.Count!=1)throw new InvalidDataException("В релизе нужен архив "+name+".");var asset=matches[0];Uri url;if(!Uri.TryCreate(asset.browser_download_url,UriKind.Absolute,out url)||url.Scheme!="https"||url.Host!="github.com"||url.Port!=443||!url.AbsolutePath.StartsWith("/"+Repository+"/releases/download/",StringComparison.OrdinalIgnoreCase)||Uri.UnescapeDataString(url.Segments.Last())!=name)throw new InvalidDataException("Архив должен быть опубликован в Releases репозитория "+Repository+".");if(asset.size<=0||asset.size>MaxDownload)throw new InvalidDataException("Неподдерживаемый размер архива.");if(!Regex.IsMatch(asset.digest??"",@"^sha256:[0-9a-fA-F]{64}$"))throw new InvalidDataException("GitHub не предоставил SHA-256 архива. Загрузи архив в новый релиз.");return asset;
 }
 public static string Hash(string file){using(var sha=SHA256.Create())using(var input=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant();}
 public static void VerifyHash(string file,string expected){if(!Regex.IsMatch(expected??"",@"^[0-9a-fA-F]{64}$")||!String.Equals(Hash(file),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Проверка целостности обновления не пройдена. Попробуй скачать его ещё раз.");}
 public static Dictionary<string,string> PreparePackage(string zipFile,string digest,string stage,string version){
  VerifyHash(zipFile,digest);if(Directory.Exists(stage)&&Directory.EnumerateFileSystemEntries(stage).Any())throw new InvalidDataException("Папка подготовки обновления должна быть пустой.");Directory.CreateDirectory(stage);var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
  using(var zip=ZipFile.OpenRead(zipFile)){if(zip.Entries.Count>20000)throw new InvalidDataException("В архиве слишком много файлов.");foreach(var entry in zip.Entries){
   string name=entry.FullName.Replace('\\','/');var parts=name.Split('/');if(name.StartsWith("/")||parts.Any(p=>p==".."||p=="."||p.Contains(":"))||parts[0].Equals("data",StringComparison.OrdinalIgnoreCase)||!names.Add(name))throw new InvalidDataException("Небезопасная структура архива обновления.");total+=entry.Length;if(total>400L*1024*1024||entry.Length>MaxDownload)throw new InvalidDataException("Архив обновления слишком большой.");
   if(name.Contains("/"))continue;if(!RequiredFiles.Contains(name,StringComparer.OrdinalIgnoreCase)&&!OptionalFiles.Contains(name,StringComparer.OrdinalIgnoreCase))continue;string canonical=RequiredFiles.Concat(OptionalFiles).First(n=>n.Equals(name,StringComparison.OrdinalIgnoreCase));using(var input=entry.Open())using(var output=File.Create(Path.Combine(stage,canonical))){var buffer=new byte[65536];long written=0;int count;while((count=input.Read(buffer,0,buffer.Length))>0){written+=count;if(written>entry.Length||written>MaxDownload)throw new InvalidDataException("Файл в архиве превысил допустимый размер.");output.Write(buffer,0,count);}if(written!=entry.Length)throw new InvalidDataException("Файл в архиве повреждён.");}
  }}
  foreach(string name in RequiredFiles)if(!File.Exists(Path.Combine(stage,name)))throw new InvalidDataException("В архиве отсутствует "+name+".");var actual=AssemblyName.GetAssemblyName(Path.Combine(stage,MainExe)).Version;if(ParseVersion(version)!=ParseVersion(actual.ToString()))throw new InvalidDataException("Версия программы в архиве не совпадает с тегом релиза.");return RequiredFiles.Concat(OptionalFiles).Where(n=>File.Exists(Path.Combine(stage,n))).ToDictionary(n=>n,n=>Hash(Path.Combine(stage,n)),StringComparer.OrdinalIgnoreCase);
 }
 static string DirectoryPath(string path){return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);}
 public static void ValidateJob(CompanionUpdateJob job){
  if(job==null||job.Protocol!=1||job.Files==null||RequiredFiles.Any(f=>!job.Files.ContainsKey(f))||job.Files.Keys.Any(f=>!RequiredFiles.Contains(f)&&!OptionalFiles.Contains(f)))throw new InvalidDataException("Неподдерживаемый пакет обновления.");var target=DirectoryPath(job.TargetDirectory);var stage=DirectoryPath(job.StageDirectory);if(target==DirectoryPath(Path.GetPathRoot(target))||stage.Equals(target,StringComparison.OrdinalIgnoreCase)||stage.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Неверные папки обновления.");if(!File.Exists(Path.Combine(target,MainExe)))throw new InvalidDataException("Не найдена установленная программа.");
  foreach(var pair in job.Files){var source=Path.Combine(stage,pair.Key);if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Ссылки на файлы не поддерживаются.");var destination=Path.Combine(target,pair.Key);if(File.Exists(destination)&&(File.GetAttributes(destination)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Ссылки на файлы не поддерживаются.");VerifyHash(source,pair.Value);}if(ParseVersion(job.Version)!=ParseVersion(AssemblyName.GetAssemblyName(Path.Combine(stage,MainExe)).Version.ToString()))throw new InvalidDataException("Версия подготовленного обновления изменилась.");
 }
 public static string Apply(CompanionUpdateJob job,Action<string> progress,Action<string> beforeCopy=null){
  ValidateJob(job);string target=DirectoryPath(job.TargetDirectory),stage=DirectoryPath(job.StageDirectory);string backup=Path.Combine(target,"update-backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);var existed=new HashSet<string>();var changed=new List<string>();
  foreach(string name in job.Files.Keys){string file=Path.Combine(target,name);if(File.Exists(file)){File.Copy(file,Path.Combine(backup,name));existed.Add(name);}}
  try{foreach(string name in job.Files.Keys.OrderBy(n=>n==MainExe?1:0)){if(progress!=null)progress("Устанавливаем "+name);if(beforeCopy!=null)beforeCopy(name);changed.Add(name);File.Copy(Path.Combine(stage,name),Path.Combine(target,name),true);VerifyHash(Path.Combine(target,name),job.Files[name]);}}catch(Exception error){
   var failures=new List<string>();foreach(string name in changed.AsEnumerable().Reverse()){try{string file=Path.Combine(target,name);if(existed.Contains(name))File.Copy(Path.Combine(backup,name),file,true);else if(File.Exists(file))File.Delete(file);}catch{failures.Add(name);}}
   if(failures.Count>0)throw new IOException("Не удалось завершить обновление и вернуть некоторые файлы. Резервная копия: "+backup,error);throw new IOException("Обновление не установлено. Предыдущие файлы восстановлены. "+error.Message,error);
  }return backup;
 }
}
