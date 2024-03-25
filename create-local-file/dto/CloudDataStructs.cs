using System;
namespace glider.dto
{
	public class CloudDataStructs
    {
		public int id;
		public string type;
		public string name;
		public string csFilePath;
		public string csNamespace;
		public string comment;
		public bool isDefault;

		public CloudDataStructsField[] fields;


        public CloudDataStructs()
		{
		}
	}
}

