using SimpleDataEngine.Core;

namespace SimpleDataEngine.Tests
{
    /// <summary>
    /// Test entity for demonstration
    /// </summary>
    public class TestUser : IEntity
    {
        public int Id { get; set; }
        public DateTime UpdateTime { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; } = true;
    }
}