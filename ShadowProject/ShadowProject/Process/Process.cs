using Newtonsoft.Json;
using ShadowProject.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor : IDisposable
    {
        public class Config
        {
            public uint ThreadCount = 4;
            public int StringBuilderPoolCapacity = 16;
            public int BufferSize = 4096;
            public int BufferPoolCapacity = 16;
        }

        public class Handle
        {
            public enum LogLevel
            {
                NONE,
                SUCCESS,
                FAIL,
                IGNORE,
            }

            public string OriginalDirectory;
            //<level,tag,msg>
            public Action<LogLevel, string, string> Log;
            public Func<bool> Retry;//retry : true, ignore : false
        }

        private TaskPool TaskQueue;
        private Pool<StringBuilder> StringBuilderPool;
        private Pool<byte[]> BufferPool;
        private bool Running = false;

        private Handle m_handle;
        private Config m_config;
        private Manifest m_manifest;

        private string NICKNAME;

        public ShadowProjectProccessor(string nickname, Handle handle)
        {
            NICKNAME = $"{nickname}--";
            m_handle = handle;
            OpenDB();
            UpdateSDWP();
        }

        ~ShadowProjectProccessor()
        {
            Dispose();
        }

        public void Dispose()
        {
            CloseDB();
        }

        private string GetSDWPDirPath(string source_dir_path)
        {
            string path = Path.GetDirectoryName(Path.GetFullPath(source_dir_path));
            path = Path.Combine(path, "__SDWP_PROFILE__");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        private string GetSDWPFilePath(string name)
        {
            return Path.GetFullPath(Path.Combine(GetSDWPDirPath(m_handle.OriginalDirectory), name));
        }

        private T GetJsonDataFile<T>(string name, T only_create__instance = default) where T : new()
        {
            string path = null;
            if (File.Exists(path = Path.Combine(GetSDWPDirPath(m_handle.OriginalDirectory), name)))
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            else
            {
                T obj = only_create__instance == null ? new T() : only_create__instance;
                File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
                return obj;
            }
        }

        public void UpdateSDWP()
        {
            if (Running) return;

            m_config = GetJsonDataFile<Config>(NICKNAME + "CONFIG.json");
            m_manifest = GetJsonDataFile<Manifest>(NICKNAME + "MANIFEST.json", new Manifest()
            {
                SourceDirectory = Path.GetFullPath(m_handle.OriginalDirectory),
                DestDirectory = Path.GetFullPath(m_handle.OriginalDirectory)
            });

            m_manifest.SourceDirectory = Path.GetFullPath(m_manifest.SourceDirectory);
            m_manifest.DestDirectory = Path.GetFullPath(m_manifest.DestDirectory);

            TaskQueue = new TaskPool(m_config.ThreadCount);
            StringBuilderPool = new Pool<StringBuilder>(() => new StringBuilder(), m_config.StringBuilderPoolCapacity);
            BufferPool = new Pool<byte[]>(() => new byte[m_config.BufferSize], m_config.BufferPoolCapacity);
        }

        public void DeleteShadow()
        {
            string path = GetSDWPFilePath(NICKNAME + "CONFIG.json");
            if (File.Exists(path)) File.Delete(path);

            path = GetSDWPFilePath(NICKNAME + "MANIFEST.json");
            if (File.Exists(path)) File.Delete(path);

            path = GetSDWPFilePath(NICKNAME + SQLDB_NAME);
            if (File.Exists(path)) File.Delete(path);

            if (Directory.Exists(GetSDWPDirPath(m_handle.OriginalDirectory)))
            {
                if(Directory.GetFiles(GetSDWPDirPath(m_handle.OriginalDirectory)).Length== 0)
                {
                    Directory.Delete(GetSDWPDirPath(m_handle.OriginalDirectory));
                }
            }
        }

        private bool IF_INVERSE(bool logic_result, bool n)
        {
            if (n)
            {
                return !logic_result;
            }
            else
            {
                return logic_result;
            }
        }

        private bool REGEX(string input, string pattern)
        {
            return Regex.IsMatch(input, pattern);
        }

        private RESULT LOGIC<RESULT>(string Logic, Func<RESULT> successed, params Tuple<int, Func<bool?>>[] lam)
        {
            List<bool> rs = new List<bool>();

            foreach (var box in from l in lam orderby l.Item1 ascending select l)
            {
            re:
                bool? result = null;
                try
                {
                    result = box.Item2();
                }
                catch (Exception e)
                {
                    m_handle.Log(Handle.LogLevel.FAIL, e.StackTrace, e.ToString());

                    if (m_handle.Retry()) goto re;
                }

                if (result == null) continue;

                rs.Add(result.Value);
            }

            if (Logic == Manifest.LogicItem.LOGIC_OR)
            {
                if (rs.Count == 0)
                {
                    return successed();
                }
                else if (rs.Contains(true))
                {
                    return successed();
                }
            }
            else if (Logic == ShadowProject.Manifest.LogicItem.LOGIC_AND)
            {
                if (rs.Count == 0)
                {
                    return successed();
                }
                else if (rs.Contains(false))
                {
                    return successed();
                }
            }
            else
            {
                throw new Exception("unknown logic");
            }

            return default;
        }

        //동기화될 파일은 미리보기를 지원하지 않는다 모든 파일을 다 입력하면 메모리를 많이 먹을것으로 예측되므로 굳지 추가 하지 않음
        public Tuple<List<string>, List<string>> PreviewTargetDirs()
        {
            return GetDirectories(new DirectoryInfo(m_manifest.SourceDirectory));
        }

        public void Processing()
        {
            if (Running) return;

            Running = true;

            var target_dirs = PreviewTargetDirs();

            {
                foreach (var d in target_dirs.Item1)
                {
                    FindTargetFilesAndSync(new DirectoryInfo(d));
                }
            }

            //디렉터리 청소
            {
                foreach (string dest_dir in Directory.EnumerateDirectories(m_manifest.DestDirectory, "*", SearchOption.AllDirectories))
                {
                    if (target_dirs.Item1.Find(_ => _ == dest_dir) == null)
                    {
                        //Remove if mirrored directory does not exist
                        if (Directory.Exists(dest_dir))
                            Directory.Delete(dest_dir, true);
                    }
                }

                OptimizeDirectory(new DirectoryInfo(m_manifest.DestDirectory));
            }

            Running = false;
        }

        private void OptimizeDirectory(DirectoryInfo info)
        {
            try
            {
                foreach (var d in info.EnumerateDirectories())
                {
                    var subdirs = d.GetDirectories();

                    if (subdirs.Length == 0 && d.GetFiles().Length == 0) d.Delete();

                    foreach (var sd in subdirs)
                    {
                        OptimizeDirectory(sd);
                    }
                }
            }
            catch (Exception e)
            {
                m_handle.Log(Handle.LogLevel.IGNORE, nameof(OptimizeDirectory), e.ToString());
            }
        }

        //<source,dest> path
        private Tuple<List<string>, List<string>> GetDirectories(DirectoryInfo root)
        {
            Tuple<List<string>, List<string>> targets = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
            var regex = m_manifest.Selection.DirectorySelectionRegex;

            targets.Item1.Add(Path.GetFullPath(root.FullName));
            targets.Item2.Add(ConvertSourceToDest(root.FullName));

            foreach (var sub in root.EnumerateDirectories("*", SearchOption.AllDirectories))
            {

                LOGIC(regex.Logic, () =>
                {
                    targets.Item1.Add(Path.GetFullPath(sub.FullName));
                    targets.Item2.Add(ConvertSourceToDest(sub.FullName));
                    return (object)null;
                }, new Tuple<int, Func<bool?>>
                (
                    m_manifest.Selection.DirectorySelectionRegex.Priority__DirNameRegex,
                    () =>
                    {
                        if (m_manifest.Selection.DirectorySelectionRegex.Use__UseDirNameRegex)
                        {
                            string nobase_relative_path = ConvertAbsToNoBaseRel(m_manifest.SourceDirectory, sub.FullName);
                            return IF_INVERSE(REGEX(nobase_relative_path, m_manifest.Selection.DirectorySelectionRegex.Regex__DirNameRegex),
                                m_manifest.Selection.DirectorySelectionRegex.N__DirNameRegex);
                        }
                        else
                        {
                            return null;
                        }
                    }
                ), new Tuple<int, Func<bool?>>
                (
                    m_manifest.Selection.DirectorySelectionRegex.Priority__DirPathRegex,
                    () =>
                    {
                        if (m_manifest.Selection.DirectorySelectionRegex.Use__UseDirPathRegex)
                        {
                            string nobase_relative_path = ConvertAbsToNoBaseRel(m_manifest.SourceDirectory, sub.FullName);
                            return IF_INVERSE(REGEX(nobase_relative_path, m_manifest.Selection.DirectorySelectionRegex.Regex__DirPathRegex),
                                m_manifest.Selection.DirectorySelectionRegex.N__DirPathRegex);
                        }
                        else
                        {
                            return null;
                        }
                    }
                ), new Tuple<int, Func<bool?>>
                (
                    m_manifest.Selection.DirectorySelectionRegex.Priority__RelativePathDirNameRegex,
                    () =>
                    {
                        if (m_manifest.Selection.DirectorySelectionRegex.Use__UseRelativePathDirNameRegex)
                        {
                            string nobase_relative_path = ConvertAbsToNoBaseRel(m_manifest.SourceDirectory, sub.FullName);
                            return IF_INVERSE(REGEX(nobase_relative_path, m_manifest.Selection.DirectorySelectionRegex.Regex__RelativePathDirNameRegex),
                                m_manifest.Selection.DirectorySelectionRegex.N__RelativePathDirNameRegex);
                        }
                        else
                        {
                            return null;
                        }
                    }
                )
                );
            }

            return targets;
        }

        //하위 파일은 건들지 않음
        //해당폴더 내에서 일어남
        private void FindTargetFilesAndSync(DirectoryInfo dir)
        {
            HashSet<string> updated_dest_files = new HashSet<string>();


            foreach (var f in dir.EnumerateFiles())
            {
                TaskQueue.Add(() =>
                {
                    string dest_file = null;
                    PredicateCheckAndSync(f, out dest_file);

                    if (dest_file != null) updated_dest_files.Add(dest_file);
                });
            }

            TaskQueue.WaitForRemainTask();

            DirectoryInfo dest = new DirectoryInfo(ConvertSourceToDest(dir.FullName));
            foreach (FileInfo dest_file in dest.EnumerateFiles())
            {
                //업데이트 목록에 있는가? || 삭제된 파일은 아닌지?
                if (!updated_dest_files.Contains(Path.GetFullPath(dest_file.FullName)) || !File.Exists(ConvertDestToSource(dest_file.FullName)))
                {
                re:
                    try
                    {
                        dest_file.Delete();
                    }
                    catch (Exception e)
                    {
                        m_handle.Log(Handle.LogLevel.FAIL, e.GetType().Name, e.Message);
                        if (m_handle.Retry()) goto re;
                    }
                }
            }
        }

        private bool PredicateCheckAndSync(FileInfo source_file, out string dest_file)
        {
            var regex = m_manifest.Selection.FileSelectionRegex;

            var result = LOGIC(m_manifest.Selection.FileSelectionRegex.Logic, () =>
            {
                FileInfo _dest_file;
                DirectoryInfo _dest_parent;
                bool r = FileProcessing(source_file, out _dest_file, out _dest_parent);

                return new { result = r, file = _dest_file, dir = _dest_parent };
            }, new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__ExtRegex,
                () =>
                {
                    if (!regex.Use__ExtRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Name + source_file.Extension, regex.Regex__ExtRegex), regex.N__ExtRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileFullNameRegex,
                () =>
                {
                    if (!regex.Use__FileFullNameRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Name + source_file.Extension, regex.Regex__FileFullNameRegex), regex.N__FileFullNameRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileNameRegex,
                () =>
                {
                    if (!regex.Use__FileNameRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Name + source_file.Extension, regex.Regex__FileNameRegex), regex.N__FileNameRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FilePathRegex,
                () =>
                {
                    if (!regex.Use__FilePathRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Name + source_file.Extension, regex.Regex__FilePathRegex), regex.N__FilePathRegex);
                }
            ),


            new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileInfo__CompareSize,
                () =>
                {
                    if (!regex.Use__FileInfo__CompareSize) return null;
                    return IF_INVERSE(!ComparePredicate__Size(source_file), regex.N__FileInfo__CompareSize);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileInfo__CompareCreatedDate,
                () =>
                {
                    if (!regex.Use__FileInfo__CompareCreatedDate) return null;
                    return IF_INVERSE(!ComparePredicate__CreatedDate(source_file), regex.N__FileInfo__CompareCreatedDate);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileInfo__CompareHash,
                () =>
                {
                    if (!regex.Use__FileInfo__CompareHash) return null;
                    return IF_INVERSE(!ComparePredicate__Hash(source_file), regex.N__FileInfo__CompareHash);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileInfo__CompareLastAccessedDate,
                () =>
                {
                    if (!regex.Use__FileInfo__CompareLastAccessedDate) return null;
                    return IF_INVERSE(!ComparePredicate__LastAccessedDate(source_file), regex.N__FileInfo__CompareLastAccessedDate);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileInfo__CompareLastModifiedDate,
                () =>
                {
                    if (!regex.Use__FileInfo__CompareLastModifiedDate) return null;
                    return IF_INVERSE(!ComparePredicate__LastModifiedDate(source_file), regex.N__FileInfo__CompareLastModifiedDate);
                }
            ));//<- add file predicate

            if (result == null)
            {
                dest_file = null;
                return false;
            }

            dest_file = Path.GetFullPath(result.file.FullName);
            return result.result;
        }

        private string ConvertAbsToNoBaseRel(string base_path, string path)
        {
            string temp = Path.GetFullPath(path).Replace(Path.GetFullPath(base_path), "");

            if (temp[0] == '/' || temp[0] == '\\')
            {
                temp = temp.Remove(0, 1);
            }

            if (temp[temp.Length - 1] == '/' || temp[temp.Length - 1] == '\\')
            {
                temp = temp.Remove(temp.Length - 1, 1);
            }

            return temp;
        }

        private string ConvertSourceToDest(string source_file)
        {
            return Path.GetFullPath(source_file).Replace(Path.GetFullPath(m_manifest.SourceDirectory), Path.GetFullPath(m_manifest.DestDirectory));
        }

        private string ConvertDestToSource(string dest_file)
        {
            return Path.GetFullPath(dest_file).Replace(Path.GetFullPath(m_manifest.DestDirectory), Path.GetFullPath(m_manifest.SourceDirectory));
        }

        private bool GetFileStream(FileInfo source_file, out FileInfo dest_file, out DirectoryInfo dest_parent, out FileStream source, out FileStream dest)
        {
            dest_parent = null;
            dest_file = null;

        re:
            source = null;
            dest = null;

            try
            {
                string dest_file_path = ConvertSourceToDest(source_file.FullName);
                string dest_parent_dir_path = Path.GetDirectoryName(dest_file_path);

                if (!Directory.Exists(dest_parent_dir_path))
                {
                    dest_parent = Directory.CreateDirectory(dest_parent_dir_path);
                }
                else
                {
                    dest_parent = new DirectoryInfo(dest_parent_dir_path);
                }

                source = new FileStream(source_file.FullName, FileMode.Open, FileAccess.Read);
                dest = new FileStream(dest_file_path, FileMode.OpenOrCreate, FileAccess.Write);
                dest_file = new FileInfo(dest_file_path);
                return true;
            }
            catch (Exception e)
            {
                try
                {
                    if (source != null) source.Close();
                }
                catch
                {

                }

                try
                {
                    if (dest != null) source.Close();
                }
                catch
                {

                }

                m_handle.Log(Handle.LogLevel.FAIL, e.GetType().FullName, e.ToString());
                if (m_handle.Retry()) goto re;
                else return false;
            }
        }

        //반환값 : 처리됨 여부
        private bool FileProcessing(FileInfo file, out FileInfo dest_file, out DirectoryInfo dest_parent)
        {
            FileStream source_stream, dest_stream;
            if (!GetFileStream(file, out dest_file, out dest_parent, out source_stream, out dest_stream)) return false;

            m_handle.Log(Handle.LogLevel.NONE, "Begin Sync", $"{file}->{dest_file}");

            using (FileStream source = source_stream)
            using (FileStream dest = dest_stream)
            {
                try
                {
                    //add file process
                    if (TextFileEditing(file, m_manifest.FileProofreader.Target_TextFile, source, dest)) return true;
                    CopyFile(file, source, dest);
                    m_handle.Log(Handle.LogLevel.SUCCESS, "End Sync", $"{file}->{dest_file}");
                    return true;
                }
                catch (Exception e)
                {
                    m_handle.Log(Handle.LogLevel.FAIL, "End Sync", $"{file}->{dest_file}, e : {e}");
                    return false;
                }
            }

        }
    }
}
