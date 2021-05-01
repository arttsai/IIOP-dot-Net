// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.SoapServices
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata;
using System.Security.Permissions;
using System.Text;
using System.Xml.Serialization;

namespace CoreRemoting
{
  [ComVisible(true)]
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
  public class SoapServices
  {
    private static Hashtable _interopXmlElementToType = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _interopTypeToXmlElement = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _interopXmlTypeToType = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _interopTypeToXmlType = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _xmlToFieldTypeMap = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _methodBaseToSoapAction = Hashtable.Synchronized(new Hashtable());
    private static Hashtable _soapActionToMethodBase = Hashtable.Synchronized(new Hashtable());
    internal static string startNS = "http://schemas.microsoft.com/clr/";
    internal static string assemblyNS = "http://schemas.microsoft.com/clr/assem/";
    internal static string namespaceNS = "http://schemas.microsoft.com/clr/ns/";
    internal static string fullNS = "http://schemas.microsoft.com/clr/nsassem/";

    private SoapServices()
    {
    }

    private static string CreateKey(string elementName, string elementNamespace) => elementNamespace == null ? elementName : elementName + " " + elementNamespace;

    public static void RegisterInteropXmlElement(string xmlElement, string xmlNamespace, Type type)
    {
      SoapServices._interopXmlElementToType[(object) SoapServices.CreateKey(xmlElement, xmlNamespace)] = (object) type;
      SoapServices._interopTypeToXmlElement[(object) type] = (object) new SoapServices.XmlEntry(xmlElement, xmlNamespace);
    }

    public static void RegisterInteropXmlType(string xmlType, string xmlTypeNamespace, Type type)
    {
      SoapServices._interopXmlTypeToType[(object) SoapServices.CreateKey(xmlType, xmlTypeNamespace)] = (object) type;
      SoapServices._interopTypeToXmlType[(object) type] = (object) new SoapServices.XmlEntry(xmlType, xmlTypeNamespace);
    }

    public static void PreLoad(Type type)
    {
      foreach (MethodBase method in type.GetMethods())
        SoapServices.RegisterSoapActionForMethodBase(method);
      SoapTypeAttribute cachedSoapAttribute1 = (SoapTypeAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) type);
      if (cachedSoapAttribute1.IsInteropXmlElement())
        SoapServices.RegisterInteropXmlElement(cachedSoapAttribute1.XmlElementName, cachedSoapAttribute1.XmlNamespace, type);
      if (cachedSoapAttribute1.IsInteropXmlType())
        SoapServices.RegisterInteropXmlType(cachedSoapAttribute1.XmlTypeName, cachedSoapAttribute1.XmlTypeNamespace, type);
      int num = 0;
      SoapServices.XmlToFieldTypeMap xmlToFieldTypeMap = new SoapServices.XmlToFieldTypeMap();
      foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
      {
        SoapFieldAttribute cachedSoapAttribute2 = (SoapFieldAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) field);
        if (cachedSoapAttribute2.IsInteropXmlElement())
        {
          string xmlElementName = cachedSoapAttribute2.XmlElementName;
          string xmlNamespace = cachedSoapAttribute2.XmlNamespace;
          if (cachedSoapAttribute2.UseAttribute)
            xmlToFieldTypeMap.AddXmlAttribute(field.FieldType, field.Name, xmlElementName, xmlNamespace);
          else
            xmlToFieldTypeMap.AddXmlElement(field.FieldType, field.Name, xmlElementName, xmlNamespace);
          ++num;
        }
      }
      if (num <= 0)
        return;
      SoapServices._xmlToFieldTypeMap[(object) type] = (object) xmlToFieldTypeMap;
    }

    public static void PreLoad(Assembly assembly)
    {
      foreach (Type type in assembly.GetTypes())
        SoapServices.PreLoad(type);
    }

    public static Type GetInteropTypeFromXmlElement(string xmlElement, string xmlNamespace) => (Type) SoapServices._interopXmlElementToType[(object) SoapServices.CreateKey(xmlElement, xmlNamespace)];

    public static Type GetInteropTypeFromXmlType(string xmlType, string xmlTypeNamespace) => (Type) SoapServices._interopXmlTypeToType[(object) SoapServices.CreateKey(xmlType, xmlTypeNamespace)];

    public static void GetInteropFieldTypeAndNameFromXmlElement(
      Type containingType,
      string xmlElement,
      string xmlNamespace,
      out Type type,
      out string name)
    {
      if (containingType == null)
      {
        type = (Type) null;
        name = (string) null;
      }
      else
      {
        SoapServices.XmlToFieldTypeMap xmlToFieldType = (SoapServices.XmlToFieldTypeMap) SoapServices._xmlToFieldTypeMap[(object) containingType];
        if (xmlToFieldType != null)
        {
          xmlToFieldType.GetFieldTypeAndNameFromXmlElement(xmlElement, xmlNamespace, out type, out name);
        }
        else
        {
          type = (Type) null;
          name = (string) null;
        }
      }
    }

    public static void GetInteropFieldTypeAndNameFromXmlAttribute(
      Type containingType,
      string xmlAttribute,
      string xmlNamespace,
      out Type type,
      out string name)
    {
      if (containingType == null)
      {
        type = (Type) null;
        name = (string) null;
      }
      else
      {
        SoapServices.XmlToFieldTypeMap xmlToFieldType = (SoapServices.XmlToFieldTypeMap) SoapServices._xmlToFieldTypeMap[(object) containingType];
        if (xmlToFieldType != null)
        {
          xmlToFieldType.GetFieldTypeAndNameFromXmlAttribute(xmlAttribute, xmlNamespace, out type, out name);
        }
        else
        {
          type = (Type) null;
          name = (string) null;
        }
      }
    }

    public static bool GetXmlElementForInteropType(
      Type type,
      out string xmlElement,
      out string xmlNamespace)
    {
      SoapServices.XmlEntry xmlEntry = (SoapServices.XmlEntry) SoapServices._interopTypeToXmlElement[(object) type];
      if (xmlEntry != null)
      {
        xmlElement = xmlEntry.Name;
        xmlNamespace = xmlEntry.Namespace;
        return true;
      }
      SoapTypeAttribute cachedSoapAttribute = (SoapTypeAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) type);
      if (cachedSoapAttribute.IsInteropXmlElement())
      {
        xmlElement = cachedSoapAttribute.XmlElementName;
        xmlNamespace = cachedSoapAttribute.XmlNamespace;
        return true;
      }
      xmlElement = (string) null;
      xmlNamespace = (string) null;
      return false;
    }

    public static bool GetXmlTypeForInteropType(
      Type type,
      out string xmlType,
      out string xmlTypeNamespace)
    {
      SoapServices.XmlEntry xmlEntry = (SoapServices.XmlEntry) SoapServices._interopTypeToXmlType[(object) type];
      if (xmlEntry != null)
      {
        xmlType = xmlEntry.Name;
        xmlTypeNamespace = xmlEntry.Namespace;
        return true;
      }
      SoapTypeAttribute cachedSoapAttribute = (SoapTypeAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) type);
      if (cachedSoapAttribute.IsInteropXmlType())
      {
        xmlType = cachedSoapAttribute.XmlTypeName;
        xmlTypeNamespace = cachedSoapAttribute.XmlTypeNamespace;
        return true;
      }
      xmlType = (string) null;
      xmlTypeNamespace = (string) null;
      return false;
    }

    public static string GetXmlNamespaceForMethodCall(MethodBase mb) => InternalRemotingServices.GetCachedSoapAttribute((object) mb).XmlNamespace;

    public static string GetXmlNamespaceForMethodResponse(MethodBase mb) => ((SoapMethodAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) mb)).ResponseXmlNamespace;

    public static void RegisterSoapActionForMethodBase(MethodBase mb)
    {
      SoapMethodAttribute cachedSoapAttribute = (SoapMethodAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) mb);
      if (!cachedSoapAttribute.SoapActionExplicitySet)
        return;
      SoapServices.RegisterSoapActionForMethodBase(mb, cachedSoapAttribute.SoapAction);
    }

    public static void RegisterSoapActionForMethodBase(MethodBase mb, string soapAction)
    {
      if (soapAction == null)
        return;
      SoapServices._methodBaseToSoapAction[(object) mb] = (object) soapAction;
      ArrayList arrayList = (ArrayList) SoapServices._soapActionToMethodBase[(object) soapAction];
      if (arrayList == null)
      {
        lock (SoapServices._soapActionToMethodBase)
        {
          arrayList = ArrayList.Synchronized(new ArrayList());
          SoapServices._soapActionToMethodBase[(object) soapAction] = (object) arrayList;
        }
      }
      arrayList.Add((object) mb);
    }

    public static string GetSoapActionFromMethodBase(MethodBase mb) => (string) SoapServices._methodBaseToSoapAction[(object) mb] ?? ((SoapMethodAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) mb)).SoapAction;

    public static bool IsSoapActionValidForMethodBase(string soapAction, MethodBase mb)
    {
      if (soapAction[0] == '"' && soapAction[soapAction.Length - 1] == '"')
        soapAction = soapAction.Substring(1, soapAction.Length - 2);
      if (string.CompareOrdinal(((SoapMethodAttribute) InternalRemotingServices.GetCachedSoapAttribute((object) mb)).SoapAction, soapAction) == 0)
        return true;
      string strA = (string) SoapServices._methodBaseToSoapAction[(object) mb];
      if (strA != null && string.CompareOrdinal(strA, soapAction) == 0)
        return true;
      string[] strArray = soapAction.Split('#');
      if (strArray.Length != 2)
        return false;
      bool assemblyIncluded;
      string soapActionNamespace = XmlNamespaceEncoder.GetTypeNameForSoapActionNamespace(strArray[0], out assemblyIncluded);
      if (soapActionNamespace == null)
        return false;
      string str1 = strArray[1];
      Type declaringType = mb.DeclaringType;
      string str2 = declaringType.FullName;
      if (assemblyIncluded)
        str2 = str2 + ", " + declaringType.Module.Assembly.nGetSimpleName();
      return str2.Equals(soapActionNamespace) && mb.Name.Equals(str1);
    }

    public static bool GetTypeAndMethodNameFromSoapAction(
      string soapAction,
      out string typeName,
      out string methodName)
    {
      if (soapAction[0] == '"' && soapAction[soapAction.Length - 1] == '"')
        soapAction = soapAction.Substring(1, soapAction.Length - 2);
      ArrayList arrayList = (ArrayList) SoapServices._soapActionToMethodBase[(object) soapAction];
      if (arrayList != null)
      {
        if (arrayList.Count > 1)
        {
          typeName = (string) null;
          methodName = (string) null;
          return false;
        }
        MethodBase methodBase = (MethodBase) arrayList[0];
        if (methodBase != null)
        {
          Type declaringType = methodBase.DeclaringType;
          typeName = declaringType.FullName + ", " + declaringType.Module.Assembly.nGetSimpleName();
          methodName = methodBase.Name;
          return true;
        }
      }
      string[] strArray = soapAction.Split('#');
      if (strArray.Length == 2)
      {
        typeName = XmlNamespaceEncoder.GetTypeNameForSoapActionNamespace(strArray[0], out bool _);
        if (typeName == null)
        {
          methodName = (string) null;
          return false;
        }
        methodName = strArray[1];
        return true;
      }
      typeName = (string) null;
      methodName = (string) null;
      return false;
    }

    public static string XmlNsForClrType => SoapServices.startNS;

    public static string XmlNsForClrTypeWithAssembly => SoapServices.assemblyNS;

    public static string XmlNsForClrTypeWithNs => SoapServices.namespaceNS;

    public static string XmlNsForClrTypeWithNsAndAssembly => SoapServices.fullNS;

    public static bool IsClrTypeNamespace(string namespaceString) => namespaceString.StartsWith(SoapServices.startNS, StringComparison.Ordinal);

    public static string CodeXmlNamespaceForClrTypeNamespace(
      string typeNamespace,
      string assemblyName)
    {
      StringBuilder sb = new StringBuilder(256);
      if (SoapServices.IsNameNull(typeNamespace))
      {
        if (SoapServices.IsNameNull(assemblyName))
          throw new ArgumentNullException("typeNamespace,assemblyName");
        sb.Append(SoapServices.assemblyNS);
        SoapServices.UriEncode(assemblyName, sb);
      }
      else if (SoapServices.IsNameNull(assemblyName))
      {
        sb.Append(SoapServices.namespaceNS);
        sb.Append(typeNamespace);
      }
      else
      {
        sb.Append(SoapServices.fullNS);
        if (typeNamespace[0] == '.')
          sb.Append(typeNamespace.Substring(1));
        else
          sb.Append(typeNamespace);
        sb.Append('/');
        SoapServices.UriEncode(assemblyName, sb);
      }
      return sb.ToString();
    }

    public static bool DecodeXmlNamespaceForClrTypeNamespace(
      string inNamespace,
      out string typeNamespace,
      out string assemblyName)
    {
      if (SoapServices.IsNameNull(inNamespace))
        throw new ArgumentNullException(nameof (inNamespace));
      assemblyName = (string) null;
      typeNamespace = "";
      if (inNamespace.StartsWith(SoapServices.assemblyNS, StringComparison.Ordinal))
        assemblyName = SoapServices.UriDecode(inNamespace.Substring(SoapServices.assemblyNS.Length));
      else if (inNamespace.StartsWith(SoapServices.namespaceNS, StringComparison.Ordinal))
      {
        typeNamespace = inNamespace.Substring(SoapServices.namespaceNS.Length);
      }
      else
      {
        if (!inNamespace.StartsWith(SoapServices.fullNS, StringComparison.Ordinal))
          return false;
        int num = inNamespace.IndexOf("/", SoapServices.fullNS.Length);
        typeNamespace = inNamespace.Substring(SoapServices.fullNS.Length, num - SoapServices.fullNS.Length);
        assemblyName = SoapServices.UriDecode(inNamespace.Substring(num + 1));
      }
      return true;
    }

    internal static void UriEncode(string value, StringBuilder sb)
    {
      switch (value)
      {
        case "":
          break;
        case null:
          break;
        default:
          for (int index = 0; index < value.Length; ++index)
          {
            if (value[index] == ' ')
              sb.Append("%20");
            else if (value[index] == '=')
              sb.Append("%3D");
            else if (value[index] == ',')
              sb.Append("%2C");
            else
              sb.Append(value[index]);
          }
          break;
      }
    }

    internal static string UriDecode(string value)
    {
      switch (value)
      {
        case "":
        case null:
          return value;
        default:
          StringBuilder stringBuilder = new StringBuilder();
          for (int index = 0; index < value.Length; ++index)
          {
            if (value[index] == '%' && value.Length - index >= 3)
            {
              if (value[index + 1] == '2' && value[index + 2] == '0')
              {
                stringBuilder.Append(' ');
                index += 2;
              }
              else if (value[index + 1] == '3' && value[index + 2] == 'D')
              {
                stringBuilder.Append('=');
                index += 2;
              }
              else if (value[index + 1] == '2' && value[index + 2] == 'C')
              {
                stringBuilder.Append(',');
                index += 2;
              }
              else
                stringBuilder.Append(value[index]);
            }
            else
              stringBuilder.Append(value[index]);
          }
          return stringBuilder.ToString();
      }
    }

    private static bool IsNameNull(string name)
    {
      switch (name)
      {
        case "":
        case null:
          return true;
        default:
          return false;
      }
    }

    private class XmlEntry
    {
      public string Name;
      public string Namespace;

      public XmlEntry(string name, string xmlNamespace)
      {
        this.Name = name;
        this.Namespace = xmlNamespace;
      }
    }

    private class XmlToFieldTypeMap
    {
      private Hashtable _attributes = new Hashtable();
      private Hashtable _elements = new Hashtable();

      public void AddXmlElement(
        Type fieldType,
        string fieldName,
        string xmlElement,
        string xmlNamespace)
      {
        this._elements[(object) SoapServices.CreateKey(xmlElement, xmlNamespace)] = (object) new SoapServices.XmlToFieldTypeMap.FieldEntry(fieldType, fieldName);
      }

      public void AddXmlAttribute(
        Type fieldType,
        string fieldName,
        string xmlAttribute,
        string xmlNamespace)
      {
        this._attributes[(object) SoapServices.CreateKey(xmlAttribute, xmlNamespace)] = (object) new SoapServices.XmlToFieldTypeMap.FieldEntry(fieldType, fieldName);
      }

      public void GetFieldTypeAndNameFromXmlElement(
        string xmlElement,
        string xmlNamespace,
        out Type type,
        out string name)
      {
        SoapServices.XmlToFieldTypeMap.FieldEntry element = (SoapServices.XmlToFieldTypeMap.FieldEntry) this._elements[(object) SoapServices.CreateKey(xmlElement, xmlNamespace)];
        if (element != null)
        {
          type = element.Type;
          name = element.Name;
        }
        else
        {
          type = (Type) null;
          name = (string) null;
        }
      }

      public void GetFieldTypeAndNameFromXmlAttribute(
        string xmlAttribute,
        string xmlNamespace,
        out Type type,
        out string name)
      {
        SoapServices.XmlToFieldTypeMap.FieldEntry attribute = (SoapServices.XmlToFieldTypeMap.FieldEntry) this._attributes[(object) SoapServices.CreateKey(xmlAttribute, xmlNamespace)];
        if (attribute != null)
        {
          type = attribute.Type;
          name = attribute.Name;
        }
        else
        {
          type = (Type) null;
          name = (string) null;
        }
      }

      private class FieldEntry
      {
        public Type Type;
        public string Name;

        public FieldEntry(Type type, string name)
        {
          this.Type = type;
          this.Name = name;
        }
      }
    }
  }
}
