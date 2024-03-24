using System.Text;
using glider.dto;
using Newtonsoft.Json;


namespace Glider
{
	public enum StaticDataCategory
	{
		Shared,
		GameServer
	}

    public class GliderGoogleSheetData
	{
		private RequestDataDto sheetData;
        private Dictionary<string, string[][]> googleSheetDataDictionary;
        public bool IsProcessing { get; private set; }

		private const string SheetNameEnums = "#Enums";
		private const string SheetNameClassCloudData = "#Struct.CloudData";
		private const string SheetNameClassCommon = "#Class.Common";
		private const string SheetNameClassStaticData = "#Class.StaticData";
		private const string SheetNameSharedStaticDataWrapper = "#SharedStaticDataWrapper";
		private const string SheetNameCloudDataWrapper = "#CloudDataWrapper";
        private static readonly string[] SheetNameStaticDataClasses = { SheetNameClassCommon, SheetNameClassStaticData };
        private static readonly string[] AllSheetNames = {
			SheetNameEnums,
			SheetNameClassCloudData,
			SheetNameClassCommon,
			SheetNameClassStaticData,
			SheetNameSharedStaticDataWrapper,
			SheetNameCloudDataWrapper
		};
        private const string EOFIdentifier = "[END]";


		public GliderGoogleSheetData(RequestDataDto googleSheetWasData)
		{
			// this.googleSheetDataDictionary = googleSheetWasData;
			this.sheetData=googleSheetWasData;
			
        }

        /**
		 * googleSheetDataDictionary 에 있는 모든 함수 실행
		 */
        public AutoCreateOutput WriteOneCsFile(string sheetName, StaticDataCategory category)
        {
			List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            AutoCreateOutput getAutoCreateResult = new AutoCreateOutput();

            switch (sheetName) {
				case SheetNameEnums:
                    getAutoCreateResult = this.WriteSheetEnumsToCsFile(category);
					filesResultList.AddRange(getAutoCreateResult.files);
                    break;

                case SheetNameClassStaticData:
                    getAutoCreateResult = this.WriteSheetStaticDataClassesToCsFile(category, SheetNameClassStaticData);
                    filesResultList.AddRange(getAutoCreateResult.files);
                    break;

                case SheetNameSharedStaticDataWrapper:
                    getAutoCreateResult = this.WriteSheetStaticDataWrapperToCsFile(category);
                    filesResultList.AddRange(getAutoCreateResult.files);
                    break;

				case SheetNameClassCloudData:
                    getAutoCreateResult = this.WriteSheetCloudDataStructsToCsFile(category);
                    filesResultList.AddRange(getAutoCreateResult.files);
                    break;
					
                case SheetNameCloudDataWrapper:
                    getAutoCreateResult = this.WriteSheetCloudDataWrapperToCsFile(category);
                    filesResultList.AddRange(getAutoCreateResult.files);
					break;

                default:
					break;

            }
			return new AutoCreateOutput(filesResultList);
        }


        /**
		 * googleSheetDataDictionary 에 있는 모든 함수 실행
		 */
        public AutoCreateOutput WriteAllCsFile(StaticDataCategory category)
		{
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();


            foreach (string sheetName in AllSheetNames){

                AutoCreateOutput  getAutoCreateResult = this.WriteOneCsFile(sheetName, category);
                filesResultList.AddRange(getAutoCreateResult.files);
                
                Console.WriteLine(sheetName);
                Console.WriteLine();
                string s1 = JsonConvert.SerializeObject(getAutoCreateResult.files);
                Console.WriteLine(s1);
                Console.WriteLine();
            }

            return new AutoCreateOutput(filesResultList);
        }


        public AutoCreateOutput WriteSheetCloudDataWrapperToCsFile(StaticDataCategory dataCategory)
		{
			IsProcessing = true;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

			CloudDataWrappers[] cloudDataWrappers = this.sheetData.cloudDataWrappers;
            if (cloudDataWrappers is null)
            {
                return new AutoCreateOutput(filesResultList);
            }

            {
				string cloudDataWrapperName = "CloudDataWrapper";
				string filepath = "Assets/Glider/Core/Scripts/SerializableData/CloudData/AutoCreated";
				string classNamespace = "Glider.Core.SerializableData";

				string classType = null;
				string fulltype = null;
				string wrapType = null;
				string className = null;
				int y = -1;
				try
				{
					var sb = new StringBuilder();
					var sbCheckDirty = new StringBuilder();
					var sbUpdateSrc = new StringBuilder();
					var sbSetPayload = new StringBuilder();
					
					sbCheckDirty.AppendLine("		public bool CheckDirty()");
					sbCheckDirty.AppendLine("		{");
					
					sbUpdateSrc.AppendLine("		public void UpdateCrc()");
					sbUpdateSrc.AppendLine("		{");
					
					sbSetPayload.AppendLine("		public void SetPayload(ref List<string> keys, ref List<string> values)");
					sbSetPayload.AppendLine("		{");
					
					
					sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
					sb.AppendLine("// Do not modify it manually!123");
					sb.AppendLine();
					sb.AppendLine("using System;");
					sb.AppendLine("using System.Collections.Generic;");
					sb.AppendLine("using UnityEngine;");
					sb.AppendLine();
					sb.AppendLine($"namespace {classNamespace}");
					sb.AppendLine("{");
					sb.AppendLine("	[Serializable]");
					sb.AppendLine($"	public class {cloudDataWrapperName}");
					sb.AppendLine("	{");

					sb.AppendLine("		private Dictionary<string, int> _crcCodes=new();");
					sb.AppendLine("		private Dictionary<string, bool> _dirty=new();");
					foreach(CloudDataWrappers cloudDataWrapper in cloudDataWrappers)
					{
						classType = cloudDataWrapper.type;
						className = cloudDataWrapper.name;
						wrapType = cloudDataWrapper.isList ? "list" : "object";
						var pascalName = char.ToUpper(className[0]) + className[1..];
						switch (wrapType)
						{
							case "list":
								var sbWrapper = new StringBuilder();
								fulltype = $"List<{classType}>";

								var wrapperType = $"{classType}Wrapper";

								sb.AppendLine($"		[SerializeField] private {wrapperType} {className} = new();");
								sb.AppendLine($"		public {wrapperType} {pascalName}=>{className};");
								
								sbWrapper.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
								sbWrapper.AppendLine("// Do not modify it manually!");
								sbWrapper.AppendLine();
								sbWrapper.AppendLine("using System;");
								sbWrapper.AppendLine("using System.Collections.Generic;");
								sbWrapper.AppendLine("using UnityEngine;");
								sbWrapper.AppendLine("using UnityEngine.Events;");
								sbWrapper.AppendLine();
								sbWrapper.AppendLine("namespace Glider.Core.SerializableData");
								sbWrapper.AppendLine("{");
								sbWrapper.AppendLine("	[Serializable]");
							
								
								sbWrapper.AppendLine($"	public class {wrapperType} : CloudDataBase");
								sbWrapper.AppendLine("	{");
								
								sbWrapper.AppendLine($"        [SerializeField] private {fulltype} list = new();");
								sbWrapper.AppendLine($"        private Dictionary<int, UnityAction<int>> _changeCallback=new();");
								sbWrapper.AppendLine($"        public event UnityAction OnAnyChange;");
								sbWrapper.AppendLine($"        public int Size => list?.Count ?? 0;");
								
								sbWrapper.AppendLine($"        public {classType} Get(int index)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine($"			if (_crcCodes[index] != list[index].CreateCrdCode())");
								sbWrapper.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to retrieve cloud data due to invalid hash.\", \"{className}\", index, JsonUtility.ToJson(list[index]));");
								sbWrapper.AppendLine($"			var res = new {classType}();");
								sbWrapper.AppendLine($"			res = list[index];");
								sbWrapper.AppendLine($"			return res;");
								sbWrapper.AppendLine("        }");
								
								sbWrapper.AppendLine($"        public void Set(int index, {classType} value)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine($"			var crdCode = list[index].CreateCrdCode();");
								sbWrapper.AppendLine($"			if (_crcCodes[index] != crdCode)");
								sbWrapper.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to set cloud data due to invalid hash.\", \"{className}\", index, JsonUtility.ToJson(list[index]), JsonUtility.ToJson(value));");
								sbWrapper.AppendLine($"			list[index]=value;");
								sbWrapper.AppendLine($"			_crcCodes[index]=value.CreateCrdCode();");
								sbWrapper.AppendLine($"			IsDirty = true;");
								sbWrapper.AppendLine($"			if(_changeCallback.ContainsKey(index))");
								sbWrapper.AppendLine($"				_changeCallback[index]?.Invoke(index);");
								sbWrapper.AppendLine($"			OnAnyChange?.Invoke();");
								sbWrapper.AppendLine("        }");

								sbWrapper.AppendLine("        public void AddChangeListener(UnityAction action)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine("        	OnAnyChange += action;");
								sbWrapper.AppendLine("        }");
								sbWrapper.AppendLine("        public void RemoveChangeListener(UnityAction action)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine("        	OnAnyChange -= action;");
								sbWrapper.AppendLine("        }");
								sbWrapper.AppendLine("        public void AddChangeListener(int index, UnityAction<int> action)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine("        	if (!_changeCallback.ContainsKey(index))");
								sbWrapper.AppendLine("        		_changeCallback.Add(index, action);");
								sbWrapper.AppendLine("        	else");
								sbWrapper.AppendLine("        		_changeCallback[index] += action;");
								sbWrapper.AppendLine("        }");
								sbWrapper.AppendLine("        public void RemoveChangeListener(int index, UnityAction<int> action)");
								sbWrapper.AppendLine("        {");
								sbWrapper.AppendLine("        	if (_changeCallback.ContainsKey(index))");
								sbWrapper.AppendLine("        		_changeCallback[index] -= action;");
								sbWrapper.AppendLine("        }");
								
								sbWrapper.AppendLine("		public void UpdateCrcCode()");
								sbWrapper.AppendLine("		{");
								sbWrapper.AppendLine("			for (int i = 0; i < list.Count; i++)");
								sbWrapper.AppendLine("			{");
								sbWrapper.AppendLine("				var code=list[i].CreateCrdCode();");
								sbWrapper.AppendLine("				if (i<_crcCodes.Count)");
								sbWrapper.AppendLine("					_crcCodes[i] = code ;");
								sbWrapper.AppendLine("				else");
								sbWrapper.AppendLine("					_crcCodes.Add(code);");
								sbWrapper.AppendLine("			}");
								sbWrapper.AppendLine("		}");
								
								
								sbWrapper.AppendLine($"		public void Add({classType} e)");
								sbWrapper.AppendLine("		{");
								sbWrapper.AppendLine("			list.Add(e);");
								sbWrapper.AppendLine("			_crcCodes.Add(e.CreateCrdCode());");
								sbWrapper.AppendLine("			IsDirty = true;");
								sbWrapper.AppendLine("		}");
								
								
								sbWrapper.AppendLine("		public void SetPayload(ref List<string> keys, ref List<string> values)");
								sbWrapper.AppendLine("		{");
								sbWrapper.AppendLine("			if (IsDirty)");
								sbWrapper.AppendLine("			{");
								sbWrapper.AppendLine($"				keys.Add(\"{className}\");");
								sbWrapper.AppendLine($"				values.Add(JsonUtility.ToJson(this));");
								sbWrapper.AppendLine($"				IsDirty = false;");
								sbWrapper.AppendLine("			}");
								sbWrapper.AppendLine("		}");
								
								
								sbWrapper.AppendLine("	}");
								sbWrapper.AppendLine("}");
								//await File.WriteAllTextAsync(Path.Combine(filepath, $"{wrapperType}.cs"), sbWrapper.ToString());
								//File.WriteAllText(Path.Combine(filepath, $"{wrapperType}.cs"), sbWrapper.ToString());

								CsFileInfo filesResultItemByList = new CsFileInfo(filepath, wrapperType, sbWrapper.ToString());
								filesResultList.Add(filesResultItemByList);

								sbCheckDirty.AppendLine($"			if ({className}.IsDirty) return true;");
								sbUpdateSrc.AppendLine($"			{className}.UpdateCrcCode();");
								sbSetPayload.AppendLine($"			if ({className}.IsDirty) {className}.SetPayload(ref keys,ref values);");
								break;
							case "object":
								var fieldName = $"FieldName{pascalName}";
								sb.AppendLine($"		private string {fieldName}=\"{className}\";");
								sb.AppendLine($"		[SerializeField] private {classType} {className} = new();");
								// sb.AppendLine($"		public {classType} {pascalName}");
								// sb.AppendLine("		{");
								// sb.AppendLine("			get");
								// sb.AppendLine("			{");
								// sb.AppendLine($"				if (_crcCodes[{fieldName}] != {className}.CreateCrdCode())");
								// sb.AppendLine($"					throw new InvalidCloudDataHashException(\"Failed to retrieve cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}));");
								// sb.AppendLine($"				return {className};");
								// sb.AppendLine("			}");
								// sb.AppendLine("			set");
								// sb.AppendLine("			{");
								// sb.AppendLine($"				var crdCode = this.{className}.CreateCrdCode();");
								// sb.AppendLine($"				if (_crcCodes[{fieldName}] != crdCode)");
								// sb.AppendLine($"					throw new InvalidCloudDataHashException(\"Failed to set cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}), JsonUtility.ToJson(value));");
								// sb.AppendLine($"				this.{className} = value;");
								// sb.AppendLine($"				_crcCodes[{fieldName}]=value.CreateCrdCode();");
								// sb.AppendLine($"				_dirty[{fieldName}] = true;");
								// sb.AppendLine("			}");
								// sb.AppendLine("		}");
								// sb.AppendLine();
								sb.AppendLine($"		public {classType} Get{pascalName}()");
								sb.AppendLine("		{");
								sb.AppendLine($"			if (_crcCodes[{fieldName}] != {className}.CreateCrdCode())");
								sb.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to retrieve cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}));");
								sb.AppendLine($"			var res = new {classType}();");
								sb.AppendLine($"			res = {className};");
								sb.AppendLine($"			return res;");
								sb.AppendLine("		}");

								sb.AppendLine($"		public void Set{pascalName}({classType} value)");
								sb.AppendLine("		{");
								sb.AppendLine($"			var crdCode = this.{className}.CreateCrdCode();");
								sb.AppendLine($"			if (_crcCodes[{fieldName}] != crdCode)");
								sb.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to set cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}), JsonUtility.ToJson(value));");
								sb.AppendLine($"			this.{className} = value;");
								sb.AppendLine($"			_crcCodes[{fieldName}]=value.CreateCrdCode();");
								sb.AppendLine($"			_dirty[{fieldName}] = true;");
								sb.AppendLine("		}");
								sb.AppendLine();

								sbCheckDirty.AppendLine($"			if (_dirty.ContainsKey({fieldName}) && _dirty[{fieldName}]) return true;");
								
								sbUpdateSrc.AppendLine($"			_crcCodes[{fieldName}] = {className}.CreateCrdCode();");
								
								sbSetPayload.AppendLine($"			if (_dirty.ContainsKey({fieldName}) && _dirty[{fieldName}])");
								sbSetPayload.AppendLine("			{");
								sbSetPayload.AppendLine($"				keys.Add({fieldName});");
								sbSetPayload.AppendLine($"				values.Add(JsonUtility.ToJson({className}));");
								sbSetPayload.AppendLine($"				_dirty[{fieldName}] = false;");
								sbSetPayload.AppendLine("			}");
								break;
						}
					}
					sbCheckDirty.AppendLine("			return false;");
					sbCheckDirty.AppendLine("		}");
					sb.AppendLine(sbCheckDirty.ToString());
					
					sbUpdateSrc.AppendLine("		}");
					sb.AppendLine(sbUpdateSrc.ToString());
					
					sbSetPayload.AppendLine("		}");
					sb.AppendLine(sbSetPayload.ToString());
					
					
					sb.AppendLine("	}");
					sb.AppendLine("}");
					//await File.WriteAllTextAsync(Path.Combine(filepath, $"{cloudDataWrapperName}.cs"), sb.ToString());
					//File.WriteAllText(Path.Combine(filepath, $"{cloudDataWrapperName}.cs"), sb.ToString());

					CsFileInfo filesResultItem = new CsFileInfo(filepath, cloudDataWrapperName, sb.ToString());
					filesResultList.Add(filesResultItem);
				}
				catch (Exception e)
				{
					Console.WriteLine($"[WriteSheetWrapperToCsFile] wrapperClassName:{cloudDataWrapperName} className:{className}, filepath:{filepath}, y:{y})");
					// Debug.LogError($"[WriteSheetWrapperToCsFile] wrapperClassName:{cloudDataWrapperName} className:{className}, filepath:{filepath}, y:{y})");
					throw e;
				}


				// AssetDatabase.Refresh();
			}
			// AssetDatabase.Refresh();
			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		// 시트로 가게되면 사용
		public AutoCreateOutput WriteSheetCloudDataWrapperToCsFileLegacy(StaticDataCategory dataCategory)
		{
			IsProcessing = true;

            string sheetName = SheetNameCloudDataWrapper;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            if (!this.googleSheetDataDictionary.ContainsKey(sheetName))
            {
                return new AutoCreateOutput(filesResultList);
            }

            {
				var table = this.googleSheetDataDictionary[sheetName];
				var height = table.Length;
				if (height >= 2)
				{
					string cloudDataWrapperName = table[1][0].ToString();
					string filepath = table[3][0].ToString();
					string classNamespace = table[5][0].ToString();

					string classType = null;
					string fulltype = null;
					string wrapType = null;
					string className = null;
					int y = -1;
					try
					{
						var sb = new StringBuilder();
						var sbCheckDirty = new StringBuilder();
						var sbUpdateSrc = new StringBuilder();
						var sbSetPayload = new StringBuilder();
						
						sbCheckDirty.AppendLine("		public bool CheckDirty()");
						sbCheckDirty.AppendLine("		{");
						
						sbUpdateSrc.AppendLine("		public void UpdateCrc()");
						sbUpdateSrc.AppendLine("		{");
						
						sbSetPayload.AppendLine("		public void SetPayload(ref List<string> keys, ref List<string> values)");
						sbSetPayload.AppendLine("		{");
						
						
						sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
						sb.AppendLine("// Do not modify it manually!");
						sb.AppendLine();
						sb.AppendLine("using System;");
						sb.AppendLine("using System.Collections.Generic;");
						sb.AppendLine("using UnityEngine;");
						sb.AppendLine();
						sb.AppendLine($"namespace {classNamespace}");
						sb.AppendLine("{");
						sb.AppendLine("	[Serializable]");
						sb.AppendLine($"	public class {cloudDataWrapperName}");
						sb.AppendLine("	{");

						sb.AppendLine("		private Dictionary<string, int> _crcCodes=new();");
						sb.AppendLine("		private Dictionary<string, bool> _dirty=new();");
						for (y = 7; y < height; y++)
						{
							if (table[y][0].ToString() == EOFIdentifier) break;

							if (!Convert.ToBoolean(table[y][1])) continue;
							classType = table[y][2].ToString();
							className = table[y][3].ToString();
							wrapType = table[y][4].ToString();
							var pascalName = char.ToUpper(className[0]) + className[1..];
							switch (wrapType)
							{
								case "list":
									var sbWrapper = new StringBuilder();
									fulltype = $"List<{classType}>";

									var wrapperType = $"{classType}Wrapper";

									sb.AppendLine($"		[SerializeField] private {wrapperType} {className} = new();");
									sb.AppendLine($"		public {wrapperType} {pascalName}=>{className};");
									
									sbWrapper.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
									sbWrapper.AppendLine("// Do not modify it manually!");
									sbWrapper.AppendLine();
									sbWrapper.AppendLine("using System;");
									sbWrapper.AppendLine("using System.Collections.Generic;");
									sbWrapper.AppendLine("using UnityEngine;");
									sbWrapper.AppendLine("using UnityEngine.Events;");
									sbWrapper.AppendLine();
									sbWrapper.AppendLine("namespace Glider.Core.SerializableData");
									sbWrapper.AppendLine("{");
									sbWrapper.AppendLine("	[Serializable]");
								
									
									sbWrapper.AppendLine($"	public class {wrapperType} : CloudDataBase");
									sbWrapper.AppendLine("	{");
									
									sbWrapper.AppendLine($"        [SerializeField] private {fulltype} list = new();");
									sbWrapper.AppendLine($"        public int Size => list?.Count ?? 0;");
									
									sbWrapper.AppendLine($"        public {classType} Get(int index)");
									sbWrapper.AppendLine("        {");
									sbWrapper.AppendLine($"			if (_crcCodes[index] != list[index].CreateCrdCode())");
									sbWrapper.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to retrieve cloud data due to invalid hash.\", \"{className}\", index, JsonUtility.ToJson(list[index]));");
									sbWrapper.AppendLine($"			var res = new {classType}();");
									sbWrapper.AppendLine($"			res = list[index];");
									sbWrapper.AppendLine($"			return res;");
									sbWrapper.AppendLine("        }");
									
									sbWrapper.AppendLine($"        public void Set(int index, {classType} value)");
									sbWrapper.AppendLine("        {");
									sbWrapper.AppendLine($"			var crdCode = list[index].CreateCrdCode();");
									sbWrapper.AppendLine($"			if (_crcCodes[index] != list[index].CreateCrdCode())");
									sbWrapper.AppendLine($"				throw new InvalidCloudDataHashException(\"Failed to set cloud data due to invalid hash.\", \"{className}\", index, JsonUtility.ToJson(list[index]), JsonUtility.ToJson(value));");
									sbWrapper.AppendLine($"			list[index]=value;");
									sbWrapper.AppendLine($"			_crcCodes[index]=crdCode;");
									sbWrapper.AppendLine($"			IsDirty = true;");
									sbWrapper.AppendLine("        }");

									sbWrapper.AppendLine("		public void UpdateCrcCode()");
									sbWrapper.AppendLine("		{");
									sbWrapper.AppendLine("			for (int i = 0; i < list.Count; i++)");
									sbWrapper.AppendLine("			{");
									sbWrapper.AppendLine("				var code=list[i].CreateCrdCode();");
									sbWrapper.AppendLine("				if (i<_crcCodes.Count)");
									sbWrapper.AppendLine("					_crcCodes[i] = code ;");
									sbWrapper.AppendLine("				else");
									sbWrapper.AppendLine("					_crcCodes.Add(code);");
									sbWrapper.AppendLine("			}");
									sbWrapper.AppendLine("		}");
									
									
									sbWrapper.AppendLine($"		public void Add({classType} e)");
									sbWrapper.AppendLine("		{");
									sbWrapper.AppendLine("			list.Add(e);");
									sbWrapper.AppendLine("			_crcCodes.Add(e.CreateCrdCode());");
									sbWrapper.AppendLine("			IsDirty = true;");
									sbWrapper.AppendLine("		}");
									
									
									sbWrapper.AppendLine("		public void SetPayload(ref List<string> keys, ref List<string> values)");
									sbWrapper.AppendLine("		{");
									sbWrapper.AppendLine("			if (IsDirty)");
									sbWrapper.AppendLine("			{");
									sbWrapper.AppendLine($"				keys.Add(\"{className}\");");
									sbWrapper.AppendLine($"				values.Add(JsonUtility.ToJson(this));");
									sbWrapper.AppendLine($"				IsDirty = false;");
									sbWrapper.AppendLine("			}");
									sbWrapper.AppendLine("		}");
									
									
									sbWrapper.AppendLine("	}");
									sbWrapper.AppendLine("}");
									//await File.WriteAllTextAsync(Path.Combine(filepath, $"{wrapperType}.cs"), sbWrapper.ToString());
									//File.WriteAllText(Path.Combine(filepath, $"{wrapperType}.cs"), sbWrapper.ToString());


									sbCheckDirty.AppendLine($"			if ({className}.IsDirty) return true;");
									sbUpdateSrc.AppendLine($"			{className}.UpdateCrcCode();");
									sbSetPayload.AppendLine($"			if ({className}.IsDirty) {className}.SetPayload(ref keys,ref values);");
									break;
								case "object":
									var fieldName = $"FieldName{pascalName}";
									sb.AppendLine($"		private string {fieldName}=\"{className}\";");
									sb.AppendLine($"		[SerializeField] private {classType} {className} = new();");
									sb.AppendLine($"		public {classType} {pascalName}");
									sb.AppendLine("		{");
									sb.AppendLine("			get");
									sb.AppendLine("			{");
									sb.AppendLine($"				if (_crcCodes[{fieldName}] != {className}.CreateCrdCode())");
									sb.AppendLine($"					throw new InvalidCloudDataHashException(\"Failed to retrieve cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}));");
									sb.AppendLine($"				return {className};");
									sb.AppendLine("			}");
									sb.AppendLine("			set");
									sb.AppendLine("			{");
									sb.AppendLine($"				var crdCode = this.{className}.CreateCrdCode();");
									sb.AppendLine($"				if (_crcCodes[{fieldName}] != crdCode)");
									sb.AppendLine($"					throw new InvalidCloudDataHashException(\"Failed to set cloud data due to invalid hash.\", \"{className}\", JsonUtility.ToJson({className}), JsonUtility.ToJson(value));");
									sb.AppendLine($"				this.{className} = value;");
									sb.AppendLine($"				_crcCodes[{fieldName}]=crdCode;");
									sb.AppendLine($"				_dirty[{fieldName}] = true;");
									sb.AppendLine("			}");
									sb.AppendLine("		}");
									sb.AppendLine();
									
									sbCheckDirty.AppendLine($"			if (_dirty.ContainsKey({fieldName}) && _dirty[{fieldName}]) return true;");
									
									sbUpdateSrc.AppendLine($"			_crcCodes[{fieldName}] = {className}.CreateCrdCode();");
									
									sbSetPayload.AppendLine($"			if (_dirty.ContainsKey({fieldName}) && _dirty[{fieldName}])");
									sbSetPayload.AppendLine("			{");
									sbSetPayload.AppendLine($"				keys.Add({fieldName});");
									sbSetPayload.AppendLine($"				values.Add(JsonUtility.ToJson({className}));");
									sbSetPayload.AppendLine($"				_dirty[{fieldName}] = false;");
									sbSetPayload.AppendLine("			}");
									break;
							}
						}

						sbCheckDirty.AppendLine("			return false;");
						sbCheckDirty.AppendLine("		}");
						sb.AppendLine(sbCheckDirty.ToString());
						
						sbUpdateSrc.AppendLine("		}");
						sb.AppendLine(sbUpdateSrc.ToString());
						
						sbSetPayload.AppendLine("		}");
						sb.AppendLine(sbSetPayload.ToString());
						
						
						sb.AppendLine("	}");
						sb.AppendLine("}");
                        //await File.WriteAllTextAsync(Path.Combine(filepath, $"{cloudDataWrapperName}.cs"), sb.ToString());
                        //File.WriteAllText(Path.Combine(filepath, $"{cloudDataWrapperName}.cs"), sb.ToString());

                        CsFileInfo filesResultItem = new CsFileInfo(filepath, cloudDataWrapperName, sb.ToString());
                        filesResultList.Add(filesResultItem);
                    }
					catch (Exception e)
					{
						Console.WriteLine($"[WriteSheetWrapperToCsFile] wrapperClassName:{cloudDataWrapperName} className:{className}, filepath:{filepath}, y:{y})");
						// Debug.LogError($"[WriteSheetWrapperToCsFile] wrapperClassName:{cloudDataWrapperName} className:{className}, filepath:{filepath}, y:{y})");
						throw e;
					}


					// AssetDatabase.Refresh();
				}

			}
			// AssetDatabase.Refresh();
			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		public AutoCreateOutput WriteSheetStaticDataWrapperToCsFile(StaticDataCategory dataCategory)
		{
			IsProcessing = true;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            if (this.sheetData.dataWrappers is null)
            {
                return new AutoCreateOutput(filesResultList);
            }
            {
				
				// TODO 동적으로 바꾸도록 수정
				string wrapperClassName = "SharedStaticDataWrapper";
				string filepath = "Assets/Glider/Core/Scripts/SerializableData/StaticData/AutoCreated";
				string classNamespace = "Glider.Core.SerializableData";
				string wrapperSuperClassName = "StaticDataBase";
				string comment = "";

				string type = null;
				string fulltype = null;
				string className = null;
				bool isArray = false;
				bool canReload = false;
				int y = -1;
				try
				{
					var sb = new StringBuilder();
					sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
					sb.AppendLine("// Do not modify it manually!");
					sb.AppendLine();	
					sb.AppendLine("using System;");
					sb.AppendLine("using UnityEngine;");
					sb.AppendLine();
					sb.AppendLine("namespace Glider.Core.SerializableData");
					sb.AppendLine("{");
					sb.AppendLine("	[Serializable]");
					if (!string.IsNullOrWhiteSpace(comment))
					{
						sb.AppendLine("    /// <summary>");
						sb.AppendLine($"    /// {comment}");
						sb.AppendLine("    /// </summary>");
					}
					if (string.IsNullOrWhiteSpace(wrapperSuperClassName))
						sb.AppendLine($"	public class {wrapperClassName}");
					else
						sb.AppendLine($"	public class {wrapperClassName} : {wrapperSuperClassName}");
					sb.AppendLine("	{");

					SheetDataWrappers[] sheetDataWrappers = this.sheetData.dataWrappers;

				
					foreach(SheetDataWrappers sheetDataWrapper in sheetDataWrappers){
						type = sheetDataWrapper.type;
						className = sheetDataWrapper.name;
						isArray = sheetDataWrapper.isArray;
						canReload = sheetDataWrapper.canReload;
						var pascalName =$"{char.ToUpper(className[0]) + className[1..]}";
						fulltype = $"{type}{(isArray ? "[]" : "")}";
						sb.AppendLine($"        [SerializeField] private {fulltype} {className};");
						sb.AppendLine($"        public {fulltype} {pascalName} => {className};");
						if(canReload)
							sb.AppendLine($"        public void Reload{pascalName}({fulltype} {className})=>this.{className} = {className};");
						sb.AppendLine();
					}
					sb.AppendLine("	}");
					sb.AppendLine("}");
					//await File.WriteAllTextAsync(Path.Combine(filepath, $"{wrapperClassName}.cs"), sb.ToString());
					//File.WriteAllText(Path.Combine(filepath, $"{wrapperClassName}.cs"), sb.ToString());

					CsFileInfo filesResultItem = new CsFileInfo(filepath, wrapperClassName, sb.ToString());
					filesResultList.Add(filesResultItem);
				}
				catch (Exception e)
				{
					// Debug.LogError($"[WriteSheetWrapperToCsFile] wrapperClassName:{wrapperClassName} className:{className}, filepath:{filepath}, y:{y})");
					Console.WriteLine($"[WriteSheetWrapperToCsFile] wrapperClassName:{wrapperClassName} className:{className}, filepath:{filepath}, y:{y})");
					throw e;
				}
				// AssetDatabase.Refresh();
			}
			// AssetDatabase.Refresh();
			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }
		
		// 시트로 돌아갈 시 사용
		public AutoCreateOutput WriteSheetStaticDataWrapperToCsFileLegacy(StaticDataCategory dataCategory)
		{
			IsProcessing = true;

            string sheetName = SheetNameSharedStaticDataWrapper;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            if (!this.googleSheetDataDictionary.ContainsKey(sheetName))
            {
                return new AutoCreateOutput(filesResultList);
            }
            {
                var table = this.googleSheetDataDictionary[sheetName];
                var height = table.Length;
                if (height >= 2)
				{
					string wrapperClassName = table[1][0].ToString();
					;
					string filepath = table[3][0].ToString();
					string classNamespace = table[5][0].ToString();
					string wrapperSuperClassName = table[7][0].ToString();
					string comment = table[9][0].ToString();

					string type = null;
					string fulltype = null;
					string className = null;
					bool isArray = false;
					bool canReload = false;
					int y = -1;
					try
					{
						var sb = new StringBuilder();
						sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
						sb.AppendLine("// Do not modify it manually!");
						sb.AppendLine();
						sb.AppendLine("using System;");
						sb.AppendLine("using UnityEngine;");
						sb.AppendLine();
						sb.AppendLine("namespace Glider.Core.SerializableData");
						sb.AppendLine("{");
						sb.AppendLine("	[Serializable]");
						if (!string.IsNullOrWhiteSpace(comment))
						{
							sb.AppendLine("    /// <summary>");
							sb.AppendLine($"    /// {comment}");
							sb.AppendLine("    /// </summary>");
						}
						if (string.IsNullOrWhiteSpace(wrapperSuperClassName))
							sb.AppendLine($"	public class {wrapperClassName}");
						else
							sb.AppendLine($"	public class {wrapperClassName} : {wrapperSuperClassName}");
						sb.AppendLine("	{");

						for (y = 11; y < height; y++)
						{
							if (table[y][0].ToString() == EOFIdentifier) break;
							if (!Convert.ToBoolean(table[y][1])) continue;
							type = table[y][2].ToString();
							className = table[y][3].ToString();
							isArray = Convert.ToBoolean(table[y][5]);
							canReload = Convert.ToBoolean(table[y][6]);
							var pascalName =$"{char.ToUpper(className[0]) + className[1..]}";
							fulltype = $"{type}{(isArray ? "[]" : "")}";
							sb.AppendLine($"        [SerializeField] private {fulltype} {className};");
							sb.AppendLine($"        public {fulltype} {pascalName} => {className};");
							if(canReload)
								sb.AppendLine($"        public void Reload{pascalName}({fulltype} {className})=>this.{className} = {className};");
							sb.AppendLine();
						}
						sb.AppendLine("	}");
						sb.AppendLine("}");
                        //await File.WriteAllTextAsync(Path.Combine(filepath, $"{wrapperClassName}.cs"), sb.ToString());
                        //File.WriteAllText(Path.Combine(filepath, $"{wrapperClassName}.cs"), sb.ToString());

                        CsFileInfo filesResultItem = new CsFileInfo(filepath, wrapperClassName, sb.ToString());
                        filesResultList.Add(filesResultItem);
                    }
					catch (Exception e)
					{
						// Debug.LogError($"[WriteSheetWrapperToCsFile] wrapperClassName:{wrapperClassName} className:{className}, filepath:{filepath}, y:{y})");
						Console.WriteLine($"[WriteSheetWrapperToCsFile] wrapperClassName:{wrapperClassName} className:{className}, filepath:{filepath}, y:{y})");
						throw e;
					}


					// AssetDatabase.Refresh();
				}

			}
			// AssetDatabase.Refresh();
			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }


public AutoCreateOutput WriteSheetStaticDataClassesToCsFile(StaticDataCategory dataCategory, string sheetName)
		{
			IsProcessing = true;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

			// this.sheetData.enums가 널이라면 
            if (this.sheetData.classes is null)
            {
                return new AutoCreateOutput(filesResultList);
            }


			SheetClasses[] sheetClasses = this.sheetData.classes;
			foreach(SheetClasses sheetClass in sheetClasses){
				string className = sheetClass.name;
				string filepath = sheetClass.csFilePath;
				string classNamespace = sheetClass.csNamespace;
				string comment = sheetClass.comment;
				try {
					var sb = new StringBuilder();
					sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
					sb.AppendLine("// Do not modify it manually!");
					sb.AppendLine();
					sb.AppendLine("using System;");
					//sb.AppendLine("using System.Collections.Generic;");
					sb.AppendLine("using UnityEngine;");
					sb.AppendLine();
					sb.AppendLine($"namespace {classNamespace}");
					sb.AppendLine("{");
					if (!string.IsNullOrWhiteSpace(comment))
					{
						sb.AppendLine("    /// <summary>");
						sb.AppendLine($"    /// {comment}");
						sb.AppendLine("    /// </summary>");
					}
					sb.AppendLine("	[Serializable]");
					sb.AppendLine($"	public class {className}");
					sb.AppendLine("	{");

					SheetClassesField[] fields = sheetClass.fields;
					foreach(SheetClassesField field in fields){
						var type = field.type;
						var name = field.name;
						var fieldComment = field.comment;
						if (!string.IsNullOrWhiteSpace(fieldComment))
						{
							sb.AppendLine("        /// <summary>");
							sb.AppendLine($"        /// {fieldComment}");
							sb.AppendLine("        /// </summary>");
						}
						sb.AppendLine($"        [SerializeField] private {type} {name};");
						sb.AppendLine($"        public {type} {char.ToUpper(name[0]) + name[1..]} => {name};");
						//sb.AppendLine($"        public {type} {char.ToUpper(name[0]) + name.Substring(1)} => {name};");
						sb.AppendLine();
					}
					sb.AppendLine("	}");
					sb.AppendLine("}");
					//await File.WriteAllTextAsync(Path.Combine(filepath, $"{className}.cs"), sb.ToString());
					//File.WriteAllText(Path.Combine(filepath, $"{className}.cs"), sb.ToString());


					CsFileInfo filesResultItem = new CsFileInfo(filepath, className, sb.ToString());
					filesResultList.Add(filesResultItem);
				} catch (Exception e) {
					Console.WriteLine($"[WriteSheetClassesToCsFile] className:{className}, filepath:{filepath}, classNamespace:{classNamespace}");
					// Debug.LogError($"[WriteSheetClassesToCsFile] className:{className}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
					throw e;
				}
			}

        
			// AssetDatabase.Refresh();

			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		// 시트로 롤백할 경우 사용
		public AutoCreateOutput WriteSheetStaticDataClassesToCsFileLegacy(StaticDataCategory dataCategory, string sheetName)
		{
			IsProcessing = true;
            
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            foreach (var SheetNameClass in SheetNameStaticDataClasses)
			{
                // SheetNameStaticDataClasses에 있는 시트인지 체크
                if (SheetNameClass != sheetName) continue;

                if (!this.googleSheetDataDictionary.ContainsKey(sheetName))
                {
                    continue;
                }


                var table = this.googleSheetDataDictionary[sheetName];

                var height = table.Length;

                
                if (height >= 3)
				{
					string className = null;
					string filepath = null;
					string classNamespace = null;
					string comment = null;
					int x = -1, y = -1;
					try
					{
						for (x = 0; x < table[0].Length; x += 3)
						{
							if (table[0][x].ToString() == EOFIdentifier)
							{
								break;
							}
							className = table[1][x]?.ToString();
							filepath = table[5][x]?.ToString();
							classNamespace = table[7][x]?.ToString();
							comment = x < table[9].Length ? table[9][x]?.ToString() : null;

							var sb = new StringBuilder();
							sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
							sb.AppendLine("// Do not modify it manually!");
							sb.AppendLine();
							sb.AppendLine("using System;");
							//sb.AppendLine("using System.Collections.Generic;");
							sb.AppendLine("using UnityEngine;");
							sb.AppendLine();
							sb.AppendLine($"namespace {classNamespace}");
							sb.AppendLine("{");
							if (!string.IsNullOrWhiteSpace(comment))
							{
								sb.AppendLine("    /// <summary>");
								sb.AppendLine($"    /// {comment}");
								sb.AppendLine("    /// </summary>");
							}
							sb.AppendLine("	[Serializable]");
							sb.AppendLine($"	public class {className}");
							sb.AppendLine("	{");

							for (y = 11; y < height; y++)
							{
								var key = table[y][x]?.ToString();
								if (key == EOFIdentifier)
								{
									break;
								}
								if (table[y][x].ToString() == EOFIdentifier) break;
								var type = table[y][x].ToString();
								var name = table[y][x + 1].ToString();
								var fieldComment = table[y][x + 2].ToString();
								if (!string.IsNullOrWhiteSpace(fieldComment))
								{
									sb.AppendLine("        /// <summary>");
									sb.AppendLine($"        /// {fieldComment}");
									sb.AppendLine("        /// </summary>");
								}
								sb.AppendLine($"        [SerializeField] private {type} {name};");
								sb.AppendLine($"        public {type} {char.ToUpper(name[0]) + name[1..]} => {name};");
								//sb.AppendLine($"        public {type} {char.ToUpper(name[0]) + name.Substring(1)} => {name};");
								sb.AppendLine();
							}
							sb.AppendLine("	}");
							sb.AppendLine("}");
                            //await File.WriteAllTextAsync(Path.Combine(filepath, $"{className}.cs"), sb.ToString());
                            //File.WriteAllText(Path.Combine(filepath, $"{className}.cs"), sb.ToString());


                            CsFileInfo filesResultItem = new CsFileInfo(filepath, className, sb.ToString());
                            filesResultList.Add(filesResultItem);
                        }

					}
					catch (Exception e)
					{
						Console.WriteLine($"[WriteSheetClassesToCsFile] className:{className}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
						// Debug.LogError($"[WriteSheetClassesToCsFile] className:{className}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
						throw e;
					}


				}
			}
			// AssetDatabase.Refresh();

			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }
		
		public AutoCreateOutput WriteSheetCloudDataStructsToCsFile(StaticDataCategory dataCategory)
		{
			IsProcessing = true;
            string sheetName = SheetNameClassCloudData;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

			CloudDataStructs[] cloudDataStructs = this.sheetData.cloudDataStructs;

			
            if (cloudDataStructs is null)
            {
                return new AutoCreateOutput(filesResultList);
            }

			string structName = null;
			string filepath = null;
			string classNamespace = null;
			string comment = null;
			int x = -1, y = -1;
			try
			{
				foreach(CloudDataStructs cloudDataStruct in cloudDataStructs)
				{
					structName = cloudDataStruct.name;
					filepath = cloudDataStruct.csFilePath;
					classNamespace = cloudDataStruct.csNamespace;
					comment = cloudDataStruct.comment;
					var sb = new StringBuilder();
					sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
					sb.AppendLine("// Do not modify it manually!");
					sb.AppendLine();
					sb.AppendLine("using System;");
					sb.AppendLine("using UnityEngine;");
					sb.AppendLine();
					sb.AppendLine($"namespace {classNamespace}");
					sb.AppendLine("{");
					if (!string.IsNullOrWhiteSpace(comment))
					{
						sb.AppendLine("    /// <summary>");
						sb.AppendLine($"    /// {comment}");
						sb.AppendLine("    /// </summary>");
					}
					sb.AppendLine("	[Serializable]");
					sb.AppendLine($"	public struct {structName}");
					sb.AppendLine("	{");

					var sbCrcFunc = new StringBuilder();
					sbCrcFunc.AppendLine("		public int CreateCrdCode()");
					sbCrcFunc.AppendLine("		{");
					sbCrcFunc.AppendLine("			var crc = 2147483647;");

					CloudDataStructsField[] fields = cloudDataStruct.fields;
					foreach(CloudDataStructsField field in fields)
					{
						string type = field.type;
						// var isArray = type[^2..] == "[]";
						bool isArray = field.isTypeArray;
						string name = field.name;
						var pascalName = char.ToUpper(name[0]) + name[1..];
						string fieldComment = field.comment;

						if (!string.IsNullOrWhiteSpace(fieldComment))
						{
							sb.AppendLine("		/// <summary>");
							sb.AppendLine($"		/// {fieldComment}");
							sb.AppendLine("		/// </summary>");
						}
						if (isArray)
						{
							var elementType = type[..^2];
							sb.AppendLine($"		[SerializeField] private {type} {name};");
							sb.AppendLine($"		public {type} Get{pascalName}()");
							sb.AppendLine("		{");
							sb.AppendLine($"			if(this.{name} == null) return null;");
							sb.AppendLine($"			var dest = new {elementType}[{name}.Length];");
							sb.AppendLine($"			Array.Copy({name}, dest, {name}.Length);");
							sb.AppendLine("			return dest;");
							sb.AppendLine("		}");
							sb.AppendLine($"		public void Set{pascalName}({type} value)");
							sb.AppendLine("		{");
							sb.AppendLine($"			this.{name} = value;");
							sb.AppendLine("		}");
					
							sbCrcFunc.AppendLine($"			for (int i = 0; i < {name}?.Length; i++) crc ^= i.GetHashCode() ^ {name}[i].GetHashCode();");
						}
						else
						{
							sb.AppendLine($"		public {type} {name};");
							if (type == "string")
								sbCrcFunc.AppendLine($"			if({name}!=null) crc ^= {name}.GetHashCode();");
							else
								sbCrcFunc.AppendLine($"			crc ^= {name}.GetHashCode();");
						}
						sb.AppendLine();
					}
					sbCrcFunc.AppendLine("			return crc;");
					sbCrcFunc.AppendLine("		}");

					sb.Append(sbCrcFunc.ToString());
					sb.AppendLine("	}");
					sb.AppendLine("}");
					CsFileInfo filesResultItem = new CsFileInfo(filepath, structName, sb.ToString());
					filesResultList.Add(filesResultItem);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"[WriteSheetClassesToCsFile] className:{structName}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
				// Debug.LogError($"[WriteSheetClassesToCsFile] className:{structName}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
				throw e;
			}
			// AssetDatabase.Refresh();
			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		// 시트로 롤백하면 사용
		public AutoCreateOutput WriteSheetCloudDataStructsToCsFileLegacy(StaticDataCategory dataCategory)
		{
			IsProcessing = true;
            string sheetName = SheetNameClassCloudData;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

            if (!this.googleSheetDataDictionary.ContainsKey(sheetName))
            {
                return new AutoCreateOutput(filesResultList);
            }
            var table = this.googleSheetDataDictionary[sheetName];

            var height = table.Length;

            if (height >= 3)
			{
				string structName = null;
				string filepath = null;
				string classNamespace = null;
				string comment = null;
				int x = -1, y = -1;
				try
				{
					for (x = 0; x < table[0].Length; x += 3)
					{
						if (table[0][x].ToString() == EOFIdentifier)
						{
							break;
						}
						structName = table[1][x]?.ToString();
						filepath = table[5][x]?.ToString();
						classNamespace = table[7][x]?.ToString();
						comment = x < table[9].Length ? table[9][x]?.ToString() : null;

						var sb = new StringBuilder();
						sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
						sb.AppendLine("// Do not modify it manually!");
						sb.AppendLine();
						sb.AppendLine("using System;");
						sb.AppendLine("using UnityEngine;");
						sb.AppendLine();
						sb.AppendLine($"namespace {classNamespace}");
						sb.AppendLine("{");
						if (!string.IsNullOrWhiteSpace(comment))
						{
							sb.AppendLine("    /// <summary>");
							sb.AppendLine($"    /// {comment}");
							sb.AppendLine("    /// </summary>");
						}
						sb.AppendLine("	[Serializable]");
						sb.AppendLine($"	public struct {structName}");
						sb.AppendLine("	{");

						var sbCrcFunc = new StringBuilder();
						sbCrcFunc.AppendLine("		public int CreateCrdCode()");
						sbCrcFunc.AppendLine("		{");
						sbCrcFunc.AppendLine("			var crc = 2147483647;");

						for (y = 11; y < height; y++)
						{
							var key = table[y][x]?.ToString();
							if (key == EOFIdentifier)
							{
								break;
							}
							if (table[y][x].ToString() == EOFIdentifier) break;
							var type = table[y][x].ToString();
							var isArray = type[^2..] == "[]";
							//var isArray = type.Substring(type.Length - 2) == "[]";
							var name = table[y][x + 1].ToString();
							var pascalName = char.ToUpper(name[0]) + name[1..];
							//var pascalName = char.ToUpper(name[0]) + name.Substring(1);
							var fieldComment = table[y][x + 2].ToString();
							if (!string.IsNullOrWhiteSpace(fieldComment))
							{
								sb.AppendLine("		/// <summary>");
								sb.AppendLine($"		/// {fieldComment}");
								sb.AppendLine("		/// </summary>");
							}
							if (isArray)
							{
								var elementType = type[..^2];
								sb.AppendLine($"		[SerializeField] private {type} {name};");
								sb.AppendLine($"		public {type} Get{pascalName}()");
								sb.AppendLine("		{");
								sb.AppendLine($"			if(this.{name} == null) return null;");
								sb.AppendLine($"			var dest = new {elementType}[{name}.Length];");
								sb.AppendLine($"			Array.Copy({name}, dest, {name}.Length);");
								sb.AppendLine("			return dest;");
								sb.AppendLine("		}");
								sb.AppendLine($"		public void Set{pascalName}({type} value)");
								sb.AppendLine("		{");
								sb.AppendLine($"			this.{name} = value;");
								sb.AppendLine("		}");
						
								sbCrcFunc.AppendLine($"			for (int i = 0; i < {name}?.Length; i++) crc ^= i.GetHashCode() ^ {name}[i].GetHashCode();");
							}
							else
							{
								sb.AppendLine($"		public {type} {name};");
								if (type == "string")
									sbCrcFunc.AppendLine($"			if({name}!=null) crc ^= {name}.GetHashCode();");
								else
									sbCrcFunc.AppendLine($"			crc ^= {name}.GetHashCode();");
							}
							sb.AppendLine();
						}
						sbCrcFunc.AppendLine("			return crc;");
						sbCrcFunc.AppendLine("		}");

						sb.Append(sbCrcFunc.ToString());
						sb.AppendLine("	}");
						sb.AppendLine("}");
                        CsFileInfo filesResultItem = new CsFileInfo(filepath, structName, sb.ToString());
                        filesResultList.Add(filesResultItem);
                    }

				}
				catch (Exception e)
				{
					Console.WriteLine($"[WriteSheetClassesToCsFile] className:{structName}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
					// Debug.LogError($"[WriteSheetClassesToCsFile] className:{structName}, filepath:{filepath}, classNamespace:{classNamespace}, ({x},{y})");
					throw e;
				}


			}
			// AssetDatabase.Refresh();

			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }
		

		public AutoCreateOutput WriteSheetEnumsToCsFile(StaticDataCategory dataCategory)
		{
            IsProcessing = true;

            string sheetName = SheetNameEnums;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();

		
			// this.sheetData.enums가 널이라면 
            if (this.sheetData.enums is null)
            {
                return new AutoCreateOutput(filesResultList);
            }


			SheetEnums[] sheetEnums = this.sheetData.enums;

			string enumName = null;
			string filepath = null;
			string enumNamespace = null;
			string comment = null;
			try{
				for(int enumIdx = 0; enumIdx <sheetEnums.Length; enumIdx++ ){
					SheetEnums targetEnum = sheetEnums[enumIdx];
					enumName = targetEnum.name;
					filepath = targetEnum.csFilePath;
					enumNamespace = targetEnum.csNamespace;
					comment = targetEnum.comment;
					SheetEnumsField[] fields = targetEnum.fields;
					var sb = new StringBuilder();
					sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
					sb.AppendLine("// Do not modify it manually!");
					sb.AppendLine($"namespace {enumNamespace}");
					sb.AppendLine("{");
					if (!string.IsNullOrWhiteSpace(comment))
					{
						sb.AppendLine("    /// <summary>");
						sb.AppendLine($"    /// {comment}");
						sb.AppendLine("    /// </summary>");
					}
					sb.AppendLine($"	public enum {enumName}");
					sb.AppendLine("	{");
					
					for(int fieldIdx = 0; fieldIdx < fields.Length; fieldIdx++){
						SheetEnumsField field = fields[fieldIdx];
						string key = field.key;
						int value = field.value;
						sb.AppendLine($"		{key} = {value},");
					}
					sb.AppendLine("		Undefined = -1");
					
					sb.AppendLine("	}");
					sb.AppendLine("}");
					//Console.WriteLine("sb.ToString()", sb.ToString());
					//await File.WriteAllTextAsync(Path.Combine(filepath, $"{enumName}.cs"), sb.ToString());
					//File.WriteAllText(Path.Combine(filepath, $"{enumName}.cs"), sb.ToString());

					CsFileInfo filesResultItem = new CsFileInfo(filepath, enumName, sb.ToString());
					filesResultList.Add(filesResultItem);
				}
			} catch (Exception e) {
				Console.WriteLine($"[WriteSheetEnumsToCsFile] enumName:{enumName}, filepath:{filepath}, enumNamespace:{enumNamespace}");
				// Debug.LogError($"[WriteSheetEnumsToCsFile] enumName:{enumName}, filepath:{filepath}, enumNamespace:{enumNamespace}, ({x},{y})");
				throw e;
			}

			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		// 시트로 롤백할 경우 사용
		public AutoCreateOutput WriteSheetEnumsToCsFileLagecy(StaticDataCategory dataCategory)
		{
            IsProcessing = true;

            string sheetName = SheetNameEnums;
            List<CsFileInfo> filesResultList = new List<CsFileInfo>();


            if (!this.googleSheetDataDictionary.ContainsKey(sheetName))
            {
                return new AutoCreateOutput(filesResultList);
            }


            var table = this.googleSheetDataDictionary[sheetName];
            var height = table.Length;
            if (height >= 3)
			{
				string enumName = null;
				string filepath = null;
				string enumNamespace = null;
				string comment = null;
				int x = -1, y = -1;
				try
				{
					for (x = 0; x < table[0].Length; x += 2)
					{
						if (table[0][x].ToString() == EOFIdentifier)
						{
							break;
						}
						enumName = table[1][x]?.ToString();
						filepath = table[3][x]?.ToString();
						enumNamespace = table[5][x]?.ToString();
						comment = x < table[7].Length ? table[7][x]?.ToString() : null;

						var sb = new StringBuilder();
						sb.AppendLine("// This file was automatically generated by Gameduo Center Manager.");
						sb.AppendLine("// Do not modify it manually!");
						sb.AppendLine($"namespace {enumNamespace}");
						sb.AppendLine("{");
						if (!string.IsNullOrWhiteSpace(comment))
						{
							sb.AppendLine("    /// <summary>");
							sb.AppendLine($"    /// {comment}");
							sb.AppendLine("    /// </summary>");
						}
						sb.AppendLine($"	public enum {enumName}");
						sb.AppendLine("	{");
						for (y = 9; y < height; y++)
						{
							var key = table[y][x]?.ToString();
							if (key == EOFIdentifier)
							{
								break;
							}
							var value = int.Parse(table[y][x + 1]?.ToString());
							sb.AppendLine($"		{key} = {value},");
						}
						sb.AppendLine("	}");
						sb.AppendLine("}");
                        //Console.WriteLine("sb.ToString()", sb.ToString());
                        //await File.WriteAllTextAsync(Path.Combine(filepath, $"{enumName}.cs"), sb.ToString());
                        //File.WriteAllText(Path.Combine(filepath, $"{enumName}.cs"), sb.ToString());

                        CsFileInfo filesResultItem = new CsFileInfo(filepath, enumName, sb.ToString());
                        filesResultList.Add(filesResultItem);

                    }
                    

                }
				catch (Exception e)
				{
					Console.WriteLine($"[WriteSheetEnumsToCsFile] enumName:{enumName}, filepath:{filepath}, enumNamespace:{enumNamespace}, ({x},{y})");
					// Debug.LogError($"[WriteSheetEnumsToCsFile] enumName:{enumName}, filepath:{filepath}, enumNamespace:{enumNamespace}, ({x},{y})");
					throw e;
				}


				// AssetDatabase.Refresh();
			}

			IsProcessing = false;
            return new AutoCreateOutput(filesResultList);
        }

		


		// private class GoogleSheetData
		// {
		// 	private GoogleSheetWasDataDto[] googleSheetWasData;

		// 	public GoogleSheetData(GoogleSheetWasDataDto[] googleSheetWasData)
		// 	{
		// 		this.googleSheetWasData = googleSheetWasData;

        //     }

		// }
	}
}
