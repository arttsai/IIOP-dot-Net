// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Contexts.ArrayWithSize
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

namespace CoreRemoting
{
    internal class ArrayWithSize
    {
        internal IDynamicMessageSink[] Sinks;
        internal int Count;

        internal ArrayWithSize(IDynamicMessageSink[] sinks, int count)
        {
            this.Sinks = sinks;
            this.Count = count;
        }
    }
}