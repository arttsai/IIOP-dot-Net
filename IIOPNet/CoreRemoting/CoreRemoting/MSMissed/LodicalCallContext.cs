// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.LogicalCallContext
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Principal;

namespace CoreRemoting
{
  [Serializable]
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  public sealed class LogicalCallContext : ISerializable, ICloneable
  {
    private const string s_CorrelationMgrSlotName = "System.Diagnostics.Trace.CorrelationManagerSlot";
    private static Type s_callContextType = typeof (LogicalCallContext);
    private Hashtable m_Datastore;
    private CallContextRemotingData m_RemotingData;
    private CallContextSecurityData m_SecurityData;
    private object m_HostContext;
    private bool m_IsCorrelationMgr;
    private Header[] _sendHeaders;
    private Header[] _recvHeaders;

    internal LogicalCallContext()
    {
    }

    internal LogicalCallContext(SerializationInfo info, StreamingContext context)
    {
      SerializationInfoEnumerator enumerator = info.GetEnumerator();
      while (enumerator.MoveNext())
      {
        if (enumerator.Name.Equals("__RemotingData"))
          this.m_RemotingData = (CallContextRemotingData) enumerator.Value;
        else if (enumerator.Name.Equals("__SecurityData"))
        {
          if (context.State == StreamingContextStates.CrossAppDomain)
            this.m_SecurityData = (CallContextSecurityData) enumerator.Value;
        }
        else if (enumerator.Name.Equals("__HostContext"))
          this.m_HostContext = enumerator.Value;
        else if (enumerator.Name.Equals("__CorrelationMgrSlotPresent"))
          this.m_IsCorrelationMgr = (bool) enumerator.Value;
        else
          this.Datastore[(object) enumerator.Name] = enumerator.Value;
      }
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof (info));
      info.SetType(LogicalCallContext.s_callContextType);
      if (this.m_RemotingData != null)
        info.AddValue("__RemotingData", (object) this.m_RemotingData);
      if (this.m_SecurityData != null && context.State == StreamingContextStates.CrossAppDomain)
        info.AddValue("__SecurityData", (object) this.m_SecurityData);
      if (this.m_HostContext != null)
        info.AddValue("__HostContext", this.m_HostContext);
      if (this.m_IsCorrelationMgr)
        info.AddValue("__CorrelationMgrSlotPresent", this.m_IsCorrelationMgr);
      if (!this.HasUserData)
        return;
      IDictionaryEnumerator enumerator = this.m_Datastore.GetEnumerator();
      while (enumerator.MoveNext())
        info.AddValue((string) enumerator.Key, enumerator.Value);
    }

    public object Clone()
    {
      LogicalCallContext logicalCallContext = new LogicalCallContext();
      if (this.m_RemotingData != null)
        logicalCallContext.m_RemotingData = (CallContextRemotingData) this.m_RemotingData.Clone();
      if (this.m_SecurityData != null)
        logicalCallContext.m_SecurityData = (CallContextSecurityData) this.m_SecurityData.Clone();
      if (this.m_HostContext != null)
        logicalCallContext.m_HostContext = this.m_HostContext;
      logicalCallContext.m_IsCorrelationMgr = this.m_IsCorrelationMgr;
      if (this.HasUserData)
      {
        IDictionaryEnumerator enumerator = this.m_Datastore.GetEnumerator();
        if (!this.m_IsCorrelationMgr)
        {
          while (enumerator.MoveNext())
            logicalCallContext.Datastore[(object) (string) enumerator.Key] = enumerator.Value;
        }
        else
        {
          while (enumerator.MoveNext())
          {
            string key = (string) enumerator.Key;
            if (key.Equals("System.Diagnostics.Trace.CorrelationManagerSlot"))
              logicalCallContext.Datastore[(object) key] = ((ICloneable) enumerator.Value).Clone();
            else
              logicalCallContext.Datastore[(object) key] = enumerator.Value;
          }
        }
      }
      return (object) logicalCallContext;
    }

    internal void Merge(LogicalCallContext lc)
    {
      if (lc == null || this == lc || !lc.HasUserData)
        return;
      IDictionaryEnumerator enumerator = lc.Datastore.GetEnumerator();
      while (enumerator.MoveNext())
        this.Datastore[(object) (string) enumerator.Key] = enumerator.Value;
    }

    public bool HasInfo
    {
      get
      {
        bool flag = false;
        if (this.m_RemotingData != null && this.m_RemotingData.HasInfo || this.m_SecurityData != null && this.m_SecurityData.HasInfo || (this.m_HostContext != null || this.HasUserData))
          flag = true;
        return flag;
      }
    }

    private bool HasUserData => this.m_Datastore != null && this.m_Datastore.Count > 0;

    internal CallContextRemotingData RemotingData
    {
      get
      {
        if (this.m_RemotingData == null)
          this.m_RemotingData = new CallContextRemotingData();
        return this.m_RemotingData;
      }
    }

    internal CallContextSecurityData SecurityData
    {
      get
      {
        if (this.m_SecurityData == null)
          this.m_SecurityData = new CallContextSecurityData();
        return this.m_SecurityData;
      }
    }

    internal object HostContext
    {
      get => this.m_HostContext;
      set => this.m_HostContext = value;
    }

    private Hashtable Datastore
    {
      get
      {
        if (this.m_Datastore == null)
          this.m_Datastore = new Hashtable();
        return this.m_Datastore;
      }
    }

    internal IPrincipal Principal
    {
      get => this.m_SecurityData != null ? this.m_SecurityData.Principal : (IPrincipal) null;
      set => this.SecurityData.Principal = value;
    }

    public void FreeNamedDataSlot(string name) => this.Datastore.Remove((object) name);

    public object GetData(string name) => this.Datastore[(object) name];

    public void SetData(string name, object data)
    {
      this.Datastore[(object) name] = data;
      if (!name.Equals("System.Diagnostics.Trace.CorrelationManagerSlot"))
        return;
      this.m_IsCorrelationMgr = true;
    }

    private Header[] InternalGetOutgoingHeaders()
    {
      Header[] sendHeaders = this._sendHeaders;
      this._sendHeaders = (Header[]) null;
      this._recvHeaders = (Header[]) null;
      return sendHeaders;
    }

    internal void InternalSetHeaders(Header[] headers)
    {
      this._sendHeaders = headers;
      this._recvHeaders = (Header[]) null;
    }

    internal Header[] InternalGetHeaders() => this._sendHeaders != null ? this._sendHeaders : this._recvHeaders;

    internal IPrincipal RemovePrincipalIfNotSerializable()
    {
      IPrincipal principal = this.Principal;
      if (principal != null && !principal.GetType().IsSerializable)
        this.Principal = (IPrincipal) null;
      return principal;
    }

    internal void PropagateOutgoingHeadersToMessage(IMessage msg)
    {
      Header[] outgoingHeaders = this.InternalGetOutgoingHeaders();
      if (outgoingHeaders == null)
        return;
      IDictionary properties = msg.Properties;
      foreach (Header header in outgoingHeaders)
      {
        if (header != null)
        {
          string propertyKeyForHeader = LogicalCallContext.GetPropertyKeyForHeader(header);
          properties[(object) propertyKeyForHeader] = (object) header;
        }
      }
    }

    internal static string GetPropertyKeyForHeader(Header header)
    {
      if (header == null)
        return (string) null;
      return header.HeaderNamespace != null ? header.Name + ", " + header.HeaderNamespace : header.Name;
    }

    internal void PropagateIncomingHeadersToCallContext(IMessage msg)
    {
      if (msg is IInternalMessage internalMessage && !internalMessage.HasProperties())
        return;
      IDictionaryEnumerator enumerator = msg.Properties.GetEnumerator();
      int length = 0;
      while (enumerator.MoveNext())
      {
        if (!((string) enumerator.Key).StartsWith("__", StringComparison.Ordinal) && enumerator.Value is Header)
          ++length;
      }
      Header[] headerArray = (Header[]) null;
      if (length > 0)
      {
        headerArray = new Header[length];
        int num = 0;
        enumerator.Reset();
        while (enumerator.MoveNext())
        {
          if (!((string) enumerator.Key).StartsWith("__", StringComparison.Ordinal) && enumerator.Value is Header header4)
            headerArray[num++] = header4;
        }
      }
      this._recvHeaders = headerArray;
      this._sendHeaders = (Header[]) null;
    }
  }
}
