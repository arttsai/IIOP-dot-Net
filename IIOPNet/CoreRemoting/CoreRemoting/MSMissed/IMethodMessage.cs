// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.IMethodMessage
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace CoreRemoting
{
    [ComVisible(true)]
    public interface IMethodMessage : IMessage
    {
        string Uri { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        string MethodName { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        string TypeName { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        object MethodSignature { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        int ArgCount { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        string GetArgName(int index);

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        object GetArg(int argNum);

        object[] Args { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        bool HasVarArgs { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        LogicalCallContext LogicalCallContext { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

        MethodBase MethodBase { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }
    }
}