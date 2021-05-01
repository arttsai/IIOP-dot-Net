// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.MethodCall
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using CoreRemoting.ClassicRemotingApi;

namespace CoreRemoting
{
  [ComVisible(true)]
  [CLSCompliant(false)]
  [Serializable]
  [SecurityPermission(SecurityAction.InheritanceDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  public class MethodCall : 
    IMethodCallMessage,
    IMethodMessage,
    IMessage,
    ISerializable,
    IInternalMessage,
    ISerializationRootObject
  {
    private const BindingFlags LookupAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags LookupPublic = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
    private string uri;
    private string methodName;
    private MethodBase MI;
    private string typeName;
    private object[] args;
    private Type[] instArgs;
    private LogicalCallContext callContext;
    private Type[] methodSignature;
    protected IDictionary ExternalProperties;
    protected IDictionary InternalProperties;
    private ServerIdentity srvID;
    private Identity identity;
    private bool fSoap;
    private bool fVarArgs;
    private ArgMapper argMapper;

    public MethodCall(Header[] h1)
    {
      this.Init();
      this.fSoap = true;
      this.FillHeaders(h1);
      this.ResolveMethod();
    }

    public MethodCall(IMessage msg)
    {
      if (msg == null)
        throw new ArgumentNullException(nameof (msg));
      this.Init();
      IDictionaryEnumerator enumerator = msg.Properties.GetEnumerator();
      while (enumerator.MoveNext())
        this.FillHeader(enumerator.Key.ToString(), enumerator.Value);
      if (msg is IMethodCallMessage methodCallMessage)
        this.MI = methodCallMessage.MethodBase;
      this.ResolveMethod();
    }

    internal MethodCall(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof (info));
      this.Init();
      this.SetObjectData(info, context);
    }

    internal MethodCall(SmuggledMethodCallMessage smuggledMsg, ArrayList deserializedArgs)
    {
      this.uri = smuggledMsg.Uri;
      this.typeName = smuggledMsg.TypeName;
      this.methodName = smuggledMsg.MethodName;
      this.methodSignature = (Type[]) smuggledMsg.GetMethodSignature(deserializedArgs);
      this.args = smuggledMsg.GetArgs(deserializedArgs);
      this.instArgs = smuggledMsg.GetInstantiation(deserializedArgs);
      this.callContext = smuggledMsg.GetCallContext(deserializedArgs);
      this.ResolveMethod();
      if (smuggledMsg.MessagePropertyCount <= 0)
        return;
      smuggledMsg.PopulateMessageProperties(this.Properties, deserializedArgs);
    }

    internal MethodCall(object handlerObject, BinaryMethodCallMessage smuggledMsg)
    {
      if (handlerObject != null)
      {
        this.uri = handlerObject as string;
        if (this.uri == null && handlerObject is MarshalByRefObject marshalByRefObject2)
        {
          this.srvID = MarshalByRefObject.GetIdentity(marshalByRefObject2, out bool _) as ServerIdentity;
          this.uri = this.srvID.URI;
        }
      }
      this.typeName = smuggledMsg.TypeName;
      this.methodName = smuggledMsg.MethodName;
      this.methodSignature = (Type[]) smuggledMsg.MethodSignature;
      this.args = smuggledMsg.Args;
      this.instArgs = smuggledMsg.InstantiationArgs;
      this.callContext = smuggledMsg.LogicalCallContext;
      this.ResolveMethod();
      if (!smuggledMsg.HasProperties)
        return;
      smuggledMsg.PopulateMessageProperties(this.Properties);
    }

    public void RootSetObjectData(SerializationInfo info, StreamingContext ctx) => this.SetObjectData(info, ctx);

    internal void SetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof (info));
      if (this.fSoap)
      {
        this.SetObjectFromSoapData(info);
      }
      else
      {
        SerializationInfoEnumerator enumerator = info.GetEnumerator();
        while (enumerator.MoveNext())
          this.FillHeader(enumerator.Name, enumerator.Value);
        if (context.State != StreamingContextStates.Remoting || context.Context == null || !(context.Context is Header[] context3))
          return;
        for (int index = 0; index < context3.Length; ++index)
          this.FillHeader(context3[index].Name, context3[index].Value);
      }
    }

    internal Type ResolveType()
    {
      Type newType = (Type) null;
      if (this.srvID == null)
        this.srvID = IdentityHolder.CasualResolveIdentity(this.uri) as ServerIdentity;
      if (this.srvID != null)
      {
        Type lastCalledType = this.srvID.GetLastCalledType(this.typeName);
        if (lastCalledType != null)
          return lastCalledType;
        int num1 = 0;
        if (string.CompareOrdinal(this.typeName, 0, "clr:", 0, 4) == 0)
          num1 = 4;
        int num2 = this.typeName.IndexOf(',', num1);
        if (num2 == -1)
          num2 = this.typeName.Length;
        Type serverType = this.srvID.ServerType;
        newType = Type.ResolveTypeRelativeTo(this.typeName, num1, num2 - num1, serverType);
      }
      if (newType == null)
        newType = RemotingServices.InternalGetTypeFromQualifiedTypeName(this.typeName);
      if (this.srvID != null)
        this.srvID.SetLastCalledType(this.typeName, newType);
      return newType;
    }

    public void ResolveMethod() => this.ResolveMethod(true);

    internal void ResolveMethod(bool bThrowIfNotResolved)
    {
      if (this.MI != null || this.methodName == null)
        return;
      RuntimeType t = this.ResolveType() as RuntimeType;
      if (this.methodName.Equals(".ctor"))
        return;
      if (t == null)
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), (object) this.typeName));
      if (this.methodSignature != null)
      {
        try
        {
          this.MI = (MethodBase) t.GetMethod(this.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, CallingConventions.Any, this.methodSignature, (ParameterModifier[]) null);
        }
        catch (AmbiguousMatchException ex)
        {
          MemberInfo[] members = t.FindMembers(MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, Type.FilterName, (object) this.methodName);
          int num = this.instArgs == null ? 0 : this.instArgs.Length;
          int length = 0;
          for (int index = 0; index < members.Length; ++index)
          {
            MethodInfo methodInfo = (MethodInfo) members[index];
            if ((methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0) == num)
            {
              if (index > length)
                members[length] = members[index];
              ++length;
            }
          }
          MethodInfo[] methodInfoArray = new MethodInfo[length];
          for (int index = 0; index < length; ++index)
            methodInfoArray[index] = (MethodInfo) members[index];
          this.MI = Type.DefaultBinder.SelectMethod(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, (MethodBase[]) methodInfoArray, this.methodSignature, (ParameterModifier[]) null);
        }
        if (this.instArgs != null && this.instArgs.Length > 0)
          this.MI = (MethodBase) ((MethodInfo) this.MI).MakeGenericMethod(this.instArgs);
      }
      else
      {
        RemotingTypeCachedData remotingTypeCachedData = (RemotingTypeCachedData) null;
        if (this.instArgs == null)
        {
          remotingTypeCachedData = InternalRemotingServices.GetReflectionCachedData((Type) t);
          this.MI = remotingTypeCachedData.GetLastCalledMethod(this.methodName);
          if (this.MI != null)
            return;
        }
        bool flag = false;
        try
        {
          this.MI = (MethodBase) t.GetMethod(this.methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
          if (this.instArgs != null)
          {
            if (this.instArgs.Length > 0)
              this.MI = (MethodBase) ((MethodInfo) this.MI).MakeGenericMethod(this.instArgs);
          }
        }
        catch (AmbiguousMatchException ex)
        {
          flag = true;
          this.ResolveOverloadedMethod(t);
        }
        if (this.MI != null && !flag && remotingTypeCachedData != null)
          remotingTypeCachedData.SetLastCalledMethod(this.methodName, this.MI);
      }
      if (this.MI == null && bThrowIfNotResolved)
        throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), (object) this.methodName, (object) this.typeName));
    }

    private void ResolveOverloadedMethod(RuntimeType t)
    {
      if (this.args == null)
        return;
      MemberInfo[] member = t.GetMember(this.methodName, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
      int length1 = member.Length;
      switch (length1)
      {
        case 0:
          break;
        case 1:
          this.MI = member[0] as MethodBase;
          break;
        default:
          int length2 = this.args.Length;
          MethodBase methodBase1 = (MethodBase) null;
          for (int index = 0; index < length1; ++index)
          {
            MethodBase methodBase2 = member[index] as MethodBase;
            if (methodBase2.GetParameters().Length == length2)
              methodBase1 = methodBase1 == null ? methodBase2 : throw new RemotingException(Environment.GetResourceString("Remoting_AmbiguousMethod"));
          }
          if (methodBase1 == null)
            break;
          this.MI = methodBase1;
          break;
      }
    }

    private void ResolveOverloadedMethod(
      RuntimeType t,
      string methodName,
      ArrayList argNames,
      ArrayList argValues)
    {
      MemberInfo[] member = t.GetMember(methodName, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
      int length = member.Length;
      switch (length)
      {
        case 0:
          break;
        case 1:
          this.MI = member[0] as MethodBase;
          break;
        default:
          MethodBase methodBase1 = (MethodBase) null;
          for (int index1 = 0; index1 < length; ++index1)
          {
            MethodBase methodBase2 = member[index1] as MethodBase;
            ParameterInfo[] parameters = methodBase2.GetParameters();
            if (parameters.Length == argValues.Count)
            {
              bool flag = true;
              for (int index2 = 0; index2 < parameters.Length; ++index2)
              {
                Type type = parameters[index2].ParameterType;
                if (type.IsByRef)
                  type = type.GetElementType();
                if (type != argValues[index2].GetType())
                {
                  flag = false;
                  break;
                }
              }
              if (flag)
              {
                methodBase1 = methodBase2;
                break;
              }
            }
          }
          this.MI = methodBase1 != null ? methodBase1 : throw new RemotingException(Environment.GetResourceString("Remoting_AmbiguousMethod"));
          break;
      }
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public void GetObjectData(SerializationInfo info, StreamingContext context) => throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));

    internal void SetObjectFromSoapData(SerializationInfo info)
    {
      this.methodName = info.GetString("__methodName");
      ArrayList arrayList = (ArrayList) info.GetValue("__paramNameList", typeof (ArrayList));
      Hashtable keyToNamespaceTable = (Hashtable) info.GetValue("__keyToNamespaceTable", typeof (Hashtable));
      if (this.MI == null)
      {
        ArrayList argValues = new ArrayList();
        ArrayList argNames = arrayList;
        for (int index = 0; index < argNames.Count; ++index)
          argValues.Add(info.GetValue((string) argNames[index], typeof (object)));
        if (!(this.ResolveType() is RuntimeType t2))
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_BadType"), (object) this.typeName));
        this.ResolveOverloadedMethod(t2, this.methodName, argNames, argValues);
        if (this.MI == null)
          throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Message_MethodMissing"), (object) this.methodName, (object) this.typeName));
      }
      RemotingMethodCachedData reflectionCachedData = InternalRemotingServices.GetReflectionCachedData(this.MI);
      ParameterInfo[] parameters = reflectionCachedData.Parameters;
      int[] marshalRequestArgMap = reflectionCachedData.MarshalRequestArgMap;
      int[] outOnlyArgMap = reflectionCachedData.OutOnlyArgMap;
      object obj = this.InternalProperties == null ? (object) null : this.InternalProperties[(object) "__UnorderedParams"];
      this.args = new object[parameters.Length];
      if (obj != null && obj is bool flag && flag)
      {
        for (int index1 = 0; index1 < arrayList.Count; ++index1)
        {
          string name = (string) arrayList[index1];
          int index2 = -1;
          for (int index3 = 0; index3 < parameters.Length; ++index3)
          {
            if (name.Equals(parameters[index3].Name))
            {
              index2 = parameters[index3].Position;
              break;
            }
          }
          if (index2 == -1)
            index2 = name.StartsWith("__param", StringComparison.Ordinal) ? int.Parse(name.Substring(7), (IFormatProvider) CultureInfo.InvariantCulture) : throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
          if (index2 >= this.args.Length)
            throw new RemotingException(Environment.GetResourceString("Remoting_Message_BadSerialization"));
          this.args[index2] = Message.SoapCoerceArg(info.GetValue(name, typeof (object)), parameters[index2].ParameterType, keyToNamespaceTable);
        }
      }
      else
      {
        for (int index = 0; index < arrayList.Count; ++index)
        {
          string name = (string) arrayList[index];
          this.args[marshalRequestArgMap[index]] = Message.SoapCoerceArg(info.GetValue(name, typeof (object)), parameters[marshalRequestArgMap[index]].ParameterType, keyToNamespaceTable);
        }
        foreach (int index in outOnlyArgMap)
        {
          Type elementType = parameters[index].ParameterType.GetElementType();
          if (elementType.IsValueType)
            this.args[index] = Activator.CreateInstance(elementType, true);
        }
      }
    }

    public virtual void Init()
    {
    }

    public int ArgCount => this.args != null ? this.args.Length : 0;

    public object GetArg(int argNum) => this.args[argNum];

    public string GetArgName(int index)
    {
      this.ResolveMethod();
      return InternalRemotingServices.GetReflectionCachedData(this.MI).Parameters[index].Name;
    }

    public object[] Args => this.args;

    public int InArgCount
    {
      get
      {
        if (this.argMapper == null)
          this.argMapper = new ArgMapper((IMethodMessage) this, false);
        return this.argMapper.ArgCount;
      }
    }

    public object GetInArg(int argNum)
    {
      if (this.argMapper == null)
        this.argMapper = new ArgMapper((IMethodMessage) this, false);
      return this.argMapper.GetArg(argNum);
    }

    public string GetInArgName(int index)
    {
      if (this.argMapper == null)
        this.argMapper = new ArgMapper((IMethodMessage) this, false);
      return this.argMapper.GetArgName(index);
    }

    public object[] InArgs
    {
      get
      {
        if (this.argMapper == null)
          this.argMapper = new ArgMapper((IMethodMessage) this, false);
        return this.argMapper.Args;
      }
    }

    public string MethodName => this.methodName;

    public string TypeName => this.typeName;

    public object MethodSignature
    {
      get
      {
        if (this.methodSignature != null)
          return (object) this.methodSignature;
        if (this.MI != null)
          this.methodSignature = Message.GenerateMethodSignature(this.MethodBase);
        return (object) null;
      }
    }

    public MethodBase MethodBase
    {
      get
      {
        if (this.MI == null)
          this.MI = RemotingServices.InternalGetMethodBaseFromMethodMessage((IMethodMessage) this);
        return this.MI;
      }
    }

    public string Uri
    {
      get => this.uri;
      set => this.uri = value;
    }

    public bool HasVarArgs => this.fVarArgs;

    public virtual IDictionary Properties
    {
      get
      {
        lock (this)
        {
          if (this.InternalProperties == null)
            this.InternalProperties = (IDictionary) new Hashtable();
          if (this.ExternalProperties == null)
            this.ExternalProperties = (IDictionary) new MCMDictionary((IMethodCallMessage) this, this.InternalProperties);
          return this.ExternalProperties;
        }
      }
    }

    public LogicalCallContext LogicalCallContext => this.GetLogicalCallContext();

    internal LogicalCallContext GetLogicalCallContext()
    {
      if (this.callContext == null)
        this.callContext = new LogicalCallContext();
      return this.callContext;
    }

    internal LogicalCallContext SetLogicalCallContext(LogicalCallContext ctx)
    {
      LogicalCallContext callContext = this.callContext;
      this.callContext = ctx;
      return callContext;
    }

    ServerIdentity IInternalMessage.ServerIdentityObject
    {
      get => this.srvID;
      set => this.srvID = value;
    }

    Identity IInternalMessage.IdentityObject
    {
      get => this.identity;
      set => this.identity = value;
    }

    void IInternalMessage.SetURI(string val) => this.uri = val;

    void IInternalMessage.SetCallContext(LogicalCallContext newCallContext) => this.callContext = newCallContext;

    bool IInternalMessage.HasProperties() => this.ExternalProperties != null || this.InternalProperties != null;

    internal void FillHeaders(Header[] h) => this.FillHeaders(h, false);

    private void FillHeaders(Header[] h, bool bFromHeaderHandler)
    {
      if (h == null)
        return;
      if (bFromHeaderHandler && this.fSoap)
      {
        for (int index = 0; index < h.Length; ++index)
        {
          Header header = h[index];
          if (header.HeaderNamespace == "http://schemas.microsoft.com/clr/soap/messageProperties")
            this.FillHeader(header.Name, header.Value);
          else
            this.FillHeader(LogicalCallContext.GetPropertyKeyForHeader(header), (object) header);
        }
      }
      else
      {
        for (int index = 0; index < h.Length; ++index)
          this.FillHeader(h[index].Name, h[index].Value);
      }
    }

    internal virtual bool FillSpecialHeader(string key, object value)
    {
      switch (key)
      {
        case "__Uri":
          this.uri = (string) value;
          goto case null;
        case "__MethodName":
          this.methodName = (string) value;
          goto case null;
        case "__MethodSignature":
          this.methodSignature = (Type[]) value;
          goto case null;
        case "__TypeName":
          this.typeName = (string) value;
          goto case null;
        case "__Args":
          this.args = (object[]) value;
          goto case null;
        case "__CallContext":
          if (value is string)
          {
            this.callContext = new LogicalCallContext();
            this.callContext.RemotingData.LogicalCallID = (string) value;
            goto case null;
          }
          else
          {
            this.callContext = (LogicalCallContext) value;
            goto case null;
          }
        case null:
          return true;
        default:
          return false;
      }
    }

    internal void FillHeader(string key, object value)
    {
      if (this.FillSpecialHeader(key, value))
        return;
      if (this.InternalProperties == null)
        this.InternalProperties = (IDictionary) new Hashtable();
      this.InternalProperties[(object) key] = value;
    }

    public virtual object HeaderHandler(Header[] h)
    {
      SerializationMonkey uninitializedObject = (SerializationMonkey) FormatterServices.GetUninitializedObject(typeof (SerializationMonkey));
      Header[] h1;
      if (h != null && h.Length > 0 && h[0].Name == "__methodName")
      {
        this.methodName = (string) h[0].Value;
        if (h.Length > 1)
        {
          h1 = new Header[h.Length - 1];
          Array.Copy((Array) h, 1, (Array) h1, 0, h.Length - 1);
        }
        else
          h1 = (Header[]) null;
      }
      else
        h1 = h;
      this.FillHeaders(h1, true);
      this.ResolveMethod(false);
      uninitializedObject._obj = (ISerializationRootObject) this;
      if (this.MI != null)
      {
        ArgMapper argMapper = new ArgMapper(this.MI, false);
        uninitializedObject.fieldNames = argMapper.ArgNames;
        uninitializedObject.fieldTypes = argMapper.ArgTypes;
      }
      return (object) uninitializedObject;
    }
  }
}
