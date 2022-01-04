using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Automation;
using System.Text;

public static class Program
{
    internal enum AccentState
    {
        ACCENT_ENABLE_BLURBEHIND = 3
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }


    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }


    internal enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

    [DllImport("USER32.DLL")]
    private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

    [DllImport("User32.dll")]
    private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private static void Apply(IntPtr hwnd)
    {
        var accent = new AccentPolicy();
        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND /* AccentState.ACCENT_INVALID_STATE */;
        // accent.AccentFlags = 2;
        // accent.GradientColor = (152 << 24) | (0x2B2B2B & 0xFFFFFF);

        var accentStructSize = Marshal.SizeOf(accent);


        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);


        var data = new WindowCompositionAttributeData();
        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
        data.SizeOfData = accentStructSize;
        data.Data = accentPtr;

        SetWindowCompositionAttribute(hwnd, ref data);

        Marshal.FreeHGlobal(accentPtr);

        Console.WriteLine($"applied to {hwnd}");
    }

    public static async Task Main()
    {
        Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Descendants, (s, _) =>
        {
            try
            {
                Console.WriteLine("New window!");
                var element = s as AutomationElement;
                Window(new IntPtr(element!.Current.NativeWindowHandle), 0);
            }
            catch { }
        });
        EnumWindows(Window, 0);
        await Task.Delay(-1);
    }

    public static bool Window(IntPtr hwnd, int lParam)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out uint procId);
            StringBuilder builder = new StringBuilder(256);
            GetClassName(hwnd, builder, builder.Capacity);
            if (Process.GetProcessById((int)procId).ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) && builder.ToString() == "CabinetWClass")
            {
                Console.WriteLine($"Found explorer window: {hwnd}");
                Apply(hwnd);
            }
        }
        catch { }

        return true;
    }
}