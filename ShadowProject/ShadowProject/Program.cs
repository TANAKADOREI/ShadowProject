using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ShadowProject
{
    public class Profile
    {
        public class Config
        {
            public uint ThreadCount = 4;
            public int StringBuilderPoolCapacity = 16;
            public int BufferSize = 4096;
            public int BufferPoolCapacity = 16;
        }

        public const string PROFILES_DIR_NAME = "PROFILES";

        public const string FILE_DB = "DB.sqlite";
        public const string FILE_MANIFEST = "MANIFEST.json";
        public const string FILE_CONFIG = "CONFIG.json";

        public string Name;

        public Lazy<Manifest> Manifest;
        public Lazy<Config> ProcConfig;
        public Lazy<SQLiteConnection> DB;
    }

    public class Program
    {
        public static string ExeDirPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private static T GetJsonDataFile<T>(string profile, string name, T only_create__instance = default) where T : new()
        {
            string path = null;

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;

            if (File.Exists(path = Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, profile, name)))
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path), settings);
            }
            else
            {
                T obj = only_create__instance == null ? new T() : only_create__instance;
                File.WriteAllText(path, JsonConvert.SerializeObject(obj, settings));
                return obj;
            }
        }

        private static SQLiteConnection CreateOrOpenDB(string profile, string name, out bool created)
        {
            name = Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, profile, name);
            created = false;
            if (!File.Exists(name))
            {
                created = true;
                SQLiteConnection.CreateFile(name);
            }

            var conn = new SQLiteConnection($"Data Source={name};Version=3;");
            conn.Open();

            return conn;
        }

        private static void CloseDB(SQLiteConnection db)
        {
            db.Close();
            db.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        static bool ContainsProfileName(string name)
        {
            return Directory.GetDirectories(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME)).Select(_ => Path.GetFileName(_)).Contains(name);
        }

        static void CreateProfile(string name)
        {
            Directory.CreateDirectory(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, name));
            var profile = LoadAndFindProfile(name);
            BeginSDWP(profile);
            EndSDWP(profile);
        }

        static List<Profile> LoadProfiles()
        {
            List<Profile> list = new List<Profile>();

            string profile_dir = Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME);

            if (!Directory.Exists(profile_dir))
            {
                Directory.CreateDirectory(profile_dir);
            }

            foreach (var d in Directory.GetDirectories(profile_dir))
            {
                Profile profile = new Profile();
                profile.Name = Path.GetFileName(d);

                profile.ProcConfig = new Lazy<Profile.Config>(() => GetJsonDataFile<Profile.Config>(profile.Name, Profile.FILE_CONFIG));
                profile.Manifest = new Lazy<Manifest>(() => GetJsonDataFile<Manifest>(profile.Name, Profile.FILE_MANIFEST));
                profile.DB = new Lazy<SQLiteConnection>(() =>
               {
                   bool created_db;
                   return CreateOrOpenDB(profile.Name, Profile.FILE_DB, out created_db);
               });

                list.Add(profile);
            }

            return list;
        }

        static Profile LoadAndFindProfile(string name)
        {
            return FindProfile(LoadProfiles(), name);
        }

        static Profile FindProfile(List<Profile> profiles, string name)
        {
            return profiles.Find(_ => _.Name == name);
        }

        static void DuplicateProfile(string source_profile, string dest_profile)
        {
            CopyDirectory(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, source_profile),
                Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, dest_profile));
        }

        static void RenameProfile(string old, string rename)
        {
            Directory.Move(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, old),
                Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, rename));
        }

        static void ClearProfiles()
        {
            Directory.Delete(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME), true);
        }

        static void DeleteProfile(string name)
        {
            Directory.Delete(Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME, name), true);
        }

        static void CommandLineArgsProc(StringBuilder builder)//<- nickname
        {
            string profile_name = builder.ToString();

            var p = LoadAndFindProfile(profile_name);

            if (p == null)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, "not found", profile_name);
                return;
            }

            LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, profile_name, "");
            Sync(p);
        }

        static void Main(string[] args)
        {
            LoadProfiles();

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
            functions.Add(new Tuple<string, Action>(nameof(NewProfile), NewProfile));
            functions.Add(new Tuple<string, Action>(nameof(DeleteProfile), DeleteProfile));
            functions.Add(new Tuple<string, Action>(nameof(DeleteAllProfiles), DeleteAllProfiles));
            functions.Add(new Tuple<string, Action>(nameof(Sync), Sync));
            functions.Add(new Tuple<string, Action>(nameof(SyncAll), SyncAll));
            functions.Add(new Tuple<string, Action>(nameof(RenameProfile), RenameProfile));
            functions.Add(new Tuple<string, Action>(nameof(DuplicateProfile), DuplicateProfile));
            functions.Add(new Tuple<string, Action>(nameof(ForceResetProgram), ForceResetProgram));
            functions.Add(new Tuple<string, Action>(nameof(Preview_Directories), Preview_Directories));
            functions.Add(new Tuple<string, Action>(nameof(Preview_Files), Preview_Files));

            functions.Add(new Tuple<string, Action>("TryOpenProfileDirectory", () =>
            {
                try { Process.Start("explorer.exe", Path.Combine(ExeDirPath(), Profile.PROFILES_DIR_NAME)); }
                catch { LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, "Fail", ""); }
            }));
            functions.Add(new Tuple<string, Action>("Exit", () => { Environment.Exit(0); }));

            while (true)
            {
                Console.WriteLine("============Menu============");
                var arr = functions;
                var r = InputLogic(InputIndex(arr.Count, i => arr[i].Item1),
                    _ =>
                    {
                        arr[_].Item2();
                    });

                if (!r) goto end;

                end:
                LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Done.", "");
                Console.ReadLine();
                Console.Clear();
                LoadProfiles();
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

        private static ShadowProjectProccessor BeginSDWP(Profile profile)
        {
            try
            {
                ShadowProjectProccessor proccessor = new ShadowProjectProccessor();
                proccessor.Start(new ShadowProjectProccessor.Handle()
                {
                    Log = LOG,
                    Retry = RETRY,
                }, profile);

                return proccessor;
            }
            catch (Exception e)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.IGNORE, e.Message, e.ToString());
                return null;
            }
        }

        private static void EndSDWP(Profile profile)
        {
            CloseDB(profile.DB.Value);
            profile = null;
            GC.Collect();
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
            Console.Write('\t');
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
                LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, i, item(i));
            }
            return ConsoleInput2<int>("select index", _ => int.Parse(_), _ => 0 <= _ && _ < size);
        }

        #endregion

        #region Func

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

        private static void Sync(Profile profile)
        {
            if (profile == null)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.IGNORE, "profile is null", "");
                return;
            }

            ShadowProjectProccessor proccessor = BeginSDWP(profile);
            {
                if (proccessor == null) return;
                proccessor.Processing(ref proccessor);
            }
            EndSDWP(profile);
        }

        #endregion

        #region Menu

        private static void Preview_Directories()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => arr[i].Name),
                _ =>
                {
                    var p = BeginSDWP(arr[_]);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.DirectorySelectionRegex.Regex__DirNameRegex),
                        p.Manifest.Selection.DirectorySelectionRegex.Regex__DirNameRegex);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.DirectorySelectionRegex.Regex__DirPathRegex),
                        p.Manifest.Selection.DirectorySelectionRegex.Regex__DirPathRegex);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.DirectorySelectionRegex.Regex__RelativePathDirNameRegex),
                        p.Manifest.Selection.DirectorySelectionRegex.Regex__RelativePathDirNameRegex);
                    foreach (var d in p.PreviewTargetDirs().Item1)
                    {
                        LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, d, "");
                    }
                    EndSDWP(arr[_]);
                });
        }

        private static void Preview_Files()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => arr[i].Name),
                _ =>
                {
                    var p = BeginSDWP(arr[_]);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.FileSelectionRegex.Regex__ExtRegex),
                        p.Manifest.Selection.FileSelectionRegex.Regex__ExtRegex);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.FileSelectionRegex.Regex__FileFullNameRegex),
                        p.Manifest.Selection.FileSelectionRegex.Regex__FileFullNameRegex);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.FileSelectionRegex.Regex__FileNameRegex),
                        p.Manifest.Selection.FileSelectionRegex.Regex__FileNameRegex);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, nameof(p.Manifest.Selection.FileSelectionRegex.Regex__FilePathRegex),
                        p.Manifest.Selection.FileSelectionRegex.Regex__FilePathRegex);
                    foreach (var f in p.PreviewTargetFiles())
                    {
                        LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, f, "");
                    }
                    EndSDWP(arr[_]);
                });
        }

        private static void ForceResetProgram()
        {
            ClearProfiles();
            GC.Collect();
        }

        private static void ShowList()
        {
            foreach (var i in LoadProfiles())
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, i.Name, "");
            }
        }

        private static void Sync()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => arr[i].Name),
                _ =>
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Start", arr[_].Name);
                    Sync(arr[_]);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Complete", arr[_].Name);
                });
        }

        private static void SyncAll()
        {

            foreach (var i in LoadProfiles())
            {
                try
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, i.Name, "Sync Begin");
                    Sync(i);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, i.Name, "Sync End");
                }
                catch (Exception e)
                {
                    LOG(ShadowProjectProccessor.Handle.LogLevel.FAIL, i.Name, e.ToString());
                }
            }
        }

        private static void DeleteProfile()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => $"{arr[i].Name}"),
                _ =>
                {
                    DeleteProfile(arr[_].Name);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Delete", arr[_].Name);
                });
        }

        private static void DeleteAllProfiles()
        {
            ClearProfiles();
        }

        private static void NewProfile()
        {
            string profile = ConsoleInput("profile name", _ => !ContainsProfileName(_));
            if (profile == null)
            {
                LOG(ShadowProjectProccessor.Handle.LogLevel.IGNORE, "Canceled", "");
                return;
            }
            CreateProfile(profile);
        }

        private static void DuplicateProfile()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => $"{arr[i].Name}"),
                _ =>
                {
                    string new_name = $"{arr[_].Name}-Duplicated-{DateTime.UtcNow.Ticks}";
                    DuplicateProfile(arr[_].Name, new_name);
                    LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, "Created", new_name);
                });
        }

        private static void RenameProfile()
        {
            var arr = LoadProfiles();
            InputLogic(InputIndex(arr.Count, i => $"{arr[i].Name}"),
                _ =>
                {
                    string rename = ConsoleInput("Rename", _ => !ContainsProfileName(_));
                    if (rename != null)
                    {
                        RenameProfile(arr[_].Name, rename);
                        LOG(ShadowProjectProccessor.Handle.LogLevel.SUCCESS, arr[_].Name, "->" + rename);
                    }
                    else
                    {
                        LOG(ShadowProjectProccessor.Handle.LogLevel.NONE, "Canceled", "");
                    }
                });
        }
        #endregion
    }
}
