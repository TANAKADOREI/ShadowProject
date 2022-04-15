using Newtonsoft.Json;

namespace ShadowProject
{

    //Regular expression check in part order
    public class Manifest
    {
        public class Item
        {
            [JsonProperty("Enable")]
            public bool Enable = false;
        }

        public class LogicItem
        {
            public const string LOGIC_AND = "AND";
            public const string LOGIC_OR = "OR";

            [JsonProperty("ChooseOneOf_" + LOGIC_AND + "_or_" + LOGIC_OR)]
            public string Logic = LOGIC_OR;
        }

        public class Selector : Item
        {
            public class FileSearchRegex : LogicItem
            {
                //only file name
                [JsonProperty("B__UseFileNameRegex")]
                public bool UseFileNameRegex = true;
                [JsonProperty("B__FileNameRegex__InvertValue")]
                public bool FileNameRegex__N = false;
                [JsonProperty("B__FileNameRegex")]
                public string FileNameRegex = @"\w";

                //only extension string
                [JsonProperty("C__UseExtRegex")]
                public bool UseExtRegex = false;
                [JsonProperty("C__ExtRegex__InvertValue")]
                public bool ExtRegex__N = false;
                [JsonProperty("C__ExtRegex")]
                public string ExtRegex = "";

                //name and ext
                [JsonProperty("A__UseFileFullNameRegex")]
                public bool UseFileFullNameRegex = false;
                [JsonProperty("A__FileFullNameRegex__InvertValue")]
                public bool FileFullNameRegex__N = false;
                [JsonProperty("A__FileFullNameRegex")]
                public string FileFullNameRegex = "";

                //file fullpath
                [JsonProperty("D__UseFilePathRegex")]
                public bool UseFilePathRegex = false;
                [JsonProperty("D__FilePathRegex__InvertValue")]
                public bool FilePathRegex__N = false;
                [JsonProperty("D__FilePathRegex")]
                public string FilePathRegex = "";

                [JsonProperty("E__UseFileInfo__CompareDateModified")]
                public bool UseFileInfo__CompareDateModified = false;
                [JsonProperty("E__FileInfo__CompareDateModified__InvertValue")]
                public bool FileInfo__CompareDateModified__N = false;

                [JsonProperty("F__UseFileInfo__CompareHash")]
                public bool UseFileInfo__CompareHash = false;
                [JsonProperty("F__FileInfo__CompareHash__InvertValue")]
                public bool FileInfo__CompareHash__N = false;
            }

            public class DirSearchRegex : LogicItem
            {
                [JsonProperty("A__UseRelativePathDirNameRegex")]
                public bool UseRelativePathDirNameRegex = true;
                [JsonProperty("A__RelativePathDirNameRegex__N__InvertValue")]
                public bool RelativePathDirNameRegex__N = false;
                [JsonProperty("A__RelativePathDirNameRegex")]
                public string RelativePathDirNameRegex = @"\w";

                //only dir name
                [JsonProperty("B__UseDirNameRegex")]
                public bool UseDirNameRegex = true;
                [JsonProperty("B__DirNameRegex__InvertValue")]
                public bool DirNameRegex__N = false;
                [JsonProperty("B__DirNameRegex")]
                public string DirNameRegex = @"\w";

                //dir fullpath
                [JsonProperty("C__UseDirPathRegex")]
                public bool UseDirPathRegex = false;
                [JsonProperty("C__DirPathRegex__InvertValue")]
                public bool DirPathRegex__N = false;
                [JsonProperty("C__DirPathRegex")]
                public string DirPathRegex = "";
            }

            [JsonProperty("DirectorySelectionRegex")]
            public DirSearchRegex DirectorySelectionRegex = new DirSearchRegex();

            [JsonProperty("FileSelectionRegex")]
            public FileSearchRegex FileSelectionRegex = new FileSearchRegex();
        }

        public class Proofreader : Item
        {
            public class TextFile : Item
            {
                public class CommentSearchRegex : Item
                {
                    [JsonProperty("SingleCommentSign")]
                    public string SingleCommentSign = "";

                    [JsonProperty("MultiCommentOpenSign")]
                    public string MultiCommentOpenSign = "";

                    [JsonProperty("MultiCommentCloseSign")]
                    public string MultiCommentCloseSign = "";
                }

                public class EncodingConverter : Item
                {
                    [JsonProperty("EncodingName__IfEmptyDontConvertEncoding")]
                    public string EncodingName = System.Text.Encoding.UTF8.HeaderName;
                }

                public class Indent : Item
                {
                    [JsonProperty("Space")]
                    public int Space = 4;

                    [JsonProperty("Tab")]
                    public int Tab = 1;

                    [JsonProperty("ConvertSapceToTab")]
                    public bool ConvertSpaceToTab = false;
                }

                public const string NEWLINE_NONE = null;
                public const string NEWLINE_ERASE = "";
                public const string NEWLINE_CR = "\r";
                public const string NEWLINE_LF = "\n";
                public const string NEWLINE_CRLF = "\r\n";

                [JsonProperty("A__Extensions")]
                public string[] Extensions = { "txt", "json" };

                [JsonProperty("B__PleaseSelectTheNewlineYouWant(" + nameof(NEWLINE_CR) + "," + nameof(NEWLINE_LF) + "," 
                    + nameof(NEWLINE_CRLF) +","+ nameof(NEWLINE_NONE) + "," + nameof(NEWLINE_ERASE) + ")")]
                public string NewLine = nameof(NEWLINE_LF);

                [JsonProperty("C__RemoveComment")]
                public bool RemoveComment = false;

                [JsonProperty("C__CommentRegex")]
                public CommentSearchRegex CommentRegex = new CommentSearchRegex();

                [JsonProperty("D__IndentConverter")]
                public Indent IndentConverter = new Indent();

                [JsonProperty("E__Encoding")]
                public EncodingConverter Encoding = new EncodingConverter();
            }

            [JsonProperty("TextFile__IsNotDocFile")]
            public TextFile[] Target_TextFile = { new TextFile() };
        }

        public const uint MANIFEST_VERSION = 1;

        [JsonProperty("MANIFEST_VERSION")]
        public uint ManifestVersion = MANIFEST_VERSION;

        [JsonProperty("IgnoreExceptions")]
        public bool IgnoreExceptions = false;

        [JsonProperty("SourceDirectory")]
        public string SourceDirectory = "";

        [JsonProperty("DestDirectory")]
        public string DestDirectory = "";

        [JsonProperty("Selection")]
        public Selector Selection = new Selector();

        [JsonProperty("FileProofreader")]
        public Proofreader FileProofreader = new Proofreader();

        [JsonProperty("BufferSize")]
        public int BufferSize = 4096;
    }
}
