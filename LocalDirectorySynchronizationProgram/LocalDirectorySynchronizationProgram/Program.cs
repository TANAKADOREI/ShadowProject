using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LocalDirectorySynchronizationProgram
{
    class Program
    {
        const string MANIFEST_SAVE_DIR = "DATA";

        //Regular expression check in part order
        class Menifest
        {
            //only file name
            public bool UseFileNameRegex = true;
            public bool FileNameRegex__N = false;
            public string FileNameRegex = @"\w";

            //only extension string
            public bool UseExtRegex = false;
            public bool ExtRegex__N = false;
            public string ExtRegex = "";

            //name and ext
            public bool UseFileFullNameRegex = false;
            public bool FileFullNameRegex__N = false;
            public string FileFullNameRegex = "";

            //only dir name
            public bool UseDirNameRegex = false;
            public bool DirNameRegex__N = false;
            public string DirNameRegex = "";

            //file fullpath
            public bool UseFilePathRegex = false;
            public bool FilePathRegex__N = false;
            public string FilePathRegex = "";

            //dir fullpath
            public bool UseDirPathRegex = false;
            public bool DirPathRegex__N = false;
            public string DirPathRegex = "";
        }

        static void Main(string[] args)
        {
            List<Action> functions = new List<Action>();
            functions.Add(AddDir);
            functions.Add(RemoveDir);
            functions.Add(Sync);
            functions.Add(OpenSyncDirManifest);

            if (!Directory.Exists(MANIFEST_SAVE_DIR))
            {
                Directory.CreateDirectory(MANIFEST_SAVE_DIR);
            }
        }

        private static string GetManifestName(string target_path)
        {
            return Path.GetFullPath(
                        Path.Combine(MANIFEST_SAVE_DIR,
                        Convert.ToBase64String(Encoding.UTF32.GetBytes(target_path)).Replace('/', '_') + ".json"
                        )
                    );
        }

        private static Menifest LoadManifest(string target_path)
        {
            return JsonConvert.DeserializeObject<Menifest>(File.ReadAllText(GetManifestName(target_path)));
        }

        private static void SaveManifest(string target_path, Menifest menifest)
        {
            File.WriteAllText(GetManifestName(target_path), JsonConvert.SerializeObject(menifest));
        }

        private static void OpenSyncDirManifest()
        {
        }

        private static void Sync()
        {

        }

        private static void RemoveDir()
        {

        }

        private static void AddDir()
        {
        }
    }
}
