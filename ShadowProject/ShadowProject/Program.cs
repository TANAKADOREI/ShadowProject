using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LocalDirectorySynchronizationProgram
{
    class Program
    {
        const string MANIFEST_SAVE_DIR = "DATA";
        const string MANIFEST_DOT_EXT = ".json";

        //Regular expression check in part order
        class Manifest
        {
            public class Item
            {
                [JsonProperty("Enable")]
                public bool Enable = false;
            }

            public class Selector : Item
            {
                public class FileSearchRegex : Item
                {
                    //only file name
                    [JsonProperty("UseFileNameRegex")]
                    public bool UseFileNameRegex = true;
                    [JsonProperty("FileNameRegex__IgnoreMode")]
                    public bool FileNameRegex__N = false;
                    [JsonProperty("FileNameRegex")]
                    public string FileNameRegex = @"\w";

                    //only extension string
                    [JsonProperty("UseExtRegex")]
                    public bool UseExtRegex = false;
                    [JsonProperty("ExtRegex__IgnoreMode")]
                    public bool ExtRegex__N = false;
                    [JsonProperty("ExtRegex")]
                    public string ExtRegex = "";

                    //name and ext
                    [JsonProperty("UseFileFullNameRegex")]
                    public bool UseFileFullNameRegex = false;
                    [JsonProperty("FileFullNameRegex__IgnoreMode")]
                    public bool FileFullNameRegex__N = false;
                    [JsonProperty("FileFullNameRegex")]
                    public string FileFullNameRegex = "";

                    //file fullpath
                    [JsonProperty("UseFilePathRegex")]
                    public bool UseFilePathRegex = false;
                    [JsonProperty("FilePathRegex__IgnoreMode")]
                    public bool FilePathRegex__N = false;
                    [JsonProperty("FilePathRegex")]
                    public string FilePathRegex = "";
                }

                public class DirSearchRegex : Item
                {
                    //only dir name
                    [JsonProperty("UseDirNameRegex")]
                    public bool UseDirNameRegex = true;
                    [JsonProperty("DirNameRegex__IgnoreMode")]
                    public bool DirNameRegex__N = false;
                    [JsonProperty("DirNameRegex")]
                    public string DirNameRegex = @"\w";

                    //dir fullpath
                    [JsonProperty("UseDirPathRegex")]
                    public bool UseDirPathRegex = false;
                    [JsonProperty("DirPathRegex__IgnoreMode")]
                    public bool DirPathRegex__N = false;
                    [JsonProperty("DirPathRegex")]
                    public string DirPathRegex = "";
                }

                [JsonProperty("DirectorySelectionRegex")]
                public DirSearchRegex DirectorySelectionRegex = new DirSearchRegex();

                [JsonProperty("FileSelectionRegex")]
                public FileSearchRegex FileSelectionRegex = new FileSearchRegex();
            }

            public class Proofreader : Item
            {
                public class TextFile : Item
                {
                    public class CommentSearchRegex : Item
                    {
                        [JsonProperty("IsLineRegex")]
                        public bool IsLineRegex = true;

                        [JsonProperty("CommentSign")]
                        public string CommentSign = "";

                        [JsonProperty("CommentOpenSign")]
                        public string CommentOpenSign = "";

                        [JsonProperty("CommentCloseSign")]
                        public string CommentCloseSign = "";
                    }

                    public class EncodingConverter : Item
                    {
                        [JsonProperty("IsLineRegex")]
                        public bool IsLineRegex = true;

                        [JsonProperty("CommentSign")]
                        public string CommentSign = "";

                        [JsonProperty("CommentOpenSign")]
                        public string CommentOpenSign = "";

                        [JsonProperty("CommentCloseSign")]
                        public string CommentCloseSign = "";
                    }
                    [JsonProperty("Extensions")]
                    public string[] Extensions = { "txt", "json" };

                    [JsonProperty("NewlineChars")]
                    public string NewLineChar = "\n";

                    [JsonProperty("CommentRemover")]
                    public CommentSearchRegex CommentRemover = new CommentSearchRegex();

                    [JsonProperty("Encoding")]
                    public EncodingConverter Encoding = new EncodingConverter();
                }

                [JsonProperty("TextFile__IsNotDocFile")]
                public TextFile Target_TextFile = new TextFile();
            }

            [JsonProperty("SourceDirectories")]
            public string[] SourceDirectories = { "" };

            [JsonProperty("Selector")]
            public Selector m_selector = new Selector();

            [JsonProperty("Proofreader")]
            public Proofreader[] m_proofreader = { new Proofreader() };
        }

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
