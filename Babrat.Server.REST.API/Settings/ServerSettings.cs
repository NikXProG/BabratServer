using System.Net;

namespace Babrat.Server.REST.API.Settings;

public class ServerSettings
{

    #region Fields

    private string _listenAddress;

    #endregion
    
    #region Properties
    
    public string ListenAddress
    {
        get =>
            _listenAddress.Equals("localhost")
                ? IPAddress.Loopback.ToString()
                : _listenAddress;

        set =>
            _listenAddress = value;
    }
    
    
    public ushort ListenPort
    {
        get;

        set;
    }
    
    public string CertPath
    {
        get;

        set;
    }
    
    
    public string CertPassword
    {
        get;

        set;
    }
    
    #endregion
    
}