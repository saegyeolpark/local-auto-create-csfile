using System;
using System.Reflection.Metadata;

namespace glider.dto
{
    public class CsFileInfo
    {
        public String filePath;
        public String fileName;
        public String content;

        public CsFileInfo(String filePath, String fileName, String content)
        {
            this.filePath = filePath;
            this.fileName = fileName;
            this.content = content;
        }
    }
}

