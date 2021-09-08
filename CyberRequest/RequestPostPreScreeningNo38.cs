using System.Collections.Generic;

namespace CyberRequest {
    public class Detail {
        public string screeningTpInfId { get; set; }
        public string screeningCode { get; set; }
        public string screeningId { get; set; }
        public string screeningNameField { get; set; }
        public string value { get; set; }
    }
    public class RequestPostPreScreeningNo38 {
        public object result { get; set; }
        public int bankId { get; set; }
        public int productId { get; set; }
        // public List<Detail> detail { get; set; }
        public Detail[] detail { get; set; }
    }
}