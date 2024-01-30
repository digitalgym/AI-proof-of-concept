// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
List<dynamic> tsaAuditQuestions;
List<dynamic> tsaResponses;
string executableDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
string solutionDirectory = System.IO.Directory.GetParent(executableDirectory).Parent.Parent.FullName;

var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = false,
};

var tsaAuditQuestionsPath = Path.Combine(solutionDirectory, "tsa_audit_questions.csv");
var tsaResponsesPath = Path.Combine(solutionDirectory, "datasets", "20015419", "tsa_responses_suncorp.csv");
var jsonl = Path.Combine(solutionDirectory, "datasets", "20015419", "dataset.jsonl");
var buildersResponses = Path.Combine(solutionDirectory, "datasets", "20015419", "builders_assessment_report_suncorp.csv");

//shouldn't need to change
using (var reader = new StreamReader(tsaAuditQuestionsPath))
using (var csv = new CsvReader(reader, configuration))
{
    tsaAuditQuestions = csv.GetRecords<dynamic>().ToList();
}

using (var reader = new StreamReader(tsaResponsesPath))
using (var csv = new CsvReader(reader, configuration))
{
    tsaResponses = csv.GetRecords<dynamic>().ToList();
}

using (var reader = new StreamReader(buildersResponses))
using (var csv = new CsvReader(reader, configuration))
{
    var records = csv.GetRecords<dynamic>();

    var groupedBuilderformresponses = records.GroupBy(_ => _.Field6);

    //UserAgentOutput(tsaAuditQuestions, tsaResponses, groupedBuilderformresponses);
    JsonlOutput(tsaAuditQuestions, tsaResponses, groupedBuilderformresponses);
}

string? getTSAQuestion(string tsaQuestionNumber)
{
    var field = tsaAuditQuestions.First(_ => _.Field1 == tsaQuestionNumber);
    return field.Field2;
}

dynamic? getTSAResponse(string tasResponseNumber)
{
    int.TryParse(tasResponseNumber, out var index);
    var row = tsaResponses[index];
    var score = row.Field3;
    return new { Note = row.Field1, Score = score };
}

void UserAgentOutput(List<dynamic> tsaAuditQuestions, List<dynamic> tsaResponses, IEnumerable<IGrouping<dynamic, dynamic>> groupedBuilderformresponses)
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
                var builderQuestion = record.Field2;
                var builderAnswer = record.Field3;
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

void JsonlOutput(List<dynamic> tsaAuditQuestions, List<dynamic> tsaResponses, IEnumerable<IGrouping<dynamic, dynamic>> groupedBuilderformresponses)
{
    var rows = new List<TrainigRow>();  

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
                var builderQuestion = record.Field2;
                var builderAnswer = record.Field3;
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


public class TrainigRow
{
    public string Input { get; set; }
    public string Output { get; set; }
    public string Instruction { get; set; }
}