using System.Collections.Generic;

// ProductGroup myDeserializedClass = JsonConvert.DeserializeObject<ProductGroup>(myJsonResponse); 
namespace CyberRequest {
    public class ProductGroupResult {
        public int productGroupId { get; set; }
        public string prodGrpCode { get; set; }
        public string prodGrpName { get; set; }
        public string prodGrpDesc { get; set; }
    }

    public class ProductGroupClassNo23 {
        public List<ProductGroupResult> result { get; set; }
        public string statusDescription { get; set; }
        public string status { get; set; }
        public int statusCode { get; set; }
    }
}