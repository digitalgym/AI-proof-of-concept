// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using Shared;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace Console1App1
{
    public class Program
    {
   


      
        private static void Main(string[] args)
        {
            List<dynamic> responseMappings;
            List<dynamic> tsaAuditQuestions;
            List<TSAAuditResponse> tsaResponses;
            List<TechicianResponse> buildersResponses;
            string responsesMappingPath;
            string jsonl;
            
            string executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string solutionDirectory = Directory.GetParent(executableDirectory).Parent.Parent.FullName;

            var tsaAuditQuestionsPath = Path.Combine(solutionDirectory, "tsa_audit_questions.csv");

            responsesMappingPath = Path.Combine(solutionDirectory, "form_mappings", "builders_assessment_report_suncorp.csv");
            jsonl = Path.Combine(solutionDirectory, "datasets", "dataset.jsonl");

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

            using (SqlConnection connection = new SqlConnection(Shared.Data.connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(Shared.Data.jobNumbers, connection))
                {

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var jobNmber = (string)reader[0];
                            Console.WriteLine(jobNmber);
                            tsaResponses = Shared.Data.AuditorsResponses(jobNmber, connection);

                            if (tsaResponses.Count > 0)
                            {
                                buildersResponses = Shared.Data.TechnicansResponses(jobNmber, connection);
                                //Add audit mappings to techicianl responses
                                for (int i = 0; i < buildersResponses.Count; i++)
                                {
                                    //Add the grouping ID's to the builder responses dataset
                                    var auditQuestion = responseMappings.First(_ => _.Field1 == buildersResponses[i].Index.ToString());
                                    buildersResponses[i].AuditQuestion = int.Parse(auditQuestion.Field2);
                                }
                                var groupedBuilderformresponses = buildersResponses.GroupBy(_ => _.AuditQuestion);
                                Shared.Data.JsonlOutput(tsaAuditQuestions, tsaResponses, groupedBuilderformresponses);
                            }
                        }
                    }
                }
            }
        }
    }
}
