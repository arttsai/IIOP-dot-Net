// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.ISerializationRootObject
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Runtime.Serialization;
using System.Security.Permissions;

namespace CoreRemoting
{
    internal interface ISerializationRootObject
    {
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        void RootSetObjectData(SerializationInfo info, StreamingContext ctx);
    }
}