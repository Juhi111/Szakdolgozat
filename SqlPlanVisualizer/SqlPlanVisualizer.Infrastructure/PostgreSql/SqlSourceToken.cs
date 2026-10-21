namespace SqlPlanVisualizer.Infrastructure.PostgreSql
{
    public class SqlSourceToken
    {
        public SqlSourceToken(string text, int start, int length)
        {
            Text = text;
            Start = start;
            Length = length;
        }

        public string Text { get; }
        public int Start { get; }
        public int Length { get; }
    }
}
