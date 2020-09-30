using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Net;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TakeHome.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CovidInfoController : ControllerBase
    {
        // GET: api/<CovidInfoController>
        //[HttpGet]
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        // GET api/<CovidInfoController>/5
        [HttpGet("{id}/{startdate}/{enddate}")]
        [HttpGet("{id}/{startdate}")]
        [HttpGet("{id}")]
        [HttpGet]
        public object Get(string id="", string startdate = "", string enddate = "")
        {
            List<CovidSummaryData> summaryDatas = new List<CovidSummaryData>();
            string message = "All Good.";
            if (!String.IsNullOrEmpty(id))
            {
                //return "value";
                DateTime dtStartDate = DateTime.MinValue;
                DateTime dtEndDate = DateTime.MinValue;
                if (startdate != "") DateTime.TryParse(startdate, out dtStartDate);
                if (enddate != "") DateTime.TryParse(enddate, out dtEndDate);
                if (dtStartDate != DateTime.MinValue && dtEndDate != DateTime.MinValue && dtStartDate > dtEndDate)
                {
                    DateTime tmpEndDate = dtStartDate;
                    dtStartDate = dtEndDate;
                    dtEndDate = tmpEndDate;
                }
                bool ErrorsEncountered = false;
                var csv = new List<string[]>();
                WebClient client = new WebClient();
                string content = client.DownloadString("https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv");
                string[] lines = content.Split(new string[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                //string[] lines = System.IO.File.ReadAllLines("https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv");

                foreach (string line in lines)
                    csv.Add(line.Split(','));

                var properties = lines[0].Split(',');
                int CountyPos = 5;
                int StatePos = 6;
                int LatPos = 8;
                int LongPos = 9;
                int FirstDatePos = 11;

                //Quickly check that the fields are what we think.  If not, we will need to devise some way to handle it
                if (properties[CountyPos] != "Admin2") ErrorsEncountered = true;
                if (properties[StatePos] != "Province_State") ErrorsEncountered = true;
                if (properties[LatPos] != "Lat") ErrorsEncountered = true;
                if (properties[LongPos] != "Long_") ErrorsEncountered = true;

                List<CovidData> listObjResult = new List<CovidData>();

                for (int i = 1; i < csv.Count; i++)
                {
                    if (csv[i].Length > 1 && (csv[i][CountyPos] == id || csv[i][StatePos] == id))
                    {
                        CovidData objResult = new CovidData();
                        //for (int j = 0; j < properties.Length; j++)
                        //    objResult.Add(properties[j], csv[i][j]);
                        objResult.County = csv[i][CountyPos];
                        objResult.State = csv[i][StatePos];
                        float lat = 0.0f;
                        float.TryParse(csv[i][LatPos], out lat);
                        objResult.Lat = lat;
                        float longv = 0.0f;
                        float.TryParse(csv[i][LongPos], out longv);
                        objResult.Long = longv;
                        int lastCaseNum = 0;
                        List<DateCase> dateCases = new List<DateCase>();
                        for (int j = FirstDatePos; j < properties.Length; j++)
                        {
                            DateTime dt = new DateTime();
                            int CaseNum = 0;
                            if (DateTime.TryParse(properties[j], out dt))
                            {
                                if (int.TryParse(csv[i][j+2], out CaseNum))  //Data contains commas ("county,state,us") so an offset is needed
                                {
                                    if ((dt >= dtStartDate || dtStartDate == DateTime.MinValue) && (dt <= dtEndDate || dtEndDate == DateTime.MinValue))
                                    {
                                        DateCase dateCase = new DateCase();
                                        dateCase.CaseCount = CaseNum;
                                        dateCase.DataDate = dt;
                                        dateCase.NewCases = CaseNum - lastCaseNum;
                                        dateCases.Add(dateCase);
                                    }
                                    lastCaseNum = CaseNum;
                                }
                                else
                                {
                                    ErrorsEncountered = true;
                                }
                            }
                            else
                            {
                                ErrorsEncountered = true;
                            }
                        }
                        objResult.DateCases = dateCases;

                        listObjResult.Add(objResult);
                    }
                }
                //var results = listObjResult.Where(a => a.Values.)

                List<CovidData> PreSummaryData = new List<CovidData>();
                foreach (CovidData covidData in listObjResult.Where(a => a.County == id)) PreSummaryData.Add(covidData);
                List<DateCase> StateDateCases = new List<DateCase>();
                foreach (CovidData covidData in listObjResult.Where(a => a.State == id))
                {
                    List<DateCase> dateCases = covidData.DateCases;
                    foreach (DateCase dateCase in dateCases)
                    {
                        DateCase StateDateCase = StateDateCases.FirstOrDefault(a => a.DataDate == dateCase.DataDate);
                        if (StateDateCase != null)
                        {
                            StateDateCase.CaseCount += dateCase.CaseCount;
                            StateDateCase.NewCases += dateCase.NewCases;
                        }
                        else
                        {
                            DateCase newDateCase = new DateCase();
                            newDateCase.DataDate = dateCase.DataDate;
                            newDateCase.CaseCount = dateCase.CaseCount;
                            newDateCase.NewCases = dateCase.NewCases;
                            StateDateCases.Add(newDateCase);
                        }
                    }
                }
                if (StateDateCases.Count > 0)
                {
                    CovidData stateCovidData = new CovidData();
                    stateCovidData.State = id;
                    stateCovidData.DateCases = StateDateCases;
                    PreSummaryData.Add(stateCovidData);
                }

                foreach (CovidData covidData in PreSummaryData)
                {
                    CovidSummaryData summaryData = new CovidSummaryData();
                    summaryData.County = covidData.County;
                    summaryData.Lat = covidData.Lat;
                    summaryData.Long = covidData.Long;
                    summaryData.State = covidData.State;
                    int minCases = -1;
                    int maxCases = -1;
                    int totalDays = 0;
                    int sumNewCases = 0;
                    DateTime MaxCasesDate = DateTime.MinValue;
                    DateTime MinCasesDate = DateTime.MinValue;
                    foreach (DateCase dateCase in covidData.DateCases)
                    {
                        if (dateCase.NewCases < minCases || minCases == -1)
                        {
                            MinCasesDate = dateCase.DataDate;
                            minCases = dateCase.NewCases;
                        }
                        if (dateCase.NewCases > maxCases || maxCases == -1)
                        {
                            MaxCasesDate = dateCase.DataDate;
                            maxCases = dateCase.NewCases;
                        }
                        sumNewCases += dateCase.NewCases;
                        totalDays++;
                    }
                    summaryData.DateOfMaximumCases = MaxCasesDate;
                    summaryData.MaximumCasesCount = maxCases;
                    summaryData.DateOfMinimumCases = MinCasesDate;
                    summaryData.MinimumCasesCount = minCases;
                    summaryData.AverageDailyCases = Math.Round(sumNewCases * 1.0f / totalDays, 1);
                    summaryDatas.Add(summaryData);
                }
                if (summaryDatas.Count == 0)
                {
                    if (ErrorsEncountered)
                        message = "No Results found and errors encountered.";
                    else
                        message = "No Results found.";
                }
                else
                {
                    if (ErrorsEncountered) message = "Errors Encountered.";
                }
            }
            else
            {
                message = "No location provided";
            }

            ReturnData retData = new ReturnData();
            retData.Message = message;
            retData.Results = summaryDatas;

            return Ok(retData);


        }

        // POST api/<CovidInfoController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        // PUT api/<CovidInfoController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        // DELETE api/<CovidInfoController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
