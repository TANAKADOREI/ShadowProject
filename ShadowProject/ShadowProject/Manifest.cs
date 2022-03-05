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

        public class Selector : Item
        {
            public class FileSearchRegex : Item
            {
                //only file name
                [JsonProperty("UseFileNameRegex")]
                public bool UseFileNameRegex = true;
                [JsonProperty("FileNameRegex__IgnoreMode")]
                public bool FileNameRegex__N = false;
                [JsonProperty("FileNameRegex")]
                public string FileNameRegex = @"\w";

                //only extension string
                [JsonProperty("UseExtRegex")]
                public bool UseExtRegex = false;
                [JsonProperty("ExtRegex__IgnoreMode")]
                public bool ExtRegex__N = false;
                [JsonProperty("ExtRegex")]
                public string ExtRegex = "";

                //name and ext
                [JsonProperty("UseFileFullNameRegex")]
                public bool UseFileFullNameRegex = false;
                [JsonProperty("FileFullNameRegex__IgnoreMode")]
                public bool FileFullNameRegex__N = false;
                [JsonProperty("FileFullNameRegex")]
                public string FileFullNameRegex = "";

                //file fullpath
                [JsonProperty("UseFilePathRegex")]
                public bool UseFilePathRegex = false;
                [JsonProperty("FilePathRegex__IgnoreMode")]
                public bool FilePathRegex__N = false;
                [JsonProperty("FilePathRegex")]
                public string FilePathRegex = "";
            }

            public class DirSearchRegex : Item
            {
                //only dir name
                [JsonProperty("UseDirNameRegex")]
                public bool UseDirNameRegex = true;
                [JsonProperty("DirNameRegex__IgnoreMode")]
                public bool DirNameRegex__N = false;
                [JsonProperty("DirNameRegex")]
                public string DirNameRegex = @"\w";

                //dir fullpath
                [JsonProperty("UseDirPathRegex")]
                public bool UseDirPathRegex = false;
                [JsonProperty("DirPathRegex__IgnoreMode")]
                public bool DirPathRegex__N = false;
                [JsonProperty("DirPathRegex")]
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
                    [JsonProperty("IsLineRegex")]
                    public bool IsLineRegex = true;

                    [JsonProperty("CommentSign")]
                    public string CommentSign = "";

                    [JsonProperty("CommentOpenSign")]
                    public string CommentOpenSign = "";

                    [JsonProperty("CommentCloseSign")]
                    public string CommentCloseSign = "";
                }

                public class EncodingConverter : Item
                {
                    [JsonProperty("EncodingName")]
                    public string EncodingName = System.Text.Encoding.UTF8.EncodingName;
                }
                [JsonProperty("Extensions")]
                public string[] Extensions = { "txt", "json" };

                [JsonProperty("NewlineChars")]
                public string NewLineChar = "\n";

                [JsonProperty("CommentRemover")]
                public CommentSearchRegex CommentRemover = new CommentSearchRegex();

                [JsonProperty("Encoding")]
                public EncodingConverter Encoding = new EncodingConverter();
            }

            [JsonProperty("TextFile__IsNotDocFile")]
            public TextFile[] Target_TextFile = { new TextFile() };
        }

        [JsonProperty("SourceDirectory")]
        public string SourceDirectories = "";

        [JsonProperty("DestDirectory")]
        public string DestDirectory = "";

        [JsonProperty("Selector")]
        public Selector m_selector = new Selector();

        [JsonProperty("Proofreader")]
        public Proofreader m_proofreader = new Proofreader();
    }
}
