﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        private bool ComparePredicate__CreatedDate(FileInfo source)
        {
            return DateDB__CompareDate(m_sql, SQL_TABLE__CREATEDTIME, source.FullName, source.CreationTimeUtc);
        }

        private bool ComparePredicate__LastModifiedDate(FileInfo source)
        {
            return DateDB__CompareDate(m_sql, SQL_TABLE__LASTMODIFIEDTIME, source.FullName, source.CreationTimeUtc);
        }

        private bool ComparePredicate__LastAccessedDate(FileInfo source)
        {
            return DateDB__CompareDate(m_sql, SQL_TABLE__LASTACCESSEDTIME, source.FullName, source.CreationTimeUtc);
        }

        //해쉬로 비교하면 해쉬화, 그후 비교지만 어차피 해쉬화 하면서 다읽을것 같으면 그냥 무식하게 처으부터 비교하는게 더빠를듯
        private bool ComparePredicate__Hash(FileInfo source)
        {
            if (!ComparePredicate__Size(source)) return false;

            using(var d_buffer = BufferPool.Get())
            using(var s_buffer = BufferPool.Get())
            using(FileStream d = File.OpenRead(ConvertSourceToDest(source.FullName)))
            using(FileStream s = File.OpenRead(source.FullName))
            {
                int read = 0;
                while (true)
                {
                    read = d.Read(d_buffer.Get);
                    read = s.Read(s_buffer.Get);

                    if (read <= 0) return true;

                    if (!d_buffer.Get.SequenceEqual(s_buffer.Get)) return false;
                }
            }
        }

        private bool ComparePredicate__Size(FileInfo source)
        {
            FileInfo dest = new FileInfo(ConvertSourceToDest(source.FullName));
            return dest.Length == source.Length;
        }

    }
}
