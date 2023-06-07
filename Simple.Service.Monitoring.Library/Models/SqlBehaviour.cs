namespace Simple.Service.Monitoring.Library.Models
{
    public class SqlBehaviour
    {
        public string Query { get; set; }
        public ResultExpression ResultExpression { get; set; }
        public SqlResultDataType SqlResultDataType { get; set; }
        public object ExpectedResult { get; set; }
    }
}
