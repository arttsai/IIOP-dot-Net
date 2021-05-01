// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Channels.ChannelServices
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using CoreRemoting.ClassicRemotingApi;

namespace CoreRemoting
{
  [ComVisible(true)]
  public sealed class ChannelServices
  {
    private static object[] s_currentChannelData = (object[]) null;
    private static object s_channelLock = new object();
    private static RegisteredChannelList s_registeredChannels = new RegisteredChannelList();
    private static IMessageSink xCtxChannel;
    private static unsafe Perf_Contexts* perf_Contexts = ChannelServices.GetPrivateContextsPerfCounters();
    private static bool unloadHandlerRegistered = false;

    internal static object[] CurrentChannelData
    {
      get
      {
        if (ChannelServices.s_currentChannelData == null)
          ChannelServices.RefreshChannelData();
        return ChannelServices.s_currentChannelData;
      }
    }

    private ChannelServices()
    {
    }

    private static long remoteCalls
    {
      get => Thread.GetDomain().RemotingData.ChannelServicesData.remoteCalls;
      set => Thread.GetDomain().RemotingData.ChannelServicesData.remoteCalls = value;
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe Perf_Contexts* GetPrivateContextsPerfCounters();

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static void RegisterChannel(IChannel chnl, bool ensureSecurity) => ChannelServices.RegisterChannelInternal(chnl, ensureSecurity);

    [Obsolete("Use System.Runtime.Remoting.ChannelServices.RegisterChannel(IChannel chnl, bool ensureSecurity) instead.", false)]
    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static void RegisterChannel(IChannel chnl) => ChannelServices.RegisterChannelInternal(chnl, false);

    internal static unsafe void RegisterChannelInternal(IChannel chnl, bool ensureSecurity)
    {
      if (chnl == null)
        throw new ArgumentNullException(nameof (chnl));
      bool tookLock = false;
      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Monitor.ReliableEnter(ChannelServices.s_channelLock, ref tookLock);
        string channelName = chnl.ChannelName;
        RegisteredChannelList registeredChannels1 = ChannelServices.s_registeredChannels;
        if (channelName == null || channelName.Length == 0 || -1 == registeredChannels1.FindChannelIndex(chnl.ChannelName))
        {
          if (ensureSecurity)
          {
            if (chnl is ISecurableChannel securableChannel6)
              securableChannel6.IsSecured = ensureSecurity;
            else
              throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Channel_CannotBeSecured"), (object) (chnl.ChannelName ?? chnl.ToString())));
          }
          RegisteredChannel[] registeredChannels2 = registeredChannels1.RegisteredChannels;
          RegisteredChannel[] channels = registeredChannels2 != null ? new RegisteredChannel[registeredChannels2.Length + 1] : new RegisteredChannel[1];
          if (!ChannelServices.unloadHandlerRegistered && !(chnl is CrossAppDomainChannel))
          {
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(ChannelServices.UnloadHandler);
            ChannelServices.unloadHandlerRegistered = true;
          }
          int channelPriority = chnl.ChannelPriority;
          int index;
          for (index = 0; index < registeredChannels2.Length; ++index)
          {
            RegisteredChannel registeredChannel = registeredChannels2[index];
            if (channelPriority > registeredChannel.Channel.ChannelPriority)
            {
              channels[index] = new RegisteredChannel(chnl);
              break;
            }
            channels[index] = registeredChannel;
          }
          if (index == registeredChannels2.Length)
          {
            channels[registeredChannels2.Length] = new RegisteredChannel(chnl);
          }
          else
          {
            for (; index < registeredChannels2.Length; ++index)
              channels[index + 1] = registeredChannels2[index];
          }
          if ((IntPtr) ChannelServices.perf_Contexts != IntPtr.Zero)
            ++ChannelServices.perf_Contexts->cChannels;
          ChannelServices.s_registeredChannels = new RegisteredChannelList(channels);
          ChannelServices.RefreshChannelData();
        }
        else
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_ChannelNameAlreadyRegistered"), (object) chnl.ChannelName));
      }
      finally
      {
        if (tookLock)
          Monitor.Exit(ChannelServices.s_channelLock);
      }
    }

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static unsafe void UnregisterChannel(IChannel chnl)
    {
      bool tookLock = false;
      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Monitor.ReliableEnter(ChannelServices.s_channelLock, ref tookLock);
        if (chnl != null)
        {
          RegisteredChannelList registeredChannels1 = ChannelServices.s_registeredChannels;
          int channelIndex = registeredChannels1.FindChannelIndex(chnl);
          if (-1 == channelIndex)
            throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_ChannelNotRegistered"), (object) chnl.ChannelName));
          RegisteredChannel[] registeredChannels2 = registeredChannels1.RegisteredChannels;
          RegisteredChannel[] channels = new RegisteredChannel[registeredChannels2.Length - 1];
          if (chnl is IChannelReceiver channelReceiver4)
            channelReceiver4.StopListening((object) null);
          int index1 = 0;
          int index2 = 0;
          while (index2 < registeredChannels2.Length)
          {
            if (index2 == channelIndex)
            {
              ++index2;
            }
            else
            {
              channels[index1] = registeredChannels2[index2];
              ++index1;
              ++index2;
            }
          }
          if ((IntPtr) ChannelServices.perf_Contexts != IntPtr.Zero)
            --ChannelServices.perf_Contexts->cChannels;
          ChannelServices.s_registeredChannels = new RegisteredChannelList(channels);
        }
        ChannelServices.RefreshChannelData();
      }
      finally
      {
        if (tookLock)
          Monitor.Exit(ChannelServices.s_channelLock);
      }
    }

    public static IChannel[] RegisteredChannels
    {
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get
      {
        RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
        int count = registeredChannels.Count;
        if (count == 0)
          return new IChannel[0];
        int length = count - 1;
        int num = 0;
        IChannel[] channelArray = new IChannel[length];
        for (int index = 0; index < count; ++index)
        {
          IChannel channel = registeredChannels.GetChannel(index);
          if (!(channel is CrossAppDomainChannel))
            channelArray[num++] = channel;
        }
        return channelArray;
      }
    }

    internal static IMessageSink CreateMessageSink(
      string url,
      object data,
      out string objectURI)
    {
      IMessageSink messageSink = (IMessageSink) null;
      objectURI = (string) null;
      RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
      int count = registeredChannels.Count;
      for (int index = 0; index < count; ++index)
      {
        if (registeredChannels.IsSender(index))
        {
          messageSink = ((IChannelSender) registeredChannels.GetChannel(index)).CreateMessageSink(url, data, out objectURI);
          if (messageSink != null)
            break;
        }
      }
      if (objectURI == null)
        objectURI = url;
      return messageSink;
    }

    internal static IMessageSink CreateMessageSink(object data) => ChannelServices.CreateMessageSink((string) null, data, out string _);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IChannel GetChannel(string name)
    {
      RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
      int channelIndex = registeredChannels.FindChannelIndex(name);
      if (0 > channelIndex)
        return (IChannel) null;
      IChannel channel = registeredChannels.GetChannel(channelIndex);
      switch (channel)
      {
        case CrossAppDomainChannel _:
        case CrossContextChannel _:
          return (IChannel) null;
        default:
          return channel;
      }
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static string[] GetUrlsForObject(MarshalByRefObject obj)
    {
      if (obj == null)
        return (string[]) null;
      RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
      int count = registeredChannels.Count;
      Hashtable hashtable = new Hashtable();
      Identity identity = MarshalByRefObject.GetIdentity(obj, out bool _);
      if (identity != null)
      {
        string objUri = identity.ObjURI;
        if (objUri != null)
        {
          for (int index1 = 0; index1 < count; ++index1)
          {
            if (registeredChannels.IsReceiver(index1))
            {
              try
              {
                string[] urlsForUri = ((IChannelReceiver) registeredChannels.GetChannel(index1)).GetUrlsForUri(objUri);
                for (int index2 = 0; index2 < urlsForUri.Length; ++index2)
                  hashtable.Add((object) urlsForUri[index2], (object) urlsForUri[index2]);
              }
              catch (NotSupportedException ex)
              {
              }
            }
          }
        }
      }
      ICollection keys = hashtable.Keys;
      string[] strArray = new string[keys.Count];
      int num = 0;
      foreach (string str in (IEnumerable) keys)
        strArray[num++] = str;
      return strArray;
    }

    internal static IMessageSink GetChannelSinkForProxy(object obj)
    {
      IMessageSink messageSink = (IMessageSink) null;
      if (RemotingServices.IsTransparentProxy(obj) && RemotingServices.GetRealProxy(obj) is RemotingProxy realProxy)
        messageSink = realProxy.IdentityObject.ChannelSink;
      return messageSink;
    }

    [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.RemotingConfiguration)]
    public static IDictionary GetChannelSinkProperties(object obj)
    {
      switch (ChannelServices.GetChannelSinkForProxy(obj))
      {
        case IClientChannelSink nextChannelSink:
          ArrayList arrayList = new ArrayList();
          do
          {
            IDictionary properties = nextChannelSink.Properties;
            if (properties != null)
              arrayList.Add((object) properties);
            nextChannelSink = nextChannelSink.NextChannelSink;
          }
          while (nextChannelSink != null);
          return (IDictionary) new AggregateDictionary((ICollection) arrayList);
        case IDictionary dictionary:
          return dictionary;
        default:
          return (IDictionary) null;
      }
    }

    internal static IMessageSink GetCrossContextChannelSink()
    {
      if (ChannelServices.xCtxChannel == null)
        ChannelServices.xCtxChannel = CrossContextChannel.MessageSink;
      return ChannelServices.xCtxChannel;
    }

    internal static unsafe void IncrementRemoteCalls(long cCalls)
    {
      ChannelServices.remoteCalls += cCalls;
      if ((IntPtr) ChannelServices.perf_Contexts == IntPtr.Zero)
        return;
      ChannelServices.perf_Contexts->cRemoteCalls += (int) cCalls;
    }

    internal static void IncrementRemoteCalls() => ChannelServices.IncrementRemoteCalls(1L);

    internal static void RefreshChannelData()
    {
      bool tookLock = false;
      RuntimeHelpers.PrepareConstrainedRegions();
      try
      {
        Monitor.ReliableEnter(ChannelServices.s_channelLock, ref tookLock);
        ChannelServices.s_currentChannelData = ChannelServices.CollectChannelDataFromChannels();
      }
      finally
      {
        if (tookLock)
          Monitor.Exit(ChannelServices.s_channelLock);
      }
    }

    private static object[] CollectChannelDataFromChannels()
    {
      RemotingServices.RegisterWellKnownChannels();
      RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
      int count = registeredChannels.Count;
      int receiverCount = registeredChannels.ReceiverCount;
      object[] objArray1 = new object[receiverCount];
      int length = 0;
      int index1 = 0;
      int index2 = 0;
      for (; index1 < count; ++index1)
      {
        IChannel channel = registeredChannels.GetChannel(index1);
        if (channel == null)
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_ChannelNotRegistered"), (object) ""));
        if (registeredChannels.IsReceiver(index1))
        {
          object channelData = ((IChannelReceiver) channel).ChannelData;
          objArray1[index2] = channelData;
          if (channelData != null)
            ++length;
          ++index2;
        }
      }
      if (length != receiverCount)
      {
        object[] objArray2 = new object[length];
        int num = 0;
        for (int index3 = 0; index3 < receiverCount; ++index3)
        {
          object obj = objArray1[index3];
          if (obj != null)
            objArray2[num++] = obj;
        }
        objArray1 = objArray2;
      }
      return objArray1;
    }

    private static bool IsMethodReallyPublic(MethodInfo mi)
    {
      if (!mi.IsPublic || mi.IsStatic)
        return false;
      if (!mi.IsGenericMethod)
        return true;
      foreach (Type genericArgument in mi.GetGenericArguments())
      {
        if (!genericArgument.IsVisible)
          return false;
      }
      return true;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static ServerProcessing DispatchMessage(
      IServerChannelSinkStack sinkStack,
      IMessage msg,
      out IMessage replyMsg)
    {
      ServerProcessing serverProcessing = ServerProcessing.Complete;
      replyMsg = (IMessage) null;
      try
      {
        if (msg == null)
          throw new ArgumentNullException(nameof (msg));
        ChannelServices.IncrementRemoteCalls();
        ServerIdentity wellKnownObject = ChannelServices.CheckDisconnectedOrCreateWellKnownObject(msg);
        if (wellKnownObject.ServerType == typeof (AppDomain))
          throw new RemotingException(Environment.GetResourceString("Remoting_AppDomainsCantBeCalledRemotely"));
        if (!(msg is IMethodCallMessage methodCallMessage2))
        {
          if (!typeof (IMessageSink).IsAssignableFrom(wellKnownObject.ServerType))
            throw new RemotingException(Environment.GetResourceString("Remoting_AppDomainsCantBeCalledRemotely"));
          serverProcessing = ServerProcessing.Complete;
          replyMsg = ChannelServices.GetCrossContextChannelSink().SyncProcessMessage(msg);
        }
        else
        {
          MethodInfo methodBase = (MethodInfo) methodCallMessage2.MethodBase;
          if (!ChannelServices.IsMethodReallyPublic(methodBase) && !RemotingServices.IsMethodAllowedRemotely((MethodBase) methodBase))
            throw new RemotingException(Environment.GetResourceString("Remoting_NonPublicOrStaticCantBeCalledRemotely"));
          InternalRemotingServices.GetReflectionCachedData((MethodBase) methodBase);
          if (RemotingServices.IsOneWay((MethodBase) methodBase))
          {
            serverProcessing = ServerProcessing.OneWay;
            ChannelServices.GetCrossContextChannelSink().AsyncProcessMessage(msg, (IMessageSink) null);
          }
          else
          {
            serverProcessing = ServerProcessing.Complete;
            if (!wellKnownObject.ServerType.IsContextful)
            {
              object[] args = new object[2]
              {
                (object) msg,
                (object) wellKnownObject.ServerContext
              };
              replyMsg = (IMessage) CrossContextChannel.SyncProcessMessageCallback(args);
            }
            else
              replyMsg = ChannelServices.GetCrossContextChannelSink().SyncProcessMessage(msg);
          }
        }
      }
      catch (Exception ex1)
      {
        if (serverProcessing != ServerProcessing.OneWay)
        {
          try
          {
            IMethodCallMessage mcm = msg != null ? (IMethodCallMessage) msg : (IMethodCallMessage) new ErrorMessage();
            replyMsg = (IMessage) new ReturnMessage(ex1, mcm);
            if (msg != null)
              ((ReturnMessage) replyMsg).SetLogicalCallContext((LogicalCallContext) msg.Properties[(object) Message.CallContextKey]);
          }
          catch (Exception ex2)
          {
          }
        }
      }
      return serverProcessing;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IMessage SyncDispatchMessage(IMessage msg)
    {
      IMessage message = (IMessage) null;
      bool flag = false;
      try
      {
        if (msg == null)
          throw new ArgumentNullException(nameof (msg));
        ChannelServices.IncrementRemoteCalls();
        if (!(msg is TransitionCall))
        {
          ChannelServices.CheckDisconnectedOrCreateWellKnownObject(msg);
          flag = RemotingServices.IsOneWay(((IMethodMessage) msg).MethodBase);
        }
        IMessageSink contextChannelSink = ChannelServices.GetCrossContextChannelSink();
        if (!flag)
          message = contextChannelSink.SyncProcessMessage(msg);
        else
          contextChannelSink.AsyncProcessMessage(msg, (IMessageSink) null);
      }
      catch (Exception ex1)
      {
        if (!flag)
        {
          try
          {
            IMethodCallMessage mcm = msg != null ? (IMethodCallMessage) msg : (IMethodCallMessage) new ErrorMessage();
            message = (IMessage) new ReturnMessage(ex1, mcm);
            if (msg != null)
              ((ReturnMessage) message).SetLogicalCallContext(mcm.LogicalCallContext);
          }
          catch (Exception ex2)
          {
          }
        }
      }
      return message;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IMessageCtrl AsyncDispatchMessage(
      IMessage msg,
      IMessageSink replySink)
    {
      IMessageCtrl messageCtrl = (IMessageCtrl) null;
      try
      {
        if (msg == null)
          throw new ArgumentNullException(nameof (msg));
        ChannelServices.IncrementRemoteCalls();
        if (!(msg is TransitionCall))
          ChannelServices.CheckDisconnectedOrCreateWellKnownObject(msg);
        messageCtrl = ChannelServices.GetCrossContextChannelSink().AsyncProcessMessage(msg, replySink);
      }
      catch (Exception ex1)
      {
        if (replySink != null)
        {
          try
          {
            IMethodCallMessage methodCallMessage = (IMethodCallMessage) msg;
            ReturnMessage returnMessage = new ReturnMessage(ex1, (IMethodCallMessage) msg);
            if (msg != null)
              returnMessage.SetLogicalCallContext(methodCallMessage.LogicalCallContext);
            replySink.SyncProcessMessage((IMessage) returnMessage);
          }
          catch (Exception ex2)
          {
          }
        }
      }
      return messageCtrl;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public static IServerChannelSink CreateServerChannelSinkChain(
      IServerChannelSinkProvider provider,
      IChannelReceiver channel)
    {
      if (provider == null)
        return (IServerChannelSink) new DispatchChannelSink();
      IServerChannelSinkProvider channelSinkProvider = provider;
      while (channelSinkProvider.Next != null)
        channelSinkProvider = channelSinkProvider.Next;
      channelSinkProvider.Next = (IServerChannelSinkProvider) new DispatchChannelSinkProvider();
      IServerChannelSink sink = provider.CreateSink(channel);
      channelSinkProvider.Next = (IServerChannelSinkProvider) null;
      return sink;
    }

    internal static ServerIdentity CheckDisconnectedOrCreateWellKnownObject(
      IMessage msg)
    {
      ServerIdentity serverIdentity = InternalSink.GetServerIdentity(msg);
      if (serverIdentity == null || serverIdentity.IsRemoteDisconnected())
      {
        string uri = InternalSink.GetURI(msg);
        if (uri != null)
        {
          ServerIdentity wellKnownObject = RemotingConfigHandler.CreateWellKnownObject(uri);
          if (wellKnownObject != null)
            serverIdentity = wellKnownObject;
        }
      }
      if (serverIdentity == null || serverIdentity.IsRemoteDisconnected())
      {
        string uri = InternalSink.GetURI(msg);
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Disconnected"), (object) uri));
      }
      return serverIdentity;
    }

    internal static void UnloadHandler(object sender, EventArgs e) => ChannelServices.StopListeningOnAllChannels();

    private static void StopListeningOnAllChannels()
    {
      try
      {
        RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
        int count = registeredChannels.Count;
        for (int index = 0; index < count; ++index)
        {
          if (registeredChannels.IsReceiver(index))
            ((IChannelReceiver) registeredChannels.GetChannel(index)).StopListening((object) null);
        }
      }
      catch (Exception ex)
      {
      }
    }

    internal static void NotifyProfiler(IMessage msg, RemotingProfilerEvent profilerEvent)
    {
      switch (profilerEvent)
      {
        case RemotingProfilerEvent.ClientSend:
          if (!RemotingServices.CORProfilerTrackRemoting())
            break;
          Guid id1;
          RemotingServices.CORProfilerRemotingClientSendingMessage(out id1, false);
          if (!RemotingServices.CORProfilerTrackRemotingCookie())
            break;
          msg.Properties[(object) "CORProfilerCookie"] = (object) id1;
          break;
        case RemotingProfilerEvent.ClientReceive:
          if (!RemotingServices.CORProfilerTrackRemoting())
            break;
          Guid id2 = Guid.Empty;
          if (RemotingServices.CORProfilerTrackRemotingCookie())
          {
            object property = msg.Properties[(object) "CORProfilerCookie"];
            if (property != null)
              id2 = (Guid) property;
          }
          RemotingServices.CORProfilerRemotingClientReceivingReply(id2, false);
          break;
      }
    }

    internal static string FindFirstHttpUrlForObject(string objectUri)
    {
      if (objectUri == null)
        return (string) null;
      RegisteredChannelList registeredChannels = ChannelServices.s_registeredChannels;
      int count = registeredChannels.Count;
      for (int index = 0; index < count; ++index)
      {
        if (registeredChannels.IsReceiver(index))
        {
          IChannelReceiver channel = (IChannelReceiver) registeredChannels.GetChannel(index);
          string fullName = channel.GetType().FullName;
          if (string.CompareOrdinal(fullName, "System.Runtime.Remoting.Channels.Http.HttpChannel") == 0 || string.CompareOrdinal(fullName, "System.Runtime.Remoting.Channels.Http.HttpServerChannel") == 0)
          {
            string[] urlsForUri = channel.GetUrlsForUri(objectUri);
            if (urlsForUri != null && urlsForUri.Length > 0)
              return urlsForUri[0];
          }
        }
      }
      return (string) null;
    }
  }
}
