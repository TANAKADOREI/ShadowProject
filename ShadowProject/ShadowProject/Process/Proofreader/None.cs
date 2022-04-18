using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectGenerator
    {
        private static void CopyFile(FileInfo file, FileStream source, FileStream dest)
        {
            byte[] buffer = NewBuffer;
            int read = 0;

            source.Seek(0, SeekOrigin.Begin);
            dest.Seek(0, SeekOrigin.Begin);

            while ((read = source.Read(buffer)) > 0)
            {
                dest.Write(buffer, 0, read);
            }
        }
    }
}
