// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Metadata.SoapTypeAttribute
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Runtime.InteropServices;

namespace CoreRemoting
{
  [ComVisible(true)]
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface)]
  public sealed class SoapTypeAttribute : SoapAttribute
  {
    private SoapTypeAttribute.ExplicitlySet _explicitlySet;
    private SoapOption _SoapOptions;
    private string _XmlElementName;
    private string _XmlTypeName;
    private string _XmlTypeNamespace;
    private XmlFieldOrderOption _XmlFieldOrder;

    internal bool IsInteropXmlElement() => (this._explicitlySet & (SoapTypeAttribute.ExplicitlySet.XmlElementName | SoapTypeAttribute.ExplicitlySet.XmlNamespace)) != SoapTypeAttribute.ExplicitlySet.None;

    internal bool IsInteropXmlType() => (this._explicitlySet & (SoapTypeAttribute.ExplicitlySet.XmlTypeName | SoapTypeAttribute.ExplicitlySet.XmlTypeNamespace)) != SoapTypeAttribute.ExplicitlySet.None;

    public SoapOption SoapOptions
    {
      get => this._SoapOptions;
      set => this._SoapOptions = value;
    }

    public string XmlElementName
    {
      get
      {
        if (this._XmlElementName == null && this.ReflectInfo != null)
          this._XmlElementName = SoapTypeAttribute.GetTypeName((Type) this.ReflectInfo);
        return this._XmlElementName;
      }
      set
      {
        this._XmlElementName = value;
        this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlElementName;
      }
    }

    public override string XmlNamespace
    {
      get
      {
        if (this.ProtXmlNamespace == null && this.ReflectInfo != null)
          this.ProtXmlNamespace = this.XmlTypeNamespace;
        return this.ProtXmlNamespace;
      }
      set
      {
        this.ProtXmlNamespace = value;
        this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlNamespace;
      }
    }

    public string XmlTypeName
    {
      get
      {
        if (this._XmlTypeName == null && this.ReflectInfo != null)
          this._XmlTypeName = SoapTypeAttribute.GetTypeName((Type) this.ReflectInfo);
        return this._XmlTypeName;
      }
      set
      {
        this._XmlTypeName = value;
        this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlTypeName;
      }
    }

    public string XmlTypeNamespace
    {
      get
      {
        if (this._XmlTypeNamespace == null && this.ReflectInfo != null)
          this._XmlTypeNamespace = XmlNamespaceEncoder.GetXmlNamespaceForTypeNamespace((Type) this.ReflectInfo, (string) null);
        return this._XmlTypeNamespace;
      }
      set
      {
        this._XmlTypeNamespace = value;
        this._explicitlySet |= SoapTypeAttribute.ExplicitlySet.XmlTypeNamespace;
      }
    }

    public XmlFieldOrderOption XmlFieldOrder
    {
      get => this._XmlFieldOrder;
      set => this._XmlFieldOrder = value;
    }

    public override bool UseAttribute
    {
      get => false;
      set => throw new RemotingException(Environment.GetResourceString("Remoting_Attribute_UseAttributeNotsettable"));
    }

    private static string GetTypeName(Type t)
    {
      if (!t.IsNested)
        return t.Name;
      string fullName = t.FullName;
      string str = t.Namespace;
      return str == null || str.Length == 0 ? fullName : fullName.Substring(str.Length + 1);
    }

    [Flags]
    [Serializable]
    private enum ExplicitlySet
    {
      None = 0,
      XmlElementName = 1,
      XmlNamespace = 2,
      XmlTypeName = 4,
      XmlTypeNamespace = 8,
    }
  }
}
