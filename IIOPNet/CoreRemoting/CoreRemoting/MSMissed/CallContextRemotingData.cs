// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.CallContextRemotingData
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System;

namespace CoreRemoting
{
    [Serializable]
    internal class CallContextRemotingData : ICloneable
    {
        private string _logicalCallID;

        internal string LogicalCallID
        {
            get => this._logicalCallID;
            set => this._logicalCallID = value;
        }

        internal bool HasInfo => this._logicalCallID != null;

        public object Clone() => (object) new CallContextRemotingData()
        {
            LogicalCallID = this.LogicalCallID
        };
    }
}