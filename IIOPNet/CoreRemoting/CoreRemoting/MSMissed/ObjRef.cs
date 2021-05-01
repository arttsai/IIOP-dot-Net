// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.ObjRef
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using CoreRemoting.ClassicRemotingApi;

namespace CoreRemoting
{
  [ComVisible(true)]
  [Serializable]
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  public class ObjRef : IObjectReference, ISerializable
  {
    internal const int FLG_MARSHALED_OBJECT = 1;
    internal const int FLG_WELLKNOWN_OBJREF = 2;
    internal const int FLG_LITE_OBJREF = 4;
    internal const int FLG_PROXY_ATTRIBUTE = 8;
    internal string uri;
    internal IRemotingTypeInfo typeInfo;
    internal IEnvoyInfo envoyInfo;
    internal IChannelInfo channelInfo;
    internal int objrefFlags;
    internal GCHandle srvIdentity;
    internal int domainID;
    private static Type orType = typeof (ObjRef);

    internal void SetServerIdentity(GCHandle hndSrvIdentity) => this.srvIdentity = hndSrvIdentity;

    internal GCHandle GetServerIdentity() => this.srvIdentity;

    internal void SetDomainID(int id) => this.domainID = id;

    internal int GetDomainID() => this.domainID;

    private ObjRef(ObjRef o)
    {
      this.uri = o.uri;
      this.typeInfo = o.typeInfo;
      this.envoyInfo = o.envoyInfo;
      this.channelInfo = o.channelInfo;
      this.objrefFlags = o.objrefFlags;
      this.SetServerIdentity(o.GetServerIdentity());
      this.SetDomainID(o.GetDomainID());
    }

    public ObjRef(MarshalByRefObject o, Type requestedType)
    {
      Identity identity = MarshalByRefObject.GetIdentity(o, out bool _);
      this.Init((object) o, identity, requestedType);
    }

    protected ObjRef(SerializationInfo info, StreamingContext context)
    {
      string str = (string) null;
      bool flag = false;
      SerializationInfoEnumerator enumerator = info.GetEnumerator();
      while (enumerator.MoveNext())
      {
        if (enumerator.Name.Equals(nameof (uri)))
          this.uri = (string) enumerator.Value;
        else if (enumerator.Name.Equals(nameof (typeInfo)))
          this.typeInfo = (IRemotingTypeInfo) enumerator.Value;
        else if (enumerator.Name.Equals(nameof (envoyInfo)))
          this.envoyInfo = (IEnvoyInfo) enumerator.Value;
        else if (enumerator.Name.Equals(nameof (channelInfo)))
          this.channelInfo = (IChannelInfo) enumerator.Value;
        else if (enumerator.Name.Equals(nameof (objrefFlags)))
        {
          object obj = enumerator.Value;
          this.objrefFlags = obj.GetType() != typeof (string) ? (int) obj : ((IConvertible) obj).ToInt32((IFormatProvider) null);
        }
        else if (enumerator.Name.Equals("fIsMarshalled"))
        {
          object obj = enumerator.Value;
          if ((obj.GetType() != typeof (string) ? (int) obj : ((IConvertible) obj).ToInt32((IFormatProvider) null)) == 0)
            flag = true;
        }
        else if (enumerator.Name.Equals("url"))
          str = (string) enumerator.Value;
        else if (enumerator.Name.Equals("SrvIdentity"))
          this.SetServerIdentity((GCHandle) enumerator.Value);
        else if (enumerator.Name.Equals("DomainId"))
          this.SetDomainID((int) enumerator.Value);
      }
      if (!flag)
        this.objrefFlags |= 1;
      else
        this.objrefFlags &= -2;
      if (str == null)
        return;
      this.uri = str;
      this.objrefFlags |= 4;
    }

    internal bool CanSmuggle()
    {
      if (this.GetType() != typeof (ObjRef) || this.IsObjRefLite())
        return false;
      Type type1 = (Type) null;
      if (this.typeInfo != null)
        type1 = this.typeInfo.GetType();
      Type type2 = (Type) null;
      if (this.channelInfo != null)
        type2 = this.channelInfo.GetType();
      if (type1 != null && type1 != typeof (CoreRemoting.TypeInfo) && type1 != typeof (DynamicTypeInfo) || (this.envoyInfo != null || type2 != null && type2 != typeof (CoreRemoting.ChannelInfo)))
        return false;
      if (this.channelInfo != null)
      {
        foreach (object obj in this.channelInfo.ChannelData)
        {
          if (!(obj is CrossAppDomainData))
            return false;
        }
      }
      return true;
    }

    internal ObjRef CreateSmuggleableCopy() => new ObjRef(this);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof (info));
      info.SetType(ObjRef.orType);
      if (!this.IsObjRefLite())
      {
        info.AddValue("uri", (object) this.uri, typeof (string));
        info.AddValue("objrefFlags", this.objrefFlags);
        info.AddValue("typeInfo", (object) this.typeInfo, typeof (IRemotingTypeInfo));
        info.AddValue("envoyInfo", (object) this.envoyInfo, typeof (IEnvoyInfo));
        info.AddValue("channelInfo", (object) this.GetChannelInfoHelper(), typeof (IChannelInfo));
      }
      else
        info.AddValue("url", (object) this.uri, typeof (string));
    }

    private IChannelInfo GetChannelInfoHelper()
    {
      if (!(this.channelInfo is System.Runtime.Remoting.ChannelInfo channelInfo1))
        return this.channelInfo;
      object[] channelData = channelInfo1.ChannelData;
      if (channelData == null)
        return (IChannelInfo) channelInfo1;
      string[] data = (string[]) CallContext.GetData("__bashChannelUrl");
      if (data == null)
        return (IChannelInfo) channelInfo1;
      string str1 = data[0];
      string str2 = data[1];
      System.Runtime.Remoting.ChannelInfo channelInfo2 = new System.Runtime.Remoting.ChannelInfo();
      channelInfo2.ChannelData = new object[channelData.Length];
      for (int index = 0; index < channelData.Length; ++index)
      {
        channelInfo2.ChannelData[index] = channelData[index];
        if (channelInfo2.ChannelData[index] is ChannelDataStore channelDataStore3)
        {
          string[] channelUris = channelDataStore3.ChannelUris;
          if (channelUris != null && channelUris.Length == 1 && channelUris[0].Equals(str1))
          {
            ChannelDataStore channelDataStore = channelDataStore3.InternalShallowCopy();
            channelDataStore.ChannelUris = new string[1];
            channelDataStore.ChannelUris[0] = str2;
            channelInfo2.ChannelData[index] = (object) channelDataStore;
          }
        }
      }
      return (IChannelInfo) channelInfo2;
    }

    public virtual string URI
    {
      get => this.uri;
      set => this.uri = value;
    }

    public virtual IRemotingTypeInfo TypeInfo
    {
      get => this.typeInfo;
      set => this.typeInfo = value;
    }

    public virtual IEnvoyInfo EnvoyInfo
    {
      get => this.envoyInfo;
      set => this.envoyInfo = value;
    }

    public virtual IChannelInfo ChannelInfo
    {
      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] get => this.channelInfo;
      set => this.channelInfo = value;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual object GetRealObject(StreamingContext context) => this.GetRealObjectHelper();

    internal object GetRealObjectHelper()
    {
      if (!this.IsMarshaledObject())
        return (object) this;
      if (this.IsObjRefLite())
      {
        int num = this.uri.IndexOf(RemotingConfiguration.ApplicationId);
        if (num > 0)
          this.uri = this.uri.Substring(num - 1);
      }
      return this.GetCustomMarshaledCOMObject(RemotingServices.Unmarshal(this, this.GetType() != typeof (ObjRef)));
    }

    private object GetCustomMarshaledCOMObject(object ret)
    {
      if (this.TypeInfo is DynamicTypeInfo)
      {
        IntPtr iunknown = Win32Native.NULL;
        if (this.IsFromThisProcess())
        {
          if (!this.IsFromThisAppDomain())
          {
            try
            {
              bool fIsURTAggregated;
              iunknown = ((__ComObject) ret).GetIUnknown(out fIsURTAggregated);
              if (iunknown != Win32Native.NULL)
              {
                if (!fIsURTAggregated)
                {
                  string typeName1 = this.TypeInfo.TypeName;
                  string typeName2 = (string) null;
                  string assemName = (string) null;
                  System.Runtime.Remoting.TypeInfo.ParseTypeAndAssembly(typeName1, out typeName2, out assemName);
                  Type t = (FormatterServices.LoadAssemblyFromStringNoThrow(assemName) ?? throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Serialization_AssemblyNotFound"), (object) assemName))).GetType(typeName2, false, false);
                  if (t != null && !t.IsVisible)
                    t = (Type) null;
                  object objectForIunknown = Marshal.GetTypedObjectForIUnknown(iunknown, t);
                  if (objectForIunknown != null)
                    ret = objectForIunknown;
                }
              }
            }
            finally
            {
              if (iunknown != Win32Native.NULL)
                Marshal.Release(iunknown);
            }
          }
        }
      }
      return ret;
    }

    public ObjRef() => this.objrefFlags = 0;

    internal bool IsMarshaledObject() => (this.objrefFlags & 1) == 1;

    internal void SetMarshaledObject() => this.objrefFlags |= 1;

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    internal bool IsWellKnown() => (this.objrefFlags & 2) == 2;

    internal void SetWellKnown() => this.objrefFlags |= 2;

    internal bool HasProxyAttribute() => (this.objrefFlags & 8) == 8;

    internal void SetHasProxyAttribute() => this.objrefFlags |= 8;

    internal bool IsObjRefLite() => (this.objrefFlags & 4) == 4;

    internal void SetObjRefLite() => this.objrefFlags |= 4;

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    private CrossAppDomainData GetAppDomainChannelData()
    {
      int index = 0;
      for (; index < this.ChannelInfo.ChannelData.Length; ++index)
      {
        if (this.ChannelInfo.ChannelData[index] is CrossAppDomainData crossAppDomainData1)
          return crossAppDomainData1;
      }
      return (CrossAppDomainData) null;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public bool IsFromThisProcess()
    {
      if (this.IsWellKnown())
        return false;
      CrossAppDomainData domainChannelData = this.GetAppDomainChannelData();
      return domainChannelData != null && domainChannelData.IsFromThisProcess();
    }

    public bool IsFromThisAppDomain()
    {
      CrossAppDomainData domainChannelData = this.GetAppDomainChannelData();
      return domainChannelData != null && domainChannelData.IsFromThisAppDomain();
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    internal int GetServerDomainId() => !this.IsFromThisProcess() ? 0 : this.GetAppDomainChannelData().DomainID;

    internal IntPtr GetServerContext(out int domainId)
    {
      IntPtr num = IntPtr.Zero;
      domainId = 0;
      if (this.IsFromThisProcess())
      {
        CrossAppDomainData domainChannelData = this.GetAppDomainChannelData();
        domainId = domainChannelData.DomainID;
        if (AppDomain.IsDomainIdValid(domainChannelData.DomainID))
          num = domainChannelData.ContextID;
      }
      return num;
    }

    internal void Init(object o, Identity idObj, Type requestedType)
    {
      this.uri = idObj.URI;
      MarshalByRefObject tpOrObject = idObj.TPOrObject;
      Type c = RemotingServices.IsTransparentProxy((object) tpOrObject) ? RemotingServices.GetRealProxy((object) tpOrObject).GetProxiedType() : tpOrObject.GetType();
      Type type = requestedType == null ? c : requestedType;
      if (requestedType != null && !requestedType.IsAssignableFrom(c) && !typeof (IMessageSink).IsAssignableFrom(c))
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_InvalidRequestedType"), (object) requestedType.ToString()));
      this.TypeInfo = !c.IsCOMObject ? (IRemotingTypeInfo) InternalRemotingServices.GetReflectionCachedData(type).TypeInfo : (IRemotingTypeInfo) new DynamicTypeInfo(type);
      if (!idObj.IsWellKnown())
      {
        this.EnvoyInfo = System.Runtime.Remoting.EnvoyInfo.CreateEnvoyInfo(idObj as ServerIdentity);
        IChannelInfo channelInfo = (IChannelInfo) new System.Runtime.Remoting.ChannelInfo();
        if (o is AppDomain)
        {
          object[] channelData = channelInfo.ChannelData;
          int length = channelData.Length;
          object[] objArray = new object[length];
          Array.Copy((Array) channelData, (Array) objArray, length);
          for (int index = 0; index < length; ++index)
          {
            if (!(objArray[index] is CrossAppDomainData))
              objArray[index] = (object) null;
          }
          channelInfo.ChannelData = objArray;
        }
        this.ChannelInfo = channelInfo;
        if (c.HasProxyAttribute)
          this.SetHasProxyAttribute();
      }
      else
        this.SetWellKnown();
      if (!ObjRef.ShouldUseUrlObjRef())
        return;
      if (this.IsWellKnown())
      {
        this.SetObjRefLite();
      }
      else
      {
        string httpUrlForObject = ChannelServices.FindFirstHttpUrlForObject(this.URI);
        if (httpUrlForObject == null)
          return;
        this.URI = httpUrlForObject;
        this.SetObjRefLite();
      }
    }

    internal static bool ShouldUseUrlObjRef() => RemotingConfigHandler.UrlObjRefMode;

    internal static bool IsWellFormed(ObjRef objectRef)
    {
      bool flag = true;
      if (objectRef == null || objectRef.URI == null || !objectRef.IsWellKnown() && !objectRef.IsObjRefLite() && (objectRef.GetType() == ObjRef.orType && objectRef.ChannelInfo == null))
        flag = false;
      return flag;
    }
  }
}
