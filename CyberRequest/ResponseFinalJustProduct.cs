using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CyberRequest {

    // ResponseFinalJustProduct myDeserializedClass = JsonConvert.DeserializeObject<ResponseFinalJustProduct>(myJsonResponse); 
    public class ProductBack {
        public string preReqNumber { get; set; }
        public string preReqStatus { get; set; }
        public string preReqStatusStr { get; set; }
        public int productId { get; set; }
        public int roundId { get; set; }
        public string guaAmount { get; set; }
        public int prdPayFeeType { get; set; }
        public object prdReduGuaType { get; set; }
        public string refNo1 { get; set; }
        public string refNo2 { get; set; }
        public object refNo3 { get; set; }
        public object productGroup { get; set; }
        public object productSubGroup { get; set; }
        public object productSubGroupName { get; set; }
        public string productName { get; set; }
        public object updateDate { get; set; }
        public string updateBy { get; set; }
        public DateTime createDate { get; set; }
        public string createBy { get; set; }
        public DateTime submitDate { get; set; }
        public string submitBy { get; set; }
        public string preReqNumberDm { get; set; }
        public string screeningFlg { get; set; }
        public object scoringFlag { get; set; }
        public object advFeeYearId { get; set; }
        public object lgNo { get; set; }
        public object lgId { get; set; }
        public object lgDueDt { get; set; }
        public object oldAmount { get; set; }
        public object customCode { get; set; }
    }

    public class ContentBack {
        public string requestId { get; set; }
        public int requestIdSeq { get; set; }
        public object ownerType { get; set; }
        public string saveType { get; set; }
        public object saveTab { get; set; }
        public int workFlowCd { get; set; }
        public int workFlowStatus { get; set; }
        public object selectState { get; set; }
        public object assigneeId { get; set; }
        public object approveUser { get; set; }
        public object selectType { get; set; }
        public string flgRenew { get; set; }
        public int documentTypeId { get; set; }
        public object guaOldAmount { get; set; }
        public ProductBack product { get; set; }
    }

    public class ResponseFinalJustProduct {
        public string status { get; set; }
        public string message { get; set; }
        public ContentBack content { get; set; }
    }
}