using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Updater
{
    class Program
    {
        private static void CopyDirectory(string source_dir_path, string dest_dir_path)
        {
            if (!Directory.Exists(dest_dir_path)) Directory.CreateDirectory(dest_dir_path);

            foreach (string dirPath in Directory.GetDirectories(source_dir_path, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(source_dir_path, dest_dir_path));
            }

            foreach (string newPath in Directory.GetFiles(source_dir_path, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(source_dir_path, dest_dir_path), true);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Waiting...");
            Thread.Sleep(5000);
            CopyDirectory(args[0], args[1]);
            Console.WriteLine("Done");
        }
    }
}
