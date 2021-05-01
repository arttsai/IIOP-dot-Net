// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.ArgMapper
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Reflection;

namespace CoreRemoting
{
  internal class ArgMapper
  {
    private int[] _map;
    private IMethodMessage _mm;
    private RemotingMethodCachedData _methodCachedData;

    internal ArgMapper(IMethodMessage mm, bool fOut)
    {
      this._mm = mm;
      this._methodCachedData = InternalRemotingServices.GetReflectionCachedData(this._mm.MethodBase);
      if (fOut)
        this._map = this._methodCachedData.MarshalResponseArgMap;
      else
        this._map = this._methodCachedData.MarshalRequestArgMap;
    }

    internal ArgMapper(MethodBase mb, bool fOut)
    {
      this._methodCachedData = InternalRemotingServices.GetReflectionCachedData(mb);
      if (fOut)
        this._map = this._methodCachedData.MarshalResponseArgMap;
      else
        this._map = this._methodCachedData.MarshalRequestArgMap;
    }

    internal int[] Map => this._map;

    internal int ArgCount => this._map == null ? 0 : this._map.Length;

    internal object GetArg(int argNum)
    {
      if (this._map == null || argNum < 0 || argNum >= this._map.Length)
        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
      return this._mm.GetArg(this._map[argNum]);
    }

    internal string GetArgName(int argNum)
    {
      if (this._map == null || argNum < 0 || argNum >= this._map.Length)
        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_InternalState"));
      return this._mm.GetArgName(this._map[argNum]);
    }

    internal object[] Args
    {
      get
      {
        if (this._map == null)
          return (object[]) null;
        object[] objArray = new object[this._map.Length];
        for (int index = 0; index < this._map.Length; ++index)
          objArray[index] = this._mm.GetArg(this._map[index]);
        return objArray;
      }
    }

    internal Type[] ArgTypes
    {
      get
      {
        Type[] typeArray = (Type[]) null;
        if (this._map != null)
        {
          ParameterInfo[] parameters = this._methodCachedData.Parameters;
          typeArray = new Type[this._map.Length];
          for (int index = 0; index < this._map.Length; ++index)
            typeArray[index] = parameters[this._map[index]].ParameterType;
        }
        return typeArray;
      }
    }

    internal string[] ArgNames
    {
      get
      {
        string[] strArray = (string[]) null;
        if (this._map != null)
        {
          ParameterInfo[] parameters = this._methodCachedData.Parameters;
          strArray = new string[this._map.Length];
          for (int index = 0; index < this._map.Length; ++index)
            strArray[index] = parameters[this._map[index]].Name;
        }
        return strArray;
      }
    }

    internal static void GetParameterMaps(
      ParameterInfo[] parameters,
      out int[] inRefArgMap,
      out int[] outRefArgMap,
      out int[] outOnlyArgMap,
      out int[] nonRefOutArgMap,
      out int[] marshalRequestMap,
      out int[] marshalResponseMap)
    {
      int length1 = 0;
      int length2 = 0;
      int length3 = 0;
      int length4 = 0;
      int length5 = 0;
      int length6 = 0;
      int[] numArray1 = new int[parameters.Length];
      int[] numArray2 = new int[parameters.Length];
      int num1 = 0;
      foreach (ParameterInfo parameter in parameters)
      {
        bool isIn = parameter.IsIn;
        bool isOut = parameter.IsOut;
        bool isByRef = parameter.ParameterType.IsByRef;
        if (!isByRef)
        {
          ++length1;
          if (isOut)
            ++length4;
        }
        else if (isOut)
        {
          ++length2;
          ++length3;
        }
        else
        {
          ++length1;
          ++length2;
        }
        bool flag1;
        bool flag2;
        if (isByRef)
        {
          if (isIn == isOut)
          {
            flag1 = true;
            flag2 = true;
          }
          else
          {
            flag1 = isIn;
            flag2 = isOut;
          }
        }
        else
        {
          flag1 = true;
          flag2 = isOut;
        }
        if (flag1)
          numArray1[length5++] = num1;
        if (flag2)
          numArray2[length6++] = num1;
        ++num1;
      }
      inRefArgMap = new int[length1];
      outRefArgMap = new int[length2];
      outOnlyArgMap = new int[length3];
      nonRefOutArgMap = new int[length4];
      int num2 = 0;
      int num3 = 0;
      int num4 = 0;
      int num5 = 0;
      for (int index = 0; index < parameters.Length; ++index)
      {
        ParameterInfo parameter = parameters[index];
        bool isOut = parameter.IsOut;
        if (!parameter.ParameterType.IsByRef)
        {
          inRefArgMap[num2++] = index;
          if (isOut)
            nonRefOutArgMap[num5++] = index;
        }
        else if (isOut)
        {
          outRefArgMap[num3++] = index;
          outOnlyArgMap[num4++] = index;
        }
        else
        {
          inRefArgMap[num2++] = index;
          outRefArgMap[num3++] = index;
        }
      }
      marshalRequestMap = new int[length5];
      Array.Copy((Array) numArray1, (Array) marshalRequestMap, length5);
      marshalResponseMap = new int[length6];
      Array.Copy((Array) numArray2, (Array) marshalResponseMap, length6);
    }
    
    //
    // internal static object[] ExpandAsyncEndArgsToSyncArgs(
    //   RemotingMethodCachedData syncMethod,
    //   object[] asyncEndArgs)
    // {
    //   object[] objArray = new object[syncMethod.Parameters.Length];
    //   int[] outRefArgMap = syncMethod.OutRefArgMap;
    //   for (int index = 0; index < outRefArgMap.Length; ++index)
    //     objArray[outRefArgMap[index]] = asyncEndArgs[index];
    //   return objArray;
    // }
  }
}
