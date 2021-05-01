// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.Header
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Runtime.InteropServices;

namespace CoreRemoting
{
    [ComVisible(true)]
    [Serializable]
    public class Header
    {
        public string Name;
        public object Value;
        public bool MustUnderstand;
        public string HeaderNamespace;

        public Header(string _Name, object _Value)
            : this(_Name, _Value, true)
        {
        }

        public Header(string _Name, object _Value, bool _MustUnderstand)
        {
            this.Name = _Name;
            this.Value = _Value;
            this.MustUnderstand = _MustUnderstand;
        }

        public Header(string _Name, object _Value, bool _MustUnderstand, string _HeaderNamespace)
        {
            this.Name = _Name;
            this.Value = _Value;
            this.MustUnderstand = _MustUnderstand;
            this.HeaderNamespace = _HeaderNamespace;
        }
    }
}