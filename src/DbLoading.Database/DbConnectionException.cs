namespace DbLoading.Database;

public class DbConnectionException : Exception
{
    public DbConnectionException(string message) : base(message) { }
    public DbConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
