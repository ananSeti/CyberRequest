using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for BankInfoClass
/// </summary>
namespace CyberRequest {
    public class Content {
        public int bankId { get; set; }
        public string bankCode { get; set; }
        public string bankNameTh { get; set; }
        public string bankNameEn { get; set; }
        public string bankNameAbbr { get; set; }
        public object bankSwiftcode { get; set; }
        public string status { get; set; }
        public object partnerGroupId { get; set; }
        public string bankType { get; set; }
        public int createBy { get; set; }
        public string createDt { get; set; }
        public int? updateBy { get; set; }
        public string updateDt { get; set; }
        public string bankHeadPosition { get; set; }
        public string bankMdName { get; set; }
        public string bankEmailFolwFee { get; set; }
        public object tcgStaffFolwFee { get; set; }
        public int? documentTypeInfId { get; set; }
        public object calRateType { get; set; }
        public object fileUploadBase64Dto { get; set; }
        public object resultBase64Dto { get; set; }
    }

    public class Sort {
        public bool sorted { get; set; }
        public bool unsorted { get; set; }
        public bool empty { get; set; }
    }

    public class Pageable {
        public Sort sort { get; set; }
        public long pageSize { get; set; }
        public int pageNumber { get; set; }
        public int offset { get; set; }
        public bool paged { get; set; }
        public bool unpaged { get; set; }
    }

    public class BankInfoResult {
        public List<Content> content { get; set; }
        public Pageable pageable { get; set; }
        public int totalElements { get; set; }
        public int totalPages { get; set; }
        public bool last { get; set; }
        public bool first { get; set; }
        public Sort sort { get; set; }
        public int numberOfElements { get; set; }
        public long size { get; set; }
        public int number { get; set; }
        public bool empty { get; set; }
    }

    public class BankInfoClass {
        public BankInfoResult result { get; set; }
        public string statusDescription { get; set; }
        public string status { get; set; }
        public int statusCode { get; set; }
    }
}