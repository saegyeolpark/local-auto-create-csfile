// See https://aka.ms/new-console-template for more information
Console.WriteLine("-- git code copy --");

var git_dir = args[0];
var copy_dir = args[1];
Console.WriteLine($"args:\n  git directory: {git_dir}\n  copy directory: {copy_dir}");

var dto_path = git_dir + "/dto";
var dto_dirInfo = new DirectoryInfo(dto_path);
var dto_fileInfos = dto_dirInfo.GetFiles();
if(Directory.Exists(copy_dir + "/dto") == false)
{
    Directory.CreateDirectory(copy_dir + "/dto");
}
foreach(var dto_file in dto_fileInfos)
{
    var csfile = File.ReadAllText(dto_file.FullName);
    File.WriteAllText(copy_dir + "/dto/" + dto_file.Name, csfile);
    Console.WriteLine(dto_file.FullName);
}

//var func_csFile = File.ReadAllText(git_dir + "/Function.cs");
var sheet_csFile = File.ReadAllText(git_dir + "/GliderGoogleSheetData.cs");
//.WriteAllText(copy_dir + "/Function.cs", func_csFile);
sheet_csFile = sheet_csFile.Replace("using AWS.Lambda.Powertools.Logging;", "")
    .Replace("Logger.LogInformation(\"[Log] 2\");", "")
    .Replace("Logger.LogInformation($\"[Log] 3 ${sheetName}\");", "");
File.WriteAllText(copy_dir + "/GliderGoogleSheetData.cs", sheet_csFile);

Console.WriteLine("-- finished --");