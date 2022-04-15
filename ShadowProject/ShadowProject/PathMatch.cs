using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShadowProject
{
    public class PathMatch : IEnumerable<KeyValuePair<string, string>>,IEnumerable<PathMatch.PathInfo>
    {
        public class PathInfo
        {
            private bool m_req_check;
            private string m_path;

            public string Path => m_path;
            public bool ReqCheck { get => m_req_check; set => m_req_check = value; }

            public PathInfo(string path)
            {
                m_path = System.IO.Path.GetFullPath(path);
                m_req_check = false;
            }

            public override int GetHashCode()
            {
                return m_path.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return m_path.Equals(obj);
            }

            public override string ToString()
            {
                return m_path;
            }
        }

        //PathInfo.Path == SourcePath
        private HashSet<PathInfo> m_state;
        private Dictionary<string, string> m_source_path;
        private Dictionary<string, string> m_dest_path;

        public PathMatch()
        {
            m_state = new HashSet<PathInfo>();
            m_dest_path = new Dictionary<string, string>();
            m_source_path = new Dictionary<string, string>();
        }

        public void Add(string source_path, string dest_path)
        {
            source_path = Path.GetFullPath(source_path);
            dest_path = Path.GetFullPath(dest_path);

        re:

            bool r1 = m_dest_path.ContainsKey(dest_path);
            bool r2 = m_source_path.ContainsKey(source_path);

            if (r1 == r2)
            {
                if (!r1)
                {
                    m_dest_path.Add(dest_path, source_path);
                    m_source_path.Add(source_path, dest_path);
                    m_state.Add(new PathInfo(source_path));
                }
            }
            else
            {
                Remove(source_path);
                goto re;
            }

        }

        public void Remove(string source_or_dest_path)
        {
            string matched_other_path = null;

            if (m_dest_path.ContainsKey(source_or_dest_path))
            {
                matched_other_path = m_dest_path[source_or_dest_path];
                m_dest_path.Remove(source_or_dest_path);


                if (matched_other_path != null && m_source_path.ContainsKey(matched_other_path))
                {
                    m_source_path.Remove(matched_other_path);
                }
            }
            else if (m_source_path.ContainsKey(source_or_dest_path))
            {
                matched_other_path = m_dest_path[source_or_dest_path];
                m_source_path.Remove(source_or_dest_path);

                if (matched_other_path != null && m_dest_path.ContainsKey(matched_other_path))
                {
                    m_dest_path.Remove(matched_other_path);
                }
            }
        }

        public string FindMatchingSourcePath(string dest_path)
        {
            if (m_dest_path.ContainsKey(dest_path)) return m_dest_path[dest_path];
            return null;
        }

        public string FindMatchingDestPath(string source_path)
        {
            if (m_source_path.ContainsKey(source_path)) return m_source_path[source_path];
            return null;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return (IEnumerator<KeyValuePair<string, string>>)m_source_path;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator<PathInfo> IEnumerable<PathInfo>.GetEnumerator()
        {
            return (IEnumerator<PathInfo>)m_state;
        }
    }
}
