namespace Shared
{

    public class TechicianResponse
    {
        public int Index { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Status { get; set; }
        public string Brand { get; set; }
        public int AuditQuestion { get; set; }
        public int QuestionNumber { get; set; }
    }
    public class TSAAuditResponse
    {
        public int Index { get; set; }
        public string Note { get; set; }
        public string Status { get; set; }
        public int Score { get; set; }
    }
    public class TrainigRow
    {
        public string Input { get; set; }
        public string Output { get; set; }
        public string Instruction { get; set; }
        public int Score { get; set; }
    }
    public class Promt
    {
        public int Score { get; set; }
        public string Audit { get; set; }
    }

    public class MetaRow { 
    
        public string Prompt { get; set; }
    }
}
