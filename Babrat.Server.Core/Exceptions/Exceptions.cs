namespace Babrat.Server.Core.Exceptions;

public static class RestException
{
    
    public class NotFoundStorageException : NullReferenceException
    {
   
        public string? TypeCallContext { get; set; }
        
        public NotFoundStorageException(string message)
            : base(message)
        {
        }
        
        public NotFoundStorageException(string message, string typeCallContext) : base(message)
        {
            TypeCallContext = typeCallContext;
        }
        
        public NotFoundStorageException(string message, Exception innerException,string? typeCallContext = null)
            : base(message, innerException)
        {
             TypeCallContext = typeCallContext;
        }
    }
    
    public class InvalidFormatFileException : FormatException
    {
        
        
        public InvalidFormatFileException(string message)
            : base(message)
        {
        }

        public InvalidFormatFileException(string message, string typeCallContext) : base(message)
        {
            TypeCallContext = typeCallContext;
        }
        
        public InvalidFormatFileException(string message, Exception innerException, string? typeCallContext = null)
            : base(message, innerException)
        {
            TypeCallContext = typeCallContext;
        }

        public string? TypeCallContext { get; set; }
        

    }
    
    
}