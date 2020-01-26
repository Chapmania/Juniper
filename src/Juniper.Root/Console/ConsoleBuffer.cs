using System;
using System.Runtime.InteropServices;
using System.Text;

using static System.Console;

namespace Juniper.Console
{
    public sealed class ConsoleBuffer :
        IConsoleBuffer
    {
        private readonly ConsoleColor startFore;
        private readonly ConsoleColor startBack;
        private ConsoleColor lastBack;
        private ConsoleColor lastFore;
        private ConsoleColor[,] back1;
        private ConsoleColor[,] back2;
        private ConsoleColor[,] fore1;
        private ConsoleColor[,] fore2;
        private char[,] grid1;
        private char[,] grid2;

        public ConsoleBuffer(int width, int height)
        {
            SetFontSize(8, 8);
            OutputEncoding = Encoding.Unicode;
            CursorVisible = false;

            lastFore = startFore = ForegroundColor;
            lastBack = startBack = BackgroundColor;

            SetWindowSize(width, height + 1);
            SetBufferSize(width, height + 1);

            CheckGrids();

            CancelKeyPress += ConsoleBuffer_CancelKeyPress;
        }

        private void ConsoleBuffer_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Teardown();
            e.Cancel = true;
        }

        ~ConsoleBuffer()
        {
            Teardown();
        }

        private void Teardown()
        {
            ForegroundColor = startFore;
            BackgroundColor = startBack;
        }

        public ConsoleBuffer()
            : this(WindowWidth, WindowHeight - 1)
        { }

        public int AbsoluteLeft => 0;
        public int AbsoluteRight => Width - 1;
        public int AbsoluteTop => 0;
        public int AbsoluteBottom => Height - 1;
        public int Width => grid1?.GetWidth() ?? -1;
        public int Height => grid1?.GetHeight() ?? -1;

        public ConsoleColor GetBackgroundColor(int x, int y)
        {
            return back1[x, y];
        }

        private void CheckGrids()
        {
            var newWidth = WindowWidth;
            var newHeight = WindowHeight - 1;
            if (newWidth != Width
                || newHeight != Height)
            {
                var lastBack1 = back1;
                var lastBack2 = back2;
                var lastFore1 = fore1;
                var lastFore2 = fore2;
                var lastGrid1 = grid1;
                var lastGrid2 = grid2;

                back1 = new ConsoleColor[newWidth, newHeight];
                back2 = new ConsoleColor[newWidth, newHeight];
                fore1 = new ConsoleColor[newWidth, newHeight];
                fore2 = new ConsoleColor[newWidth, newHeight];
                grid1 = new char[newWidth, newHeight];
                grid2 = new char[newWidth, newHeight];

                for (var x = 0; x < newWidth; ++x)
                {
                    for (var y = 0; y < newHeight; ++y)
                    {
                        if (x < lastBack1?.GetWidth()
                            && y < lastBack1?.GetHeight())
                        {
                            back1[x, y] = lastBack1[x, y];
                            back2[x, y] = lastBack2[x, y];
                            fore1[x, y] = lastFore1[x, y];
                            fore2[x, y] = lastFore2[x, y];
                            grid1[x, y] = lastGrid1[x, y];
                            grid2[x, y] = lastGrid2[x, y];
                        }
                        else
                        {
                            back1[x, y] = ConsoleColor.Black;
                            back2[x, y] = ConsoleColor.Black;
                            fore1[x, y] = ConsoleColor.Gray;
                            fore2[x, y] = ConsoleColor.Gray;
                            grid1[x, y] = ' ';
                            grid2[x, y] = ' ';
                        }
                    }
                }
            }
        }

        public void Draw(int x, int y, char c, ConsoleColor f, ConsoleColor b)
        {
            back1[x, y] = b;
            fore1[x, y] = f;
            grid1[x, y] = c;
        }

        public void Flush()
        {
            for (var y = 0; y < Height; ++y)
            {
                for (var x = 0; x < Width; ++x)
                {
                    var changed = grid1[x, y] != grid2[x, y]
                        || fore1[x, y] != fore2[x, y]
                        || back1[x, y] != back2[x, y];

                    if (changed)
                    {
                        CheckCursor(x, y);

                        if (fore1[x, y] != lastFore)
                        {
                            lastFore = ForegroundColor = fore1[x, y];
                        }

                        if (back1[x, y] != lastBack)
                        {
                            lastBack = BackgroundColor = back1[x, y];
                        }

                        if (x < Width - 1)
                        {
                            Write(grid1[x, y]);
                        }
                        else
                        {
                            WriteLine(grid1[x, y]);
                        }
                    }

                    fore2[x, y] = fore1[x, y];
                    back2[x, y] = back1[x, y];
                    grid2[x, y] = grid1[x, y];
                }
            }

            CheckGrids();
        }

        private static void CheckCursor(int x, int y)
        {
            var rowChanged = y != CursorTop;
            var colChanged = x != CursorLeft;
            if (rowChanged && colChanged)
            {
                SetCursorPosition(x, y);
            }
            else if (colChanged)
            {
                CursorLeft = x;
            }
            else if (rowChanged)
            {
                CursorTop = y;
            }
        }

        public void Prompt(string message, bool pause)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            this.Draw(0, Height - 1, message, ConsoleColor.Red, ConsoleColor.DarkRed);
            Flush();

            CursorLeft = message.Length;
            CursorTop = Height - 1;

            if (pause)
            {
                _ = ReadKey(true);
            }
        }

        public static bool SetFont(int index)
        {
#if NETSTANDARD || NETCOREAPP
            return false;
#else
            var hnd = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
            if (hnd == NativeMethods.INVALID_HANDLE_VALUE)
            {
                return false;
            }


            var info = new NativeMethods.CONSOLE_FONT_INFO_EX();
            info.cbSize = (uint)Marshal.SizeOf(info);
            info.nFont = (uint)index;
            return NativeMethods.SetCurrentConsoleFontEx(hnd, false, info);
#endif
        }

        public static bool SetFont(string fontFace)
        {
            if (fontFace is null)
            {
                throw new ArgumentNullException(nameof(fontFace));
            }

#if NETSTANDARD || NETCOREAPP
            return false;
#else
            unsafe
            {
                var hnd = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
                if (hnd != NativeMethods.INVALID_HANDLE_VALUE)
                {
                    var info = new NativeMethods.CONSOLE_FONT_INFO_EX();
                    info.cbSize = (uint)Marshal.SizeOf(info);
                    if (NativeMethods.GetCurrentConsoleFontEx(hnd, false, ref info))
                    {
                        var curFontFace = string.Intern(new string(info.FaceName));
                        if (curFontFace != fontFace)
                        {
                            var newInfo = new NativeMethods.CONSOLE_FONT_INFO_EX();
                            newInfo.cbSize = (uint)Marshal.SizeOf(newInfo);
                            newInfo.FontFamily = NativeMethods.TMPF_TRUETYPE;
                            var ptr = new IntPtr(newInfo.FaceName);
                            Marshal.Copy(fontFace.ToCharArray(), 0, ptr, fontFace.Length);
                            var size = Math.Max(info.dwFontSize.X, info.dwFontSize.Y);
                            newInfo.dwFontSize = new NativeMethods.COORD(size, size);
                            newInfo.FontWeight = info.FontWeight;
                            return NativeMethods.SetCurrentConsoleFontEx(hnd, false, newInfo);
                        }
                    }
                }
            }

            return false;
#endif
        }

        public static bool SetFontSize(int width, int height)
        {
#if NETSTANDARD || NETCOREAPP
            return false;
#else
            unsafe
            {
                var hnd = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
                if (hnd != NativeMethods.INVALID_HANDLE_VALUE)
                {
                    var info = new NativeMethods.CONSOLE_FONT_INFO_EX();
                    info.cbSize = (uint)Marshal.SizeOf(info);
                    if (NativeMethods.GetCurrentConsoleFontEx(hnd, false, ref info))
                    {
                        info.dwFontSize.X = (short)width;
                        info.dwFontSize.Y = (short)height;
                        return NativeMethods.SetCurrentConsoleFontEx(hnd, false, info);
                    }
                }
            }

            return false;
#endif
        }

        public static bool IsKeyDown(VirtualKeyState key)
        {
#if NETSTANDARD || NETCOREAPP
            return false;
#else
            return NativeMethods.GetKeyState(key) < 0;
#endif
        }

#if !NETSTANDARD && !NETCOREAPP
        internal static class NativeMethods
        {
            [DllImport("user32")]
            internal static extern short GetKeyState(VirtualKeyState key);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int nStdHandle);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern bool GetCurrentConsoleFontEx(
                   IntPtr consoleOutput,
                   bool maximumWindow,
                   ref CONSOLE_FONT_INFO_EX lpConsoleCurrentFontEx);

            [DllImport("kernel32", SetLastError = true)]
            internal static extern bool SetCurrentConsoleFontEx(
                   IntPtr consoleOutput,
                   bool maximumWindow,
                   CONSOLE_FONT_INFO_EX consoleCurrentFontEx);

            internal const int STD_OUTPUT_HANDLE = -11;
            internal const int TMPF_TRUETYPE = 4;
            internal const int LF_FACESIZE = 32;
            internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [StructLayout(LayoutKind.Sequential)]
            internal struct COORD
            {
                internal short X;
                internal short Y;

                internal COORD(short x, short y)
                {
                    X = x;
                    Y = y;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            internal unsafe struct CONSOLE_FONT_INFO_EX
            {
                internal uint cbSize;
                internal uint nFont;
                internal COORD dwFontSize;
                internal int FontFamily;
                internal int FontWeight;
                internal fixed char FaceName[LF_FACESIZE];
            }
        }
#endif
    }
}
