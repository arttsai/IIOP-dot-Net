// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.MessageSmuggler
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using CoreRemoting.ClassicRemotingApi;

namespace CoreRemoting.Messaging
{
  internal class MessageSmuggler
  {
    private static bool CanSmuggleObjectDirectly(object obj) => obj is string || obj.GetType() == typeof (void) || obj.GetType().IsPrimitive;

    protected static object[] FixupArgs(object[] args, ref ArrayList argsToSerialize)
    {
      object[] objArray = new object[args.Length];
      int length = args.Length;
      for (int index = 0; index < length; ++index)
        objArray[index] = MessageSmuggler.FixupArg(args[index], ref argsToSerialize);
      return objArray;
    }

    protected static object FixupArg(object arg, ref ArrayList argsToSerialize)
    {
      if (arg == null)
        return (object) null;
      if (arg is MarshalByRefObject marshalByRefObject)
      {
        if (!RemotingServices.IsTransparentProxy((object) marshalByRefObject) || RemotingServices.GetRealProxy((object) marshalByRefObject) is RemotingProxy)
        {
          ObjRef objRef = RemotingServices.MarshalInternal(marshalByRefObject, (string) null, (Type) null);
          if (objRef.CanSmuggle())
          {
            if (!RemotingServices.IsTransparentProxy((object) marshalByRefObject))
            {
              ServerIdentity identity = (ServerIdentity) MarshalByRefObject.GetIdentity(marshalByRefObject);
              identity.SetHandle();
              objRef.SetServerIdentity(identity.GetHandle());
              objRef.SetDomainID(AppDomain.CurrentDomain.GetId());
            }
            ObjRef smuggleableCopy = objRef.CreateSmuggleableCopy();
            smuggleableCopy.SetMarshaledObject();
            return (object) new SmuggledObjRef(smuggleableCopy);
          }
        }
        if (argsToSerialize == null)
          argsToSerialize = new ArrayList();
        int count = argsToSerialize.Count;
        argsToSerialize.Add(arg);
        return (object) new MessageSmuggler.SerializedArg(count);
      }
      if (MessageSmuggler.CanSmuggleObjectDirectly(arg))
        return arg;
      if (arg is Array array)
      {
        Type elementType = array.GetType().GetElementType();
        if (elementType.IsPrimitive || elementType == typeof (string))
          return array.Clone();
      }
      if (argsToSerialize == null)
        argsToSerialize = new ArrayList();
      int count1 = argsToSerialize.Count;
      argsToSerialize.Add(arg);
      return (object) new MessageSmuggler.SerializedArg(count1);
    }

    protected static object[] UndoFixupArgs(object[] args, ArrayList deserializedArgs)
    {
      object[] objArray = new object[args.Length];
      int length = args.Length;
      for (int index = 0; index < length; ++index)
        objArray[index] = MessageSmuggler.UndoFixupArg(args[index], deserializedArgs);
      return objArray;
    }

    protected static object UndoFixupArg(object arg, ArrayList deserializedArgs)
    {
      switch (arg)
      {
        case SmuggledObjRef smuggledObjRef:
          return smuggledObjRef.ObjRef.GetRealObjectHelper();
        case MessageSmuggler.SerializedArg serializedArg:
          return deserializedArgs[serializedArg.Index];
        default:
          return arg;
      }
    }

    protected static int StoreUserPropertiesForMethodMessage(
      IMethodMessage msg,
      ref ArrayList argsToSerialize)
    {
      IDictionary properties = msg.Properties;
      if (properties is MessageDictionary messageDictionary)
      {
        if (!messageDictionary.HasUserData())
          return 0;
        int num = 0;
        foreach (DictionaryEntry dictionaryEntry in messageDictionary.InternalDictionary)
        {
          if (argsToSerialize == null)
            argsToSerialize = new ArrayList();
          argsToSerialize.Add((object) dictionaryEntry);
          ++num;
        }
        return num;
      }
      int num1 = 0;
      foreach (DictionaryEntry dictionaryEntry in properties)
      {
        if (argsToSerialize == null)
          argsToSerialize = new ArrayList();
        argsToSerialize.Add((object) dictionaryEntry);
        ++num1;
      }
      return num1;
    }

    protected class SerializedArg
    {
      private int _index;

      public SerializedArg(int index) => this._index = index;

      public int Index => this._index;
    }
  }
}
