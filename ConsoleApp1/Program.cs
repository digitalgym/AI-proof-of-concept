// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;


internal class Program
{
    static List<dynamic> responseMappings;
    static List<dynamic> tsaAuditQuestions;


    static string checklist = @"SELECT Item, ItemStatus, Score
  FROM [Ambrose].[dbo].[CheckListItemsView]
  WHERE JobNumber = @jobId
	AND ItemStatus != ''
  ORDER BY ItemNumber";

    static string responses = @"SELECT jq.ElementOrder, jq.Question, jq.Answer,ReviewRequest.Status, j.InsurerBrand
FROM Job AS j INNER JOIN
	JobQuestion AS jq ON j.RecID = jq.JobRecID INNER JOIN
	JobForm AS jf ON jq.JobFormRecID = jf.RecID INNER JOIN
	Form AS f ON jq.FormID = f.RecID INNER JOIN
	ReviewRequest ON j.RecID = ReviewRequest.JobID
WHERE
(f.FormName = 'Builders Assessment Report v2') AND
(jq.Status = 'Active') 
AND (dbo.fn_CleanCherwellDate(jf.CompletedDateTime) IS NOT NULL) 
AND (jq.Answer <> '') AND 
(ReviewRequest.Status = 'Complete')
AND ReviewRequest.CreatedDateTime > DATEADD(MONTH, -12, GETDATE())
AND j.JobID = @jobId
AND jq.Answer != 'N/A' AND jq.Answer != '0.00'
ORDER BY ElementOrder";

    static string jobNumbers = @"SELECT DISTINCT(j.JobID)
FROM Job AS j INNER JOIN
	JobQuestion AS jq ON j.RecID = jq.JobRecID INNER JOIN
	JobForm AS jf ON jq.JobFormRecID = jf.RecID INNER JOIN
	Form AS f ON jq.FormID = f.RecID INNER JOIN
	ReviewRequest ON j.RecID = ReviewRequest.JobID
WHERE
(f.FormName = 'Builders Assessment Report v2') AND
(jq.Status = 'Active') 
AND (dbo.fn_CleanCherwellDate(jf.CompletedDateTime) IS NOT NULL) 
AND (jq.Answer <> '') AND 
(ReviewRequest.Status = 'Complete')
AND ReviewRequest.CreatedDateTime > DATEADD(MONTH, -12, GETDATE())
AND j.InsurerBrand = 'Suncorp'";
    private static List<TSAAuditResponse> tsaResponses;
    private static List<TechicianResponse> buildersResponses;

    private static void Main(string[] args)
    {
        List<TrainigRow> rows = new();
        string executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        string solutionDirectory = Directory.GetParent(executableDirectory).Parent.Parent.FullName;

        var tsaAuditQuestionsPath = Path.Combine(solutionDirectory, "tsa_audit_questions.csv");

        var responsesMappingPath = Path.Combine(solutionDirectory, "form_mappings", "builders_assessment_report_suncorp.csv");
        var jsonl = Path.Combine(solutionDirectory, "datasets", "dataset.jsonl");

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
        };

        //shouldn't need to change
        using (var reader = new StreamReader(tsaAuditQuestionsPath))
        using (var csv = new CsvReader(reader, configuration))
        {
            tsaAuditQuestions = csv.GetRecords<dynamic>().ToList();
        }


        using (var reader = new StreamReader(responsesMappingPath))
        using (var csv = new CsvReader(reader, configuration))
        {
            responseMappings = csv.GetRecords<dynamic>().ToList();
        }

        string connectionString = "server=192.168.200.28; database=Ambrose;User ID=sa;Password=Ambr0s3@kunda;MultipleActiveResultSets=True";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand(jobNumbers, connection))
            {

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var ccc =(string) reader[0];
                        Console.WriteLine(ccc);
                        tsaResponses = AuditorsResponses(ccc, connection);

                        if (tsaResponses.Count > 0)
                        {
                            buildersResponses = TechnicansResponses(ccc, connection);


                            //Add audit mappings to techicianl responses
                            for (int i = 0; i < buildersResponses.Count; i++)
                            {
                                //Add the grouping ID's to the builder responses dataset
                                var auditQuestion = responseMappings.First(_ => _.Field1 == buildersResponses[i].Index.ToString());
                                buildersResponses[i].AuditQuestion = int.Parse(auditQuestion.Field2);
                            }

                            var groupedBuilderformresponses = buildersResponses.GroupBy(_ => _.AuditQuestion);
                            JsonlOutput(tsaAuditQuestions, tsaResponses, groupedBuilderformresponses);
                        }
                    }
                }
            }
        }
       
        string? getTSAQuestion(int tsaQuestionNumber)
        {
            var field = tsaAuditQuestions.First(_ => _.Field1 == tsaQuestionNumber.ToString());
            return field.Field2;
        }

        dynamic? getTSAResponse(int tasResponseNumber)
        {
            var row = tsaResponses[tasResponseNumber];
            var score = row.Score;
            return new { Note = row.Note, Score = score };
        }

        void UserAgentOutput(List<TechicianResponse> tsaAuditQuestions, List<TSAAuditResponse> tsaResponses, IEnumerable<IGrouping<int, TechicianResponse>> groupedBuilderformresponses)
        {
            foreach (var group in groupedBuilderformresponses)
            {
                var groupId = group.Key;
                var tsaQuestion = getTSAQuestion(groupId);
                var tsaResponse = getTSAResponse(groupId);

                if (tsaQuestion != null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("User:");
                    Console.WriteLine($"Audit Question: {tsaQuestion}"); //Audit question
                    Console.ResetColor(); // Reset the color to the default
                    Console.WriteLine();
                    Console.WriteLine("Builder Answers:");
                    foreach (var record in group) // builder responses that relate to the qudit question
                    {
                        var builderQuestion = record.Question;
                        var builderAnswer = record.Answer;
                        Console.WriteLine($"{builderQuestion}: {builderAnswer}");
                    }

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Agent:");

                    if (tsaResponse != null)
                    {
                        var responseCleaned = tsaResponse.Note.Substring(3, tsaResponse.Note.Length - 3).Trim();
                        Console.WriteLine($"{responseCleaned} | Score:{tsaResponse.Score}");
                    }
                    else
                        Console.WriteLine("##No TSA Response##");
                    Console.ResetColor(); // Reset the color to the default
                    Console.WriteLine();
                }
            }
        }

        void JsonlOutput(List<dynamic> tsaAuditQuestions, List<TSAAuditResponse> tsaResponses, IEnumerable<IGrouping<int, TechicianResponse>> groupedBuilderformresponses)
        {
            

            foreach (var group in groupedBuilderformresponses)
            {
                var trainingrow = new TrainigRow();
                var groupId = group.Key;
                var tsaQuestion = getTSAQuestion(groupId);
                var tsaResponse = getTSAResponse(groupId);

                if (tsaQuestion != null)
                {
                    var input = new StringBuilder($"Question: {tsaQuestion}");
                    input.AppendLine("Answers:");
                    foreach (var record in group) // builder responses that relate to the qudit question
                    {
                        var builderQuestion = record.Question;
                        var builderAnswer = record.Answer;
                        input.AppendLine($"{builderQuestion}: {builderAnswer}");
                    }
                    trainingrow.Input = input.ToString();


                    trainingrow.Instruction = "Review the responses for the audit question, highlight any required responses (*) not provided, ignore others. Where a response is unsatisfactory provide comments about why the response isn't correct and provide details about how the response can be improved.";


                    var output = new StringBuilder("Agent:");

                    if (tsaResponse != null)
                    {
                        var responseCleaned = tsaResponse.Note.Substring(3, tsaResponse.Note.Length - 3).Trim();
                        output.AppendLine($"{responseCleaned} | Score:{tsaResponse.Score}");
                    }
                    else
                        output.AppendLine("##No TSA Response##");

                    trainingrow.Output = output.ToString();
                    rows.Add(trainingrow);
                }
            }


            using (StreamWriter writer = new StreamWriter(jsonl))
            {
                foreach (var row in rows)
                {
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(row);
                    Console.WriteLine(jsonString);
                    writer.WriteLine(jsonString);
                }
            }
        }
    }

    private static List<TechicianResponse> TechnicansResponses(string jobId, SqlConnection connection)
    {

        var data = new List<TechicianResponse>();
        using (SqlCommand command = new SqlCommand(responses, connection))
        {
            command.Parameters.AddWithValue("@jobId", jobId);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {

                    var index = (int)reader[0];

                    if (!new List<int> { 460, 730 }.Contains(index) ) //some questions are not supported like file uploads, Cost
                    {
                        data.Add(new TechicianResponse
                        {
                            Index = (int)reader[0],
                            Question = (string)reader[1],
                            Answer = (string)reader[2],
                            Status = (string)reader[3],
                            Brand = (string)reader[4],
                            //AuditQuestion =  (string)reader[5] //Added via form mapping
                        });
                    }
                    //Console.WriteLine(string.Format("{0}, {1}", reader[0], reader[1]));
                }
            }
        }

        return data;
    }

    private static List<TSAAuditResponse> AuditorsResponses(string jobId, SqlConnection connection)
    {
        var data = new List<TSAAuditResponse>();

        using (SqlCommand command = new SqlCommand(checklist, connection))
        {
            command.Parameters.AddWithValue("@jobId", jobId);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    data.Add(new TSAAuditResponse
                    {
                        Note = (string)reader[0],
                        Status = (string)reader[1],
                        Score = (int)reader[2]
                    });
                    //Console.WriteLine(string.Format("{0}, {1}", reader[0], reader[1]));
                }
            }
        }
        return data;
    }
}


public class TechicianResponse
{
    public int Index { get;  set; }
    public string Question { get;  set; }
    public string Answer { get;  set; }
    public string Status { get;  set; }
    public string Brand { get;  set; }
    public int AuditQuestion { get;  set; }
}
public class TSAAuditResponse
{
    public string Index { get;  set; }
    public string Note { get;  set; }
    public string Status { get;  set; }
    public int Score { get;  set; }
}
public class TrainigRow
{
    public string Input { get; set; }
    public string Output { get; set; }
    public string Instruction { get; set; }
}