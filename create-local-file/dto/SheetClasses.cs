using System;
namespace glider.dto
{
	public class SheetClasses
    {
		public int id;
		public string name;
		public string csFilePath;
		public string csNamespace;
		public string comment;
		public bool isDefault;

		public SheetClassesField[] fields;


        public SheetClasses()
		{
		}
	}
}

