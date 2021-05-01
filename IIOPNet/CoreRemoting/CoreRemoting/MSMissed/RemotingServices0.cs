// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.RemotingServices
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Services;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace System.Runtime.Remoting
{
  [ComVisible(true)]
  public sealed class RemotingServices
  {
    private const BindingFlags LookupAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const string FieldGetterName = "FieldGetter";
    private const string FieldSetterName = "FieldSetter";
    private const string IsInstanceOfTypeName = "IsInstanceOfType";
    private const string CanCastToXmlTypeName = "CanCastToXmlType";
    private const string InvokeMemberName = "InvokeMember";
    internal static SecurityPermission s_RemotingInfrastructurePermission;
    internal static Assembly s_MscorlibAssembly;
    private static MethodBase s_FieldGetterMB;
    private static MethodBase s_FieldSetterMB;
    private static MethodBase s_IsInstanceOfTypeMB;
    private static MethodBase s_CanCastToXmlTypeMB;
    private static MethodBase s_InvokeMemberMB;
    private static bool s_bRemoteActivationConfigured;
    private static bool s_bRegisteredWellKnownChannels;
    private static bool s_bInProcessOfRegisteringWellKnownChannels;
    private static object s_delayLoadChannelLock;

    static RemotingServices()
    {
      CodeAccessPermission.AssertAllPossible();
      RemotingServices.s_RemotingInfrastructurePermission = new SecurityPermission(SecurityPermissionFlag.Infrastructure);
      RemotingServices.s_MscorlibAssembly = typeof (RemotingServices).Assembly;
      RemotingServices.s_FieldGetterMB = (MethodBase) null;
      RemotingServices.s_FieldSetterMB = (MethodBase) null;
      RemotingServices.s_bRemoteActivationConfigured = false;
      RemotingServices.s_bRegisteredWellKnownChannels = false;
      RemotingServices.s_bInProcessOfRegisteringWellKnownChannels = false;
      RemotingServices.s_delayLoadChannelLock = new object();
    }

    private RemotingServices()
    {
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern bool IsTransparentProxy(object proxy);

    public static bool IsObjectOutOfContext(object tp)
    {
      if (!RemotingServices.IsTransparentProxy(tp))
        return false;
      RealProxy realProxy = RemotingServices.GetRealProxy(tp);
      return !(realProxy.IdentityObject is ServerIdentity identityObject) || !(realProxy is RemotingProxy) || Thread.CurrentContext != identityObject.ServerContext;
    }

    public static bool IsObjectOutOfAppDomain(object tp) => RemotingServices.IsClientProxy(tp);

    internal static bool IsClientProxy(object obj)
    {
      if (!(obj is MarshalByRefObject marshalByRefObject))
        return false;
      bool flag = false;
      switch (MarshalByRefObject.GetIdentity(marshalByRefObject, out bool _))
      {
        case null:
        case ServerIdentity _:
          return flag;
        default:
          flag = true;
          goto case null;
      }
    }

    internal static bool IsObjectOutOfProcess(object tp)
    {
      if (!RemotingServices.IsTransparentProxy(tp))
        return false;
      Identity identityObject = RemotingServices.GetRealProxy(tp).IdentityObject;
      if (identityObject is ServerIdentity)
        return false;
      if (identityObject == null)
        return true;
      ObjRef objectRef = identityObject.ObjectRef;
      return objectRef == null || !objectRef.IsFromThisProcess();
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [MethodImpl(MethodImplOptions.InternalCall)]
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static extern RealProxy GetRealProxy(object proxy);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern object CreateTransparentProxy(
      RealProxy rp,
      RuntimeType typeToProxy,
      IntPtr stub,
      object stubData);

    internal static object CreateTransparentProxy(
      RealProxy rp,
      Type typeToProxy,
      IntPtr stub,
      object stubData)
    {
      if (!(typeToProxy is RuntimeType typeToProxy1))
        throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_WrongType"), (object) nameof (typeToProxy)));
      return RemotingServices.CreateTransparentProxy(rp, typeToProxy1, stub, stubData);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern MarshalByRefObject AllocateUninitializedObject(
      RuntimeType objectType);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void CallDefaultCtor(object o);

    internal static MarshalByRefObject AllocateUninitializedObject(Type objectType) => objectType is RuntimeType objectType1 ? RemotingServices.AllocateUninitializedObject(objectType1) : throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_WrongType"), (object) nameof (objectType)));

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern MarshalByRefObject AllocateInitializedObject(
      RuntimeType objectType);

    internal static MarshalByRefObject AllocateInitializedObject(Type objectType) => objectType is RuntimeType objectType1 ? RemotingServices.AllocateInitializedObject(objectType1) : throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_WrongType"), (object) nameof (objectType)));

    internal static bool RegisterWellKnownChannels()
    {
      if (!RemotingServices.s_bRegisteredWellKnownChannels)
      {
        bool tookLock = false;
        object configLock = Thread.GetDomain().RemotingData.ConfigLock;
        RuntimeHelpers.PrepareConstrainedRegions();
        try
        {
          Monitor.ReliableEnter(configLock, ref tookLock);
          if (!RemotingServices.s_bRegisteredWellKnownChannels)
          {
            if (!RemotingServices.s_bInProcessOfRegisteringWellKnownChannels)
            {
              RemotingServices.s_bInProcessOfRegisteringWellKnownChannels = true;
              CrossAppDomainChannel.RegisterChannel();
              RemotingServices.s_bRegisteredWellKnownChannels = true;
            }
          }
        }
        finally
        {
          if (tookLock)
            Monitor.Exit(configLock);
        }
      }
      return true;
    }

    internal static void InternalSetRemoteActivationConfigured()
    {
      if (RemotingServices.s_bRemoteActivationConfigured)
        return;
      RemotingServices.nSetRemoteActivationConfigured();
      RemotingServices.s_bRemoteActivationConfigured = true;
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern void nSetRemoteActivationConfigured();

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static string GetSessionIdForMethodMessage(IMethodMessage msg) => msg.Uri;

    public static object GetLifetimeService(MarshalByRefObject obj) => obj?.GetLifetimeService();

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static string GetObjectUri(MarshalByRefObject obj) => MarshalByRefObject.GetIdentity(obj, out bool _)?.URI;

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static void SetObjectUriForMarshal(MarshalByRefObject obj, string uri)
    {
      Identity identity1 = MarshalByRefObject.GetIdentity(obj, out bool _);
      Identity identity2 = (Identity) (identity1 as ServerIdentity);
      if (identity1 != null && identity2 == null)
        throw new RemotingException(Environment.GetResourceString("Remoting_SetObjectUriForMarshal__ObjectNeedsToBeLocal"));
      if (identity1 != null && identity1.URI != null)
        throw new RemotingException(Environment.GetResourceString("Remoting_SetObjectUriForMarshal__UriExists"));
      if (identity1 == null)
      {
        Context defaultContext = Thread.GetDomain().GetDefaultContext();
        ServerIdentity id = new ServerIdentity(obj, defaultContext, uri);
        if ((Identity) obj.__RaceSetServerIdentity(id) != id)
          throw new RemotingException(Environment.GetResourceString("Remoting_SetObjectUriForMarshal__UriExists"));
      }
      else
        identity1.SetOrCreateURI(uri, true);
    }

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static ObjRef Marshal(MarshalByRefObject Obj) => RemotingServices.MarshalInternal(Obj, (string) null, (Type) null);

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static ObjRef Marshal(MarshalByRefObject Obj, string URI) => RemotingServices.MarshalInternal(Obj, URI, (Type) null);

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static ObjRef Marshal(MarshalByRefObject Obj, string ObjURI, Type RequestedType) => RemotingServices.MarshalInternal(Obj, ObjURI, RequestedType);

    internal static ObjRef MarshalInternal(
      MarshalByRefObject Obj,
      string ObjURI,
      Type RequestedType)
    {
      return RemotingServices.MarshalInternal(Obj, ObjURI, RequestedType, true);
    }

    internal static ObjRef MarshalInternal(
      MarshalByRefObject Obj,
      string ObjURI,
      Type RequestedType,
      bool updateChannelData)
    {
      if (Obj == null)
        return (ObjRef) null;
      Identity identity = RemotingServices.GetOrCreateIdentity(Obj, ObjURI);
      if (RequestedType != null && identity is ServerIdentity serverIdentity1)
      {
        serverIdentity1.ServerType = RequestedType;
        serverIdentity1.MarshaledAsSpecificType = true;
      }
      ObjRef or = identity.ObjectRef;
      if (or == null)
      {
        ObjRef objRefGiven = !RemotingServices.IsTransparentProxy((object) Obj) ? Obj.CreateObjRef(RequestedType) : RemotingServices.GetRealProxy((object) Obj).CreateObjRef(RequestedType);
        or = identity.RaceSetObjRef(objRefGiven);
      }
      if (identity is ServerIdentity serverIdentity2)
      {
        MarshalByRefObject marshalByRefObject = (MarshalByRefObject) null;
        serverIdentity2.GetServerObjectChain(out marshalByRefObject);
        Lease lease = identity.Lease;
        if (lease != null)
        {
          lock (lease)
          {
            if (lease.CurrentState == LeaseState.Expired)
              lease.ActivateLease();
            else
              lease.RenewInternal(identity.Lease.InitialLeaseTime);
          }
        }
        if (updateChannelData && or.ChannelInfo != null)
        {
          object[] currentChannelData = ChannelServices.CurrentChannelData;
          if (!(Obj is AppDomain))
          {
            or.ChannelInfo.ChannelData = currentChannelData;
          }
          else
          {
            int length = currentChannelData.Length;
            object[] objArray = new object[length];
            Array.Copy((Array) currentChannelData, (Array) objArray, length);
            for (int index = 0; index < length; ++index)
            {
              if (!(objArray[index] is CrossAppDomainData))
                objArray[index] = (object) null;
            }
            or.ChannelInfo.ChannelData = objArray;
          }
        }
      }
      TrackingServices.MarshaledObject((object) Obj, or);
      return or;
    }

    private static Identity GetOrCreateIdentity(MarshalByRefObject Obj, string ObjURI)
    {
      Identity identity;
      if (RemotingServices.IsTransparentProxy((object) Obj))
      {
        identity = RemotingServices.GetRealProxy((object) Obj).IdentityObject;
        if (identity == null)
        {
          identity = (Identity) IdentityHolder.FindOrCreateServerIdentity(Obj, ObjURI, 2);
          identity.RaceSetTransparentProxy((object) Obj);
        }
        if (identity is ServerIdentity serverIdentity2)
        {
          identity = (Identity) IdentityHolder.FindOrCreateServerIdentity(serverIdentity2.TPOrObject, ObjURI, 2);
          if (ObjURI != null && ObjURI != Identity.RemoveAppNameOrAppGuidIfNecessary(identity.ObjURI))
            throw new RemotingException(Environment.GetResourceString("Remoting_URIExists"));
        }
        else if (ObjURI != null && ObjURI != identity.ObjURI)
          throw new RemotingException(Environment.GetResourceString("Remoting_URIToProxy"));
      }
      else
        identity = (Identity) IdentityHolder.FindOrCreateServerIdentity(Obj, ObjURI, 2);
      return identity;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
    {
      if (obj == null)
        throw new ArgumentNullException(nameof (obj));
      if (info == null)
        throw new ArgumentNullException(nameof (info));
      RemotingServices.MarshalInternal((MarshalByRefObject) obj, (string) null, (Type) null).GetObjectData(info, context);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static object Unmarshal(ObjRef objectRef) => RemotingServices.InternalUnmarshal(objectRef, (object) null, false);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static object Unmarshal(ObjRef objectRef, bool fRefine) => RemotingServices.InternalUnmarshal(objectRef, (object) null, fRefine);

    [ComVisible(true)]
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static object Connect(Type classToProxy, string url) => RemotingServices.Unmarshal(classToProxy, url, (object) null);

    [ComVisible(true)]
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static object Connect(Type classToProxy, string url, object data) => RemotingServices.Unmarshal(classToProxy, url, data);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static bool Disconnect(MarshalByRefObject obj) => RemotingServices.Disconnect(obj, true);

    internal static bool Disconnect(MarshalByRefObject obj, bool bResetURI)
    {
      Identity identity = obj != null ? MarshalByRefObject.GetIdentity(obj, out bool _) : throw new ArgumentNullException(nameof (obj));
      bool flag = false;
      if (identity != null)
      {
        if (!(identity is ServerIdentity))
          throw new RemotingException(Environment.GetResourceString("Remoting_CantDisconnectClientProxy"));
        if (identity.IsInIDTable())
        {
          IdentityHolder.RemoveIdentity(identity.URI, bResetURI);
          flag = true;
        }
        TrackingServices.DisconnectedObject((object) obj);
      }
      return flag;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IMessageSink GetEnvoyChainForProxy(MarshalByRefObject obj)
    {
      IMessageSink messageSink = (IMessageSink) null;
      if (RemotingServices.IsObjectOutOfContext((object) obj))
      {
        Identity identityObject = RemotingServices.GetRealProxy((object) obj).IdentityObject;
        if (identityObject != null)
          messageSink = identityObject.EnvoyChain;
      }
      return messageSink;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static ObjRef GetObjRefForProxy(MarshalByRefObject obj)
    {
      ObjRef objRef = (ObjRef) null;
      Identity identity = RemotingServices.IsTransparentProxy((object) obj) ? RemotingServices.GetRealProxy((object) obj).IdentityObject : throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_BadType"));
      if (identity != null)
        objRef = identity.ObjectRef;
      return objRef;
    }

    internal static object Unmarshal(Type classToProxy, string url) => RemotingServices.Unmarshal(classToProxy, url, (object) null);

    internal static object Unmarshal(Type classToProxy, string url, object data)
    {
      if (classToProxy == null)
        throw new ArgumentNullException(nameof (classToProxy));
      if (url == null)
        throw new ArgumentNullException(nameof (url));
      if (!classToProxy.IsMarshalByRef && !classToProxy.IsInterface)
        throw new RemotingException(Environment.GetResourceString("Remoting_NotRemotableByReference"));
      Identity idObj = IdentityHolder.ResolveIdentity(url);
      if (idObj == null || idObj.ChannelSink == null || idObj.EnvoyChain == null)
      {
        IMessageSink chnlSink = (IMessageSink) null;
        IMessageSink envoySink = (IMessageSink) null;
        string envoyAndChannelSinks = RemotingServices.CreateEnvoyAndChannelSinks(url, data, out chnlSink, out envoySink);
        if (chnlSink == null)
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Connect_CantCreateChannelSink"), (object) url));
        idObj = envoyAndChannelSinks != null ? IdentityHolder.FindOrCreateIdentity(envoyAndChannelSinks, url, (ObjRef) null) : throw new ArgumentException(Environment.GetResourceString("Argument_InvalidUrl"));
        RemotingServices.SetEnvoyAndChannelSinks(idObj, chnlSink, envoySink);
      }
      return RemotingServices.GetOrCreateProxy(classToProxy, idObj);
    }

    internal static object Wrap(ContextBoundObject obj) => RemotingServices.Wrap(obj, (object) null, true);

    internal static object Wrap(ContextBoundObject obj, object proxy, bool fCreateSinks)
    {
      if (obj == null || RemotingServices.IsTransparentProxy((object) obj))
        return (object) obj;
      Identity idObj;
      if (proxy != null)
      {
        RealProxy realProxy = RemotingServices.GetRealProxy(proxy);
        if (realProxy.UnwrappedServerObject == null)
          realProxy.AttachServerHelper((MarshalByRefObject) obj);
        idObj = MarshalByRefObject.GetIdentity((MarshalByRefObject) obj);
      }
      else
        idObj = (Identity) IdentityHolder.FindOrCreateServerIdentity((MarshalByRefObject) obj, (string) null, 0);
      proxy = RemotingServices.GetOrCreateProxy(idObj, proxy, true);
      RemotingServices.GetRealProxy(proxy).Wrap();
      if (fCreateSinks)
      {
        IMessageSink chnlSink = (IMessageSink) null;
        IMessageSink envoySink = (IMessageSink) null;
        RemotingServices.CreateEnvoyAndChannelSinks((MarshalByRefObject) proxy, (ObjRef) null, out chnlSink, out envoySink);
        RemotingServices.SetEnvoyAndChannelSinks(idObj, chnlSink, envoySink);
      }
      RealProxy realProxy1 = RemotingServices.GetRealProxy(proxy);
      if (realProxy1.UnwrappedServerObject == null)
        realProxy1.AttachServerHelper((MarshalByRefObject) obj);
      return proxy;
    }

    internal static string GetObjectUriFromFullUri(string fullUri)
    {
      if (fullUri == null)
        return (string) null;
      int num = fullUri.LastIndexOf('/');
      return num == -1 ? fullUri : fullUri.Substring(num + 1);
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern object Unwrap(ContextBoundObject obj);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern object AlwaysUnwrap(ContextBoundObject obj);

    internal static object InternalUnmarshal(ObjRef objectRef, object proxy, bool fRefine)
    {
      Context currentContext1 = Thread.CurrentContext;
      if (!ObjRef.IsWellFormed(objectRef))
        throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_BadObjRef"), (object) "Unmarshal"));
      if (objectRef.IsWellKnown())
      {
        object obj = RemotingServices.Unmarshal(typeof (MarshalByRefObject), objectRef.URI);
        Identity identity = IdentityHolder.ResolveIdentity(objectRef.URI);
        if (identity.ObjectRef == null)
          identity.RaceSetObjRef(objectRef);
        return obj;
      }
      Identity orCreateIdentity = IdentityHolder.FindOrCreateIdentity(objectRef.URI, (string) null, objectRef);
      Context currentContext2 = Thread.CurrentContext;
      object obj1;
      if (orCreateIdentity is ServerIdentity serverIdentity)
      {
        Context currentContext3 = Thread.CurrentContext;
        if (!serverIdentity.IsContextBound)
        {
          if (proxy != null)
            throw new ArgumentException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadInternalState_ProxySameAppDomain")));
          obj1 = (object) serverIdentity.TPOrObject;
        }
        else
        {
          IMessageSink chnlSink = (IMessageSink) null;
          IMessageSink envoySink = (IMessageSink) null;
          RemotingServices.CreateEnvoyAndChannelSinks(serverIdentity.TPOrObject, (ObjRef) null, out chnlSink, out envoySink);
          RemotingServices.SetEnvoyAndChannelSinks(orCreateIdentity, chnlSink, envoySink);
          obj1 = RemotingServices.GetOrCreateProxy(orCreateIdentity, proxy, true);
        }
      }
      else
      {
        IMessageSink chnlSink = (IMessageSink) null;
        IMessageSink envoySink = (IMessageSink) null;
        if (!objectRef.IsObjRefLite())
          RemotingServices.CreateEnvoyAndChannelSinks((MarshalByRefObject) null, objectRef, out chnlSink, out envoySink);
        else
          RemotingServices.CreateEnvoyAndChannelSinks(objectRef.URI, (object) null, out chnlSink, out envoySink);
        RemotingServices.SetEnvoyAndChannelSinks(orCreateIdentity, chnlSink, envoySink);
        if (objectRef.HasProxyAttribute())
          fRefine = true;
        obj1 = RemotingServices.GetOrCreateProxy(orCreateIdentity, proxy, fRefine);
      }
      TrackingServices.UnmarshaledObject(obj1, objectRef);
      return obj1;
    }

    private static object GetOrCreateProxy(Identity idObj, object proxy, bool fRefine)
    {
      if (proxy == null)
      {
        Type classToProxy;
        if (idObj is ServerIdentity serverIdentity2)
        {
          classToProxy = serverIdentity2.ServerType;
        }
        else
        {
          IRemotingTypeInfo typeInfo = idObj.ObjectRef.TypeInfo;
          classToProxy = (Type) null;
          if (typeInfo is TypeInfo && !fRefine || typeInfo == null)
          {
            classToProxy = typeof (MarshalByRefObject);
          }
          else
          {
            string typeName1 = typeInfo.TypeName;
            if (typeName1 != null)
            {
              string typeName2 = (string) null;
              string assemName = (string) null;
              TypeInfo.ParseTypeAndAssembly(typeName1, out typeName2, out assemName);
              Assembly assembly = FormatterServices.LoadAssemblyFromStringNoThrow(assemName);
              if (assembly != null)
                classToProxy = assembly.GetType(typeName2, false, false);
            }
          }
          if (classToProxy == null)
            throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), (object) typeInfo.TypeName));
        }
        proxy = (object) RemotingServices.SetOrCreateProxy(idObj, classToProxy, (object) null);
      }
      else
        proxy = (object) RemotingServices.SetOrCreateProxy(idObj, (Type) null, proxy);
      return proxy != null ? proxy : throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_UnexpectedNullTP")));
    }

    private static object GetOrCreateProxy(Type classToProxy, Identity idObj)
    {
      object obj = (object) idObj.TPOrObject ?? (object) RemotingServices.SetOrCreateProxy(idObj, classToProxy, (object) null);
      if (idObj is ServerIdentity serverIdentity)
      {
        Type serverType = serverIdentity.ServerType;
        if (!classToProxy.IsAssignableFrom(serverType))
          throw new InvalidCastException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("InvalidCast_FromTo"), (object) serverType.FullName, (object) classToProxy.FullName));
      }
      return obj;
    }

    private static MarshalByRefObject SetOrCreateProxy(
      Identity idObj,
      Type classToProxy,
      object proxy)
    {
      RealProxy realProxy = (RealProxy) null;
      if (proxy == null)
      {
        ServerIdentity serverIdentity = idObj as ServerIdentity;
        if (idObj.ObjectRef != null)
          realProxy = ActivationServices.GetProxyAttribute(classToProxy).CreateProxy(idObj.ObjectRef, classToProxy, (object) null, (Context) null);
        if (realProxy == null)
          realProxy = ActivationServices.DefaultProxyAttribute.CreateProxy(idObj.ObjectRef, classToProxy, (object) null, serverIdentity == null ? (Context) null : serverIdentity.ServerContext);
      }
      else
        realProxy = RemotingServices.GetRealProxy(proxy);
      realProxy.IdentityObject = idObj;
      proxy = realProxy.GetTransparentProxy();
      proxy = idObj.RaceSetTransparentProxy(proxy);
      return (MarshalByRefObject) proxy;
    }

    private static bool AreChannelDataElementsNull(object[] channelData)
    {
      foreach (object obj in channelData)
      {
        if (obj != null)
          return false;
      }
      return true;
    }

    internal static void CreateEnvoyAndChannelSinks(
      MarshalByRefObject tpOrObject,
      ObjRef objectRef,
      out IMessageSink chnlSink,
      out IMessageSink envoySink)
    {
      chnlSink = (IMessageSink) null;
      envoySink = (IMessageSink) null;
      if (objectRef == null)
      {
        chnlSink = ChannelServices.GetCrossContextChannelSink();
        envoySink = Thread.CurrentContext.CreateEnvoyChain(tpOrObject);
      }
      else
      {
        object[] channelData = objectRef.ChannelInfo.ChannelData;
        if (channelData != null && !RemotingServices.AreChannelDataElementsNull(channelData))
        {
          for (int index = 0; index < channelData.Length; ++index)
          {
            chnlSink = ChannelServices.CreateMessageSink(channelData[index]);
            if (chnlSink != null)
              break;
          }
          if (chnlSink == null)
          {
            lock (RemotingServices.s_delayLoadChannelLock)
            {
              for (int index = 0; index < channelData.Length; ++index)
              {
                chnlSink = ChannelServices.CreateMessageSink(channelData[index]);
                if (chnlSink != null)
                  break;
              }
              if (chnlSink == null)
              {
                foreach (object data in channelData)
                {
                  chnlSink = RemotingConfigHandler.FindDelayLoadChannelForCreateMessageSink((string) null, data, out string _);
                  if (chnlSink != null)
                    break;
                }
              }
            }
          }
        }
        if (objectRef.EnvoyInfo != null && objectRef.EnvoyInfo.EnvoySinks != null)
          envoySink = objectRef.EnvoyInfo.EnvoySinks;
        else
          envoySink = EnvoyTerminatorSink.MessageSink;
      }
    }

    internal static string CreateEnvoyAndChannelSinks(
      string url,
      object data,
      out IMessageSink chnlSink,
      out IMessageSink envoySink)
    {
      string channelSink = RemotingServices.CreateChannelSink(url, data, out chnlSink);
      envoySink = EnvoyTerminatorSink.MessageSink;
      return channelSink;
    }

    private static string CreateChannelSink(string url, object data, out IMessageSink chnlSink)
    {
      string objectURI = (string) null;
      chnlSink = ChannelServices.CreateMessageSink(url, data, out objectURI);
      if (chnlSink == null)
      {
        lock (RemotingServices.s_delayLoadChannelLock)
        {
          chnlSink = ChannelServices.CreateMessageSink(url, data, out objectURI);
          if (chnlSink == null)
            chnlSink = RemotingConfigHandler.FindDelayLoadChannelForCreateMessageSink(url, data, out objectURI);
        }
      }
      return objectURI;
    }

    internal static void SetEnvoyAndChannelSinks(
      Identity idObj,
      IMessageSink chnlSink,
      IMessageSink envoySink)
    {
      if (idObj.ChannelSink == null && chnlSink != null)
        idObj.RaceSetChannelSink(chnlSink);
      if (idObj.EnvoyChain != null)
        return;
      if (envoySink == null)
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadInternalState_FailEnvoySink")));
      idObj.RaceSetEnvoyChain(envoySink);
    }

    private static bool CheckCast(RealProxy rp, Type castType)
    {
      bool flag = false;
      if (castType == typeof (object))
        return true;
      if (!castType.IsInterface && !castType.IsMarshalByRef)
        return false;
      if (castType != typeof (IObjectReference))
      {
        if (rp is IRemotingTypeInfo remotingTypeInfo2)
        {
          flag = remotingTypeInfo2.CanCastTo(castType, rp.GetTransparentProxy());
        }
        else
        {
          Identity identityObject = rp.IdentityObject;
          if (identityObject != null)
          {
            ObjRef objectRef = identityObject.ObjectRef;
            if (objectRef != null)
            {
              IRemotingTypeInfo typeInfo = objectRef.TypeInfo;
              if (typeInfo != null)
                flag = typeInfo.CanCastTo(castType, rp.GetTransparentProxy());
            }
          }
        }
      }
      return flag;
    }

    internal static bool ProxyCheckCast(RealProxy rp, Type castType) => RemotingServices.CheckCast(rp, castType);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern object CheckCast(object objToExpand, Type type);

    internal static GCHandle CreateDelegateInvocation(
      WaitCallback waitDelegate,
      object state)
    {
      return GCHandle.Alloc((object) new object[2]
      {
        (object) waitDelegate,
        state
      });
    }

    internal static void DisposeDelegateInvocation(GCHandle delegateCallToken) => delegateCallToken.Free();

    internal static object CreateProxyForDomain(int appDomainId, IntPtr defCtxID) => (object) (AppDomain) RemotingServices.Unmarshal(RemotingServices.CreateDataForDomain(appDomainId, defCtxID));

    internal static object CreateDataForDomainCallback(object[] args)
    {
      RemotingServices.RegisterWellKnownChannels();
      ObjRef objRef = RemotingServices.MarshalInternal((MarshalByRefObject) Thread.CurrentContext.AppDomain, (string) null, (Type) null, false);
      ServerIdentity identity = (ServerIdentity) MarshalByRefObject.GetIdentity((MarshalByRefObject) Thread.CurrentContext.AppDomain);
      identity.SetHandle();
      objRef.SetServerIdentity(identity.GetHandle());
      objRef.SetDomainID(AppDomain.CurrentDomain.GetId());
      return (object) objRef;
    }

    internal static ObjRef CreateDataForDomain(int appDomainId, IntPtr defCtxID)
    {
      RemotingServices.RegisterWellKnownChannels();
      InternalCrossContextDelegate ftnToCall = new InternalCrossContextDelegate(RemotingServices.CreateDataForDomainCallback);
      return (ObjRef) Thread.CurrentThread.InternalCrossContextCallback((Context) null, defCtxID, appDomainId, ftnToCall, (object[]) null);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static MethodBase GetMethodBaseFromMethodMessage(IMethodMessage msg) => RemotingServices.InternalGetMethodBaseFromMethodMessage(msg);

    internal static MethodBase InternalGetMethodBaseFromMethodMessage(IMethodMessage msg)
    {
      if (msg == null)
        return (MethodBase) null;
      Type qualifiedTypeName = RemotingServices.InternalGetTypeFromQualifiedTypeName(msg.TypeName);
      if (qualifiedTypeName == null)
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), (object) msg.TypeName));
      Type[] methodSignature = (Type[]) msg.MethodSignature;
      return RemotingServices.GetMethodBase(msg, qualifiedTypeName, methodSignature);
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static bool IsMethodOverloaded(IMethodMessage msg) => InternalRemotingServices.GetReflectionCachedData(msg.MethodBase).IsOverloaded();

    private static MethodBase GetMethodBase(IMethodMessage msg, Type t, Type[] signature)
    {
      MethodBase methodBase = (MethodBase) null;
      switch (msg)
      {
        case IConstructionCallMessage _:
        case IConstructionReturnMessage _:
          if (signature == null)
          {
            ConstructorInfo[] constructorInfoArray = t is RuntimeType runtimeType6 ? runtimeType6.GetConstructors() : t.GetConstructors();
            methodBase = 1 == constructorInfoArray.Length ? (MethodBase) constructorInfoArray[0] : throw new AmbiguousMatchException(Environment.GetResourceString("Remoting_AmbiguousCTOR"));
            break;
          }
          methodBase = t is RuntimeType runtimeType7 ? (MethodBase) runtimeType7.GetConstructor(signature) : (MethodBase) t.GetConstructor(signature);
          break;
        case IMethodCallMessage _:
        case IMethodReturnMessage _:
          methodBase = signature != null ? (t is RuntimeType runtimeType8 ? (MethodBase) runtimeType8.GetMethod(msg.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, CallingConventions.Any, signature, (ParameterModifier[]) null) : (MethodBase) t.GetMethod(msg.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, signature, (ParameterModifier[]) null)) : (t is RuntimeType runtimeType9 ? (MethodBase) runtimeType9.GetMethod(msg.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : (MethodBase) t.GetMethod(msg.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
          break;
      }
      return methodBase;
    }

    internal static bool IsMethodAllowedRemotely(MethodBase method)
    {
      if (RemotingServices.s_FieldGetterMB == null || RemotingServices.s_FieldSetterMB == null || (RemotingServices.s_IsInstanceOfTypeMB == null || RemotingServices.s_InvokeMemberMB == null) || RemotingServices.s_CanCastToXmlTypeMB == null)
      {
        CodeAccessPermission.AssertAllPossible();
        if (RemotingServices.s_FieldGetterMB == null)
          RemotingServices.s_FieldGetterMB = (MethodBase) typeof (object).GetMethod("FieldGetter", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (RemotingServices.s_FieldSetterMB == null)
          RemotingServices.s_FieldSetterMB = (MethodBase) typeof (object).GetMethod("FieldSetter", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (RemotingServices.s_IsInstanceOfTypeMB == null)
          RemotingServices.s_IsInstanceOfTypeMB = (MethodBase) typeof (MarshalByRefObject).GetMethod("IsInstanceOfType", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (RemotingServices.s_CanCastToXmlTypeMB == null)
          RemotingServices.s_CanCastToXmlTypeMB = (MethodBase) typeof (MarshalByRefObject).GetMethod("CanCastToXmlType", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (RemotingServices.s_InvokeMemberMB == null)
          RemotingServices.s_InvokeMemberMB = (MethodBase) typeof (MarshalByRefObject).GetMethod("InvokeMember", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      }
      return method == RemotingServices.s_FieldGetterMB || method == RemotingServices.s_FieldSetterMB || (method == RemotingServices.s_IsInstanceOfTypeMB || method == RemotingServices.s_InvokeMemberMB) || method == RemotingServices.s_CanCastToXmlTypeMB;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static bool IsOneWay(MethodBase method) => method != null && InternalRemotingServices.GetReflectionCachedData(method).IsOneWayMethod();

    internal static bool FindAsyncMethodVersion(
      MethodInfo method,
      out MethodInfo beginMethod,
      out MethodInfo endMethod)
    {
      beginMethod = (MethodInfo) null;
      endMethod = (MethodInfo) null;
      string str1 = "Begin" + method.Name;
      string str2 = "End" + method.Name;
      ArrayList params1_1 = new ArrayList();
      ArrayList params1_2 = new ArrayList();
      Type type = typeof (IAsyncResult);
      Type returnType = method.ReturnType;
      foreach (ParameterInfo parameter in method.GetParameters())
      {
        if (parameter.IsOut)
          params1_2.Add((object) parameter);
        else if (parameter.ParameterType.IsByRef)
        {
          params1_1.Add((object) parameter);
          params1_2.Add((object) parameter);
        }
        else
          params1_1.Add((object) parameter);
      }
      params1_1.Add((object) typeof (AsyncCallback));
      params1_1.Add((object) typeof (object));
      params1_2.Add((object) typeof (IAsyncResult));
      foreach (MethodInfo method1 in method.DeclaringType.GetMethods())
      {
        ParameterInfo[] parameters = method1.GetParameters();
        if (method1.Name.Equals(str1) && method1.ReturnType == type && RemotingServices.CompareParameterList(params1_1, parameters))
          beginMethod = method1;
        else if (method1.Name.Equals(str2) && method1.ReturnType == returnType && RemotingServices.CompareParameterList(params1_2, parameters))
          endMethod = method1;
      }
      return beginMethod != null && endMethod != null;
    }

    private static bool CompareParameterList(ArrayList params1, ParameterInfo[] params2)
    {
      if (params1.Count != params2.Length)
        return false;
      int index = 0;
      foreach (object obj in params1)
      {
        ParameterInfo parameterInfo1 = params2[index];
        if (obj is ParameterInfo parameterInfo3)
        {
          if (parameterInfo3.ParameterType != parameterInfo1.ParameterType || parameterInfo3.IsIn != parameterInfo1.IsIn || parameterInfo3.IsOut != parameterInfo1.IsOut)
            return false;
        }
        else if ((Type) obj != parameterInfo1.ParameterType && parameterInfo1.IsIn)
          return false;
        ++index;
      }
      return true;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static Type GetServerTypeForUri(string URI)
    {
      Type type = (Type) null;
      if (URI != null)
      {
        ServerIdentity serverIdentity = (ServerIdentity) IdentityHolder.ResolveIdentity(URI);
        type = serverIdentity != null ? serverIdentity.ServerType : RemotingConfigHandler.GetServerTypeForUri(URI);
      }
      return type;
    }

    internal static void DomainUnloaded(int domainID)
    {
      IdentityHolder.FlushIdentityTable();
      CrossAppDomainSink.DomainUnloaded(domainID);
    }

    internal static IntPtr GetServerContextForProxy(object tp)
    {
      ObjRef objRef = (ObjRef) null;
      return RemotingServices.GetServerContextForProxy(tp, out objRef, out bool _, out int _);
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    internal static int GetServerDomainIdForProxy(object tp) => RemotingServices.GetRealProxy(tp).IdentityObject.ObjectRef.GetServerDomainId();

    internal static void GetServerContextAndDomainIdForProxy(
      object tp,
      out IntPtr contextId,
      out int domainId)
    {
      contextId = RemotingServices.GetServerContextForProxy(tp, out ObjRef _, out bool _, out domainId);
    }

    private static IntPtr GetServerContextForProxy(
      object tp,
      out ObjRef objRef,
      out bool bSameDomain,
      out int domainId)
    {
      IntPtr num = IntPtr.Zero;
      objRef = (ObjRef) null;
      bSameDomain = false;
      domainId = 0;
      if (RemotingServices.IsTransparentProxy(tp))
      {
        Identity identityObject = RemotingServices.GetRealProxy(tp).IdentityObject;
        if (identityObject != null)
        {
          if (identityObject is ServerIdentity serverIdentity4)
          {
            bSameDomain = true;
            num = serverIdentity4.ServerContext.InternalContextID;
            domainId = Thread.GetDomain().GetId();
          }
          else
          {
            objRef = identityObject.ObjectRef;
            num = objRef == null ? IntPtr.Zero : objRef.GetServerContext(out domainId);
          }
        }
        else
          num = Context.DefaultContext.InternalContextID;
      }
      return num;
    }

    internal static Context GetServerContext(MarshalByRefObject obj)
    {
      Context context = (Context) null;
      if (!RemotingServices.IsTransparentProxy((object) obj) && obj is ContextBoundObject)
        context = Thread.CurrentContext;
      else if (RemotingServices.GetRealProxy((object) obj).IdentityObject is ServerIdentity identityObject2)
        context = identityObject2.ServerContext;
      return context;
    }

    private static object GetType(object tp)
    {
      Type type = (Type) null;
      Identity identityObject = RemotingServices.GetRealProxy(tp).IdentityObject;
      if (identityObject != null && identityObject.ObjectRef != null && identityObject.ObjectRef.TypeInfo != null)
      {
        string typeName = identityObject.ObjectRef.TypeInfo.TypeName;
        if (typeName != null)
          type = RemotingServices.InternalGetTypeFromQualifiedTypeName(typeName);
      }
      return (object) type;
    }

    internal static byte[] MarshalToBuffer(object o)
    {
      MemoryStream memoryStream = new MemoryStream();
      RemotingSurrogateSelector surrogateSelector = new RemotingSurrogateSelector();
      new BinaryFormatter()
      {
        SurrogateSelector = ((ISurrogateSelector) surrogateSelector),
        Context = new StreamingContext(StreamingContextStates.Other)
      }.Serialize((Stream) memoryStream, o, (Header[]) null, false);
      return memoryStream.GetBuffer();
    }

    internal static object UnmarshalFromBuffer(byte[] b) => new BinaryFormatter()
    {
      AssemblyFormat = FormatterAssemblyStyle.Simple,
      SurrogateSelector = ((ISurrogateSelector) null),
      Context = new StreamingContext(StreamingContextStates.Other)
    }.Deserialize((Stream) new MemoryStream(b), (HeaderHandler) null, false);

    internal static object UnmarshalReturnMessageFromBuffer(byte[] b, IMethodCallMessage msg) => new BinaryFormatter()
    {
      SurrogateSelector = ((ISurrogateSelector) null),
      Context = new StreamingContext(StreamingContextStates.Other)
    }.DeserializeMethodResponse((Stream) new MemoryStream(b), (HeaderHandler) null, msg);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IMethodReturnMessage ExecuteMessage(
      MarshalByRefObject target,
      IMethodCallMessage reqMsg)
    {
      RealProxy realProxy = target != null ? RemotingServices.GetRealProxy((object) target) : throw new ArgumentNullException(nameof (target));
      if (realProxy is RemotingProxy && !realProxy.DoContextsMatch())
        throw new RemotingException(Environment.GetResourceString("Remoting_Proxy_WrongContext"));
      return (IMethodReturnMessage) new StackBuilderSink(target).SyncProcessMessage((IMessage) reqMsg, 0, true);
    }

    internal static string DetermineDefaultQualifiedTypeName(Type type)
    {
      if (type == null)
        throw new ArgumentNullException(nameof (type));
      string xmlType = (string) null;
      string xmlTypeNamespace = (string) null;
      return SoapServices.GetXmlTypeForInteropType(type, out xmlType, out xmlTypeNamespace) ? "soap:" + xmlType + ", " + xmlTypeNamespace : type.AssemblyQualifiedName;
    }

    internal static string GetDefaultQualifiedTypeName(Type type) => InternalRemotingServices.GetReflectionCachedData(type).QualifiedTypeName;

    internal static string InternalGetClrTypeNameFromQualifiedTypeName(string qualifiedTypeName) => qualifiedTypeName.Length > 4 && string.CompareOrdinal(qualifiedTypeName, 0, "clr:", 0, 4) == 0 ? qualifiedTypeName.Substring(4) : (string) null;

    private static int IsSoapType(string qualifiedTypeName) => qualifiedTypeName.Length > 5 && string.CompareOrdinal(qualifiedTypeName, 0, "soap:", 0, 5) == 0 ? qualifiedTypeName.IndexOf(',', 5) : -1;

    internal static string InternalGetSoapTypeNameFromQualifiedTypeName(
      string xmlTypeName,
      string xmlTypeNamespace)
    {
      string typeNamespace;
      string assemblyName;
      if (!SoapServices.DecodeXmlNamespaceForClrTypeNamespace(xmlTypeNamespace, out typeNamespace, out assemblyName))
        return (string) null;
      string str = typeNamespace == null || typeNamespace.Length <= 0 ? xmlTypeName : typeNamespace + "." + xmlTypeName;
      try
      {
        return str + ", " + assemblyName;
      }
      catch
      {
      }
      return (string) null;
    }

    internal static string InternalGetTypeNameFromQualifiedTypeName(string qualifiedTypeName)
    {
      string str = qualifiedTypeName != null ? RemotingServices.InternalGetClrTypeNameFromQualifiedTypeName(qualifiedTypeName) : throw new ArgumentNullException(nameof (qualifiedTypeName));
      if (str != null)
        return str;
      int num = RemotingServices.IsSoapType(qualifiedTypeName);
      if (num != -1)
      {
        string qualifiedTypeName1 = RemotingServices.InternalGetSoapTypeNameFromQualifiedTypeName(qualifiedTypeName.Substring(5, num - 5), qualifiedTypeName.Substring(num + 2, qualifiedTypeName.Length - (num + 2)));
        if (qualifiedTypeName1 != null)
          return qualifiedTypeName1;
      }
      return qualifiedTypeName;
    }

    internal static Type InternalGetTypeFromQualifiedTypeName(
      string qualifiedTypeName,
      bool partialFallback)
    {
      string typeName = qualifiedTypeName != null ? RemotingServices.InternalGetClrTypeNameFromQualifiedTypeName(qualifiedTypeName) : throw new ArgumentNullException(nameof (qualifiedTypeName));
      if (typeName != null)
        return RemotingServices.LoadClrTypeWithPartialBindFallback(typeName, partialFallback);
      int num = RemotingServices.IsSoapType(qualifiedTypeName);
      if (num != -1)
      {
        string str = qualifiedTypeName.Substring(5, num - 5);
        string xmlTypeNamespace = qualifiedTypeName.Substring(num + 2, qualifiedTypeName.Length - (num + 2));
        Type interopTypeFromXmlType = SoapServices.GetInteropTypeFromXmlType(str, xmlTypeNamespace);
        if (interopTypeFromXmlType != null)
          return interopTypeFromXmlType;
        string qualifiedTypeName1 = RemotingServices.InternalGetSoapTypeNameFromQualifiedTypeName(str, xmlTypeNamespace);
        if (qualifiedTypeName1 != null)
          return RemotingServices.LoadClrTypeWithPartialBindFallback(qualifiedTypeName1, true);
      }
      return RemotingServices.LoadClrTypeWithPartialBindFallback(qualifiedTypeName, partialFallback);
    }

    internal static Type InternalGetTypeFromQualifiedTypeName(string qualifiedTypeName) => RemotingServices.InternalGetTypeFromQualifiedTypeName(qualifiedTypeName, true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Type LoadClrTypeWithPartialBindFallback(
      string typeName,
      bool partialFallback)
    {
      if (!partialFallback)
        return Type.GetType(typeName, false);
      StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
      return (Type) new RuntimeTypeHandle(RuntimeTypeHandle._GetTypeByName(typeName, false, false, false, ref stackMark, true)).GetRuntimeType();
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern bool CORProfilerTrackRemoting();

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern bool CORProfilerTrackRemotingCookie();

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern bool CORProfilerTrackRemotingAsync();

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void CORProfilerRemotingClientSendingMessage(out Guid id, bool fIsAsync);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void CORProfilerRemotingClientReceivingReply(Guid id, bool fIsAsync);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void CORProfilerRemotingServerReceivingMessage(Guid id, bool fIsAsync);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void CORProfilerRemotingServerSendingReply(out Guid id, bool fIsAsync);

    [Conditional("REMOTING_PERF")]
    [Obsolete("Use of this method is not recommended. The LogRemotingStage existed for internal diagnostic purposes only.")]
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static void LogRemotingStage(int stage)
    {
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern void ResetInterfaceCache(object proxy);
  }
}
