using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
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
                if (rs.Contains(true))
                {
                    return successed();
                }
            }
            else if (Logic == Manifest.LogicItem.LOGIC_AND)
            {
                if (!rs.Contains(false))
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
    }
}
