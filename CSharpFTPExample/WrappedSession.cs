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
        }

        public void Dispose()
        {
            session.Dispose();
        }

        public void Open(SessionOptions sessionOptions)
        {
            session.Open(sessionOptions);
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
