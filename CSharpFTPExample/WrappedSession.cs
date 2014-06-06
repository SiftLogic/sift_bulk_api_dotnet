using WinSCP;

namespace CSharpFTPExample
{
    /// <summary>
    /// Wrapped WinSCP Session for testing purposes. All methods defined in the interface must call Session's from here. This is
    /// necessary because WinSCP's session cannot not be inherited from.
    /// </summary>
    public class WrappedSession : ISession
    {
        private Session session;

        public WrappedSession()
        {
            session = new Session();
            session.ReconnectTime = new System.TimeSpan(10);
        }

        public void Open(SessionOptions sessionOptions)
        {
            session.Open(sessionOptions);
        }

        public TransferOperationResult PutFiles(string localPath, string remotePath)
        {
            return session.PutFiles(localPath, remotePath);
        }

        public TransferOperationResult GetFiles(string remotePath, string localPath, bool remove = false)
        {
            return session.GetFiles(remotePath, localPath, remove);
        }

        public RemoteDirectoryInfo ListDirectory(string path)
        {
            return session.ListDirectory(path);
        }

        public void Dispose()
        {
            session.Dispose();
        }
    }

    /// <summary>
    /// Factory of the wrapped  WinSCP Session. Put any used properties of Session here.
    /// </summary>
    public class WrappedSessionFactory : ISessionFactory
    {
        #region IWrappedSessionFactory implementation

        private WrappedSession session;

        public ISession Create()
        {
            session = new WrappedSession();
            return session;
        }

        #endregion
    }
}
