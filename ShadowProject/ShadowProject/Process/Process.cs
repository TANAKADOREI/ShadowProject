using Newtonsoft.Json;
using ShadowProject.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        public class Handle
        {
            public enum LogLevel
            {
                NONE,
                SUCCESS,
                FAIL,
                IGNORE,
            }

            //<level,tag,msg>
            public Action<LogLevel, object, object> Log;
            public Func<bool> Retry;//retry : true, ignore : false
        }

        public const string NICKNAME_NAME_SEP = "--";
        public const string NAME_MANIFEST = "MANIFEST.json";
        public const string NAME_CONFIG = "CONFIG.json";
        public const string NAME_DB = "DB.sqlite";

        private Pool<StringBuilder> StringBuilderPool;
        private Pool<byte[]> BufferPool;
        private bool Running = false;

        private Handle m_handle;
        private Profile.Config m_config;
        private Manifest m_manifest;
        private SQLiteConnection m_db;

        public Manifest Manifest => m_manifest;

        public void Start(Handle handle, Profile profile)
        {
            m_handle = handle;

            m_config = profile.ProcConfig.Value;
            m_manifest = profile.Manifest.Value;
            m_db = profile.DB.Value;

            PrepareDB();

            //TaskQueue = new TaskPool(m_config.ThreadCount);
            StringBuilderPool = new Pool<StringBuilder>(() => new StringBuilder(), m_config.StringBuilderPoolCapacity);
            BufferPool = new Pool<byte[]>(() => new byte[m_config.BufferSize], m_config.BufferPoolCapacity);
        }

        public Tuple<List<string>, List<string>> PreviewTargetDirs()
        {
            return GetDirectories(new DirectoryInfo(m_manifest.SourceDirectory));
        }

        public HashSet<string> PreviewTargetFiles()
        {
            var target_dirs = PreviewTargetDirs();
            HashSet<string> target_files = new HashSet<string>();

            foreach (var d in target_dirs.Item1)
            {
                GetFiles(target_files, new DirectoryInfo(d));
            }

            return target_files;
        }

        public void Processing(ref ShadowProjectProccessor _this)
        {
            if (Running) return;

            Running = true;

            if (!Directory.Exists(m_manifest.DestDirectory))
            {
                Directory.CreateDirectory(m_manifest.DestDirectory);
            }

            HashSet<string> target_files = PreviewTargetFiles();

            if (m_manifest.SyncProcessing.RemoveAsymmetricDirectories) OptimizingDirectory__AsymmetryRemoval(target_files);

            if (m_manifest.SyncProcessing.RemoveAsymmetricFiles) OptimizingFile__AsymmetryRemoval(target_files);

            RemoveNonTargetFromComparisonFile(target_files);

            if (m_manifest.SyncProcessing.RemoveEmptyDirectories) OptimizeDirectory__Empty(new DirectoryInfo(m_manifest.DestDirectory));

            SyncTargetFiles(target_files);

            _this = null;
            Running = false;
        }

        private void SyncTargetFiles(HashSet<string> target_files)
        {
            foreach (var f in target_files)
            {
                FileProcessing(new FileInfo(f));
            }
        }

        private void RemoveNonTargetFromComparisonFile(HashSet<string> target_files)
        {
            var comparision = m_manifest.Selection.FileComparison;
            List<string> remove_target = new List<string>();

            foreach (string path in target_files)
            {
                FileInfo source_file = new FileInfo(path);

                var reuslt = LOGIC(m_manifest.Selection.FileComparison.Logic, () =>
                {
                    return (bool?)true;
                }, new Tuple<int, Func<bool?>>(
                    m_manifest.Selection.FileComparison.Priority__FileInfo__CompareSize,
                    () =>
                    {
                        if (!comparision.Use__FileInfo__CompareSize) return null;
                        return IF_INVERSE(!ComparePredicate__Size(source_file), comparision.N__FileInfo__CompareSize);
                    }
                ), new Tuple<int, Func<bool?>>(
                    m_manifest.Selection.FileComparison.Priority__FileInfo__CompareCreatedDate,
                    () =>
                    {
                        if (!comparision.Use__FileInfo__CompareCreatedDate) return null;
                        return IF_INVERSE(!ComparePredicate__CreatedDate(source_file), comparision.N__FileInfo__CompareCreatedDate);
                    }
                ), new Tuple<int, Func<bool?>>(
                    m_manifest.Selection.FileComparison.Priority__FileInfo__CompareHash,
                    () =>
                    {
                        if (!comparision.Use__FileInfo__CompareHash) return null;
                        return IF_INVERSE(!ComparePredicate__Hash(source_file), comparision.N__FileInfo__CompareHash);
                    }
                ), new Tuple<int, Func<bool?>>(
                    m_manifest.Selection.FileComparison.Priority__FileInfo__CompareLastAccessedDate,
                    () =>
                    {
                        if (!comparision.Use__FileInfo__CompareLastAccessedDate) return null;
                        return IF_INVERSE(!ComparePredicate__LastAccessedDate(source_file), comparision.N__FileInfo__CompareLastAccessedDate);
                    }
                ), new Tuple<int, Func<bool?>>(
                    m_manifest.Selection.FileComparison.Priority__FileInfo__CompareLastModifiedDate,
                    () =>
                    {
                        if (!comparision.Use__FileInfo__CompareLastModifiedDate) return null;
                        return IF_INVERSE(!ComparePredicate__LastModifiedDate(source_file), comparision.N__FileInfo__CompareLastModifiedDate);
                    }
                ));

                if (reuslt == null || !reuslt.Value)
                {
                    remove_target.Add(path);
                }
            }

            foreach (var f in remove_target)
            {
                target_files.Remove(f);
            }
        }

        private void OptimizingFile__AsymmetryRemoval(HashSet<string> target_files)
        {
            foreach (var dest_file in Directory.EnumerateFiles(m_manifest.DestDirectory, "*", SearchOption.AllDirectories))
            {
                if (!target_files.Contains(ConvertDestToSource(dest_file)))
                {
                re:
                    try
                    {
                        File.Delete(dest_file);
                        m_handle.Log(Handle.LogLevel.SUCCESS, "Delete file", dest_file);

                    }
                    catch (Exception e)
                    {
                        m_handle.Log(Handle.LogLevel.FAIL, e.GetType(), e);
                        if (m_handle.Retry()) goto re;
                    }
                }
            }
        }

        //일단 대칭인 파일들 대칭인지 확인후 하나도 대칭 파일이 포함되지 않는 폴더라면 지우기
        private void OptimizingDirectory__AsymmetryRemoval(HashSet<string> target_files)
        {
            foreach (var dest_dir in Directory.EnumerateDirectories(m_manifest.DestDirectory, "*", SearchOption.AllDirectories))
            {
                bool contains = false;
                int in_file_count = 0;
                foreach (var dest_file in Directory.EnumerateFiles(dest_dir))
                {
                    in_file_count++;
                    if (target_files.Contains(ConvertDestToSource(Path.GetFullPath(dest_file))))
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    if (in_file_count == 0) continue;//빈폴더는 제외
                    re:
                    try
                    {
                        Directory.Delete(dest_dir, true);
                        m_handle.Log(Handle.LogLevel.SUCCESS, "Delete Dir", dest_dir);
                    }
                    catch (Exception e)
                    {
                        m_handle.Log(Handle.LogLevel.FAIL, e.GetType(), e);
                        if (m_handle.Retry()) goto re;
                    }
                }
            }
        }

        private void OptimizeDirectory__Empty(DirectoryInfo info)
        {
            try
            {
                foreach (var d in info.EnumerateDirectories())
                {
                    var subdirs = d.GetDirectories();

                    if (d.GetDirectories().Length == 0 && d.GetFiles().Length == 0)
                    {
                        d.Delete();
                        m_handle.Log(Handle.LogLevel.SUCCESS, "Delete Dir", d.FullName);
                    }

                    foreach (var sd in subdirs)
                    {
                        OptimizeDirectory__Empty(sd);
                    }
                }
            }
            catch (Exception e)
            {
                m_handle.Log(Handle.LogLevel.IGNORE, nameof(OptimizeDirectory__Empty), e.ToString());
            }
        }

        //<source,dest> path
        private Tuple<List<string>, List<string>> GetDirectories(DirectoryInfo root)
        {
            Tuple<List<string>, List<string>> targets = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());

            if (root.Attributes.HasFlag(FileAttributes.Hidden)) return targets;

            var regex = m_manifest.Selection.DirectorySelectionRegex;

            targets.Item1.Add(Path.GetFullPath(root.FullName));
            targets.Item2.Add(ConvertSourceToDest(root.FullName));

            foreach (var sub in root.EnumerateDirectories())
            {
                var result = LOGIC(regex.Logic, () =>
                {
                    return (bool?)true;
                }, new Tuple<int, Func<bool?>>
                (
                    m_manifest.Selection.DirectorySelectionRegex.Priority__DirNameRegex,
                    () =>
                    {
                        if (m_manifest.Selection.DirectorySelectionRegex.Use__UseDirNameRegex)
                        {
                            return IF_INVERSE(REGEX(sub.Name, m_manifest.Selection.DirectorySelectionRegex.Regex__DirNameRegex),
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
                            return IF_INVERSE(REGEX(sub.FullName, m_manifest.Selection.DirectorySelectionRegex.Regex__DirPathRegex),
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

                if (result != null && result.Value)
                {
                    var dirs = GetDirectories(sub);
                    targets.Item1.AddRange(dirs.Item1);
                    targets.Item2.AddRange(dirs.Item2);
                }
            }

            return targets;
        }

        //하위 파일은 건들지 않음
        //해당폴더 내에서 일어남
        private void GetFiles(HashSet<string> target_files, DirectoryInfo dir)
        {
            foreach (var f in dir.EnumerateFiles())
            {
                if (f.Attributes.HasFlag(FileAttributes.Hidden)) continue;

                if (GetFiles(f))
                {
                    target_files.Add(Path.GetFullPath(f.FullName));
                }
            }
        }

        private bool GetFiles(FileInfo source_file)
        {
            var regex = m_manifest.Selection.FileSelectionRegex;

            var result = LOGIC(m_manifest.Selection.FileSelectionRegex.Logic, () =>
            {
                return (bool?)true;
            }, new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__ExtRegex,
                () =>
                {
                    if (!regex.Use__ExtRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Extension == null ? "" : source_file.Extension.Length <= 1 ? "" : source_file.Extension.Remove(0, 1),
                        regex.Regex__ExtRegex), regex.N__ExtRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileFullNameRegex,
                () =>
                {
                    if (!regex.Use__FileFullNameRegex) return null;
                    return IF_INVERSE(REGEX(source_file.Name, regex.Regex__FileFullNameRegex), regex.N__FileFullNameRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FileNameRegex,
                () =>
                {
                    if (!regex.Use__FileNameRegex) return null;
                    string ext = (source_file.Extension == null || source_file.Extension.Length == 1) ? "" : source_file.Extension.Remove(0, 1);
                    return IF_INVERSE(REGEX(source_file.Name.Replace(ext, "").Replace(".", ""),
                        regex.Regex__FileNameRegex), regex.N__FileNameRegex);
                }
            ), new Tuple<int, Func<bool?>>(
                m_manifest.Selection.FileSelectionRegex.Priority__FilePathRegex,
                () =>
                {
                    if (!regex.Use__FilePathRegex) return null;
                    return IF_INVERSE(REGEX(source_file.FullName, regex.Regex__FilePathRegex), regex.N__FilePathRegex);
                }
            ));//<- add file predicate

            if (result == null)
            {
                return false;
            }

            return result.Value;
        }

        private bool GetFileStream(FileInfo source_file, out FileStream source, out FileStream dest)
        {
        re:
            source = null;
            dest = null;

            try
            {
                string dest_file_path = ConvertSourceToDest(source_file.FullName);
                string dest_parent_dir_path = Path.GetDirectoryName(dest_file_path);
                DirectoryInfo dest_parent;

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
        private bool FileProcessing(FileInfo source_file)
        {
            FileInfo dest_file = new FileInfo(ConvertSourceToDest(source_file.FullName));

            if (!source_file.Directory.Exists) source_file.Directory.Create();
            if (!dest_file.Directory.Exists) dest_file.Directory.Create();

            m_handle.Log(Handle.LogLevel.NONE, "Begin Sync", $"{source_file}->{dest_file}");

            try
            {
                //add file process
                if (m_manifest.FileProofreader.Enable) {
                    TextFileEditing(source_file, dest_file, m_manifest.FileProofreader.Target_TextFile);
                }
                else CopyFile(source_file, dest_file);
                m_handle.Log(Handle.LogLevel.SUCCESS, "End Sync", $"{source_file}->{dest_file}");
                return true;
            }
            catch (Exception e)
            {
                m_handle.Log(Handle.LogLevel.FAIL, "End Sync", $"{source_file}->{dest_file}, e : {e}");
                return false;
            }
        }
    }
}
