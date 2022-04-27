using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        private void CopyFile(FileInfo source, FileInfo dest)
        {
            File.Copy(source.FullName, dest.FullName, true);
        }
    }
}
