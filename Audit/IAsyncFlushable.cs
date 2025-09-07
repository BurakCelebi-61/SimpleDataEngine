namespace SimpleDataEngine.Audit
{
    public interface IAsyncFlushable
    {
        Task FlushAsync();
    }
}