// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Metadata.SoapOption
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Runtime.InteropServices;

namespace CoreRemoting
{
    [Flags]
    [ComVisible(true)]
    [Serializable]
    public enum SoapOption
    {
        None = 0,
        AlwaysIncludeTypes = 1,
        XsdString = 2,
        EmbedAll = 4,
        Option1 = 8,
        Option2 = 16, // 0x00000010
    }
}