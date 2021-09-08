using System.Collections.Generic;

namespace CyberRequest {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class DetailR {
        public string screeningTpId { get; set; }
        public string screeningCode { get; set; }
        public string screeningId { get; set; }
        public string screeningNameField { get; set; }
        public string screeningName { get; set; }
        public string screeningTpInfId { get; set; }
        public string value { get; set; }
        public object result { get; set; }
        public object score { get; set; }
        public object scoreStatus { get; set; }
        public object totalScore { get; set; }
        public object cnfType { get; set; }
    }
    public class ResponsePostPreScreeningNo38 {
        public string result { get; set; }
        public string message { get; set; }
        public object refRequestId { get; set; }
        public int productId { get; set; }
        public int bankId { get; set; }
        public object totalScore { get; set; }
        public object scoreStatus { get; set; }
        public object checkType { get; set; }
        public List<DetailR> detail { get; set; }
    }


}