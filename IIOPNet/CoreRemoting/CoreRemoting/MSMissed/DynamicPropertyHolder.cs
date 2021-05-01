// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Contexts.DynamicPropertyHolder
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Globalization;

namespace CoreRemoting
{
  internal class DynamicPropertyHolder
  {
    private const int GROW_BY = 8;
    private IDynamicProperty[] _props;
    private int _numProps;
    private IDynamicMessageSink[] _sinks;

    internal virtual bool AddDynamicProperty(IDynamicProperty prop)
    {
      lock (this)
      {
        DynamicPropertyHolder.CheckPropertyNameClash(prop.Name, this._props, this._numProps);
        bool flag = false;
        if (this._props == null || this._numProps == this._props.Length)
        {
          this._props = DynamicPropertyHolder.GrowPropertiesArray(this._props);
          flag = true;
        }
        this._props[this._numProps++] = prop;
        if (flag)
          this._sinks = DynamicPropertyHolder.GrowDynamicSinksArray(this._sinks);
        if (this._sinks == null)
        {
          this._sinks = new IDynamicMessageSink[this._props.Length];
          for (int index = 0; index < this._numProps; ++index)
            this._sinks[index] = ((IContributeDynamicSink) this._props[index]).GetDynamicSink();
        }
        else
          this._sinks[this._numProps - 1] = ((IContributeDynamicSink) prop).GetDynamicSink();
        return true;
      }
    }

    internal virtual bool RemoveDynamicProperty(string name)
    {
      lock (this)
      {
        for (int index = 0; index < this._numProps; ++index)
        {
          if (this._props[index].Name.Equals(name))
          {
            this._props[index] = this._props[this._numProps - 1];
            --this._numProps;
            this._sinks = (IDynamicMessageSink[]) null;
            return true;
          }
        }
        // todo artt: this should be easy to modify, do it later. 
        // throw new RemotingException(string.Format((IFormatProvider) CultureInfo.CurrentCulture, Environment.GetResourceString("Remoting_Contexts_NoProperty"), (object) name));
      }
    }

    internal virtual IDynamicProperty[] DynamicProperties
    {
      get
      {
        if (this._props == null)
          return (IDynamicProperty[]) null;
        lock (this)
        {
          IDynamicProperty[] dynamicPropertyArray = new IDynamicProperty[this._numProps];
          Array.Copy((Array) this._props, (Array) dynamicPropertyArray, this._numProps);
          return dynamicPropertyArray;
        }
      }
    }

    internal virtual ArrayWithSize DynamicSinks
    {
      get
      {
        if (this._numProps == 0)
          return (ArrayWithSize) null;
        lock (this)
        {
          if (this._sinks == null)
          {
            this._sinks = new IDynamicMessageSink[this._numProps + 8];
            for (int index = 0; index < this._numProps; ++index)
              this._sinks[index] = ((IContributeDynamicSink) this._props[index]).GetDynamicSink();
          }
        }
        return new ArrayWithSize(this._sinks, this._numProps);
      }
    }

    private static IDynamicMessageSink[] GrowDynamicSinksArray(
      IDynamicMessageSink[] sinks)
    {
      IDynamicMessageSink[] dynamicMessageSinkArray = new IDynamicMessageSink[(sinks != null ? sinks.Length : 0) + 8];
      if (sinks != null)
        Array.Copy((Array) sinks, (Array) dynamicMessageSinkArray, sinks.Length);
      return dynamicMessageSinkArray;
    }

    internal static void NotifyDynamicSinks(
      IMessage msg,
      ArrayWithSize dynSinks,
      bool bCliSide,
      bool bStart,
      bool bAsync)
    {
      for (int index = 0; index < dynSinks.Count; ++index)
      {
        if (bStart)
          dynSinks.Sinks[index].ProcessMessageStart(msg, bCliSide, bAsync);
        else
          dynSinks.Sinks[index].ProcessMessageFinish(msg, bCliSide, bAsync);
      }
    }

    internal static void CheckPropertyNameClash(string name, IDynamicProperty[] props, int count)
    {
      for (int index = 0; index < count; ++index)
      {
        // todo artt: modify this later.
        // if (props[index].Name.Equals(name))
        //   throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DuplicatePropertyName"));
      }
    }

    internal static IDynamicProperty[] GrowPropertiesArray(IDynamicProperty[] props)
    {
      IDynamicProperty[] dynamicPropertyArray = new IDynamicProperty[(props != null ? props.Length : 0) + 8];
      if (props != null)
        Array.Copy((Array) props, (Array) dynamicPropertyArray, props.Length);
      return dynamicPropertyArray;
    }
  }
}
