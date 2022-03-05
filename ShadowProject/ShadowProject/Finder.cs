using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public static class Process
    {
        private static Manifest Manifest;

        private static List<DirectoryInfo> GetDirectories()
        {
        }

        //하위 파일은 건들지 않음
        private static void DirectoryProcessor(DirectoryInfo dir)
        {

        }

        private static void FileProcessor(FileInfo file)
        {

        }

        public static void Run(in Manifest manifest)
        {
            if (Manifest != null) return;
            Manifest = manifest;

            Run();

            Manifest = null;
        }

        private static void Run()
        {
            List<DirectoryInfo> target_dirs = GetDirectories();
        }
    }
}
