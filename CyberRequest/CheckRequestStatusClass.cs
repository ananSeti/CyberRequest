using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CyberRequest {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class BillNumberAll {
        public string billNumber { get; set; }
        public string payerType { get; set; }
        public string paidLgFlg { get; set; }
        public string paymentStatus { get; set; }
    }
    public class CSCResult {
        public int order { get; set; }
        public int requestId { get; set; }
        public string requestNum { get; set; }
        public string requestNumDm { get; set; }
        public string idCard { get; set; }
        public string borrowerName { get; set; }
        public string customerType { get; set; }
        public object creditAmount { get; set; }
        public string guaranteeAmont { get; set; }
        public string creditProvider { get; set; }
        public string statusStr { get; set; }
        public string status { get; set; }
        public object lgId { get; set; }
        public object lgNo { get; set; }
        public int documentTypeId { get; set; }
        public string productName { get; set; }
        public int productId { get; set; }
        public object submitDate { get; set; }
        public object ncbStatus { get; set; }
        public object ncbStatusStr { get; set; }
        public object guaType { get; set; }
        public object remark { get; set; }
        public List<object> receiptNo { get; set; }
        public List<object> billAfterLgList { get; set; }
        public string afterLgPaidFlg { get; set; }
        public string billAfterLg { get; set; }
        public string billFlag { get; set; }
        public List<object> receiptNo2 { get; set; }
        public object workflowCd { get; set; }
        public object workflowStatus { get; set; }
        public object assignee { get; set; }
        public object assigneeGroup { get; set; }
        public object assigner { get; set; }
        public object assignerGroup { get; set; }
        public string owner { get; set; }
        public object payStatusStr { get; set; }
        // public List<BillNumberAll> billNumberAll { get; set; }
        public BillNumberAll[] billNumberAll { get; set; }
        public object versionCount { get; set; }
        public string versionOwner { get; set; }
        public object workflowVersion { get; set; }
        public string productType { get; set; }
        public object productSubGroupName { get; set; }
        public object documentTypeName { get; set; }
        public string ownerGroup { get; set; }
        public object guaranteeDocType { get; set; }
        public List<string> payerType { get; set; }
    }
    public class CSCContent {
        public int totalElements { get; set; }
        public int totalPages { get; set; }
        public int currentPage { get; set; }
        //public List<CSCResult> result { get; set; }
        public CSCResult[] result { get; set; }
    }
    public class CheckRequestStatus {
        public string status { get; set; }
        public string message { get; set; }
        public CSCContent content { get; set; }
        public object page { get; set; }
        public object perPage { get; set; }
        public object totalPage { get; set; }
        public object totalElement { get; set; }
    }
}