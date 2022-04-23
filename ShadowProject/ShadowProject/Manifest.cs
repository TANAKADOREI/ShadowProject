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
            public string Logic = LOGIC_AND;
        }

        public class Selector : Item
        {

            public class FileSearchRegex : LogicItem
            {
                public int Priority__FileNameRegex = 0;
                public bool Use__FileNameRegex = false;
                public bool N__FileNameRegex = false;
                public string Regex__FileNameRegex = "";

                public int Priority__ExtRegex = 0;
                public bool Use__ExtRegex = false;
                public bool N__ExtRegex = false;
                public string Regex__ExtRegex = "";

                public int Priority__FileFullNameRegex = 0;
                public bool Use__FileFullNameRegex = false;
                public bool N__FileFullNameRegex = false;
                public string Regex__FileFullNameRegex = "";

                public int Priority__FilePathRegex = 0;
                public bool Use__FilePathRegex = false;
                public bool N__FilePathRegex = false;
                public string Regex__FilePathRegex = "";
            }

            public class DirSearchRegex : LogicItem
            {
                public int Priority__RelativePathDirNameRegex = 0;
                public bool Use__UseRelativePathDirNameRegex = false;
                public bool N__RelativePathDirNameRegex = false;
                public string Regex__RelativePathDirNameRegex = "";

                //only dir name
                public int Priority__DirNameRegex = 0;
                public bool Use__UseDirNameRegex = false;
                public bool N__DirNameRegex = false;
                public string Regex__DirNameRegex = "";

                //dir fullpath
                public int Priority__DirPathRegex = 0;
                public bool Use__UseDirPathRegex = false;
                public bool N__DirPathRegex = false;
                public string Regex__DirPathRegex = "";
            }

            public class FileCompare : LogicItem
            {
                public int Priority__FileInfo__CompareLastModifiedDate = 0;
                public bool Use__FileInfo__CompareLastModifiedDate = false;
                public bool N__FileInfo__CompareLastModifiedDate = false;

                public int Priority__FileInfo__CompareLastAccessedDate = 0;
                public bool Use__FileInfo__CompareLastAccessedDate = false;
                public bool N__FileInfo__CompareLastAccessedDate = false;

                public int Priority__FileInfo__CompareCreatedDate = 0;
                public bool Use__FileInfo__CompareCreatedDate = false;
                public bool N__FileInfo__CompareCreatedDate = false;

                public int Priority__FileInfo__CompareHash = 0;
                public bool Use__FileInfo__CompareHash = false;
                public bool N__FileInfo__CompareHash = false;

                public int Priority__FileInfo__CompareSize = 0;
                public bool Use__FileInfo__CompareSize = false;
                public bool N__FileInfo__CompareSize = false;
            }

            [JsonProperty("DirectorySelectionRegex")]
            public DirSearchRegex DirectorySelectionRegex = new DirSearchRegex();

            [JsonProperty("FileSelectionRegex")]
            public FileSearchRegex FileSelectionRegex = new FileSearchRegex();

            [JsonProperty("FileComparison")]
            public FileCompare FileComparison = new FileCompare();
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

        public class SyncProcess
        {
            public bool RemoveAsymmetricDirectories = true;
            public bool RemoveAsymmetricFiles = true;
            public bool RemoveEmptyDirectories = true;
        }

        public const uint MANIFEST_VERSION = 3;

        public uint ManifestVersion = MANIFEST_VERSION;

        [JsonIgnore]
        public string SourceDirectory = "";

        [JsonIgnore]
        public string DestDirectory = "";

        public bool FromSourceToDest = true;

        public Selector Selection = new Selector();

        public Proofreader FileProofreader = new Proofreader();

        public SyncProcess SyncProcessing = new SyncProcess();
    }
}
