using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectGenerator
    {
        private bool Predicate__CreatedDate(string source_file_path)
        {
            FileInfo dest = new FileInfo(ConvertSourceToDest(source_file_path));
            FileInfo 
        }

        private bool Predicate__LastModifiedDate(string source_file_path)
        {
        }

        private bool Predicate__LastAccessedDate(string source_file_path)
        {
        }

        private bool Predicate__Hash(string source_file_path)
        {
        }

        private bool Predicate__Size(string source_file_path)
        {
        }

    }
}
