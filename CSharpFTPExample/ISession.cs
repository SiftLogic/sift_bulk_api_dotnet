using System;
using WinSCP;

namespace CSharpFTPExample
{
    /// <summary>
    /// Interface for WinSCP Session for testing purposes. All used properties and methods must be specified here.
    /// </summary>
    public interface ISession : IDisposable
    {
        void Open(SessionOptions sessionOptions);
    }

    /// <summary>
    /// Factory of ISession.
    /// </summary>
    public interface ISessionFactory
    {
        ISession Create();
    }
}
