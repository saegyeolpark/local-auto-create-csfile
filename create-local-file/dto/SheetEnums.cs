using System;
namespace glider.dto
{
	public class SheetEnums
    {
		public int id;
		public string name;
		public string csFilePath;
		public string csNamespace;
		public string comment;
		public bool isDefault;

		public SheetEnumsField[] fields;


        public SheetEnums()
		{
		}
	}
}

