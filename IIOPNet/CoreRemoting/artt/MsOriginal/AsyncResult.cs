// Decompiled with JetBrains decompiler
// Type: System.Runtime.Remoting.Messaging.AsyncResult
// Assembly: mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// MVID: 26BACF2A-B3E7-4E5B-9AB6-134973DBE886
// Assembly location: C:\Windows\Microsoft.NET\Framework\v2.0.50727\mscorlib.dll

using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using CoreRemoting;

namespace System.Runtime.Remoting.Messaging
{
  [ComVisible(true)]
  public class AsyncResult : IAsyncResult, IMessageSink
  {
    private IMessageCtrl _mc;
    private AsyncCallback _acbd;
    private IMessage _replyMsg;
    private bool _isCompleted;
    private bool _endInvokeCalled;
    private ManualResetEvent _AsyncWaitHandle;
    private Delegate _asyncDelegate;
    private object _asyncState;

    internal AsyncResult(Message m)
    {
      m.GetAsyncBeginInfo(out this._acbd, out this._asyncState);
      this._asyncDelegate = (Delegate) m.GetThisPtr();
    }

    public virtual bool IsCompleted => this._isCompleted;

    public virtual object AsyncDelegate => (object) this._asyncDelegate;

    public virtual object AsyncState => this._asyncState;

    public virtual bool CompletedSynchronously => false;

    public bool EndInvokeCalled
    {
      get => this._endInvokeCalled;
      set => this._endInvokeCalled = value;
    }

    private void FaultInWaitHandle()
    {
      lock (this)
      {
        if (this._AsyncWaitHandle != null)
          return;
        this._AsyncWaitHandle = new ManualResetEvent(this._isCompleted);
      }
    }

    public virtual WaitHandle AsyncWaitHandle
    {
      get
      {
        this.FaultInWaitHandle();
        return (WaitHandle) this._AsyncWaitHandle;
      }
    }

    public virtual void SetMessageCtrl(IMessageCtrl mc) => this._mc = mc;

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public virtual IMessage SyncProcessMessage(IMessage msg)
    {
      this._replyMsg = msg != null ? (msg is IMethodReturnMessage ? msg : (IMessage) new ReturnMessage((Exception) new RemotingException(Environment.GetResourceString("Remoting_Message_BadType")), (IMethodCallMessage) new ErrorMessage())) : (IMessage) new ReturnMessage((Exception) new RemotingException(Environment.GetResourceString("Remoting_NullMessage")), (IMethodCallMessage) new ErrorMessage());
      lock (this)
      {
        this._isCompleted = true;
        if (this._AsyncWaitHandle != null)
          this._AsyncWaitHandle.Set();
      }
      if (this._acbd != null)
        this._acbd((IAsyncResult) this);
      return (IMessage) null;
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    public virtual IMessageCtrl AsyncProcessMessage(
      IMessage msg,
      IMessageSink replySink)
    {
      throw new NotSupportedException(Environment.GetResourceString("NotSupported_Method"));
    }

    public IMessageSink NextSink
    {
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)] get => (IMessageSink) null;
    }

    public virtual IMessage GetReplyMessage() => this._replyMsg;
  }
}
