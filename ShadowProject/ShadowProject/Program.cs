using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShadowProject
{
    public class Program
    {
        const string MANIFEST_SAVE_DIR = "DATA";
        const string MANIFEST_DOT_EXT = ".json";

        static void Main(string[] args)
        {
            List<Tuple<string, Action>> functions = new List<Tuple<string, Action>>();
            functions.Add(new Tuple<string, Action>(nameof(OpenManifestDirectory), OpenManifestDirectory));
            functions.Add(new Tuple<string, Action>(nameof(CreateAndOpenManifest), CreateAndOpenManifest));
            functions.Add(new Tuple<string, Action>(nameof(SyncAll), SyncAll));
            functions.Add(new Tuple<string, Action>(nameof(Sync), Sync));

            while (true)
            {
                if (!Directory.Exists(MANIFEST_SAVE_DIR))
                {
                    Directory.CreateDirectory(MANIFEST_SAVE_DIR);
                }

                Console.WriteLine("Functions...");
                for (int i = 0; i < functions.Count; i++)
                {
                    Console.WriteLine($"[{i}] : {functions[i].Item1}");
                }

                int? index = ConsoleInput2("Select number", _ => int.Parse(_));

                if (index == null) goto end;

                if (0 <= index && index < functions.Count)
                {
                    try
                    {
                        functions[index.Value].Item2();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine();
                        Console.WriteLine("================");
                        Console.WriteLine(e);
                        Console.WriteLine("================");
                        Console.WriteLine();
                    }
                }

            end:
                Console.WriteLine("Done.");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static string ConsoleInput(string title, Predicate<string> predicate = null)
        {
        retry:
            Console.WriteLine("';' Type a semicolon to cancel");
            Console.Write($"{title} : ");
            string temp = Console.ReadLine();
            if (temp == null || temp.Length == 0) goto retry;
            if (temp == ";") return null;
            if (predicate != null && !predicate(temp)) goto retry;
            return temp;
        }

        private static T? ConsoleInput2<T>(string title, Converter<string, T> converter = null) where T : struct
        {
        retry:
            Console.WriteLine("';' Type a semicolon to cancel");
            Console.Write($"{title} : ");
            string temp = Console.ReadLine();

            if (temp == null || temp.Length == 0) goto retry;
            if (temp == ";") return null;

            T result;
            try
            {
                result = converter(temp);
            }
            catch
            {
                goto retry;
            }

            return result;
        }

        private static void Sync()
        {
            throw new NotImplementedException();
        }

        private static void SyncAll()
        {
            throw new NotImplementedException();
        }

        private static void CreateAndOpenManifest()
        {
            string filename = ConsoleInput("Profile name", (_) =>
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    if (_.Contains(c)) return false;
                }
                return true;
            });

            if (filename == null) return;

            string path = Path.Combine(MANIFEST_SAVE_DIR, filename + MANIFEST_DOT_EXT);

            path = Path.GetFullPath(path);

            if(File.Exists(path))
            {
                Console.WriteLine("already exist");
                return;
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(new Manifest(), Formatting.Indented), Encoding.UTF8);

            try
            {
                Process.Start(path);
            }
            catch
            {
                Console.WriteLine(path);
            }
        }

        private static void OpenManifestDirectory()
        {
            string path = Path.GetFullPath(MANIFEST_SAVE_DIR);

            try
            {
                Process.Start(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(path);
            }
        }
    }
}
