// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Metadata.SoapAttribute
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Runtime.InteropServices;

namespace CoreRemoting
{
    [ComVisible(true)]
    public class SoapAttribute : Attribute
    {
        protected string ProtXmlNamespace;
        private bool _bUseAttribute;
        private bool _bEmbedded;
        protected object ReflectInfo;

        internal void SetReflectInfo(object info) => this.ReflectInfo = info;

        public virtual string XmlNamespace
        {
            get => this.ProtXmlNamespace;
            set => this.ProtXmlNamespace = value;
        }

        public virtual bool UseAttribute
        {
            get => this._bUseAttribute;
            set => this._bUseAttribute = value;
        }

        public virtual bool Embedded
        {
            get => this._bEmbedded;
            set => this._bEmbedded = value;
        }
    }
}