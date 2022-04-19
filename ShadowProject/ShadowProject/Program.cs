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
        public static string ExeDirPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        static Dictionary<string, string> g_registered_sync_directories = new Dictionary<string, string>();

        static void LoadReigsteredDirs()
        {
            const string FILE = "targets.json";

            string path = Path.Combine(ExeDirPath(), FILE);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(g_registered_sync_directories));
            }

            g_registered_sync_directories = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
        }

        static void SaveRegisteredDirs()
        {
            const string FILE = "targets.json";
            string path = Path.Combine(ExeDirPath(), FILE);
            File.WriteAllText(path, JsonConvert.SerializeObject(g_registered_sync_directories));
        }

        static void CommandLineArgsProc(StringBuilder builder)//<- nickname
        {
            if (!g_registered_sync_directories.ContainsKey(builder.ToString()))
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, "not found", builder.ToString());
                return;
            }

            LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, builder.ToString(), g_registered_sync_directories[builder.ToString()]);
            Sync(builder.ToString(),g_registered_sync_directories[builder.ToString()]);
        }

        static void Main(string[] args)
        {
            LoadReigsteredDirs();

            if (args != null && args.Length > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (var a in args)
                {
                    builder.Append(a);
                    builder.Append(' ');
                }

                if (builder[builder.Length - 1] == ' ') builder.Remove(builder.Length - 1, 1);

                CommandLineArgsProc(builder);
                return;
            }

            //add menu func
            List<Tuple<string, Action>> functions = new List<Tuple<string, Action>>();
            functions.Add(new Tuple<string, Action>(nameof(ShowList), ShowList));
            functions.Add(new Tuple<string, Action>(nameof(Register), Register));
            functions.Add(new Tuple<string, Action>(nameof(SyncAll), SyncAll));
            functions.Add(new Tuple<string, Action>(nameof(Sync), Sync));
            functions.Add(new Tuple<string, Action>(nameof(DeleteShadow), DeleteShadow));
            functions.Add(new Tuple<string, Action>(nameof(DeleteAllShadow), DeleteAllShadow));
            functions.Add(new Tuple<string, Action>("Exit", () => { Environment.Exit(0); }));

            while (true)
            {
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
                LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS,"Done.","");
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

        private static T? ConsoleInput2<T>(string title, Converter<string, T> converter = null, Predicate<T> predicate = null) where T : struct
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

            if (predicate != null)
            {
                if (!predicate(result)) goto retry;
            }

            return result;
        }

        private static ShadowProjectProccessor GenSDWPP(string nickname,string dir)
        {
            return new ShadowProjectProccessor(nickname, new ShadowProjectProccessor.Handle()
            {
                Log = LOG,
                OriginalDirectory = dir,
                Retry = RETRY
            });
        }

        private static void LOG(ShadowProjectProccessor.Handle.LogLevel arg1, string arg2, string arg3)
        {
            ConsoleColor color = ConsoleColor.White;

            switch (arg1)
            {
                case ShadowProjectProccessor.Handle.LogLevel.NONE:
                    color = ConsoleColor.White;
                    break;
                case ShadowProjectProccessor.Handle.LogLevel.SUCCESS:
                    color = ConsoleColor.Blue;
                    break;
                case ShadowProjectProccessor.Handle.LogLevel.FAIL:
                    color = ConsoleColor.Red;
                    break;
                case ShadowProjectProccessor.Handle.LogLevel.IGNORE:
                    color = ConsoleColor.Yellow;
                    break;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write('[');
            Console.ForegroundColor = color;
            Console.Write(arg2);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(']');
            Console.ForegroundColor = color;
            Console.WriteLine(arg3);

            Console.ForegroundColor = ConsoleColor.White;
        }

        private static bool RETRY()
        {
        re:
            Console.Write("\nRetry?(y/n) : ");
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
                default:
                    goto re;
            }
        }

        private static void DeleteShadow(string nickname, string dir_path)
        {
            using (ShadowProjectProccessor proccessor = GenSDWPP(nickname, dir_path))
            {
                proccessor.Dispose();
                proccessor.DeleteShadow();
            }
        }

        private static void Sync(string nickname, string dir_path)
        {
            using (ShadowProjectProccessor proccessor = GenSDWPP(nickname, dir_path))
            {
                proccessor.Processing();
            }
        }

        private static void ShowList()
        {
            foreach (var i in g_registered_sync_directories)
            {
                Console.WriteLine($"nickname : {i.Key}, path : {i.Value}");
            }
        }

        private static void Sync()
        {
            LoadReigsteredDirs();
            var arr = g_registered_sync_directories.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Console.WriteLine($"[{i}] : {arr[i].Key}--{arr[i].Value}");
            }
            int? index = ConsoleInput2<int>("select index", _ => int.Parse(_), _ => 0 <= _ && _ < arr.Length);

            if (index == null) return;

            Sync(arr[index.Value].Key, arr[index.Value].Value);
        }

        private static void DeleteShadow()
        {
            LoadReigsteredDirs();
            var arr = g_registered_sync_directories.ToArray();
            for (int i = 0; i < arr.Length; i++)
            {
                Console.WriteLine($"[{i}] : {arr[i].Key}--{arr[i].Value}");
            }
            int? index = ConsoleInput2<int>("select index", _ => int.Parse(_), _ => 0 <= _ && _ < arr.Length);

            if (index == null) return;

            DeleteShadow(arr[index.Value].Key, arr[index.Value].Value);
            g_registered_sync_directories.Remove(arr[index.Value].Key);
            SaveRegisteredDirs();
        }

        private static void SyncAll()
        {
            LoadReigsteredDirs();
            foreach (var i in g_registered_sync_directories)
            {
                try
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, $"[{i.Key}]:{i.Value}", "Sync Begin");
                    Sync(i.Key,i.Value);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, $"[{i.Key}]:{i.Value}", "Sync End");
                }
                catch (Exception e)
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, $"[{i.Key}]:{i.Value}", e.ToString());
                }
            }
        }

        private static void DeleteAllShadow()
        {
            LoadReigsteredDirs();
            foreach (var i in g_registered_sync_directories)
            {
                try
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, $"[{i.Key}]:{i.Value}", "Delete Begin");
                    DeleteShadow(i.Key, i.Value);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, $"[{i.Key}]:{i.Value}", "Delete End");
                }
                catch (Exception e)
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, $"[{i.Key}]:{i.Value}", e.ToString());
                }
            }
        }

        private static void Register()
        {
            string dir_path = Path.GetFullPath(ConsoleInput("Source directory path to sync", _ => Directory.Exists(_)));
            string name = ConsoleInput("directory alias");
            LoadReigsteredDirs();
            try
            {
                if (g_registered_sync_directories.ContainsKey(name)) throw new Exception($"already exists name -> {name}");
                using (ShadowProjectProccessor proccessor = GenSDWPP(name,dir_path))
                {
                }
                g_registered_sync_directories.Add(name, dir_path);
                SaveRegisteredDirs();
                return;
            }
            catch (Exception e)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, e.StackTrace, e.ToString());
            }
        }
    }
}
