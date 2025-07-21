namespace Babrat.Server.REST.API.Settings;

public class AuthApiSettings
{

    public string Secret
    {
        get; 
        set;
    }

    public string Issuer
    {
        get;
        set;
    }
    
    public string Audience
    {
        get;
        set;
    }
    
    public int ExpireMinutes
    {
        get;
        set;
    }
    
    public int RefreshTokenLifeTime
    {
        get;
        set;
    }

  
    public int SecureTokenLength
    {
        get;
        set;
    }
    
}