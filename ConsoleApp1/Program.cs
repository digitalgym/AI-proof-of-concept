// See https://aka.ms/new-console-template for more information
namespace Console1App1
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string solutionDirectory = Directory.GetParent(executableDirectory).Parent.Parent.FullName;

            Shared.Data.path = Path.Combine(solutionDirectory, "datasets", "dataset.jsonl");
            Shared.Data.GenerateDataSet(Shared.OutputFormat.Jsonl);

            Shared.Data.path = Path.Combine(solutionDirectory, "datasets", "dataset.csv");
            Shared.Data.GenerateDataSet(Shared.OutputFormat.CSV);

            Shared.Data.path = Path.Combine(solutionDirectory, "datasets", "meta.csv");
            Shared.Data.GenerateDataSet(Shared.OutputFormat.Meta);
        }
    }
}
