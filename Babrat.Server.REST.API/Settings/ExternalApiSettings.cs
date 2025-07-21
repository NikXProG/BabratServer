using System.Net;

namespace Babrat.Server.REST.API.Settings;

public class ExternalApiSettings
{
    #region Fields

    private string _listenAddress;

    #endregion
    
    #region Properties
    
    public string BaseAddress
    {
        get =>
            _listenAddress.Equals("localhost")
                ? IPAddress.Loopback.ToString()
                : _listenAddress;

        set =>
            _listenAddress = value;
    }
    
    
    public ushort BasePort
    {
        get;

        set;
    }
    
    public string CertPath
    {
        get;

        set;
    }
    
    public double TimeoutSeconds
    {
        get;
        set;
    } = 30.0;
    
    
    public string CertPassword
    {
        get;

        set;
    }
    
    #endregion

}