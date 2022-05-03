using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShadowProject
{
    public partial class ShadowProjectProccessor
    {
        //result : processed check. if it has never been dealt with false
        private void TextFileEditing(FileInfo source,FileInfo dest, Manifest.Proofreader.TextFile[] proofreaders)
        {
            using (StreamReader reader = source.OpenText())
            using (var Builder = StringBuilderPool.Get())
            {

                Builder.Get.Clear();
                Builder.Get.Append(reader.ReadToEnd());

                foreach (var p in proofreaders)
                {
                    if (!p.Enable) continue;

                    {
                        string ext = source.Extension.Length == 0 ? "" : source.Extension.Remove(0, 1);
                        if (p.Extensions.Where(e => e == ext) == null) continue;
                    }

                    {
                        const string TEMP_NEWLINE_SIGN = "<<NEWLINE>>";
                        string newline = null;

                        switch (p.NewLine)
                        {
                            case nameof(Manifest.Proofreader.TextFile.NEWLINE_CR):
                                newline = Manifest.Proofreader.TextFile.NEWLINE_CR;
                                break;
                            case nameof(Manifest.Proofreader.TextFile.NEWLINE_LF):
                                newline = Manifest.Proofreader.TextFile.NEWLINE_LF;
                                break;
                            case nameof(Manifest.Proofreader.TextFile.NEWLINE_CRLF):
                                newline = Manifest.Proofreader.TextFile.NEWLINE_CRLF;
                                break;
                            case nameof(Manifest.Proofreader.TextFile.NEWLINE_NONE):
                                newline = Manifest.Proofreader.TextFile.NEWLINE_NONE;
                                break;
                            case nameof(Manifest.Proofreader.TextFile.NEWLINE_ERASE):
                                newline = Manifest.Proofreader.TextFile.NEWLINE_ERASE;
                                break;
                            default:
                                throw new Exception("unknown newline : " + p.NewLine);
                        }

                        if (newline != null)
                        {
                            Builder.Get.Replace(Manifest.Proofreader.TextFile.NEWLINE_CRLF, TEMP_NEWLINE_SIGN);
                            Builder.Get.Replace(Manifest.Proofreader.TextFile.NEWLINE_LF, TEMP_NEWLINE_SIGN);
                            Builder.Get.Replace(Manifest.Proofreader.TextFile.NEWLINE_CR, TEMP_NEWLINE_SIGN);
                            Builder.Get.Replace(TEMP_NEWLINE_SIGN, newline);
                        }
                    }

                    #region early
                    /*
                    if (p.CommentRegex.Enable)
                    {
                        //주석 구간. 시작 인덱스,길이
                        List<Tuple<int, int>> section = new List<Tuple<int, int>>();

                        int start_start = -1, start_count = -1;
                        int end_start = -1, end_count = -1;

                        for (int i = 0; i < Builder.Length; i++)
                        {
                            if (PredicateCommentSign(Builder, i, p.CommentRegex.SingleCommentSign, ref start_start, ref start_count))
                            {
                                for (i = start_count; i < Builder.Length; i++)
                                {
                                    foreach (var n in new string[] {
                                    Manifest.Proofreader.TextFile.NEWLINE_CRLF,
                                    Manifest.Proofreader.TextFile.NEWLINE_CR,
                                    Manifest.Proofreader.TextFile.NEWLINE_LF
                                })
                                    {
                                        if (PredicateCommentSign(Builder, i, n, ref end_start, ref end_count))
                                        {
                                            section.Add(new Tuple<int, int>(start_start, end_count - start_start));
                                            goto sign_break;
                                        }
                                    }
                                }

                            sign_break:
                                start_start = start_count = -1;
                                end_start = end_count = -1;
                            }
                            else if (PredicateCommentSign(Builder, i, p.CommentRegex.MultiCommentOpenSign, ref start_start, ref start_count))
                            {
                                for (i = start_count; i < Builder.Length; i++)
                                {
                                    if (PredicateCommentSign(Builder, i, p.CommentRegex.MultiCommentCloseSign, ref end_start, ref end_count))
                                    {
                                        section.Add(new Tuple<int, int>(start_start, end_count - start_start));
                                        break;
                                    }
                                }

                                start_start = start_count = -1;
                                end_start = end_count = -1;
                            }
                        }

                        //주석 관련 처리 부분

                        if (p.RemoveComment)
                        {
                            foreach (var sec in section)
                            {
                                try
                                {
                                    Builder.Remove(sec.Item1, sec.Item2);
                                }
                                catch(Exception e)
                                {
                                    goto end;
                                }
                            }
                        }
                    }*/
                    #endregion

                    if (p.IndentConverter.Enable)
                    {
                        string space_block = new string(new char[p.IndentConverter.Space]).Replace('\0', ' ');
                        string tab = "\t";

                        if (p.IndentConverter.ConvertSpaceToTab)
                        {
                            Builder.Get.Replace(space_block, tab);
                        }
                        else
                        {
                            Builder.Get.Replace(tab, space_block);
                        }
                    }

                    {
                        StreamWriter writer = null;

                        if (p.Encoding.Enable)
                        {
                            writer = new StreamWriter(dest.Open(FileMode.OpenOrCreate), Encoding.GetEncoding(p.Encoding.EncodingName));
                        }
                        else
                        {
                            writer = new StreamWriter(dest.Open(FileMode.OpenOrCreate), reader.CurrentEncoding);
                        }

                        writer.Write(Builder.Get);
                        writer.Flush();
                        writer.Close();
                        goto end;
                    }
                }

            end:

                Builder.Get.Clear();
            }
        }

        private static bool PredicateCommentSign(StringBuilder builder, int start_index, string find_str, ref int start, ref int count)
        {
            if (builder.Length < start_index + find_str.Length) return false;

            start = start_index;

            for (count = start_index; count < start_index + find_str.Length; count++)
            {
                if (builder[count] != find_str[count - start_index]) return false;
            }

            return true;
        }
    }
}
