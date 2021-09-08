using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CyberRequest {
    public class ProductGroup {
        public int productGroupId { get; set; }
    }

    public class BeforePreScreeningNo24 {
        public int productId { get; set; }
        public string productCode { get; set; }
        public string productName { get; set; }
        public int productParent { get; set; }
        public int productLevel { get; set; }
        public string productDesc { get; set; }
        public object limitIndvStart { get; set; }
        public object limitIndvEnd { get; set; }
        public object limitCorpStart { get; set; }
        public object limitCorpEnd { get; set; }
        public ProductGroup productGroup { get; set; }
        public object screeningFlg { get; set; }
        public object screeningTpId { get; set; }
        public object getProductSubDto { get; set; }
        public string prdNameWithMain { get; set; }
        public object prdNameHierarchy { get; set; }
    }
}