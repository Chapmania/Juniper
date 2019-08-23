namespace Juniper.HTTP
{
    public partial class MediaType
    {
        public sealed class Multipart : MediaType
        {
            public Multipart(string value, string[] extensions = null) : base("multipart/" + value, extensions) {}

            public static readonly Multipart Alternative = new Multipart("alternative");
            public static readonly Multipart Appledouble = new Multipart("appledouble");
            public static readonly Multipart Byteranges = new Multipart("byteranges");
            public static readonly Multipart Digest = new Multipart("digest");
            public static readonly Multipart Encrypted = new Multipart("encrypted");
            public static readonly Multipart Example = new Multipart("example");
            public static readonly Multipart FormData = new Multipart("form-data");
            public static readonly Multipart HeaderSet = new Multipart("header-set");
            public static readonly Multipart Mixed = new Multipart("mixed");
            public static readonly Multipart Multilingual = new Multipart("multilingual");
            public static readonly Multipart Parallel = new Multipart("parallel");
            public static readonly Multipart Related = new Multipart("related");
            public static readonly Multipart Report = new Multipart("report");
            public static readonly Multipart Signed = new Multipart("signed");
            public static readonly Multipart VendorBintMedPlus = new Multipart("vnd.bint.med-plus");
            public static readonly Multipart VoiceMessage = new Multipart("voice-message");
            public static readonly Multipart XMixedReplace = new Multipart("x-mixed-replace");
        }
    }
}
