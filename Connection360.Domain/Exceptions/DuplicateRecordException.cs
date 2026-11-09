namespace Connection360.Domain.Exceptions
{
    /// <summary>
    /// Se lanza cuando se intenta crear un registro que ya existe según su(s) campo(s) únicos
    /// (por ejemplo, un cliente con la misma identificación). La API la traduce a HTTP 409 (Conflict).
    /// </summary>
    public class DuplicateRecordException : Exception
    {
        public DuplicateRecordException(String message) : base(message)
        {
        }

        public DuplicateRecordException(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
