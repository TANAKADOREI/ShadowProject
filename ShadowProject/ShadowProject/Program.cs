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

        static Dictionary<string, Tuple<string, string>> g_registered_sync_directories = new Dictionary<string, Tuple<string, string>>();

        static void LoadReigsteredDirs()
        {
            const string FILE = "targets.json";

            string path = Path.Combine(ExeDirPath(), FILE);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(g_registered_sync_directories));
            }

            g_registered_sync_directories = JsonConvert.DeserializeObject<Dictionary<string, Tuple<string, string>>>(File.ReadAllText(path));
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
            Sync(builder.ToString());
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
            functions.Add(new Tuple<string, Action>(nameof(RegisterDirectory), RegisterDirectory));
            functions.Add(new Tuple<string, Action>(nameof(RemoveRegisterDirectory), RemoveRegisterDirectory));
            functions.Add(new Tuple<string, Action>(nameof(RemoveRegisterAllDirectory), RemoveRegisterAllDirectory));
            functions.Add(new Tuple<string, Action>(nameof(Sync), Sync));
            functions.Add(new Tuple<string, Action>(nameof(SyncAll), SyncAll));
            functions.Add(new Tuple<string, Action>(nameof(EditRegistration), EditRegistration));
            functions.Add(new Tuple<string, Action>(nameof(RegistrationReplication), RegistrationReplication));
            functions.Add(new Tuple<string, Action>(nameof(ForceResetProgram), ForceResetProgram));
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
                LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Done.", "");
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

        private static ShadowProjectProccessor GenSDWP(string nickname)
        {
            try
            {
                return new ShadowProjectProccessor(nickname, new ShadowProjectProccessor.Handle()
                {
                    Log = LOG,
                    SourceDirectory = g_registered_sync_directories[nickname].Item1,
                    DestDirectory = g_registered_sync_directories[nickname].Item2,
                    Retry = RETRY,
                });
            }
            catch (Exception e)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.IGNORE, e.Message, e.ToString());
                return null;
            }
        }

        private static void LOG(ShadowProjectProccessor.Handle.LogLevel arg1, object arg2, object arg3)
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
                    LOG(ShadowProjectProccessor.Handle.LogLevel.IGNORE, "Ignored", "");
                    return false;
                default:
                    goto re;
            }
        }


        #region Input

        private static bool InputLogic<RESULT>(Nullable<RESULT> nullable, Action<RESULT> successed) where RESULT : struct
        {
            if (nullable != null) successed(nullable.Value);

            return nullable != null;
        }

        //func<index,msg>
        private static int? InputIndex(int size, Func<int, string> item)
        {
            for (int i = 0; i < size; i++)
            {
                Console.WriteLine($"[{i}] : {item(i)}");
            }
            return ConsoleInput2<int>("select index", _ => int.Parse(_), _ => 0 <= _ && _ < size);
        }

        #endregion

        #region Func

        private static void RegisterDirectory(string nickname, string source_dir_path, string dest_dir_path)
        {
            LoadReigsteredDirs();
            try
            {
                if (g_registered_sync_directories.ContainsKey(nickname))
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, "already exists", "");
                }

                g_registered_sync_directories.Add(nickname, new Tuple<string, string>(source_dir_path, dest_dir_path));

                GenSDWP(nickname).Dispose();

                SaveRegisteredDirs();
                return;
            }
            catch (Exception e)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, e.StackTrace, e.ToString());
            }
        }

        public static void EditRegistration(string old_nickname, string nickname, string source_dir_path, string dest_dir_path)
        {
            source_dir_path = Path.GetFullPath(source_dir_path);
            dest_dir_path = Path.GetFullPath(dest_dir_path);

            LoadReigsteredDirs();

            if (!g_registered_sync_directories.ContainsKey(old_nickname))
            {
                return;
            }

            var p = GenSDWP(old_nickname);
            p.Rename(nickname, ref p);

            g_registered_sync_directories.Remove(old_nickname);
            g_registered_sync_directories.Add(nickname, new Tuple<string, string>(source_dir_path, dest_dir_path));

            SaveRegisteredDirs();
        }

        private static void DeleteRegisterDirectory(string nickname)
        {
            if (!g_registered_sync_directories.ContainsKey(nickname)) return;

            ShadowProjectProccessor proccessor = GenSDWP(nickname);
            {
                if (proccessor == null) return;
                proccessor.Dispose();
                proccessor.Delete(ref proccessor);
            }

            g_registered_sync_directories.Remove(nickname);
        }

        private static void Sync(string nickname)
        {
            ShadowProjectProccessor proccessor = GenSDWP(nickname);
            {
                if (proccessor == null) return;
                proccessor.Processing(ref proccessor);
            }
        }

        #endregion

        #region Menu

        private static void ForceResetProgram()
        {
            LoadReigsteredDirs();
            g_registered_sync_directories.Clear();
            SaveRegisteredDirs();
            GC.Collect();
            GC.WaitForPendingFinalizers();
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
            InputLogic(InputIndex(arr.Length, i => $"{arr[i].Key}--{arr[i].Value}"),
                _ =>
                {
                    Sync(arr[_].Key);
                });
        }

        private static void RemoveRegisterDirectory()
        {
            LoadReigsteredDirs();
            var arr = g_registered_sync_directories.ToArray();
            InputLogic(InputIndex(arr.Length, i => $"{arr[i].Key}--{arr[i].Value}"),
                _ =>
                {
                    DeleteRegisterDirectory(arr[_].Key);
                    g_registered_sync_directories.Remove(arr[_].Key);
                    SaveRegisteredDirs();
                });
        }

        private static void SyncAll()
        {
            LoadReigsteredDirs();
            foreach (var i in g_registered_sync_directories)
            {
                try
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, $"[{i.Key}]:{i.Value}", "Sync Begin");
                    Sync(i.Key);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, $"[{i.Key}]:{i.Value}", "Sync End");
                }
                catch (Exception e)
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, $"[{i.Key}]:{i.Value}", e.ToString());
                }
            }
        }

        private static void RemoveRegisterAllDirectory()
        {
            LoadReigsteredDirs();
            foreach (var i in g_registered_sync_directories)
            {
                try
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, $"[{i.Key}]:{i.Value}", "Delete Begin");
                    DeleteRegisterDirectory(i.Key);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, $"[{i.Key}]:{i.Value}", "Delete End");
                    g_registered_sync_directories.Remove(i.Key);
                }
                catch (Exception e)
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, $"[{i.Key}]:{i.Value}", e.ToString());
                }
            }
            SaveRegisteredDirs();
        }

        private static void RegisterDirectory()
        {
            string source_dir_path = Path.GetFullPath(ConsoleInput("Source directory path to sync", _ => Directory.Exists(_)));
            string dest_dir_path = Path.GetFullPath(ConsoleInput("Dest directory path to sync", _ => Path.GetFullPath(_) != source_dir_path));

            string nickname = ConsoleInput("directory nickname",_=>!g_registered_sync_directories.ContainsKey(_));
            RegisterDirectory(nickname, source_dir_path, dest_dir_path);
        }

        private static void RegistrationReplication()
        {
            LoadReigsteredDirs();

            var arr = g_registered_sync_directories.ToArray();
            InputLogic(InputIndex(arr.Length, i => $"{arr[i].Key}--{arr[i].Value}"),
                _ =>
                {
                    var p = GenSDWP(arr[_].Key);
                    string new_name = $"{arr[_].Key}-Duplicated";
                    p.DuplicateInfo(new_name,ref p);

                    g_registered_sync_directories.Add(new_name, arr[_].Value);
                    SaveRegisteredDirs();
                });
        }

        private static void EditRegistration()
        {
            LoadReigsteredDirs();

            var arr = g_registered_sync_directories.ToArray();
            InputLogic(InputIndex(arr.Length, i => $"{arr[i].Key}--{arr[i].Value}"),
                _ =>
                {
                    string new_nickname = ConsoleInput("rename(nickname)",_=>!g_registered_sync_directories.ContainsKey(_));
                    string new_source_path = ConsoleInput("source path",_=>Directory.Exists(_));
                    string new_dest_path = ConsoleInput("dest path",_=>Path.GetFullPath(_) != Path.GetFullPath(new_source_path));

                    if (new_dest_path == null || new_source_path == null || new_nickname == null) return;

                    EditRegistration(arr[_].Key,new_nickname,Path.GetFullPath(new_source_path),Path.GetFullPath(new_dest_path));

                    SaveRegisteredDirs();
                });
        }
        #endregion
    }
}
