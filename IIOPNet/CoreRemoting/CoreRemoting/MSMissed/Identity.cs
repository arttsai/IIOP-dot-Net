// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Identity
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading;
using CoreRemoting.ClassicRemotingApi;
using MarshalByRefObject = CoreRemoting.MSMissed.MarshalByRefObject;

namespace CoreRemoting
{
  internal class Identity
  {
    protected const int IDFLG_DISCONNECTED_FULL = 1;
    protected const int IDFLG_DISCONNECTED_REM = 2;
    protected const int IDFLG_IN_IDTABLE = 4;
    protected const int IDFLG_CONTEXT_BOUND = 16;
    protected const int IDFLG_WELLKNOWN = 256;
    protected const int IDFLG_SERVER_SINGLECALL = 512;
    protected const int IDFLG_SERVER_SINGLETON = 1024;
    private static string s_originalAppDomainGuid = Guid.NewGuid().ToString().Replace('-', '_');
    private static string s_configuredAppDomainGuid = (string) null;
    private static string s_originalAppDomainGuidString = "/" + Identity.s_originalAppDomainGuid.ToLower(CultureInfo.InvariantCulture) + "/";
    private static string s_configuredAppDomainGuidString = (string) null;
    private static string s_IDGuidString = "/" + Identity.s_originalAppDomainGuid.ToLower(CultureInfo.InvariantCulture) + "/";
    private static RNGCryptoServiceProvider s_rng = new RNGCryptoServiceProvider();
    internal int _flags;
    internal object _tpOrObject;
    protected string _ObjURI;
    protected string _URL;
    internal object _objRef;
    internal object _channelSink;
    internal object _envoyChain;
    internal DynamicPropertyHolder _dph;
    internal Lease _lease;

    internal static string ProcessIDGuid => SharedStatics.Remoting_Identity_IDGuid;

    internal static string AppDomainUniqueId => Identity.s_configuredAppDomainGuid != null ? Identity.s_configuredAppDomainGuid : Identity.s_originalAppDomainGuid;

    internal static string IDGuidString => Identity.s_IDGuidString;

    internal static string RemoveAppNameOrAppGuidIfNecessary(string uri)
    {
      if (uri == null || uri.Length <= 1 || uri[0] != '/')
        return uri;
      if (Identity.s_configuredAppDomainGuidString != null)
      {
        string domainGuidString = Identity.s_configuredAppDomainGuidString;
        if (uri.Length > domainGuidString.Length && Identity.StringStartsWith(uri, domainGuidString))
          return uri.Substring(domainGuidString.Length);
      }
      string domainGuidString1 = Identity.s_originalAppDomainGuidString;
      if (uri.Length > domainGuidString1.Length && Identity.StringStartsWith(uri, domainGuidString1))
        return uri.Substring(domainGuidString1.Length);
      string applicationName = RemotingConfiguration.ApplicationName;
      if (applicationName != null && uri.Length > applicationName.Length + 2 && (string.Compare(uri, 1, applicationName, 0, applicationName.Length, true, CultureInfo.InvariantCulture) == 0 && uri[applicationName.Length + 1] == '/'))
        return uri.Substring(applicationName.Length + 2);
      uri = uri.Substring(1);
      return uri;
    }

    private static bool StringStartsWith(string s1, string prefix) => s1.Length >= prefix.Length && string.CompareOrdinal(s1, 0, prefix, 0, prefix.Length) == 0;

    internal static string ProcessGuid => Identity.ProcessIDGuid;

    private static int GetNextSeqNum() => SharedStatics.Remoting_Identity_GetNextSeqNum();

    private static byte[] GetRandomBytes()
    {
      byte[] data = new byte[18];
      Identity.s_rng.GetBytes(data);
      return data;
    }

    internal Identity(string objURI, string URL)
    {
      if (URL != null)
      {
        this._flags |= 256;
        this._URL = URL;
      }
      this.SetOrCreateURI(objURI, true);
    }

    internal Identity(bool bContextBound)
    {
      if (!bContextBound)
        return;
      this._flags |= 16;
    }

    internal bool IsContextBound => (this._flags & 16) == 16;

    internal bool IsWellKnown() => (this._flags & 256) == 256;

    internal void SetInIDTable()
    {
      int flags;
      int num;
      do
      {
        flags = this._flags;
        num = this._flags | 4;
      }
      while (flags != Interlocked.CompareExchange(ref this._flags, num, flags));
    }

    internal void ResetInIDTable(bool bResetURI)
    {
      int flags;
      int num;
      do
      {
        flags = this._flags;
        num = this._flags & -5;
      }
      while (flags != Interlocked.CompareExchange(ref this._flags, num, flags));
      if (!bResetURI)
        return;
      ((ObjRef) this._objRef).URI = (string) null;
      this._ObjURI = (string) null;
    }

    internal bool IsInIDTable() => (this._flags & 4) == 4;

    internal void SetFullyConnected()
    {
      int flags;
      int num;
      do
      {
        flags = this._flags;
        num = this._flags & -4;
      }
      while (flags != Interlocked.CompareExchange(ref this._flags, num, flags));
    }

    internal bool IsFullyDisconnected() => (this._flags & 1) == 1;

    internal bool IsRemoteDisconnected() => (this._flags & 2) == 2;

    internal bool IsDisconnected() => this.IsFullyDisconnected() || this.IsRemoteDisconnected();

    internal string URI => this.IsWellKnown() ? this._URL : this._ObjURI;

    internal string ObjURI => this._ObjURI;

    internal MarshalByRefObject TPOrObject => (MarshalByRefObject) this._tpOrObject;

    internal object RaceSetTransparentProxy(object tpObj)
    {
      if (this._tpOrObject == null)
        Interlocked.CompareExchange(ref this._tpOrObject, tpObj, (object) null);
      return this._tpOrObject;
    }

    internal ObjRef ObjectRef => (ObjRef) this._objRef;

    internal ObjRef RaceSetObjRef(ObjRef objRefGiven)
    {
      if (this._objRef == null)
        Interlocked.CompareExchange(ref this._objRef, (object) objRefGiven, (object) null);
      return (ObjRef) this._objRef;
    }

    internal IMessageSink ChannelSink => (IMessageSink) this._channelSink;

    internal IMessageSink RaceSetChannelSink(IMessageSink channelSink)
    {
      if (this._channelSink == null)
        Interlocked.CompareExchange(ref this._channelSink, (object) channelSink, (object) null);
      return (IMessageSink) this._channelSink;
    }

    internal IMessageSink EnvoyChain => (IMessageSink) this._envoyChain;

    internal Lease Lease
    {
      get => this._lease;
      set => this._lease = value;
    }

    internal IMessageSink RaceSetEnvoyChain(IMessageSink envoyChain)
    {
      if (this._envoyChain == null)
        Interlocked.CompareExchange(ref this._envoyChain, (object) envoyChain, (object) null);
      return (IMessageSink) this._envoyChain;
    }

    internal void SetOrCreateURI(string uri) => this.SetOrCreateURI(uri, false);

    internal void SetOrCreateURI(string uri, bool bIdCtor)
    {
      if (!bIdCtor && this._ObjURI != null)
        throw new RemotingException(Environment.GetResourceString("Remoting_SetObjectUriForMarshal__UriExists"));
      if (uri == null)
        this._ObjURI = (Identity.IDGuidString + Convert.ToBase64String(Identity.GetRandomBytes()).Replace('/', '_') + "_" + (object) Identity.GetNextSeqNum() + ".rem").ToLower(CultureInfo.InvariantCulture);
      else if (this is ServerIdentity)
        this._ObjURI = Identity.IDGuidString + uri;
      else
        this._ObjURI = uri;
    }

    internal static string GetNewLogicalCallID() => Identity.IDGuidString + (object) Identity.GetNextSeqNum();

    [Conditional("_DEBUG")]
    internal virtual void AssertValid()
    {
      if (this.URI == null)
        return;
      IdentityHolder.ResolveIdentity(this.URI);
    }

    internal bool AddProxySideDynamicProperty(IDynamicProperty prop)
    {
      lock (this)
      {
        if (this._dph == null)
        {
          DynamicPropertyHolder dynamicPropertyHolder = new DynamicPropertyHolder();
          lock (this)
          {
            if (this._dph == null)
              this._dph = dynamicPropertyHolder;
          }
        }
        return this._dph.AddDynamicProperty(prop);
      }
    }

    internal bool RemoveProxySideDynamicProperty(string name)
    {
      lock (this)
        return this._dph != null ? this._dph.RemoveDynamicProperty(name) : throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Contexts_NoProperty"), (object) name));
    }

    internal ArrayWithSize ProxySideDynamicSinks => this._dph == null ? (ArrayWithSize) null : this._dph.DynamicSinks;
  }
}
