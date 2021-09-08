using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CyberRequest {

    // Root myDeserializedClass = JsonConvert.Deserializestring<Root>(myJsonResponse); 
    public class Product {
        public string preReqStatus { get; set; }
        public int productId { get; set; }
        public int roundId { get; set; }
        public string guaAmount { get; set; }
        public int prdPayFeeType { get; set; }
        public string prdReduGuaType { get; set; }
        public string refNo1 { get; set; }
        public string refNo2 { get; set; }
        public string refNo3 { get; set; }
        public string advFeeYearId { get; set; }
        public string reduce { get; set; }
        public string preReqSendDt { get; set; }
        public string rejectFlg { get; set; } = "";
    }

    public class Bank {
        public int bankId { get; set; }
        public string bankBrnUseLimit { get; set; }
        public string bankBrnSendOper { get; set; }
        public string guaCareName { get; set; }
        public string guaCareMobile { get; set; }
        public string guaCarePhone { get; set; }
        public string guaCareEmail { get; set; }
        public string guaApproveEmail { get; set; }
        public string guaRemark { get; set; } = "-";
    }

    public class Address {
        public bool active { get; set; } = true;
        public string addressType { get; set; }
        public string addressNo { get; set; }
        public string addressMoo { get; set; }
        public string addressAlley { get; set; }
        public string addressRoad { get; set; }
        public int subDistrictId { get; set; }
        public int districtId { get; set; }
        public int provinceId { get; set; }
        public string postalCode { get; set; }
        public string countryId { get; set; }
        public string addressOversea { get; set; }
    }

    public class Spouse {
        public string identification { get; set; }
        public string identificationType { get; set; }
        public int titleId { get; set; }
        public string cusNameTh { get; set; }
        public string cusSurnameTh { get; set; }
        public string cusNameEn { get; set; }
        public string cusSurnameEn { get; set; }
        public string birthDate { get; set; }
        public string telephoneNo { get; set; } = "0800000000";
        public string mobilePhoneNo { get; set; } = "0800000000";
        public string faxNo { get; set; } = "0800000000";
        public string email { get; set; }
        public string registerCapital { get; set; } = "0";
        // public List<Address> address { get; set; }
        public Address[] address { get; set; }
    }

    public class Relation {
        public int title { get; set; } = 1;
        public string name { get; set; } = "-";
        public string surname { get; set; } = "-";
        public string identification { get; set; } = "0000000000000";
        public string identificationType { get; set; } = "C";
        public string relationshipCode { get; set; } = "05";
        public string shareholderAmount { get; set; } = "50";
        public string customerType { get; set; } = "02";
    }

    public class Customer {
        public string customerUserType { get; set; }
        public string identification { get; set; }
        public string identificationType { get; set; }
        public string customerStatus { get; set; }
        public string customerType { get; set; }
        public string customerGrade { get; set; } = "-";
        public string customerScore { get; set; } = "0";
        public string raceId { get; set; }
        public string raceStr { get; set; }
        public string nationalityId { get; set; }
        public string nationalityStr { get; set; }
        public string refReqNumber { get; set; }
        public int customerId { get; set; }
        public string borrowerType { get; set; }
        public int titleId { get; set; }
        public string cusNameTh { get; set; }
        public string cusSurnameTh { get; set; }
        public string cusNameEn { get; set; }
        public string cusSurnameEn { get; set; }
        public string gender { get; set; }
        public string marriedStatus { get; set; }
        public string birthDate { get; set; }
        public string educationLevel { get; set; }
        //public List<int> career { get; set; }
        public int[] career { get; set; }
        public string telephoneNo { get; set; } = "0800000000";
        public string mobilePhoneNo { get; set; } = "0800000000";
        public string faxNo { get; set; } = "0800000000";
        public string email { get; set; }
        public int depLevelId { get; set; } = 1;
        public string proveDate { get; set; }
        public string businessExp { get; set; }
        public string registerDate { get; set; }
        public string registerCapital { get; set; } = "0";
        public string certificateDate { get; set; }
        public string customerAlive { get; set; }
        public string amountCol { get; set; }
        public string kycResult { get; set; }
        public string kycDate { get; set; }
        public string guarantorRelationCode { get; set; }
        public string guarantorRelationStr { get; set; }
        public string seq { get; set; }
        //public List<Address> address { get; set; }
        public Address[] address { get; set; }
        //public List<string> relation { get; set; }
        //public string[] relation { get; set; }
        public Relation[] relation { get; set; }
        public Spouse spouse { get; set; }
    }

    public class Manager {
        public string idCard { get; set; }
        public int titleId { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string identificationType { get; set; }
        public string exp { get; set; }
        public int customerId { get; set; }
        public string registerCapital { get; set; } = "0";
    }

    public class Asset {
        public string fixedAssetType { get; set; }
        public string amt { get; set; }
    }

    public class Finance {
        public int busFinId { get; set; }
        public string amtType { get; set; }
        public string yearPast1 { get; set; }
        public string yearPast2 { get; set; }
        public string yearPast3 { get; set; }
        public string yearPast4 { get; set; }
        public string yearPast5 { get; set; }
        public string yearCurrent { get; set; }
        public string yearEstimate1 { get; set; }
        public string yearEstimate2 { get; set; }
        public string yearEstimate3 { get; set; }
        public string yearEstimate4 { get; set; }
        public string yearEstimate5 { get; set; }
    }

    public class Finances {
        public string isicId { get; set; }
        public string isicCodeNameTh { get; set; }
        public string operation { get; set; } = "-";
        public string startDate { get; set; }
        public int employeeAmount { get; set; }
        //public string employeeAdd { get; set; }
        public int employeeAdd { get; set; }
        //public List<Manager> manager { get; set; }
        public Manager[] manager { get; set; }
        //public List<Asset> asset { get; set; }
        public Asset[] asset { get; set; }
        //public List<Finance> finance { get; set; }
        public Finance[] finance { get; set; }
        public Address address { get; set; }
        public string ebidta { get; set; }
        public string amtCreditOwn { get; set; }
        public int tcgBusinessId { get; set; }
        public string tcgBusinessName { get; set; }
        public string amtCredit { get; set; }
        public string dscr { get; set; }
        public string typeEstablishment { get; set; }
        public string ownerBusinessLocation { get; set; }
    }

    public class Credit {
        public string loanName { get; set; } = "-";
        public int contractId { get; set; }
        public string loanLimit { get; set; } = "0";
        public string loanBal { get; set; } = "0";
        public string loanInterest { get; set; }
        public string guaByTcg { get; set; }
        public int debtMonth { get; set; }
        public string rateType { get; set; }
        public string ratio { get; set; }
        public string contractName { get; set; } = "สัญญากู้เงิน";
        public string loanTypeName { get; set; }
        public string contractNo { get; set; }
        public string contractDate { get; set; }
        public string purposeCode { get; set; }
        public string debtPeriod { get; set; }
        public string debtDescription { get; set; }
        public string guaLimit { get; set; }
        public string contractNoDm { get; set; }
        public int contractDetailId { get; set; }
        public int debtorTypeId { get; set; }
    }

    public class Credits {
        public string guaLoadPurpose { get; set; }
        //public List<Credit> oldCredit { get; set; }
        public Credit[] oldCredit { get; set; }
        //public List<Credit> credit { get; set; }
        public Credit[] credit { get; set; }
        //public List<string> col { get; set; }
        public string[] col { get; set; }
        //public List<Customer> guarantorContract { get; set; }
        public Customer[] guarantorContract { get; set; }
        //public List<Customer> guarantorTcg { get; set; }
        public Customer[] guarantorTcg { get; set; }
    }

    public class Contract {
        public int loanContractId { get; set; }
        public string loanName { get; set; } = "-";
        public string contractNo { get; set; }
        public string contractName { get; set; } = "สัญญากู้เงิน";
        public string contractDate { get; set; }
        public string purposeCode { get; set; }
        public string debtPeriod { get; set; }
        public string debtDescription { get; set; }
        public string loanLimit { get; set; }
        public string guaLimit { get; set; }
        public int debtMonth { get; set; }
        public string rateType { get; set; }
        public string ratio { get; set; }
        public string loanTypeName { get; set; }
        public string contractNoDm { get; set; }
        public string loanInterest { get; set; } = "0";
        public int contractDetailId { get; set; }
        public int debtorTypeId { get; set; }
    }

    public class Contracts {
        public string preRequestName { get; set; }
        public string issuedAs { get; set; }
        public string issuedAsName { get; set; }
        //public List<Contract> contract { get; set; }
        public Contract[] contract { get; set; }
    }

    public class ListFile {
        public string fileName { get; set; }
        public string fileBase64 { get; set; }
    }

    public class Files {
        public int fileTypeId { get; set; }
        public string name { get; set; }
        public string remark { get; set; }
        //public List<ListFile> listFile { get; set; }
        public ListFile[] listFile { get; set; }
    }

    public class PayInSlip {
        public int documentTypeId { get; set; }
        public string lgId { get; set; }
        public string payinslipDt { get; set; }
        public string payinslipAmount { get; set; }
        public string bankAccountName { get; set; }
        public string bankAccountNo { get; set; }
        public string bankId { get; set; }
        public string bankName { get; set; }
        public string branchId { get; set; }
        public string branchName { get; set; }
        public string chequeName { get; set; }
        public string status { get; set; }
        public int createBy { get; set; }
        public string createDt { get; set; }
        public string updateBy { get; set; }
        public string updateDt { get; set; }
    }

    public class Answers {
        public int answerId { get; set; }
        public bool status { get; set; }
    }

    public class Answer {
        public int questionDetailId { get; set; }
        //public List<Answers> answer { get; set; }
        public Answers[] answers { get; set; }
    }

    public class PreScreenInitialNewData {
        public string screeningNameField { get; set; }
        public string value { get; set; }
    }

    public class RequestNo6 {
        public Product product { get; set; }
        public Bank bank { get; set; }
        //public List<Customer> customer { get; set; }
        public Customer[] customer { get; set; }
        public Finances finance { get; set; }
        public Credits credit { get; set; }
        public Contracts contract { get; set; }
        //public List<Files> file { get; set; }
        public Files[] file { get; set; }
        public PayInSlip payInSlip { get; set; }
        //public List<Answers> answer { get; set; }
        public Answer[] answer { get; set; }
        public string remark { get; set; }
        //public List<string> preScreeningInitialNew { get; set; }
        public PreScreenInitialNewData[] preScreeningInitialNew { get; set; }
        public string isSubmit { get; set; }
        public RequestNo6() {
            isSubmit = "Y";
        }
    }
}