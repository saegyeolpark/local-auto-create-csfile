using System;
namespace glider.dto
{
	public class AutoCreateOutput
	{
		public List<CsFileInfo> files;
		public AutoCreateOutput()
		{
			this.files = new List<CsFileInfo>();

		}

        public AutoCreateOutput(List<CsFileInfo> filesResultList)
		{
			this.files = filesResultList;

        }
	}
}

