// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.CallContextSecurityData
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;
using System.Security.Principal;

namespace CoreRemoting
{
    [Serializable]
    internal class CallContextSecurityData : ICloneable
    {
        private IPrincipal _principal;

        internal IPrincipal Principal
        {
            get => this._principal;
            set => this._principal = value;
        }

        internal bool HasInfo => null != this._principal;

        public object Clone() => (object) new CallContextSecurityData()
        {
            _principal = this._principal
        };
    }
}