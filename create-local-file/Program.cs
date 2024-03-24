// See https://aka.ms/new-console-template for more information
using create_local_file;
using glider.dto;
using Newtonsoft.Json;

Console.WriteLine("-- create local file start --");


var create = new LocalCreate();
var jsonFile = File.ReadAllText("./apiFile.json");
create.CreateFile(jsonFile);

Console.WriteLine("-- finished --");