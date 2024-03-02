namespace Todd.ApplicationKernel.Persistence.FreeSql
{
    public class FreeSqlOption
    {
        public List<ConnectionString> ConnectionString { get; set; } = new List<ConnectionString>();
    }

    public class ConnectionString
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
