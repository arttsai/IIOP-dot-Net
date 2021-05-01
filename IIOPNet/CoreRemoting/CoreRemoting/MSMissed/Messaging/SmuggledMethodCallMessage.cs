// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.SmuggledMethodCallMessage
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.IO;
using CoreRemoting;
using CoreRemoting.ClassicRemotingApi;
using CoreRemoting.Messaging;

namespace CoreRemoting.Messaging; 
{
  internal class SmuggledMethodCallMessage : MessageSmuggler
  {
    private string _uri;
    private string _methodName;
    private string _typeName;
    private object[] _args;
    private byte[] _serializedArgs;
    private MessageSmuggler.SerializedArg _methodSignature;
    private MessageSmuggler.SerializedArg _instantiation;
    private object _callContext;
    private int _propertyCount;

    internal static SmuggledMethodCallMessage SmuggleIfPossible(
      IMessage msg)
    {
      return !(msg is IMethodCallMessage mcm) ? (SmuggledMethodCallMessage) null : new SmuggledMethodCallMessage(mcm);
    }

    private SmuggledMethodCallMessage()
    {
    }

    private SmuggledMethodCallMessage(IMethodCallMessage mcm)
    {
      this._uri = mcm.Uri;
      this._methodName = mcm.MethodName;
      this._typeName = mcm.TypeName;
      ArrayList argsToSerialize = (ArrayList) null;
      if (!(mcm is IInternalMessage internalMessage) || internalMessage.HasProperties())
        this._propertyCount = MessageSmuggler.StoreUserPropertiesForMethodMessage((IMethodMessage) mcm, ref argsToSerialize);
      if (mcm.MethodBase.IsGenericMethod)
      {
        Type[] genericArguments = mcm.MethodBase.GetGenericArguments();
        if (genericArguments != null && genericArguments.Length > 0)
        {
          if (argsToSerialize == null)
            argsToSerialize = new ArrayList();
          this._instantiation = new MessageSmuggler.SerializedArg(argsToSerialize.Count);
          argsToSerialize.Add((object) genericArguments);
        }
      }
      if (RemotingServices.IsMethodOverloaded((IMethodMessage) mcm))
      {
        if (argsToSerialize == null)
          argsToSerialize = new ArrayList();
        this._methodSignature = new MessageSmuggler.SerializedArg(argsToSerialize.Count);
        argsToSerialize.Add(mcm.MethodSignature);
      }
      LogicalCallContext logicalCallContext = mcm.LogicalCallContext;
      if (logicalCallContext == null)
        this._callContext = (object) null;
      else if (logicalCallContext.HasInfo)
      {
        if (argsToSerialize == null)
          argsToSerialize = new ArrayList();
        this._callContext = (object) new MessageSmuggler.SerializedArg(argsToSerialize.Count);
        argsToSerialize.Add((object) logicalCallContext);
      }
      else
        this._callContext = (object) logicalCallContext.RemotingData.LogicalCallID;
      this._args = MessageSmuggler.FixupArgs(mcm.Args, ref argsToSerialize);
      if (argsToSerialize == null)
        return;
      this._serializedArgs = CrossAppDomainSerializer.SerializeMessageParts(argsToSerialize).GetBuffer();
    }

    internal ArrayList FixupForNewAppDomain()
    {
      ArrayList arrayList = (ArrayList) null;
      if (this._serializedArgs != null)
      {
        arrayList = CrossAppDomainSerializer.DeserializeMessageParts(new MemoryStream(this._serializedArgs));
        this._serializedArgs = (byte[]) null;
      }
      return arrayList;
    }

    internal string Uri => this._uri;

    internal string MethodName => this._methodName;

    internal string TypeName => this._typeName;

    internal Type[] GetInstantiation(ArrayList deserializedArgs) => this._instantiation != null ? (Type[]) deserializedArgs[this._instantiation.Index] : (Type[]) null;

    internal object[] GetMethodSignature(ArrayList deserializedArgs) => this._methodSignature != null ? (object[]) deserializedArgs[this._methodSignature.Index] : (object[]) null;

    internal object[] GetArgs(ArrayList deserializedArgs) => MessageSmuggler.UndoFixupArgs(this._args, deserializedArgs);

    internal LogicalCallContext GetCallContext(ArrayList deserializedArgs)
    {
      if (this._callContext == null)
        return (LogicalCallContext) null;
      if (!(this._callContext is string))
        return (LogicalCallContext) deserializedArgs[((MessageSmuggler.SerializedArg) this._callContext).Index];
      return new LogicalCallContext()
      {
        RemotingData = {
          LogicalCallID = (string) this._callContext
        }
      };
    }

    internal int MessagePropertyCount => this._propertyCount;

    internal void PopulateMessageProperties(IDictionary dict, ArrayList deserializedArgs)
    {
      for (int index = 0; index < this._propertyCount; ++index)
      {
        DictionaryEntry deserializedArg = (DictionaryEntry) deserializedArgs[index];
        dict[deserializedArg.Key] = deserializedArg.Value;
      }
    }
  }
}
