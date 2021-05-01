// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.XmlNamespaceEncoder
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Reflection;
using System.Text;

namespace CoreRemoting
{
  internal static class XmlNamespaceEncoder
  {
    internal static string GetXmlNamespaceForType(Type type, string dynamicUrl)
    {
      string fullName = type.FullName;
      Assembly assembly1 = type.Module.Assembly;
      StringBuilder stringBuilder = new StringBuilder(256);
      Assembly assembly2 = typeof (string).Module.Assembly;
      if (assembly1 == assembly2)
      {
        stringBuilder.Append(SoapServices.namespaceNS);
        stringBuilder.Append(fullName);
      }
      else
      {
        stringBuilder.Append(SoapServices.fullNS);
        stringBuilder.Append(fullName);
        stringBuilder.Append('/');
        stringBuilder.Append(assembly1.nGetSimpleName());
      }
      return stringBuilder.ToString();
    }

    internal static string GetXmlNamespaceForTypeNamespace(Type type, string dynamicUrl)
    {
      string str = type.Namespace;
      Assembly assembly1 = type.Module.Assembly;
      StringBuilder stringBuilder = new StringBuilder(256);
      Assembly assembly2 = typeof (string).Module.Assembly;
      if (assembly1 == assembly2)
      {
        stringBuilder.Append(SoapServices.namespaceNS);
        stringBuilder.Append(str);
      }
      else
      {
        stringBuilder.Append(SoapServices.fullNS);
        stringBuilder.Append(str);
        stringBuilder.Append('/');
        stringBuilder.Append(assembly1.nGetSimpleName());
      }
      return stringBuilder.ToString();
    }

    internal static string GetTypeNameForSoapActionNamespace(string uri, out bool assemblyIncluded)
    {
      assemblyIncluded = false;
      string fullNs = SoapServices.fullNS;
      string namespaceNs = SoapServices.namespaceNS;
      if (uri.StartsWith(fullNs, StringComparison.Ordinal))
      {
        uri = uri.Substring(fullNs.Length);
        char[] chArray = new char[1]{ '/' };
        string[] strArray = uri.Split(chArray);
        if (strArray.Length != 2)
          return (string) null;
        assemblyIncluded = true;
        return strArray[0] + ", " + strArray[1];
      }
      if (!uri.StartsWith(namespaceNs, StringComparison.Ordinal))
        return (string) null;
      string simpleName = typeof (string).Module.Assembly.nGetSimpleName();
      assemblyIncluded = true;
      return uri.Substring(namespaceNs.Length) + ", " + simpleName;
    }
  }
}
