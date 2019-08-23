namespace Juniper.HTTP
{
    public partial class MediaType
    {
        public sealed class Video : MediaType
        {
            public Video(string value, string[] extensions = null) : base("video/" + value, extensions) {}

            public static readonly Video BMPEG = new Video("bmpeg");
            public static readonly Video BT656 = new Video("bt656");
            public static readonly Video CelB = new Video("celb");
            public static readonly Video DV = new Video("dv");
            public static readonly Video Encaprtp = new Video("encaprtp");
            public static readonly Video Example = new Video("example");
            public static readonly Video Flexfec = new Video("flexfec");
            public static readonly Video H261 = new Video("h261", new string[] {"h261"});
            public static readonly Video H263 = new Video("h263", new string[] {"h263"});
            public static readonly Video H2631998 = new Video("h263-1998");
            public static readonly Video H2632000 = new Video("h263-2000");
            public static readonly Video H264 = new Video("h264", new string[] {"h264"});
            public static readonly Video H264RCDO = new Video("h264-rcdo");
            public static readonly Video H264SVC = new Video("h264-svc");
            public static readonly Video H265 = new Video("h265");
            public static readonly Video IsoSegment = new Video("iso.segment");
            public static readonly Video JPEG = new Video("jpeg", new string[] {"jpgv"});
            public static readonly Video Jpeg2000 = new Video("jpeg2000");
            public static readonly Video Jpm = new Video("jpm", new string[] {"jpm", "jpgm"});
            public static readonly Video Mj2 = new Video("mj2", new string[] {"mj2", "mjp2"});
            public static readonly Video MP1S = new Video("mp1s");
            public static readonly Video MP2P = new Video("mp2p");
            public static readonly Video MP2T = new Video("mp2t");
            public static readonly Video Mp4 = new Video("mp4", new string[] {"mp4", "mp4v", "mpg4"});
            public static readonly Video MP4VES = new Video("mp4v-es");
            public static readonly Video Mpeg = new Video("mpeg", new string[] {"mpeg", "mpg", "mpe", "m1v", "m2v"});
            public static readonly Video Mpeg4Generic = new Video("mpeg4-generic");
            public static readonly Video MPV = new Video("mpv");
            public static readonly Video Nv = new Video("nv");
            public static readonly Video Ogg = new Video("ogg", new string[] {"ogv"});
            public static readonly Video Parityfec = new Video("parityfec");
            public static readonly Video Pointer = new Video("pointer");
            public static readonly Video Quicktime = new Video("quicktime", new string[] {"qt", "mov"});
            public static readonly Video Raptorfec = new Video("raptorfec");
            public static readonly Video Raw = new Video("raw");
            public static readonly Video RtpEncAescm128 = new Video("rtp-enc-aescm128");
            public static readonly Video Rtploopback = new Video("rtploopback");
            public static readonly Video Rtx = new Video("rtx");
            public static readonly Video Smpte291 = new Video("smpte291");
            public static readonly Video SMPTE292M = new Video("smpte292m");
            public static readonly Video Ulpfec = new Video("ulpfec");
            public static readonly Video Vc1 = new Video("vc1");
            public static readonly Video Vc2 = new Video("vc2");
            public static readonly Video Vendor1dInterleavedParityfec = new Video("1d-interleaved-parityfec");
            public static readonly Video Vendor3gpp = new Video("3gpp", new string[] {"3gp"});
            public static readonly Video Vendor3gpp2 = new Video("3gpp2", new string[] {"3g2"});
            public static readonly Video Vendor3gppTt = new Video("3gpp-tt");
            public static readonly Video VendorCCTV = new Video("vnd.cctv");
            public static readonly Video VendorDeceHd = new Video("vnd.dece.hd", new string[] {"uvh", "uvvh"});
            public static readonly Video VendorDeceMobile = new Video("vnd.dece.mobile", new string[] {"uvm", "uvvm"});
            public static readonly Video VendorDeceMp4 = new Video("vnd.dece.mp4");
            public static readonly Video VendorDecePd = new Video("vnd.dece.pd", new string[] {"uvp", "uvvp"});
            public static readonly Video VendorDeceSd = new Video("vnd.dece.sd", new string[] {"uvs", "uvvs"});
            public static readonly Video VendorDeceVideo = new Video("vnd.dece.video", new string[] {"uvv", "uvvv"});
            public static readonly Video VendorDirectvMpeg = new Video("vnd.directv.mpeg");
            public static readonly Video VendorDirectvMpegTts = new Video("vnd.directv.mpeg-tts");
            public static readonly Video VendorDlnaMpegTts = new Video("vnd.dlna.mpeg-tts");
            public static readonly Video VendorDvbFile = new Video("vnd.dvb.file", new string[] {"dvb"});
            public static readonly Video VendorFvt = new Video("vnd.fvt", new string[] {"fvt"});
            public static readonly Video VendorHnsVideo = new Video("vnd.hns.video");
            public static readonly Video VendorIptvforum1dparityfec1010 = new Video("vnd.iptvforum.1dparityfec-1010");
            public static readonly Video VendorIptvforum1dparityfec2005 = new Video("vnd.iptvforum.1dparityfec-2005");
            public static readonly Video VendorIptvforum2dparityfec1010 = new Video("vnd.iptvforum.2dparityfec-1010");
            public static readonly Video VendorIptvforum2dparityfec2005 = new Video("vnd.iptvforum.2dparityfec-2005");
            public static readonly Video VendorIptvforumTtsavc = new Video("vnd.iptvforum.ttsavc");
            public static readonly Video VendorIptvforumTtsmpeg2 = new Video("vnd.iptvforum.ttsmpeg2");
            public static readonly Video VendorMotorolaVideo = new Video("vnd.motorola.video");
            public static readonly Video VendorMotorolaVideop = new Video("vnd.motorola.videop");
            public static readonly Video VendorMpegurl = new Video("vnd.mpegurl", new string[] {"mxu", "m4u"});
            public static readonly Video VendorMsPlayreadyMediaPyv = new Video("vnd.ms-playready.media.pyv", new string[] {"pyv"});
            public static readonly Video VendorNokiaInterleavedMultimedia = new Video("vnd.nokia.interleaved-multimedia");
            public static readonly Video VendorNokiaMp4vr = new Video("vnd.nokia.mp4vr");
            public static readonly Video VendorNokiaVideovoip = new Video("vnd.nokia.videovoip");
            public static readonly Video VendorObjectvideo = new Video("vnd.objectvideo");
            public static readonly Video VendorRadgamettoolsBink = new Video("vnd.radgamettools.bink");
            public static readonly Video VendorRadgamettoolsSmacker = new Video("vnd.radgamettools.smacker");
            public static readonly Video VendorSealedmediaSoftsealMov = new Video("vnd.sealedmedia.softseal.mov");
            public static readonly Video VendorSealedMpeg1 = new Video("vnd.sealed.mpeg1");
            public static readonly Video VendorSealedMpeg4 = new Video("vnd.sealed.mpeg4");
            public static readonly Video VendorSealedSwf = new Video("vnd.sealed.swf");
            public static readonly Video VendorUvvuMp4 = new Video("vnd.uvvu.mp4", new string[] {"uvu", "uvvu"});
            public static readonly Video VendorVivo = new Video("vnd.vivo", new string[] {"viv"});
            public static readonly Video VendorYoutubeYt = new Video("vnd.youtube.yt");
            public static readonly Video VP8 = new Video("vp8");
            public static readonly Video Webm = new Video("webm", new string[] {"webm"});
            public static readonly Video XF4v = new Video("x-f4v", new string[] {"f4v"});
            public static readonly Video XFli = new Video("x-fli", new string[] {"fli"});
            public static readonly Video XFlv = new Video("x-flv", new string[] {"flv"});
            public static readonly Video XM4v = new Video("x-m4v", new string[] {"m4v"});
            public static readonly Video XMatroska = new Video("x-matroska", new string[] {"mkv", "mk3d", "mks"});
            public static readonly Video XMng = new Video("x-mng", new string[] {"mng"});
            public static readonly Video XMsAsf = new Video("x-ms-asf", new string[] {"asf", "asx"});
            public static readonly Video XMsvideo = new Video("x-msvideo", new string[] {"avi"});
            public static readonly Video XMsVob = new Video("x-ms-vob", new string[] {"vob"});
            public static readonly Video XMsWm = new Video("x-ms-wm", new string[] {"wm"});
            public static readonly Video XMsWmv = new Video("x-ms-wmv", new string[] {"wmv"});
            public static readonly Video XMsWmx = new Video("x-ms-wmx", new string[] {"wmx"});
            public static readonly Video XMsWvx = new Video("x-ms-wvx", new string[] {"wvx"});
            public static readonly Video XSgiMovie = new Video("x-sgi-movie", new string[] {"movie"});
            public static readonly Video XSmv = new Video("x-smv", new string[] {"smv"});
        }
    }
}
