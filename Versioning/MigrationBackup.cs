namespace SimpleDataEngine.Versioning
{
    /// <summary>
    /// Migration backup data structure
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class MigrationBackup<T>
    {
        public string EntityType { get; set; }
        public DataVersion Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MigrationName { get; set; }
        public List<T> Data { get; set; }
    }
}