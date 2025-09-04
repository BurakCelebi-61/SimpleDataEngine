namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Repository operation result
    /// </summary>
    /// <typeparam name="T">Result data type</typeparam>
    public class RepositoryResult<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }
        public int AffectedCount { get; set; }
        public TimeSpan Duration { get; set; }

        public static RepositoryResult<T> CreateSuccess(T data, int affectedCount = 1)
        {
            return new RepositoryResult<T>
            {
                Success = true,
                Data = data,
                AffectedCount = affectedCount
            };
        }

        public static RepositoryResult<T> CreateFailure(string errorMessage, Exception exception = null)
        {
            return new RepositoryResult<T>
            {
                Success = false,
                ErrorMessage = errorMessage,
                Exception = exception
            };
        }
    }
}