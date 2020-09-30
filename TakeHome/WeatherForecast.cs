using System;
using System.Collections.Generic;

namespace TakeHome
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class CovidData
    {
        public bool ErrorsEncountered { get; set; }

        public string County { get; set; }

        public string State { get; set; }

        public float Lat { get; set; }

        public float Long { get; set; }

        public List<DateCase> DateCases { get; set; }
    }

    public class CovidSummaryData
    {
        public string County { get; set; }

        public string State { get; set; }

        public float Lat { get; set; }

        public float Long { get; set; }

        public DateTime DateOfMinimumCases { get; set; }

        public int MinimumCasesCount { get; set; }

        public DateTime DateOfMaximumCases { get; set; }

        public int MaximumCasesCount { get; set; }

        public double AverageDailyCases { get; set; }
    }

    public class DateCase
    {
        public DateTime DataDate { get; set; }
        public int CaseCount { get; set; }
        public int NewCases { get; set; }
    }

    public class ReturnData
    {
        public string Message { get; set; }
        public List<CovidSummaryData> Results { get; set; }
    }
}
