// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.ServerIdentity
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace CoreRemoting
{
  internal class ServerIdentity : Identity
  {
    internal Context _srvCtx;
    internal IMessageSink _serverObjectChain;
    internal StackBuilderSink _stackBuilderSink;
    internal DynamicPropertyHolder _dphSrv;
    internal Type _srvType;
    private ServerIdentity.LastCalledType _lastCalledType;
    internal bool _bMarshaledAsSpecificType;
    internal int _firstCallDispatched;
    internal GCHandle _srvIdentityHandle;

    internal Type GetLastCalledType(string newTypeName)
    {
      ServerIdentity.LastCalledType lastCalledType = this._lastCalledType;
      if (lastCalledType == null)
        return (Type) null;
      string typeName = lastCalledType.typeName;
      Type type = lastCalledType.type;
      if (typeName == null || type == null)
        return (Type) null;
      return typeName.Equals(newTypeName) ? type : (Type) null;
    }

    internal void SetLastCalledType(string newTypeName, Type newType) => this._lastCalledType = new ServerIdentity.LastCalledType()
    {
      typeName = newTypeName,
      type = newType
    };

    internal void SetHandle()
    {
      bool tookLock = false;
      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Monitor.ReliableEnter((object) this, ref tookLock);
        if (!this._srvIdentityHandle.IsAllocated)
          this._srvIdentityHandle = new GCHandle((object) this, GCHandleType.Normal);
        else
          this._srvIdentityHandle.Target = (object) this;
      }
      finally
      {
        if (tookLock)
          Monitor.Exit((object) this);
      }
    }

    internal void ResetHandle()
    {
      bool tookLock = false;
      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Monitor.ReliableEnter((object) this, ref tookLock);
        this._srvIdentityHandle.Target = (object) null;
      }
      finally
      {
        if (tookLock)
          Monitor.Exit((object) this);
      }
    }

    internal GCHandle GetHandle() => this._srvIdentityHandle;

    internal ServerIdentity(MarshalByRefObject obj, Context serverCtx)
      : base(obj is ContextBoundObject)
    {
      if (obj != null)
        this._srvType = RemotingServices.IsTransparentProxy((object) obj) ? RemotingServices.GetRealProxy((object) obj).GetProxiedType() : obj.GetType();
      this._srvCtx = serverCtx;
      this._serverObjectChain = (IMessageSink) null;
      this._stackBuilderSink = (StackBuilderSink) null;
    }

    internal ServerIdentity(MarshalByRefObject obj, Context serverCtx, string uri)
      : this(obj, serverCtx)
    {
      this.SetOrCreateURI(uri, true);
    }

    internal Context ServerContext
    {
      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)] get => this._srvCtx;
    }

    internal void SetSingleCallObjectMode() => this._flags |= 512;

    internal void SetSingletonObjectMode() => this._flags |= 1024;

    internal bool IsSingleCall() => (this._flags & 512) != 0;

    internal bool IsSingleton() => (this._flags & 1024) != 0;

    internal IMessageSink GetServerObjectChain(out MarshalByRefObject obj)
    {
      obj = (MarshalByRefObject) null;
      if (!this.IsSingleCall())
      {
        if (this._serverObjectChain == null)
        {
          bool tookLock = false;
          RuntimeHelpers.PrepareConstrainedRegions();
          try
          {
            Monitor.ReliableEnter((object) this, ref tookLock);
            if (this._serverObjectChain == null)
              this._serverObjectChain = this._srvCtx.CreateServerObjectChain(this.TPOrObject);
          }
          finally
          {
            if (tookLock)
              Monitor.Exit((object) this);
          }
        }
        return this._serverObjectChain;
      }
      MarshalByRefObject serverObj;
      IMessageSink messageSink;
      if (this._tpOrObject != null && this._firstCallDispatched == 0 && Interlocked.CompareExchange(ref this._firstCallDispatched, 1, 0) == 0)
      {
        serverObj = (MarshalByRefObject) this._tpOrObject;
        messageSink = this._serverObjectChain ?? this._srvCtx.CreateServerObjectChain(serverObj);
      }
      else
      {
        serverObj = (MarshalByRefObject) Activator.CreateInstance(this._srvType, true);
        if (RemotingServices.GetObjectUri(serverObj) != null)
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_WellKnown_CtorCantMarshal"), (object) this.URI));
        if (!RemotingServices.IsTransparentProxy((object) serverObj))
          serverObj.__RaceSetServerIdentity(this);
        else
          RemotingServices.GetRealProxy((object) serverObj).IdentityObject = (Identity) this;
        messageSink = this._srvCtx.CreateServerObjectChain(serverObj);
      }
      obj = serverObj;
      return messageSink;
    }

    internal Type ServerType
    {
      get => this._srvType;
      set => this._srvType = value;
    }

    internal bool MarshaledAsSpecificType
    {
      get => this._bMarshaledAsSpecificType;
      set => this._bMarshaledAsSpecificType = value;
    }

    internal IMessageSink RaceSetServerObjectChain(IMessageSink serverObjectChain)
    {
      if (this._serverObjectChain == null)
      {
        bool tookLock = false;
        RuntimeHelpers.PrepareConstrainedRegions();
        try
        {
          Monitor.ReliableEnter((object) this, ref tookLock);
          if (this._serverObjectChain == null)
            this._serverObjectChain = serverObjectChain;
        }
        finally
        {
          if (tookLock)
            Monitor.Exit((object) this);
        }
      }
      return this._serverObjectChain;
    }

    internal bool AddServerSideDynamicProperty(IDynamicProperty prop)
    {
      if (this._dphSrv == null)
      {
        DynamicPropertyHolder dynamicPropertyHolder = new DynamicPropertyHolder();
        bool tookLock = false;
        RuntimeHelpers.PrepareConstrainedRegions();
        try
        {
          Monitor.ReliableEnter((object) this, ref tookLock);
          if (this._dphSrv == null)
            this._dphSrv = dynamicPropertyHolder;
        }
        finally
        {
          if (tookLock)
            Monitor.Exit((object) this);
        }
      }
      return this._dphSrv.AddDynamicProperty(prop);
    }

    internal bool RemoveServerSideDynamicProperty(string name) => this._dphSrv != null ? this._dphSrv.RemoveDynamicProperty(name) : throw new ArgumentException(Environment.GetResourceString("Arg_PropNotFound"));

    internal ArrayWithSize ServerSideDynamicSinks => this._dphSrv == null ? (ArrayWithSize) null : this._dphSrv.DynamicSinks;

    internal override void AssertValid()
    {
      if (this.TPOrObject == null)
        return;
      RemotingServices.IsTransparentProxy((object) this.TPOrObject);
    }

    private class LastCalledType
    {
      public string typeName;
      public Type type;
    }
  }
}
