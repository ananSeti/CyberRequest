using System;
using System.Collections.Generic;

namespace CyberRequest {
    // DebtorTypeInfo myDeserializedClass = JsonConvert.DeserializeObject<DebtorTypeInfo>(myJsonResponse); 
    public class AResult {
        public int debtorTypeId { get; set; }
        public int bankId { get; set; }
        public string debtorTypeName { get; set; }
        public string status { get; set; }
        //public int createBy { get; set; }
        //public string createDt { get; set; }
        //public int updateBy { get; set; }
        //public string updateDt { get; set; }
    }

    public class DebtorTypeInfo {
        public List<AResult> result { get; set; }
        public string statusDescription { get; set; }
        public string status { get; set; }
        public int statusCode { get; set; }
    }
}