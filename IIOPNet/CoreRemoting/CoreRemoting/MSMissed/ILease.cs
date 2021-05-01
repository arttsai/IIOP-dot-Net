// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Lifetime.ILease
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace CoreRemoting
{
  [ComVisible(true)]
  public interface ILease
  {
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    void Register(ISponsor obj, TimeSpan renewalTime);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    void Register(ISponsor obj);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    void Unregister(ISponsor obj);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    TimeSpan Renew(TimeSpan renewalTime);

    TimeSpan RenewOnCallTime { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] set; }

    TimeSpan SponsorshipTimeout { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] set; }

    TimeSpan InitialLeaseTime { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] set; }

    TimeSpan CurrentLeaseTime { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }

    LeaseState CurrentState { [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get; }
  }
}
