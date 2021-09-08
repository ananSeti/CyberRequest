using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for BeforePreScreeningNo37
/// </summary>

namespace CyberRequest {
    public class ResultItem37 {
        public string screeningCode { get; set; }
        public string screeningId { get; set; }
        public string conditionValue { get; set; }
        public string screeningNameField { get; set; }
        public string screeningTpInfId { get; set; }
        public string conditionType { get; set; }
        public string screeningName { get; set; }
        public string screeningTpId { get; set; }
        public string status { get; set; }
        public string value { get; set; }
    }

    public class BeforePreScreeningNo37 {
        public int responseCode { get; set; }
        public string responseStatus { get; set; }
        public string responseMessage { get; set; }
        public List<ResultItem37> result { get; set; }
    }
}