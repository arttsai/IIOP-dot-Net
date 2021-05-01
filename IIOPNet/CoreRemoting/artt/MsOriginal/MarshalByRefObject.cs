// Decompiled with JetBrains decompiler
// Type: System.MarshalByRefObject
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Threading;

namespace System
{
  [ComVisible(true)]
  [Serializable]
  public abstract class MarshalByRefObject
  {
    private object __identity;

    private object Identity
    {
      get => this.__identity;
      set => this.__identity = value;
    }

    internal IntPtr GetComIUnknown(bool fIsBeingMarshalled) => !RemotingServices.IsTransparentProxy((object) this) ? Marshal.GetIUnknownForObject((object) this) : RemotingServices.GetRealProxy((object) this).GetCOMIUnknown(fIsBeingMarshalled);

    [MethodImpl(MethodImplOptions.InternalCall)]
    internal static extern IntPtr GetComIUnknown(MarshalByRefObject o);

    internal bool IsInstanceOfType(Type T) => T.IsInstanceOfType((object) this);

    internal object InvokeMember(
      string name,
      BindingFlags invokeAttr,
      Binder binder,
      object[] args,
      ParameterModifier[] modifiers,
      CultureInfo culture,
      string[] namedParameters)
    {
      Type type = this.GetType();
      if (!type.IsCOMObject)
        throw new InvalidOperationException(Environment.GetResourceString("Arg_InvokeMember"));
      return type.InvokeMember(name, invokeAttr, binder, (object) this, args, modifiers, culture, namedParameters);
    }

    protected MarshalByRefObject MemberwiseClone(bool cloneIdentity)
    {
      MarshalByRefObject marshalByRefObject = (MarshalByRefObject) this.MemberwiseClone();
      if (!cloneIdentity)
        marshalByRefObject.Identity = (object) null;
      return marshalByRefObject;
    }

    internal static System.Runtime.Remoting.Identity GetIdentity(
      MarshalByRefObject obj,
      out bool fServer)
    {
      fServer = true;
      System.Runtime.Remoting.Identity identity = (System.Runtime.Remoting.Identity) null;
      if (obj != null)
      {
        if (!RemotingServices.IsTransparentProxy((object) obj))
        {
          identity = (System.Runtime.Remoting.Identity) obj.Identity;
        }
        else
        {
          fServer = false;
          identity = RemotingServices.GetRealProxy((object) obj).IdentityObject;
        }
      }
      return identity;
    }

    internal static System.Runtime.Remoting.Identity GetIdentity(MarshalByRefObject obj) => MarshalByRefObject.GetIdentity(obj, out bool _);

    internal ServerIdentity __RaceSetServerIdentity(ServerIdentity id)
    {
      if (this.__identity == null)
      {
        if (!id.IsContextBound)
          id.RaceSetTransparentProxy((object) this);
        Interlocked.CompareExchange(ref this.__identity, (object) id, (object) null);
      }
      return (ServerIdentity) this.__identity;
    }

    internal void __ResetServerIdentity() => this.__identity = (object) null;

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public object GetLifetimeService() => (object) LifetimeServices.GetLease(this);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public virtual object InitializeLifetimeService() => (object) LifetimeServices.GetLeaseInitial(this);

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public virtual ObjRef CreateObjRef(Type requestedType)
    {
      if (this.__identity == null)
        throw new RemotingException(Environment.GetResourceString("Remoting_NoIdentityEntry"));
      return new ObjRef(this, requestedType);
    }

    internal bool CanCastToXmlType(string xmlTypeName, string xmlTypeNamespace)
    {
      Type type = SoapServices.GetInteropTypeFromXmlType(xmlTypeName, xmlTypeNamespace);
      if (type == null)
      {
        string typeNamespace;
        string assemblyName;
        if (!SoapServices.DecodeXmlNamespaceForClrTypeNamespace(xmlTypeNamespace, out typeNamespace, out assemblyName))
          return false;
        string name = typeNamespace == null || typeNamespace.Length <= 0 ? xmlTypeName : typeNamespace + "." + xmlTypeName;
        try
        {
          type = Assembly.Load(assemblyName).GetType(name, false, false);
        }
        catch
        {
          return false;
        }
      }
      return type != null && type.IsAssignableFrom(this.GetType());
    }

    internal static bool CanCastToXmlTypeHelper(Type castType, MarshalByRefObject o)
    {
      if (castType == null)
        throw new ArgumentNullException(nameof (castType));
      if (!castType.IsInterface && !castType.IsMarshalByRef)
        return false;
      string xmlType = (string) null;
      string xmlTypeNamespace = (string) null;
      if (!SoapServices.GetXmlTypeForInteropType(castType, out xmlType, out xmlTypeNamespace))
      {
        xmlType = castType.Name;
        xmlTypeNamespace = SoapServices.CodeXmlNamespaceForClrTypeNamespace(castType.Namespace, castType.Module.Assembly.nGetSimpleName());
      }
      return o.CanCastToXmlType(xmlType, xmlTypeNamespace);
    }
  }
}
