using Microsoft.AspNetCore.Mvc;
using Shared;
using System.Data.SqlClient;
using System.Security.Cryptography;

namespace ai_reports_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TSAController : ControllerBase
    {
        [HttpGet("GetResponses")]
        public IEnumerable<TechicianResponse> GetResponses([FromQuery] string jobNumber)
        {
            var auditQuestions = Shared.Data.GetResponses(jobNumber);
            return auditQuestions;
        }

        [HttpGet("GetPrompt")]
        public Promt GetPromt([FromQuery] string prompt)
        {
            //May want to do two seperate prompts here, one for audit notes and another for score
     
            //Audit the answers provided

            //Scope the answers provided

            return new Promt {
            
            Score = 1,
            Audit = @"Great, thank you for providing the answers to the audit question.
              Based on what you have shared, here is my assessment:\n
              Report Details (Asbestos): Accurate The builder has reported that
              there are no asbestos materials in the dwelling, which is
              consistent with the age of the home. Therefore, this aspect of the
              report is accurate. Owner Maintenance and Defects: Incomplete
              While the builder has provided some information about the
              condition of the dwelling, including the construction material and
              age, there is no mention of any maintenance or defects. It would
              be important to include this information in the report to ensure
              that all aspects of the property are accounted for. Accommodation:
              Incomplete Similar to the previous point, while the total area of
              the dwelling has been provided, there is no further detail about
              the accommodation, such as the number of bedrooms or living areas.
              This information is necessary to provide a comprehensive
              understanding of the property. Overall, while there are some
              incomplete sections of the report, it appears that the majority of
              the required information has been provided. However, it is
              important to double-check each section to ensure accuracy and
              completeness before submitting the report."
            };
        }
    }
}
