using System.Net;

namespace Babrat.Server.REST.API.Settings;

public class RedisSettings
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
    
    public int BasePort
    {
        get;

        set;
    }

    #endregion
    
}