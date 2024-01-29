// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
List<dynamic> tsaAuditQuestions;
List<dynamic> buildersResponses;
List<dynamic> tsaResponses;
var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = false,
};


//shouldn't need to change
using (var reader = new StreamReader("tsa_audit_questions.csv"))
using (var csv = new CsvReader(reader, configuration))
{
   tsaAuditQuestions = csv.GetRecords<dynamic>().ToList();
}

using (var reader = new StreamReader("Builders_assessment_report_v2_tsa_audit_responses.csv"))
using (var csv = new CsvReader(reader, configuration))
{
    tsaResponses = csv.GetRecords<dynamic>().ToList();
}



using (var reader = new StreamReader("Builders_assessment_report_v2.csv"))
using (var csv = new CsvReader(reader, configuration))
{
    buildersResponses = csv.GetRecords<dynamic>().ToList();
}


using (var reader = new StreamReader("Builders_assessment_report_tsa_mappings_suncorp.csv"))
using (var csv = new CsvReader(reader, configuration))
{
    var records = csv.GetRecords<dynamic>();

    var groupedBuilderformresponses = records.GroupBy(_ => _.Field3);

    foreach (var group in groupedBuilderformresponses)
    {
        Console.WriteLine("------ NEW PROMPT ------");
        var groupId = group.Key;
        var tsaQuestion = getTSAQuestion(groupId);
        var tsaResponse = getTSAResponse(groupId);

        if (tsaQuestion != null)
        {
            Console.WriteLine("#Prompt");
            Console.WriteLine($"Audit Question:{tsaQuestion}"); //Audit question
            Console.WriteLine();
            foreach (var record in group) // builder responses that relate to the qudit question
            {
                var builderResponseRow = buildersResponses.Find(x => x.Field5 == record.Field1);

                var builderQuestion = builderResponseRow.Field6;
                var builderAnswer = builderResponseRow.Field7;
                //possible answer options: var builderAnswer2 = builderResponseRow.Field8;
                Console.WriteLine($"- Q: {builderQuestion} A: {builderAnswer}");
                // Access the properties of the record as needed
                var field1 = record.Field1;
                var field2 = record.Field2;
            }
            Console.WriteLine();
            Console.WriteLine("#Instruction");
            Console.WriteLine("Review the responses for the audit question, highlight any required responses (*) not provided, ignore others. Where a response is unsatisfactory provide comments about why the response isn't correct and provide details about how the response can be improved.");

            Console.WriteLine();

            Console.WriteLine("#Output");

            if (tsaResponse != null)
            {
                var responseCleaned = tsaResponse.Note.Substring(3, tsaResponse.Note.Length - 3).Trim();
                Console.WriteLine($"Auditors Note: {responseCleaned} | Auditors Score:{tsaResponse.Score}");
            }
            else
                Console.WriteLine("##No TSA Response##");

            Console.WriteLine();
        }
    }
}

string? getTSAQuestion(string field1)
{
    if (field1 == "N/A")
        return null;
   var field = tsaAuditQuestions.First(_ => _.Field1 == field1);

    return field.Field2;
}

dynamic? getTSAResponse(string field1)
{
    if (field1 == "N/A")
        return null;
    var field = tsaResponses.First(_ => _.Field1 == field1);

    var score = field.Field4;
    return new { Note = field.Field2, Score = score };
}


