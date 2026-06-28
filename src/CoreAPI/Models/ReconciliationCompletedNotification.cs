namespace CoreAPI.Models
{
    public class ReconciliationCompletedNotification
    {
        public Guid BatchId { get; set; }
        public int MatchedGroupCount { get; set; }
        public int BreakGroupCount { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }
}
