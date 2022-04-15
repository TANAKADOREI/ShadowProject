using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadowProject
{
    public static partial class ShadowProjectGenerator
    {
        public class Resource
        {
            public Manifest Manifest;
            public Action<object> Log;
            public Action<Exception> ExceptionCallback;
            public Func<Exception, bool> AccessDenied;//retry : true, ignore : false
        }

        private static Resource Res;

        private static Manifest Manifest => Res.Manifest;

        private static byte[] NewBuffer => new byte[Manifest.BufferSize];

        private static StringBuilder Builder = new StringBuilder();

        private static void EXCEPTIONS(Exception e)
        {
            Res.ExceptionCallback(e);

            if (!Res.Manifest.IgnoreExceptions)
            {
                throw new Exception("throw : ", e);
            }
        }

        private static bool REGEX(string input, string pattern, bool n = false)
        {
            bool result = Regex.IsMatch(input, pattern);
            if (n)
            {
                return !result;
            }
            else
            {
                return result;
            }
        }

        private static RESULT LOGIC<RESULT>(string Logic, Func<RESULT> successed, params Func<bool?>[] lam)
        {
            List<bool> rs = new List<bool>();

            foreach (var l in lam)
            {
                bool? result = null;
                try
                {
                    result = l();
                }
                catch (Exception e)
                {
                    EXCEPTIONS(e);

                    if (Manifest.IgnoreExceptions)
                    {
                        continue;
                    }
                }

                if (result == null) continue;

                rs.Add(result.Value);
            }

            if (Logic == ShadowProject.Manifest.LogicItem.LOGIC_OR)
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

        private static HashSet<string> Run(DirectoryInfo root)
        {
            HashSet<string> updated_dest_dirs = new HashSet<string>();
            var regex = Manifest.Selection.DirectorySelectionRegex;

            {
                DirectoryInfo dest_dir;
                Log("Targeting", root);
                DirectoryFileEnumeration(root, out dest_dir);
                if (dest_dir != null) updated_dest_dirs.Add(dest_dir.FullName);
            }

            foreach (var sub in root.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                LOGIC(regex.Logic, () =>
                {
                    DirectoryInfo dest_dir;
                    Log("SubTargeting", sub);
                    DirectoryFileEnumeration(sub, out dest_dir);
                    if (dest_dir != null) updated_dest_dirs.Add(dest_dir.FullName);
                    return (object)null;
                }, () =>
                {
                    if (regex.UseRelativePathDirNameRegex)
                    {
                        string nobase_relative_path = ConvertAbsToNoBaseRel(Manifest.SourceDirectory, sub.FullName);
                        return REGEX(nobase_relative_path, regex.RelativePathDirNameRegex, regex.RelativePathDirNameRegex__N);
                    }
                    else
                    {
                        return null;
                    }
                }, () =>
                {
                    if (regex.UseDirNameRegex)
                    {
                        return REGEX(sub.Name, regex.DirNameRegex, regex.DirNameRegex__N);
                    }
                    else
                    {
                        return null;
                    }
                }, () =>
                {
                    if (regex.UseDirPathRegex)
                    {
                        return REGEX(sub.FullName, regex.DirPathRegex, regex.DirPathRegex__N);
                    }
                    else
                    {
                        return null;
                    }
                });
            }

            return updated_dest_dirs;
        }

        //하위 파일은 건들지 않음
        //해당폴더 내에서 일어남
        private static void DirectoryFileEnumeration(DirectoryInfo dir, out DirectoryInfo dest_parent)
        {
            dest_parent = null;
            HashSet<string> dest_updated_files = new HashSet<string>();

            foreach (var f in dir.EnumerateFiles())
            {
                FileInfo dest_file = null;
                try
                {
                    DirectoryInfo temp_dest_parent;
                    if (PredicateFile(f, out dest_file, out temp_dest_parent))
                    {
                        if (dest_file == null)
                        {
                            throw new Exception("dest file is null. source : " + f);
                        }

                        if (temp_dest_parent == null)
                        {
                            throw new Exception("dest parent dir is null. source : " + dir);
                        }

                        if (dest_parent == null)
                        {
                            dest_parent = temp_dest_parent;
                        }
                        else
                        {
                            if (dest_parent.FullName != temp_dest_parent.FullName)
                                throw new Exception($"Parents can't be different origin : {dest_parent} current : {temp_dest_parent}");
                        }
                    }
                }
                catch (Exception e)
                {
                    EXCEPTIONS(e);
                }

                if (dest_file != null)
                {
                    dest_updated_files.Add(dest_file.FullName);
                }
            }

            if (dest_parent != null)
            {
                foreach (var file in dest_parent.EnumerateFiles())
                {
                    if (!dest_updated_files.Contains(file.FullName))
                    {
                    //업데이트 된 목록에 없다면 제거함

                    re:
                        try
                        {
                            if (file.Exists)
                                file.Delete();
                        }
                        catch (Exception e)
                        {
                            if (Res.AccessDenied(e)) goto re;
                            else continue;
                        }
                    }
                }
            }
        }

        //modified data check
        private static bool PredicateFileInfo__Date(FileInfo source_file)
        {
            //todo
        }

        //file hash check
        private static bool PredicateFileInfo__Hash(FileInfo source_file)
        {
            //todo
        }

        private static bool PredicateFile(FileInfo file, out FileInfo dest_file, out DirectoryInfo dest_parent)
        {
            dest_file = null;
            dest_parent = null;

            var regex = Manifest.Selection.FileSelectionRegex;

            var result = LOGIC(Manifest.Selection.FileSelectionRegex.Logic, () =>
            {
                FileInfo _dest_file;
                DirectoryInfo _dest_parent;
                bool r = FileProcessing(file, out _dest_file, out _dest_parent);

                return new { result = r, file = _dest_file, dir = _dest_parent };
            }, () =>
            {
                if (!regex.UseFileFullNameRegex) return null;
                return REGEX(file.Name + file.Extension, regex.FileFullNameRegex, regex.FileFullNameRegex__N);
            }, () =>
            {
                if (!regex.UseFileNameRegex) return null;
                return REGEX(file.Name, regex.FileNameRegex, regex.FileNameRegex__N);
            }, () =>
            {
                if (!regex.UseExtRegex) return null;
                return REGEX(file.Extension.Length == 0 ? "" : file.Extension.Remove(0, 1), regex.ExtRegex, regex.ExtRegex__N);
            }, () =>
            {
                if (!regex.UseFilePathRegex) return null;
                return REGEX(file.FullName, regex.FilePathRegex, regex.FilePathRegex__N);
            }, () =>
            {
                if (!regex.UseFileInfo__CompareDateModified) return null;
                return PredicateFileInfo__Date(file);
            }, () =>
            {
                if (!regex.UseFileInfo__CompareHash) return null;
                return PredicateFileInfo__Hash(file);
            });

            if (result == null)
            {
                dest_file = null;
                dest_parent = null;
                return false;
            }

            dest_file = result.file;
            dest_parent = result.dir;
            return result.result;
        }

        private static string ConvertAbsToNoBaseRel(string base_path, string path)
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

        private static string ConvertSourceToDest(string source_file)
        {
            return Path.GetFullPath(source_file).Replace(Path.GetFullPath(Manifest.SourceDirectory), Path.GetFullPath(Manifest.DestDirectory));
        }

        private static string ConvertDestToSource(string dest_file)
        {
            return Path.GetFullPath(dest_file).Replace(Path.GetFullPath(Manifest.DestDirectory), Path.GetFullPath(Manifest.SourceDirectory));
        }

        private static bool GetFileStream(FileInfo source_file, out FileInfo dest_file, out DirectoryInfo dest_parent, out FileStream source, out FileStream dest)
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

                if (Res.AccessDenied(e)) goto re;
                else return false;
            }
        }

        //반환값 : 처리됨 여부
        private static bool FileProcessing(FileInfo file, out FileInfo dest_file, out DirectoryInfo dest_parent)
        {
            FileStream source_stream, dest_stream;
            if (!GetFileStream(file, out dest_file, out dest_parent, out source_stream, out dest_stream)) return false;

            Log("Start processing", $"{file}->{dest_file}");

            using (FileStream source = source_stream)
            using (FileStream dest = dest_stream)
            {
                try
                {
                    //can file process add
                    if (TextFileEditing(file, Manifest.FileProofreader.Target_TextFile, source, dest)) return true;
                    CopyFile(file, source, dest);
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
        }

        private static void Log(object title, object o = null)
        {
            Res.Log($"[{(title is string ? title.ToString() : title.GetType().ToString())}]: {o}");
        }

        public static void Run(in Resource resource)
        {
            if (Res != null) return;
            Res = resource;

            Log("Begin");

            HashSet<string> updated_dest_dirs = Run(new DirectoryInfo(Manifest.SourceDirectory));

            foreach (var dest_dir in Directory.EnumerateDirectories(Manifest.DestDirectory, "*", SearchOption.AllDirectories))
            {
                if (!updated_dest_dirs.Contains(dest_dir))
                {
                    //Remove if mirrored directory does not exist
                    if (Directory.Exists(dest_dir))
                        Directory.Delete(dest_dir, true);
                }
            }

            OptimizeDirectory(new DirectoryInfo(Manifest.DestDirectory));

            Log("End");

            Res = null;
        }

        public static void OptimizeDirectory(DirectoryInfo info)
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
                EXCEPTIONS(e);
            }
        }
    }
}
