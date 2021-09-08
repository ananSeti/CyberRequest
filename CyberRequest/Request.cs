using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using jcs.clientdb;
using jcs.clientdb.odbc;
using jcs.clientdb.sql;
using RestSharp;
using System.Reflection;
using System.Configuration;


namespace CyberRequest
{
    public class Request
    {
        const int intArraySize = 360;

        private int StationID = 0;
        private static Stack<string> stkToken = new Stack<string>();
        //private static string strToken = GetToken();
        private static string strToken = "";
        public static string StrToken
        {
            get
            {
                strToken = stkToken.Peek();
                return strToken;
            }
        }

        private static DataSet dtsTemp = null;
        private static string strResponseJSONContent = "";
        private static string strResult = "";
        private static string strFinalJsonResponse = "";
        private static string strTempOnlineID = "";
        private static string strTempRequestNo = "";
        private static string strTempRequestNo6Json = "";
        private static string strTempPreScreenData = "";
        private static string[] ar = new string[intArraySize];
        private static string[] arn = new string[intArraySize];
        private static int intIndex = 0;

        private static string strPk = "";
        private static string strCustomerType = "";
        private RequestNo6 reqNo6 = new RequestNo6();
        private Product pd = new Product();
        private Bank bk = new Bank();
        private Customer cm = new Customer();
        private Finances fns = new Finances();
        private Finance fn2110224I = new Finance();
        private Finance fn2110225C = new Finance();
        private Finance fn2110226E = new Finance();
        private Finance fn2110227N = new Finance();
        private Finance fn2110228G = new Finance();
        private Credits cr = new Credits();
        private Contracts ct = new Contracts();
        private Files[] fl = null;
        private PayInSlip ps = new PayInSlip();
        private Answers aws = new Answers(); // inner
        private Answer aw = new Answer(); // outer
        private Address address = new Address();
        private RequestPostPreScreeningNo38 rp38 = null;
        private PreScreenInitialNewData[] psind = null;
        private static string strTokenServerURL = "http://192.168.12.16:31090";
        private static string strWorkServerURL = "http://192.168.12.16:31090";
        private static string strDebtorJson = "";
        private static string strJSONRequestFilePath = "";
        private static string strJSONResponseFilePath = "";

        // used in log db at final place and other related
        private static string _T01Online_ID = "";
        private static string _T01Request_No = "";
        private static string _T01Send_Date = "";
        private static string _T01Send_Date_Cyber = "";
        private static string _T01Send_Time_Cyber = "";
        private static string _T01Project_Type = "";
        private static string _T01Name_Thai = "";
        private static string _T01Surname_Thai = "";
        private static string _T01Last_Status = "";
        private static string _T01House_Province = "";
        private static string _productCode = "";
        private static string _productId = "";
        private static string _status = "";
        private static string _requestId = "";
        private static string _requestIdSeq = "";
        private static string _preReqNumber = "";
        private static string _preReqStatus = "";
        private static string _preReqStatusStr = "";
        private static string _jsonRequestFileLocation = "";
        private static string _jsonResponseFileLocation = "";
        private static string _writtenDateTime = "";
        private static string _writtenBy = "";
        private static string _T01Loan_Type_1 = "";
        private static string _T01Loan_Subject_1 = "";
        private static string _T01Bank_Code = "";
        private static int _BankId = 0;
        private static string _T01Total_Asset = "";
        private static string _T01Total_Debt = "";
        private static float _T01CostEstimate = 0;
        private static float _T01Total_Loan_Amount = 0;
        private static string _RejectFlag = "";

        // used in log db at final place
        private static int intAIndex = 0;
        private static string strProductName = "";

        static string NullToString(Object Value)
        {
            return Value == null ? "" : Value.ToString();
        }

        public Request()
        {
            StationID = Program.StationID;
        }

        public InterfaceDatabase ConnectCgs(string dns)
        {
            InterfaceDatabase db = null;

            db = new OdbcDatabase();
            db.ConnectionString = "Dsn=" + dns;

            if (db.Open() == false)
            {
                Console.WriteLine("Can't open database CGS->"+dns+ " " + db.LastError);
                db = null;
            }
            //}
            return db;
        }

        public InterfaceDatabase ConnectCyber()
        {
            InterfaceDatabase db = null;

            string strCon = ConfigurationManager.ConnectionStrings["CGSAConnectionString"].ConnectionString;
            //db = new SQLDatabase("sa", "sicgcadmin", @"192.168.10.17", "DB_CGSAPI_MASTER", 300);
            db = new SQLDatabase();
            db.ConnectionString = strCon;
            //db = new SQLDatabase("dbverify", "sicgcadmin", @"192.168.0.83", "DB_Pre_Migration", 300);
            //db.ConnectionString = ConnectionString;
            if (db.Open() == false)
            {
                Console.WriteLine("Can't open database Cyber" + db.LastError);
                db = null;
            }
            return db;

        }


        public InterfaceDatabase ConnectMasterCgs()
        {
            InterfaceDatabase db = null;

            string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
            //db = new SQLDatabase("sa", "sicgcadmin", @"192.168.10.17", "DB_CGSAPI_MASTER", 300);
            db = new SQLDatabase();
            db.ConnectionString = strCon;
            //db = new SQLDatabase("dbverify", "sicgcadmin", @"192.168.0.83", "DB_Pre_Migration", 300);
            //db.ConnectionString = ConnectionString;
            if (db.Open() == false)
            {
                Console.WriteLine("Can't open database MasterCSGDB" + db.LastError);
                db = null;
            }
            return db;

        }
        /// <summary>
        /// </summary>
        /// <param name="ref_no3"></param>
        /// <returns>-1=not connect 0=not dupl 1=dupl</returns>
        public int CheckDuplInCGS(string ref_no3)
        {
            int ret = -1;
            InterfaceDatabase conProd = ConnectCgs("CGSPROD");
            if (conProd != null)
            {
                Recordset rs = conProd.GetRecordset("select ref_no3,pre_req_status,pre_req_number from TBL_RD_GUA_PRE_REQUEST where ref_no3='" + ref_no3 + "' and STATUS='A'",1);
                if (rs.RecordCount > 0)
                {
                    //ปฏิเสธ ให้ยิงซ้ำได้
                    if (rs["pre_req_status"].Int32Value == 21)
                    {
                        ret = 2; //re sent

                    }
                    else
                    {
                        ret = 1; //dupl
                    }
                }
                else
                {
                    ret = 0; //no dupl
                }
                rs.Close();
            }
            return ret;
        }

        /// <summary>
        /// Assign Queue from cyber to CGS
        /// </summary>
        public void AssignQueueCybertoCGS(string t01projecttype_toassigntocgs, string t01send_date_start_toassigntocgs,int numberof_toassigntocgs)
        {
            if (t01projecttype_toassigntocgs.Length==0)
            {
                Console.WriteLine("Not assign variable value: t01projecttype_toassigntocgs");
                return;
            }
            if (t01send_date_start_toassigntocgs.Length==0)
            {
                Console.WriteLine("Not assign variable value: t01send_date_start_toassigntocgs");
                return;
            }
            if(numberof_toassigntocgs<=0 || numberof_toassigntocgs>20)
            {
                Console.WriteLine("Maximum limit number of assign queues is 20");
                return;
            }

            string sql = @"insert into [TBL_CI_Import_Status](
                   [T01Online_ID]
                  ,[T01Send_Date]
                  ,[T01Send_Time]
                  ,[T01Project_Type]
                  ,[T01Last_Status]
                  ,[T01Bank_Code]
                  ,[T01House_Province]
	              ,[CreateDateTime]
            )
            SELECT 
                  [T01Online_ID]
                  ,[T01Send_Date]
                  ,[T01Send_Time]
                  ,[T01Project_Type]
                  ,[T01Last_Status]
                  ,[T01Bank_Code]
                  ,[T01House_Province]
	              ,getDate() as [CreateDateTime]

              FROM[DB_CGSAPI_MASTER].[dbo].[V_AssignQueueToCGS]
              where[T01Project_Type] IN (" + t01projecttype_toassigntocgs + ") " + " and T01Send_Date>='" + t01send_date_start_toassigntocgs  + "' order by t01send_date,T01Send_Time";
             
            InterfaceDatabase conmastecgs = ConnectMasterCgs();

            string sqlAssignStatus3toQueue = "UPDATE TBL_CI_Import_Status SET Imported=null where Imported='3'";
            int roweff3 = conmastecgs.Execute(sqlAssignStatus3toQueue);
            Console.WriteLine("Put old Status 3  to rerun again." + roweff3 + " Rows Effected.\r" + conmastecgs.LastError);


            int roweff = conmastecgs.Execute(sql);
            Console.WriteLine("Insert data to Queue " + roweff + " Rows Effected.\r" + conmastecgs.LastError);
            if (roweff > 0)
            {
                string setstation = @"UPDATE
                                        TABLE_A
                                    SET
                                        Table_A.StationID = Table_B.NewStationID
                                    FROM
                                        [DB_CGSAPI_MASTER].[dbo].[TBL_CI_Import_Status] Table_A
                                        INNER JOIN 
	                                       (
		                                    SELECT [T01Online_ID]
			                                      ,(ROW_NUMBER() OVER(ORDER BY T01Online_ID ASC)%" + numberof_toassigntocgs + ")+1 AS newStationID "+
                                           @"       ,[Imported]
			                                      ,[T01Send_Date]
			                                      ,[T01Send_Time]
			                                      ,[T01Project_Type]
			                                      ,[T01Last_Status]
			                                      ,[T01Bank_Code]
			                                      ,[T01House_Province]
			                                      ,[CreateDateTime]
			                                      ,[StationID]
		                                      FROM [DB_CGSAPI_MASTER].[dbo].[TBL_CI_Import_Status] where Imported is null and stationid is null

	                                       ) Table_B
                                            ON Table_A.[T01Online_ID] = Table_B.[T01Online_ID] and Table_A.[T01Send_Date] = Table_B.[T01Send_Date]
                                    WHERE
                                        Table_A.[Imported] is null";
                            int eff = conmastecgs.Execute(setstation);
                            Console.WriteLine(eff + " Rows Assign StationID.\r" + conmastecgs.LastError);

            }

            conmastecgs.Close();

        }

        /// <summary>
        /// Start all in job
        /// </summary>
        public void StartAllInJob()
        {
            int intRow = 0;
            string strTempResult = "";
            string strKey = "";
            Console.WriteLine("Get new online id.. ");

            try
            {
                Console.WriteLine("Connection db.. ");
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                //Console.WriteLine("Connection db.. "+ strCon);
                string strSql = string.Format("SELECT top 1 [T01Online_ID] FROM [dbo].[TBL_CI_Import_Status] WHERE ([Imported] is null ) and StationID=" + StationID + " order by T01Send_Date,T01Online_ID");
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                Console.WriteLine("Connected");
                DataSet dts = new DataSet("_Temp_CI_Import_Status_DS");
                dta.Fill(dts, "_Temp_CI_Import_Status");
                Console.WriteLine("Start job =>" + dts.Tables[0].Rows.Count + " rows to be fire." + Environment.NewLine);
                foreach (var i in dts.Tables[0].Rows)
                {

                    string result = "";
                    strKey = ((DataRow)i)[0].ToString(); //  dts.Tables[0].Rows[i][0].ToString(); // T01Online_ID

                    Console.WriteLine("Checking " + strKey + " in CGS...");
                    int dupl=CheckDuplInCGS(strKey);
                    if (dupl == 0 || dupl==2) //not duplication in cgs
                    {

                        Console.WriteLine("###Sending =>" + strKey);

                        ClearAndInitializeAllVariables();
                        if (strToken.Length > 30)
                        {
                            result = GetDataFromT01Online_ID(strKey);
                            Console.WriteLine("###Return =>" + result);
                            Console.WriteLine("###End =>" + strKey);
                        }
                        else
                        {
                            result = "Token => fail(" + strToken + ")";
                            Console.WriteLine("###Token =>fail (" + strToken + ")");

                        }
                    }
                    else
                    {
                        if (dupl > 0)
                        {
                            result = "Duplicate in CGS..";
                            UpdateImportStatusDb(strKey, "YD"); //YD พบข้อมูลใน CGS แล้ว

                        }
                        else
                        {
                            result = "not connect CGS..";
                        }
                    }
                    System.Threading.Thread.Sleep(1000);
                    strTempResult = result + Environment.NewLine;
                    intRow++;
                    WriteLogFile(strTempResult);
                    System.Diagnostics.Debug.WriteLine(strTempResult);
                }
                con.Close();
                WriteLogFile(string.Format("Total {0} transactions were fired to 'TBL_CI_Import_Status'", intRow.ToString()));

            }
            
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            
            

        }


        public void ClearAndInitializeAllVariables()
        {
            stkToken.Clear();
            stkToken = new Stack<string>();
            Console.WriteLine("Getting Token...");
            strToken = GetToken();
            Console.WriteLine("Token return..."+ strToken);
            dtsTemp = null;
            strResponseJSONContent = "";
            strResult = "";
            strFinalJsonResponse = "";
            strTempOnlineID = "";
            strTempRequestNo = "";
            strTempRequestNo6Json = "";
            strTempPreScreenData = "";
            ar = null;
            arn = null;
            ar = new string[intArraySize];
            arn = new string[intArraySize];
            intIndex = 0;

            //// used in log db at final place
            _T01Online_ID = "";
            _T01Request_No = "";
            _T01Send_Date = "";
            _T01Send_Date_Cyber = "";
            _T01Send_Time_Cyber = "";
            _T01Project_Type = "";
            _T01Name_Thai = "";
            _T01Surname_Thai = "";
            _T01Last_Status = "";
            _T01House_Province = "";
            _productCode = "";
            _productId = "";
            _status = "";
            _requestId = "";
            _requestIdSeq = "";
            _preReqNumber = "";
            _preReqStatus = "";
            _preReqStatusStr = "";
            _jsonRequestFileLocation = "";
            _jsonResponseFileLocation = "";
            _writtenDateTime = "";
            _writtenBy = "";
            _T01Loan_Type_1 = "";
            _T01Loan_Subject_1 = "";
            _T01Bank_Code = "";
            _BankId = 0;
            _T01Total_Asset = "";
            _T01Total_Debt = "";
            _T01CostEstimate = 0;
            _T01Total_Loan_Amount = 0;
            _RejectFlag = "";

            strPk = "";
            strCustomerType = "";
            reqNo6 = null;
            pd = null;
            bk = null;
            cm = null;
            fns = null;
            fn2110224I = null;
            fn2110225C = null;
            fn2110226E = null;
            fn2110227N = null;
            fn2110228G = null;
            cr = null;
            ct = null;
            fl = null;
            ps = null;
            aws = null; // inner
            aw = null; // outer
            address = null;
            rp38 = null;

            reqNo6 = new RequestNo6();
            pd = new Product();
            bk = new Bank();
            cm = new Customer();
            fns = new Finances();
            fn2110224I = new Finance();
            fn2110225C = new Finance();
            fn2110226E = new Finance();
            fn2110227N = new Finance();
            fn2110228G = new Finance();
            cr = new Credits();
            ct = new Contracts();
            fl = null;
            ps = new PayInSlip();
            aws = new Answers(); // inner
            aw = new Answer(); // outer
            address = new Address();
            rp38 = null;
            psind = null;

           strTokenServerURL = "http://192.168.12.16:31090";
            strWorkServerURL = "http://192.168.12.16:31090";
            strDebtorJson = "";
            strJSONRequestFilePath = "";
            strJSONResponseFilePath = "";

            //// used in log db at final place
            intAIndex = 0;
            strProductName = "";

            return;
        }

        public void WriteLogFile(string strData)
        {
            Console.WriteLine(strData);
            try
            {
                DateTime dtmNow = DateTime.Now;
                string strFilename = string.Format("Log{0}{1:00}{2:00}.txt", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);
                StreamWriter sw = File.AppendText(@"d:\Cybter\Log\" + strFilename);
                sw.WriteLine(dtmNow.ToString());
                sw.WriteLine(strData);
                sw.WriteLine("----------");
                sw.Close();
            }
            catch (Exception ex)
            {

            }
        }

        public bool IsProjectType995(string strT01Online_ID)
        {
            bool bln995 = false;
            string strGettingData = "";
            string strConnectionString = "CGConnectionString";
            string strDbTableName = "[dbo].[T01_Request_Online]";
            string strPrimaryKey = "T01Online_ID";
            string strSql = "SELECT count(1) FROM " + strDbTableName + " WHERE ((T01Online_ID='" + strT01Online_ID + "') AND (T01Project_Type = '00995'));";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            strGettingData = dtsResult.Tables[0].Rows[0][0].ToString();
            int i = 0;
            int.TryParse(strGettingData, out i);
            if (i > 0)
            {
                bln995 = true;
            }
            return bln995;
        }
        public bool IsInExcelList995(string strT01Online_ID)
        {
            //public static bool IsInExcelList995(string strT01Online_ID) {
            bool blnInExcel = false;
            string strGettingData = "";
            string strConnectionString = "PaymentSA995ConnectionString";
            string strDbTableName = "[dbo].[BOT_ImportRequest]";
            string strPrimaryKey = "id";
            string strSql = "SELECT count(1) FROM " + strDbTableName + " WHERE online_ID='" + strT01Online_ID + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            strGettingData = dtsResult.Tables[0].Rows[0][0].ToString();
            int i = 0;
            int.TryParse(strGettingData, out i);
            if (i > 0)
            {
                blnInExcel = true;
            }
            return blnInExcel;
        }

        
        //public static bool PaidFor995(string strT01Online_ID) {
        public bool IsPaidFor995(string strT01Online_ID)
        {
            bool blnPaid = false;
            string strGettingData = "";

            string strSqlOldBill = "SELECT top 1 T01Online_ID,T01OldBillPayment_Ref1 FROM [T01_Request_Online] WHERE T01Online_ID='" + strT01Online_ID + "';";
            DataSet dtsOldBill = GetData(strSqlOldBill, "[dbo].[DigitalPassStatement]", "T01Online_ID", "CGSAConnectionString");
            string sOldBill=dtsOldBill.Tables[0].Rows[0]["T01OldBillPayment_Ref1"].ToString();

            string strKey = strT01Online_ID.Replace("O", "9");
            //use old bill no
            if (sOldBill.Trim().Length > 5)
            {
                strKey = sOldBill;
            }
            string strConnectionString = "PaymentSA995ConnectionString";
            string strDbTableName = "[dbo].[DigitalPassStatement]";
            string strPrimaryKey = "Id";
            string strSql = "SELECT count(1) FROM " + strDbTableName + " WHERE reference1='" + strKey + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            strGettingData = dtsResult.Tables[0].Rows[0][0].ToString();
            int i = 0;
            int.TryParse(strGettingData, out i);
            if (i > 0)
            {
                blnPaid = true;
            }

            return blnPaid;
        }
        



        public bool Is995NotPaidIn3Days(string strT01Online_ID)
        {
            bool blnIs995NotPaidIn3Days = false;
            string strGettingData = "";
            string strConnectionString = "CGConnectionString";
            string strDbTableName = "[dbo].[T01_Request_Online]";
            string strPrimaryKey = "T01Online_ID";
            string strSql = "SELECT T01Send_Date FROM " + strDbTableName + " WHERE T01Online_ID='" + strT01Online_ID + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            if (dtsResult == null)
            {
                return false;
            }
            strGettingData = dtsResult.Tables[0].Rows[0][0].ToString();
            string strYear = strGettingData.Substring(0, 4);
            string strMonth = strGettingData.Substring(4, 2);
            string strDay = strGettingData.Substring(6, 2);
            DateTime dtmSend = Convert.ToDateTime(strDay + "-" + strMonth + "-" + strYear);


            DateTime dtmNow = DateTime.Now;
            //int intInterval = (dtmNow - dtmSend).Days;
            int intInterval = DateDiffWithHoliday(dtmSend, dtmNow);
            if (intInterval > 3)
            {
                blnIs995NotPaidIn3Days = true;
            }
            return blnIs995NotPaidIn3Days;
        }

        //public static bool PaidFor995(string strT01Online_ID) {
        public bool IsProjectHavingSendDate(string strT01Online_ID)
        {
            bool blnIsProjectHavingSendDate = false;
            string strGettingData = "";
            string strConnectionString = "CGConnectionString";
            string strDbTableName = "[dbo].[T01_Request_Online]";
            string strPrimaryKey = "T01Online_ID";
            string strSql = "SELECT T01Send_Date FROM " + strDbTableName + " WHERE T01Online_ID='" + strT01Online_ID + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            if (dtsResult != null)
            {
                blnIsProjectHavingSendDate = true;
            }
            return blnIsProjectHavingSendDate;
        }
        public static void WriteOutputFile(string strData)
        {
            try
            {
                DateTime dtmNow = DateTime.Now;
                string strFilename = string.Format("Output{0}{1:00}{2:00}.txt", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);
                //StreamWriter sw = File.AppendText(new Page().Server.MapPath("~/Output/" + strFilename));
                StreamWriter sw = File.AppendText(@"d:\Output\" + strFilename);
                sw.WriteLine(strData);
                sw.Close();
            }
            catch (Exception ex)
            {

            }
        }

        public string GetTokenNonStatic()
        {
            string strTokenNonStatic = "";
            strTokenNonStatic = GetToken();
            return strTokenNonStatic;
        }

        public bool CheckingIsValidProduct(string strT01Online_ID)
        {
            bool blnValidProduct = false;
            DataSet dts = null;
            try
            {
                string strTableName = "[dbo].[T01_Request_Online]";
                string strCon = ConfigurationManager.ConnectionStrings["CGConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT T01Project_Type FROM {0} WHERE T01Online_ID='{1}'", strTableName, strT01Online_ID);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                dts = new DataSet("TempDS");
                dta.Fill(dts, strTableName);
                int intProjectType = Convert.ToInt32(dts.Tables[strTableName].Rows[0][0].ToString());
                if (intProjectType >= 900)
                {
                    blnValidProduct = true;
                }
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
            }
            return blnValidProduct;
        }

        public bool CheckingUsedToBeFired(string strT01Online_ID)
        {
            bool blnUsedToBeFired = false;
            DataSet dts = null;
            try
            {
                string strTableName = "[dbo].[TBL_CI_LOG]";
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT COUNT(1) FROM {0} WHERE T01Online_ID='{1}'", strTableName, strT01Online_ID);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                dts = new DataSet("TempDS");
                dta.Fill(dts, strTableName);
                int intHaveRow = Convert.ToInt32(dts.Tables[strTableName].Rows[0][0].ToString());
                if (intHaveRow > 0)
                {
                    blnUsedToBeFired = true;
                }
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
            }
            return blnUsedToBeFired;
        }
        public static string GetToken()
        {
            strToken = "";
            //var client = new RestClient("http://192.168.12.16:31090/authentication-service/oauth/token");
            var client = new RestClient("http://192.168.12.16:31090/authentication-service/oauth/token");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic eSrTcpfOZ1O6ZmkkN4YbWlSg1X9JYpFexMZSAprl7gM=");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("username", "JeTwLJALsikYUPXYhQtXag==");
            request.AddParameter("password", "3UjGoHL3x0kCJ2+Bu0n0Yg==");
            request.AddParameter("grant_type", "password");
            IRestResponse response = client.Execute(request);
            strResponseJSONContent = response.Content;
            strToken = ReadTokenFromJSON(strResponseJSONContent);
            //strToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6ImNncy1rZXktaWQifQ.eyJicmFuY2gtcGVybWl0IjpbXSwiYXVkIjpbIndlYl9hcGkiXSwic2FsdCI6IkRKVmFOdTRsd0pCbG9lNlciLCJ1c2VyX25hbWUiOiJUQ0dfU1lTVEVNIiwic2NvcGUiOlsiUkVBRCIsIkNSRUFURSIsIlVQREFURSIsIkRFTEVURSJdLCJpc3MiOiJKb2pvZUBnZGwiLCJleHAiOjE2MTgwOTM5NzEsInVzZXJJZCI6NDE5NiwiaWF0IjoxNjE3MzczOTcxOTc4LCJqdGkiOiJmOTY1YjJhNS0wNmVmLTQ1ZDQtODNlZS04YThmMjBkNDU5MTgiLCJjbGllbnRfaWQiOiJ3ZWJfcG9ydGFsIn0.UXKfl-YtjJnNnIRrYToT2tdqcruYcdJZDLJq2Q6D5XckKmC6Fs1GFbgqa7qskh2KwPsqNo8lIVLQ-TxkkLjhzaFNkJF6iRzw_EclcRx2YNP8VtIM20WEQnBxZF-vXn3Lri-N4q13YnS88ZNqnWOTfC38CY0gj5XtwfzDLO0pf3p5vqGLb2GboxgLXfMgKVq0UT6OR3JxW8oZZ-zQeQCCWejKyvDlRZvgpSIPv4kpta-mDhSHA1WBMRKNKOtai_sarvHSrczQIXtdMV94giugwKFH4evtA3YLoaCXHkrv7lvvz70or7FhUIM6Ch7fF_Of7acrgRDYXB0-QOqjvSMBIHWg";
            stkToken.Push(strToken);
            return strToken;
        }
        private static string ReadTokenFromJSON(string strSource)
        {
            string strToken = "";
            try
            {
                TokenRelatedClass ds = JsonSerializer.Deserialize<TokenRelatedClass>(strSource);
                if (ds == null)
                {
                    strToken = "ds is null.  Should exit program and contact your admin.";
                }
                else if (ds.access_token == "" || ds.access_token == null)
                {
                    strToken = "ds.access_token is null or is blank string.  Should exit program and contact your admin.";
                }
                else
                {
                    strToken += ds.access_token.ToString();
                }
                return strToken;
            }
            catch
            {
                return "Exception in getting token";
            }
        }
        string JSONFromSendOldToNew(DataSet dts)
        {
            string strJSONResult = "";
            DataTable dtb = dts.Tables[0];
            //try {
            var varTempOnlineID = dtb.Rows[0]["T01Online_ID"];
            strTempOnlineID = ((varTempOnlineID == null) || (Convert.ToString(varTempOnlineID) == "")) ? "" : varTempOnlineID.ToString();
            //ar[intAIndex] = strTempOnlineID;
            //arn[intAIndex++] = "strTempOnlineID";
            _T01Online_ID = strTempOnlineID; // used in log

            var tempData = dtb.Rows[0]["T01Request_No"];
            strTempRequestNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = strTempRequestNo;
            //arn[intAIndex++] = "strTempRequestNo";
            _T01Request_No = strTempRequestNo; // used in log

            tempData = dtb.Rows[0]["T01Send_Date"];
            string strSendDate = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = pd.preReqSendDt;
            //arn[intAIndex++] = "pd.preReqSendDt";
            _T01Send_Date_Cyber = strSendDate; // used in status


            tempData = dtb.Rows[0]["T01Send_Time"];
            string strSendTime = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            _T01Send_Time_Cyber = strSendTime; // used in status

            tempData = dtb.Rows[0]["T01Last_Status"];
            _T01Last_Status = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();  // used in status


            pd.preReqSendDt = this.ConvertDateTime(strSendDate, strSendTime);
            _T01Send_Date = pd.preReqSendDt; // used in log

            tempData = dtb.Rows[0]["T01Ref_1"];
            //pd.refNo1 = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            pd.refNo1 = ((tempData == null) || (tempData == "")) ? dtb.Rows[0]["T01Online_ID"].ToString() : tempData.ToString();
            //ar[intAIndex] = pd.refNo1;
            //arn[intAIndex++] = "pd.refNo1";

            tempData = dtb.Rows[0]["T01Ref_2"];
            pd.refNo2 = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = pd.refNo2;
            //arn[intAIndex++] = "pd.refNo2";

            //tempData = dtb.Rows[0]["T01Ref_2"];
            tempData = dtb.Rows[0]["T01Online_ID"];
            pd.refNo3 = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //add online id
            //ar[intAIndex] = pd.refNo3;
            //arn[intAIndex++] = "pd.refNo3";

            tempData = dtb.Rows[0]["T01Project_Type"];
            string strTempT01Project_Type = "";
            strTempT01Project_Type = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            _T01Project_Type = strTempT01Project_Type;
            //pd.productId = Convert.ToInt32(strTempProductId);
            pd.productId = GetProductId(strTempT01Project_Type);
            //ar[intAIndex] = pd.productId + " (int)";
            //arn[intAIndex++] = "pd.productId";
            _productId = pd.productId.ToString(); // used in log

            tempData = dtb.Rows[0]["T01Request_Amount"];
            pd.guaAmount = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[200] = pd.guaAmount;
            //arn[200] = "pd.guaAmount";

            /*
            กบอธิบายว่า  เกี่ยวกับ T01Fee_Before_Flag ชำระค่าธรรมเนียมล่วงหน้า
            field นี้ต่อเนื่องจากการชำระค่าธรรมเนียมล่วงหน้าครับ คือถามก่อนว่าชำระค่าธรรมเนียมล่วหน้าหรือไม่ ถ้าชำระ ถึงมีถามต่อว่า แบบลดภาระหรือไม่ ดังนั้น T01Not_Reduce = NULL เพราะเขาไม่ได้เลือกชำระค่าธรรมเนยีมล่วงหน้า ส่วน T01Not_Reduce = 1 คือแบบไม่ลดภาระค้ำ T01Not_Reduce = 0 คือแบบลดภาระค้ำ ครับ
            GCS will be prdReduGuaType 
            01 ลดภาระ
            02 ไม่ลดภาระ
             */
            tempData = dtb.Rows[0]["T01Not_Reduce"].ToString();
            string strTCGNotReduce = ((tempData == null) || (tempData == "") || tempData.ToString().ToLower() == "false") ? "0" : "1";
            pd.prdReduGuaType = GetCGSNotReduce(strTCGNotReduce);
            //ar[232] = pd.reduce;
            //arn[232] = "pd.reduce";

            tempData = dtb.Rows[0]["T01Bank_Code"];
            _T01Bank_Code = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            string strTempBankCode = _T01Bank_Code;
            strTempBankCode = strTempBankCode.Trim();
            bk.bankId = GetBankId(strTempBankCode); // Add
            _BankId = bk.bankId;
            //ar[intAIndex] = bk.bankId + " (int)"; //5
            //arn[intAIndex++] = "bk.bankId";

            tempData = dtb.Rows[0]["T01Branch_Code"];
            string strTempBranchCode0 = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            strTempBranchCode0 = strTempBranchCode0.Trim();
            //bk.bankBrnSendOper = GetBankBrnSendOper(strTempBranchCode0, strTempBankCode); // Add
            bk.bankBrnSendOper = GetBankBrnSendOper(strTempBranchCode0, bk.bankId.ToString()); // Add
            if (bk.bankBrnSendOper == "")
            {
                bk.bankBrnSendOper = null;
            }
            //ar[intAIndex] = bk.bankBrnSendOper; //6
            //arn[intAIndex++] = "bk.bankBrnSendOper";

            tempData = dtb.Rows[0]["T01Branch_Code"];
            string strTempBranchCode1 = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            strTempBranchCode1 = strTempBranchCode1.Trim();
            //bk.bankBrnUseLimit = GetBankBrnUseLimit(strTempBranchCode1, strTempBankCode); // Add
            bk.bankBrnUseLimit = GetBankBrnUseLimit(strTempBranchCode1, bk.bankId.ToString()); // Add
                                                                                               //ar[intAIndex] = bk.bankBrnUseLimit; //6
                                                                                               //arn[intAIndex++] = "bk.bankBrnUseLimit";
            if(bk.bankBrnUseLimit == "")
            {
                bk.bankBrnUseLimit = null;
            }



            // ***** Require productId & BankId
            tempData = dtb.Rows[0]["T01Fee_Before"];
            int intTempData = Convert.ToInt32((tempData == null) || (tempData == "") ? "0" : tempData.ToString());
            int intPrdPayFeeType = (intTempData > 0) ? 2 : 1;
            pd.prdPayFeeType = intPrdPayFeeType;
            //ar[intAIndex] = pd.prdPayFeeType.ToString();
            //arn[intAIndex++] = "pd.prdPayFeeType";
            if (intTempData > 0)
            {
                pd.advFeeYearId = this.GetAdvFeeYearIdFromProductIdAndBankId(pd.productId.ToString(), bk.bankId.ToString(), intTempData.ToString()).ToString();
                //ar[intAIndex] = pd.advFeeYearId;
                //arn[intAIndex++] = "pd.advFeeYearId";
            }

            // for 995
            pd.rejectFlg = _RejectFlag;

            tempData = dtb.Rows[0]["T01Brn_Cnt_Person"];
            bk.guaCareName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = bk.guaCareName; //7
            //arn[intAIndex++] = "bk.guaCareName";

            tempData = dtb.Rows[0]["T01Bank_Cnt_Telephone"];
            bk.guaCarePhone = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = bk.guaCarePhone; //8
            //arn[intAIndex++] = "bk.guaCarePhone";

            tempData = dtb.Rows[0]["T01Bank_Cnt_Mobile"];
            bk.guaCareMobile = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = bk.guaCareMobile; //9
            //arn[intAIndex++] = "bk.guaCareMobile";

            tempData = dtb.Rows[0]["T01Bank_Cnt_Email"];
            bk.guaCareEmail = ((tempData == null) || (tempData == "")) ? "fight@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = bk.guaCareEmail; //10
            //arn[intAIndex++] = "bk.guaCareEmail";

            bk.guaApproveEmail = bk.guaCareEmail;
            //ar[intAIndex] = bk.guaApproveEmail; //290
            //arn[intAIndex++] = "bk.guaApproveEmail";

            fns.address = new Address(); // instanciate

            tempData = dtb.Rows[0]["T89Enterprise_No"];
            fns.address.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = fns.address.addressNo; //11
            //arn[intAIndex++] = "fns.address.addressNo";

            tempData = dtb.Rows[0]["T89Enterprise_Soi"];
            fns.address.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = fns.address.addressAlley; //12
            //arn[intAIndex++] = "fns.address.addressAlley";

            tempData = dtb.Rows[0]["T89Enterprise_Road"];
            fns.address.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = fns.address.addressRoad; //13
            //arn[intAIndex++] = "fns.address.addressRoad";

            tempData = dtb.Rows[0]["T89Enterprise_Distinct_ID"];
            string strTempEnterpriseDistinctID = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            fns.address.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strTempEnterpriseDistinctID); // Add
                                                                                                                       //ar[intAIndex] = fns.address.subDistrictId + " (int)"; //14
                                                                                                                       //arn[intAIndex++] = "fns.address.subDistrictId";

            tempData = dtb.Rows[0]["T89Enterprise_Ampure_ID"];
            string strTempEnterpriseAmpureID = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            fns.address.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strTempEnterpriseAmpureID); // Add
                                                                                                          //ar[intAIndex] = fns.address.districtId + " (int)"; //15
                                                                                                          //arn[intAIndex++] = "fns.address.districtId";

            tempData = dtb.Rows[0]["T89Enterprise_Province"];
            string strTempEnterpriseProvince = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            fns.address.provinceId = GetProvinceId(strTempEnterpriseProvince); // Add
                                                                               //ar[intAIndex] = fns.address.provinceId + " (int)"; //16
                                                                               //arn[intAIndex++] = "fns.address.provinceId";

            tempData = dtb.Rows[0]["T89Enterprise_Zip_Code"];
            fns.address.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = fns.address.postalCode; //17
            //arn[intAIndex++] = "fns.address.postalCode";

            //tempData = dtb.Rows[0]["T01Industry_Name"]; // this is free text, don't use
            //string strTempIndustryName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //fns.tcgBusinessId = GetTcgBusinessId(strTempIndustryName); // Add
            //ar[intAIndex179] = fns.tcgBusinessId + " (int)";
            //arn[intAIndex++179] = "fns.tcgBusinessId";

            tempData = dtb.Rows[0]["T01Industry_Name"]; // this is free text, put it in remark
            string strTempIndustryName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //reqNo6.remark = strTempIndustryName; // Add
            //ar[intAIndex179] = reqNo6.remark;
            //arn[intAIndex++179] = "reqNo6.remark";
            fns.operation = strTempIndustryName; // Add
                                                 //ar[intAIndex] = fns.operation; //179
                                                 //arn[intAIndex++] = "fns.operation";

            tempData = dtb.Rows[0]["T01ISIC_Code"]; // use this instead
            string strTempISICCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            fns.tcgBusinessId = GetTcgBusinessId(strTempISICCode);
            //ar[intAIndex] = fns.tcgBusinessId + " (int)"; //226
            //arn[intAIndex++] = "fns.tcgBusinessId";

            //tempData = dtb.Rows[0]["T01Industry_Name"]; // use this instead *****
            //string strTempTCGBusinessName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            // use ISICCode to find TCGBusinessName
            if (strTempISICCode != "" && strTempISICCode != null)
            {
                fns.tcgBusinessName = GetTCGBusinessNameFromISICCode(strTempISICCode);
                //ar[intAIndex] = fns.tcgBusinessName; //291
            }
            //arn[intAIndex++] = "fns.tcgBusinessName";

            tempData = dtb.Rows[0]["T01Staff_Amount"];
            string strTempStaffAmount = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            fns.employeeAmount = StringTools.ToInt32(strTempStaffAmount);
            //ar[intAIndex] = fns.employeeAmount + " (int)"; //180
            //arn[intAIndex++] = "fns.employeeAmount";

            tempData = dtb.Rows[0]["T01Staff_Amount_Inc"];
            string stremployeeAdd = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            fns.employeeAdd = StringTools.ToInt32(stremployeeAdd);
            //ar[intAIndex] = fns.employeeAdd; //181
            //arn[intAIndex++] = "fns.employeeAdd";

            // Not so sure
            // There are 3 types of asset; money, building and machine

            // v2. not use
            Asset aMoney = new Asset();
            tempData = dtb.Rows[0]["T01Asset_Money"];
            string strTempAssetMoney = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //aMoney.amt = strTempAssetMoney;
            ////ar[intAIndex182] = aMoney.amt;
            ////arn[intAIndex++182] = "aMoney.amt";
            ////ar[intAIndex] = "-"; //182
            ////arn[intAIndex++] = "-";

            Asset aBuilding = new Asset();
            tempData = dtb.Rows[0]["T01Asset_Money_Building"];
            string strTempAssetMoneyBuilding = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            // if building is 0 check all asset and add to building (Khun Aon told)
            if (strTempAssetMoneyBuilding.Equals("0"))
            {
                Double dblTempAssetMoneyBuilding = Convert.ToDouble(strTempAssetMoneyBuilding);
                Double dblTempAssetMoney = Convert.ToDouble(strTempAssetMoney);
                dblTempAssetMoneyBuilding += dblTempAssetMoney;
                strTempAssetMoneyBuilding = Math.Floor(dblTempAssetMoneyBuilding).ToString();
            }
            aBuilding.amt = strTempAssetMoneyBuilding;
            if (aBuilding.amt != "0")
            {
                aBuilding.fixedAssetType = "02";
            }
            //ar[intAIndex] = aBuilding.amt; //273
            //arn[intAIndex++] = "aBuilding.amt";

            Asset aMachine = new Asset(); ;
            tempData = dtb.Rows[0]["T01Asset_Money_Machine"];
            string strTempAssetMoneyMachine = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            aMachine.amt = strTempAssetMoneyMachine;
            //if (aMachine.amt != "0") {
            aMachine.fixedAssetType = "01";
            //}
            //ar[intAIndex] = aMachine.amt; //274
            //arn[intAIndex++] = "aMachine.amt";

            List<Asset> lstTempFnsAsset = new List<Asset>();
            //lstTempFnsAsset.Add(aMoney);
            lstTempFnsAsset.Add(aMachine);
            lstTempFnsAsset.Add(aBuilding);
            fns.asset = lstTempFnsAsset.ToArray();
            //fns.asset = new List<Asset>();
            //fns.asset.Add(aMoney);
            //fns.asset.Add(aBuilding);
            //fns.asset.Add(aMachine);

            tempData = dtb.Rows[0]["T01ISIC_Code"];
            string strTempT01IsicCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            fns.isicId = GetIsicId(strTempT01IsicCode); // Add
                                                        //ar[intAIndex] = fns.isicId; //226
                                                        //arn[intAIndex++] = "fns.isicId";

            tempData = dtb.Rows[0]["T01DSCR"];
            fns.dscr = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = fns.dscr; //276
            //arn[intAIndex++] = "fns.dscr";

            // Fix null with [] ***** Need to be reviewed *****
            List<Finance> lstTempFnsFinance = new List<Finance>();

            //fn2110224I.busFinId = 2210224;
            fn2110224I.amtType = "I";

            tempData = dtb.Rows[0]["T01_Year_Later"];
            fn2110224I.yearPast1 = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();

            tempData = dtb.Rows[0]["T01_Year_Now"];
            fn2110224I.yearCurrent = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();

            tempData = dtb.Rows[0]["T01_1Year_Next"];
            fn2110224I.yearEstimate1 = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();

            lstTempFnsFinance.Add(fn2110224I);

            fns.finance = lstTempFnsFinance.ToArray();
            // Fix null with [] ***** Need to be reviewed *****

            // Not so sure
            tempData = dtb.Rows[0]["T01Experience_Direct"];
            Manager m = new Manager();

            m.exp = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString(); // Should it be integer???
                                                                                          //ar[intAIndex] = m.exp; //277
                                                                                          //arn[intAIndex++] = "m.exp";

            List<Manager> lstTempFnsManager = new List<Manager>();
            lstTempFnsManager.Add(m);
            fns.manager = lstTempFnsManager.ToArray();
            //fns.manager = new List<Manager>();
            //fns.manager.Add(m);

            tempData = dtb.Rows[0]["T01Start_Date_Business"];
            fns.startDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = fns.startDate; //278
            //arn[intAIndex++] = "fns.startDate";

            // Not so sure
            Customer cMain = new Customer();
            tempData = dtb.Rows[0]["T01Title_Name_Thai"];
            string strTitleNameThaiCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cMain.titleId = GetTitleId(strTitleNameThaiCode); // Add
                                                              //ar[intAIndex] = cMain.titleId + " (int)"; //18
                                                              //arn[intAIndex++] = "cMain.titleId";

            tempData = dtb.Rows[0]["T01Name_Thai"];
            cMain.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cMain.cusNameTh; //19
            //arn[intAIndex++] = "cMain.cusNameTh";
            _T01Name_Thai = cMain.cusNameTh; // used in log

            tempData = dtb.Rows[0]["T01Surname_Thai"];
            cMain.cusSurnameTh = ((tempData == null) || (tempData == "") || (tempData == "-")) ? "" : tempData.ToString();
            //ar[intAIndex] = cMain.cusSurnameTh; //20
            //arn[intAIndex++] = "cMain.cusSurnameTh";
            _T01Surname_Thai = cMain.cusSurnameTh; // used in log

            // Get gender
            cMain.gender = this.GetGender(strTitleNameThaiCode);
            //ar[intAIndex] = cMain.gender; //279
            //arn[intAIndex++] = "cMain.gender";

            tempData = dtb.Rows[0]["T01Marital_Status"];
            string strT01MaritalStatus = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cMain.marriedStatus = GetMaritalStatus(strT01MaritalStatus); // Add
                                                                         //ar[intAIndex] = cMain.marriedStatus; //21
                                                                         //arn[intAIndex++] = "cMain.marriedStatus";

            tempData = dtb.Rows[0]["T01Card_Type"];
            string strT01CardType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cMain.identificationType = GetIdentificationType(strT01CardType); // Add
                                                                              //ar[intAIndex] = cMain.identificationType; //23
                                                                              //arn[intAIndex++] = "cMain.identificationType";

            tempData = dtb.Rows[0]["T01Card_ID1"];
            cMain.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cMain.identification; //24
            //arn[intAIndex++] = "cMain.identification";

            // ID of organization is started with 0 *****
            tempData = dtb.Rows[0]["T01Customer_Type"];
            string strT01CustomerType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //cMain.customerType = GetCustomerType(strT01CustomerType); // Add
            // cMain.customerType = this.GetCustomerType(strTitleNameThaiCode, cMain.cusSurnameTh);
            cMain.customerType = this.GetCustomerTypeFromID(cMain.identification);
            cMain.borrowerType = "01"; // Sole Borrower
                                       //ar[intAIndex] = cMain.customerType; //22
                                       //arn[intAIndex++] = "cMain.customerType";

            tempData = dtb.Rows[0]["T01Experience_Direct"];
            cMain.businessExp = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString(); // Should it be integer???
                                                                                                      //ar[intAIndex] = cMain.businessExp; //288
                                                                                                      //arn[intAIndex++] = "cMain.businessExp";

            Relation r01 = new Relation();
            cMain.relation = new Relation[] { r01 };

            Address aMain = new Address();
            aMain.addressType = "C";

            tempData = dtb.Rows[0]["T01House_Num"];
            aMain.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aMain.addressNo; //25
            //arn[intAIndex++] = "aMain.addressNo";

            tempData = dtb.Rows[0]["T01House_Soi"];
            aMain.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aMain.addressAlley; //26
            //arn[intAIndex++] = "aMain.addressAlley";

            tempData = dtb.Rows[0]["T01House_Road"];
            aMain.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aMain.addressRoad; //27
            //arn[intAIndex++] = "aMain.addressRoad";

            tempData = dtb.Rows[0]["T01House_Distinct_ID"];
            string strT01HouseDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aMain.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT01HouseDistinctId); // ??? from function or not
                                                                                                           //ar[intAIndex] = aMain.subDistrictId + " (int)"; //28
                                                                                                           //arn[intAIndex++] = "aMain.subDistrictId";

            tempData = dtb.Rows[0]["T01House_Ampure_ID"];
            string strT01HouseAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aMain.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT01HouseAmpureId); // ???
                                                                                              //ar[intAIndex] = aMain.districtId + " (int)"; //29
                                                                                              //arn[intAIndex++] = "aMain.districtId";

            tempData = dtb.Rows[0]["T01House_Province"];
            string strT01HouseProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aMain.provinceId = GetProvinceId(strT01HouseProvince); // ???
                                                                   //ar[intAIndex] = aMain.provinceId + " (int)"; //30
                                                                   //arn[intAIndex++] = "aMain.provinceId";
            _T01House_Province = strT01HouseProvince;

            tempData = dtb.Rows[0]["T01House_Zip_Code"];
            aMain.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aMain.postalCode; //31
            //arn[intAIndex++] = "aMain.postalCode";

            //========================================== DUT EDIT =================================================
            Address aDocMain = new Address();
            aDocMain.addressType = "M";

            //Address No
            tempData = dtb.Rows[0]["addressNo"];
            aDocMain.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();

            //Moo + FIX SUBSTRING LEN > 3
            tempData = "";//dtb.Rows[0]["addressMoo"];
            aDocMain.addressMoo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();

            //Alley
            tempData = dtb.Rows[0]["T01Contract_Soi"];
            aDocMain.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();

            //Road
            tempData = dtb.Rows[0]["T01Contract_Road"];
            aDocMain.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();

            //Province
            tempData = dtb.Rows[0]["T01Contract_Province"];
            string strProvinceCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocMain.provinceId = findProvince(strProvinceCode);
            //aDocMain.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT01CensusDistinct); 

            //District
            tempData = dtb.Rows[0]["T01Contract_Ampure"];
            string strDistrictName = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocMain.districtId = findDistrict(strProvinceCode, strDistrictName);
            //aDocMain.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT01CensusAmpure); 

            //Subdistrict
            tempData = dtb.Rows[0]["T01Contract_Distinct"];
            string strSubdistrictName = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocMain.subDistrictId = findSubDistrict(strProvinceCode, strDistrictName, strSubdistrictName);
            //aDocMain.provinceId = GetProvinceId(strT01CensusProvince); 




            tempData = dtb.Rows[0]["T01Contract_Zip_Code"];
            aDocMain.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();



            //==================================== END DUT EDIT ======================================================

            List<Address> lstTempCMainAddress = new List<Address>();
            lstTempCMainAddress.Add(aMain);
            lstTempCMainAddress.Add(aDocMain);
            cMain.address = lstTempCMainAddress.ToArray();
            //cMain.address = new List<Address>();
            //cMain.address.Add(aMain);
            //cMain.address.Add(aDocMain);

            tempData = dtb.Rows[0]["T01House_Phone"];
            cMain.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cMain.telephoneNo; //32
            //arn[intAIndex++] = "cMain.telephoneNo";

            tempData = dtb.Rows[0]["T01House_Fax"];
            cMain.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cMain.faxNo; //33
            //arn[intAIndex++] = "cMain.faxNo";

            tempData = dtb.Rows[0]["T01House_Mobile"];
            cMain.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cMain.mobilePhoneNo; //34
            //arn[intAIndex++] = "cMain.mobilePhoneNo";

            /*
            tempData = dtb.Rows[0]["T01House_Email"];
            cMain.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = cMain.email; //35
            //arn[intAIndex++] = "cMain.email";
            */
            tempData = dtb.Rows[0]["T01House_Email"];
            cMain.email = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cMain.email; //35
            //arn[intAIndex++] = "cMain.email";


            tempData = dtb.Rows[0]["T01Birth_Date"];
            cMain.birthDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cMain.birthDate; //268
            //arn[intAIndex++] = "cMain.birthDate";

            tempData = dtb.Rows[0]["T01Birth_Date"];
            cMain.registerDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cMain.registerDate; //268
            //arn[intAIndex++] = "cMain.registerDate";

            tempData = dtb.Rows[0]["T01BOT_Account_Classify"]; // การจัดชั้นลูกหนี้ตามเกณฑ์ ธปท :
            int intTempBOTAccountClassify = 0;
            Int32.TryParse(Convert.ToString(tempData), out intTempBOTAccountClassify);
            cMain.depLevelId = intTempBOTAccountClassify;

            tempData = dtb.Rows[0]["T01BOTAccountClassify_Date"]; // วัน/เดือน/ปี ที่ตรวจสอบ
            cMain.proveDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());

            Spouse sMain = new Spouse();
            tempData = dtb.Rows[0]["T01Title_Name_Thai2"];
            sMain.titleId = GetTitleId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                              //ar[intAIndex] = sMain.titleId + " (int)"; //36
                                                                                                              //arn[intAIndex++] = "sMain.titleId";

            tempData = dtb.Rows[0]["T01Name_Thai2"];
            sMain.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sMain.cusNameTh; //37
            //arn[intAIndex++] = "sMain.cusNameTh";

            tempData = dtb.Rows[0]["T01Surname_Thai2"];
            sMain.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sMain.cusSurnameTh; //38
            //arn[intAIndex++] = "sMain.cusSurnameTh";

            tempData = dtb.Rows[0]["T01Card_ID2"];
            sMain.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sMain.identification; //39
            //arn[intAIndex++] = "sMain.identification";

            Address asMain = new Address();
            tempData = dtb.Rows[0]["T01House_Num2"];
            asMain.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asMain.addressNo; //40
            //arn[intAIndex++] = "asMain.addressNo";

            tempData = dtb.Rows[0]["T01House_Soi2"];
            asMain.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asMain.addressAlley; //41
            //arn[intAIndex++] = "asMain.addressAlley";

            tempData = dtb.Rows[0]["T01House_Road2"];
            asMain.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asMain.addressRoad; //42
            //arn[intAIndex++] = "asMain.addressRoad";

            tempData = dtb.Rows[0]["T01House_Distinct2"]; // don't have data whether it is id or not ... have to check *****
            asMain.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ??? from function or not
                                                                                                                                                           //ar[intAIndex] = asMain.subDistrictId.ToString() + " (int)"; //43
                                                                                                                                                           //arn[intAIndex++] = "asMain.subDistrictId";

            tempData = dtb.Rows[0]["T01House_Ampure2"]; // don't have data whether it is id or not ... have to check *****
            asMain.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                                                //ar[intAIndex] = asMain.districtId.ToString() + " (int)"; //44
                                                                                                                                                //arn[intAIndex++] = "asMain.districtId";

            tempData = dtb.Rows[0]["T01House_Province2"];
            asMain.provinceId = GetProvinceId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                     //ar[intAIndex] = asMain.provinceId + " (int)"; //45
                                                                                                                     //arn[intAIndex++] = "asMain.provinceId";

            tempData = dtb.Rows[0]["T01House_Zip_Code2"];
            asMain.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asMain.postalCode; //46
            //arn[intAIndex++] = "asMain.postalCode";

            List<Address> lstTempSMainAddress = new List<Address>();
            lstTempCMainAddress.Add(asMain);
            sMain.address = lstTempCMainAddress.ToArray();
            //sMain.address = new List<Address>();
            //sMain.address.Add(asMain);
            tempData = dtb.Rows[0]["T01House_Phone2"];
            sMain.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sMain.telephoneNo; //47
            //arn[intAIndex++] = "sMain.telephoneNo";

            tempData = dtb.Rows[0]["T01House_Fax2"];
            sMain.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sMain.faxNo; //48
            //arn[intAIndex++] = "sMain.faxNo";

            tempData = dtb.Rows[0]["T01House_Mobile2"];
            sMain.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sMain.mobilePhoneNo; //49
            //arn[intAIndex++] = "sMain.mobilePhoneNo";

            tempData = dtb.Rows[0]["T01House_Email2"];
            sMain.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = sMain.email; //50
            //arn[intAIndex++] = "sMain.email";
            cMain.spouse = sMain;

            //======

            // Not so sure
            Customer cCo1 = new Customer();
            tempData = dtb.Rows[0]["T02Title_Name_Thai"];
            strTitleNameThaiCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            cCo1.titleId = GetTitleId(strTitleNameThaiCode); // Add
                                                             //ar[intAIndex] = cCo1.titleId + " (int)"; //51
                                                             //arn[intAIndex++] = "cCo1.titleId";

            tempData = dtb.Rows[0]["T02Name_Thai"];
            cCo1.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo1.cusNameTh; //52
            //arn[intAIndex++] = "cCo1.cusNameTh"; //52

            tempData = dtb.Rows[0]["T02Surname_Thai"];
            cCo1.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo1.cusSurnameTh; //53
            //arn[intAIndex++] = "cCo1.cusSurnameTh";

            cCo1.customerType = this.GetCustomerType(strTitleNameThaiCode, cCo1.cusSurnameTh);
            //ar[intAIndex] = cCo1.customerType; //280
            //arn[intAIndex++] = "cCo1.customerType";

            cCo1.gender = this.GetGender(strTitleNameThaiCode);
            //ar[intAIndex] = cCo1.gender; //281
            //arn[intAIndex++] = "cCo1.gender";

            tempData = dtb.Rows[0]["T02Marital_Status"];
            string strT02MaritalStatus = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo1.marriedStatus = GetMaritalStatus(strT02MaritalStatus); // Add
                                                                        //ar[intAIndex] = cCo1.marriedStatus; //54
                                                                        //arn[intAIndex++] = "cCo1.marriedStatus";

            tempData = dtb.Rows[0]["T02Card_Type"];
            string strT02CardType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo1.identificationType = GetIdentificationType(strT02CardType); // Add
                                                                             //ar[intAIndex] = cCo1.identificationType; //55
                                                                             //arn[intAIndex++] = "cCo1.identificationType";

            tempData = dtb.Rows[0]["T02Card_ID1"];
            cCo1.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo1.identification; //56
            //arn[intAIndex++] = "cCo1.identification";

            // Add Manager 

            if (cMain.customerType == "02")
            { // in case of individual, let manager be cMain
                m.titleId = cMain.titleId;
                m.name = cMain.cusNameTh;
                m.surname = cMain.cusSurnameTh;
                m.idCard = cMain.identification;
                m.identificationType = cMain.identificationType;
                m.exp = cMain.businessExp;
            }

            if (cMain.customerType == "01")
            { // in case of organization, let add manager
                if (cCo1.cusNameTh != "" && cCo1.cusNameTh != null)
                {
                    m.titleId = cCo1.titleId;
                    m.name = cCo1.cusNameTh;
                    m.surname = cCo1.cusSurnameTh;
                    m.idCard = cCo1.identification;
                    m.identificationType = cCo1.identificationType;
                    m.exp = cCo1.businessExp;
                }
                else
                {
                    m.titleId = 0;
                    m.name = "-";
                    m.surname = "-";
                    m.idCard = "0000000000000";
                    m.identificationType = "C";
                    m.exp = "0";
                }
            }

            Address aCo1 = new Address();
            aCo1.addressType = "C";

            tempData = dtb.Rows[0]["T02House_Num"];
            aCo1.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo1.addressNo; //57
            //arn[intAIndex++] = "aCo1.addressNo";

            tempData = dtb.Rows[0]["T02House_Soi"];
            aCo1.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo1.addressAlley; //58
            //arn[intAIndex++] = "aCo1.addressAlley";

            tempData = dtb.Rows[0]["T02House_Road"];
            aCo1.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo1.addressRoad; //59
            //arn[intAIndex++] = "aCo1.addressRoad";

            tempData = dtb.Rows[0]["T02House_Distinct_ID"];
            string strT02HouseDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo1.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT02HouseDistinctId); // ??? from function or not
                                                                                                          //ar[intAIndex] = aCo1.subDistrictId + " (int)"; //60
                                                                                                          //arn[intAIndex++] = "aCo1.subDistrictId";

            tempData = dtb.Rows[0]["T02House_Ampure_ID"];
            string strT02HouseAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo1.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT02HouseAmpureId); // ???
                                                                                             //ar[intAIndex] = aCo1.districtId + " (int)"; //61
                                                                                             //arn[intAIndex++] = "aCo1.districtId";

            tempData = dtb.Rows[0]["T02House_Province"];
            string strT02HouseProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo1.provinceId = GetProvinceId(strT02HouseProvince); // ???
                                                                  //ar[intAIndex] = aCo1.provinceId + " (int)"; //62
                                                                  //arn[intAIndex++] = "aCo1.provinceId";

            tempData = dtb.Rows[0]["T02House_Zip_Code"];
            aCo1.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo1.postalCode; //63
            //arn[intAIndex++] = "aCo1.postalCode";

            Address aDocCo1 = new Address();
            aDocCo1.addressType = "M";

            tempData = dtb.Rows[0]["T02Census_Num"];
            aDocCo1.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo1.addressNo; //240
            //arn[intAIndex++] = "aDocCo1.addressNo";

            tempData = dtb.Rows[0]["T02Census_Soi"];
            aDocCo1.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo1.addressAlley; //241
            //arn[intAIndex++] = "aDocCo1.addressAlley";

            tempData = dtb.Rows[0]["T02Census_Road"];
            aDocCo1.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo1.addressRoad; //242
            //arn[intAIndex++] = "aDocCo1.addressRoad";

            tempData = dtb.Rows[0]["T02Census_Distinct"]; // census already used ID
            string strT02CensusDistinct = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo1.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT02CensusDistinct); // ??? from function or not
                                                                                                            //ar[intAIndex] = aDocCo1.subDistrictId + " (int)"; //243
                                                                                                            //arn[intAIndex++] = "aDocCo1.subDistrictId";

            tempData = dtb.Rows[0]["T02Census_Ampure"]; // census already used ID
            string strT02CensusAmpure = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo1.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT02CensusAmpure); // ???
                                                                                               //ar[intAIndex] = aDocCo1.districtId + " (int)"; //244
                                                                                               //arn[intAIndex++] = "aDocCo1.districtId";

            tempData = dtb.Rows[0]["T02Census_Province"];
            string strT02CensusProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo1.provinceId = GetProvinceId(strT02CensusProvince); // ???
                                                                      //ar[intAIndex] = aDocCo1.provinceId + " (int)"; //245
                                                                      //arn[intAIndex++] = "aDocCo1.provinceId";

            tempData = dtb.Rows[0]["T02Census_Zip_Code"];
            aDocCo1.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo1.postalCode; //246
            //arn[intAIndex++] = "aDocCo1.postalCode";

            List<Address> lstTempCCo1Address = new List<Address>();
            lstTempCCo1Address.Add(aCo1);
            lstTempCCo1Address.Add(aDocCo1);
            cCo1.address = lstTempCCo1Address.ToArray();
            //cCo1.address = new List<Address>();
            //cCo1.address.Add(aCo1);
            //cCo1.address.Add(aDocCo1);

            tempData = dtb.Rows[0]["T02House_Phone"];
            cCo1.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo1.telephoneNo; //64
            //arn[intAIndex++] = "cCo1.telephoneNo";

            tempData = dtb.Rows[0]["T02House_Fax"];
            cCo1.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo1.faxNo; //65
            //arn[intAIndex++] = "cCo1.faxNo";

            tempData = dtb.Rows[0]["T02House_Mobile"];
            cCo1.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo1.mobilePhoneNo; //66
            //arn[intAIndex++] = "cCo1.mobilePhoneNo";

            tempData = dtb.Rows[0]["T02House_Email"];
            cCo1.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = cCo1.email; //67
            //arn[intAIndex++] = "cCo1.email";

            tempData = dtb.Rows[0]["T02Birth_Date"];
            cCo1.birthDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo1.birthDate; //269
            //arn[intAIndex++] = "cCo1.birthDate";

            tempData = dtb.Rows[0]["T02Birth_Date"];
            cCo1.registerDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo1.registerDate; //269
            //arn[intAIndex++] = "cCo1.registerDate";

            Spouse sCo1 = new Spouse();
            tempData = dtb.Rows[0]["T02Title_Name_Thai2"];
            sCo1.titleId = GetTitleId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                             //ar[intAIndex] = sCo1.titleId + " (int)"; //68
                                                                                                             //arn[intAIndex++] = "sCo1.titleId";

            tempData = dtb.Rows[0]["T02Name_Thai2"];
            sCo1.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo1.cusNameTh; //69
            //arn[intAIndex++] = "sCo1.cusNameTh";

            tempData = dtb.Rows[0]["T02Surname_Thai2"];
            sCo1.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo1.cusSurnameTh; //70
            //arn[intAIndex++] = "sCo1.cusSurnameTh";

            tempData = dtb.Rows[0]["T02Card_ID2"];
            sCo1.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo1.identification; //71
            //arn[intAIndex++] = "sCo1.identification";

            Address asCo1 = new Address();
            tempData = dtb.Rows[0]["T02House_Num2"];
            asCo1.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo1.addressNo; //72
            //arn[intAIndex++] = "asCo1.addressNo";

            tempData = dtb.Rows[0]["T02House_Soi2"];
            asCo1.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo1.addressAlley; //73
            //arn[intAIndex++] = "asCo1.addressAlley";

            tempData = dtb.Rows[0]["T02House_Road2"];
            asCo1.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo1.addressRoad; //74
            //arn[intAIndex++] = "asCo1.addressRoad";

            tempData = dtb.Rows[0]["T02House_Distinct2"]; // don't have data whether it is id or not ... have to check *****
            asCo1.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ??? from function or not
                                                                                                                                                          //ar[intAIndex] = asCo1.subDistrictId + " (int)"; //75
                                                                                                                                                          //arn[intAIndex++] = "asCo1.subDistrictId";

            tempData = dtb.Rows[0]["T02House_Ampure2"]; // don't have data whether it is id or not ... have to check *****
            asCo1.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                                               //ar[intAIndex] = asCo1.districtId + " (int)"; //76
                                                                                                                                               //arn[intAIndex++] = "asCo1.districtId";

            tempData = dtb.Rows[0]["T02House_Province2"];
            asCo1.provinceId = GetProvinceId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                    //ar[intAIndex] = asCo1.provinceId + " (int)"; //77
                                                                                                                    //arn[intAIndex++] = "asCo1.provinceId";

            tempData = dtb.Rows[0]["T02House_Zip_Code2"];
            asCo1.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo1.postalCode; //78
            //arn[intAIndex++] = "asCo1.postalCode";

            List<Address> lstTempSCo1Address = new List<Address>();
            lstTempCCo1Address.Add(asCo1);
            sCo1.address = lstTempCCo1Address.ToArray();
            //sCo1.address = new List<Address>();
            //sCo1.address.Add(asCo1);

            tempData = dtb.Rows[0]["T02House_Phone2"];
            sCo1.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo1.telephoneNo; //79
            //arn[intAIndex++] = "sCo1.telephoneNo";

            tempData = dtb.Rows[0]["T02House_Fax2"];
            sCo1.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo1.faxNo; //80
            //arn[intAIndex++] = "sCo1.faxNo";

            tempData = dtb.Rows[0]["T02House_Mobile2"];
            sCo1.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo1.mobilePhoneNo; //81
            //arn[intAIndex++] = "sCo1.mobilePhoneNo";

            tempData = dtb.Rows[0]["T02House_Email2"];
            sCo1.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = sCo1.email; //82
            //arn[intAIndex++] = "sCo1.email";
            cCo1.spouse = sCo1;

            //=====

            // Not so sure
            Customer cCo2 = new Customer();
            tempData = dtb.Rows[0]["T03Title_Name_Thai"];
            strTitleNameThaiCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            cCo2.titleId = GetTitleId(strTitleNameThaiCode); // Add
                                                             //ar[intAIndex] = cCo2.titleId + " (int)"; //83
                                                             //arn[intAIndex++] = "cCo2.titleId";

            tempData = dtb.Rows[0]["T03Name_Thai"];
            cCo2.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo2.cusNameTh; //84
            //arn[intAIndex++] = "cCo2.cusNameTh";

            tempData = dtb.Rows[0]["T03Surname_Thai"];
            cCo2.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo2.cusSurnameTh; //85
            //arn[intAIndex++] = "cCo2.cusSurnameTh";

            cCo2.customerType = this.GetCustomerType(strTitleNameThaiCode, cCo2.cusSurnameTh);
            //ar[intAIndex] = cCo2.customerType; //282
            //arn[intAIndex++] = "cCo2.customerType";

            cCo2.gender = this.GetGender(strTitleNameThaiCode);
            //ar[intAIndex] = cCo2.gender; //283
            //arn[intAIndex++] = "cCo2.gender";

            tempData = dtb.Rows[0]["T03Marital_Status"];
            string strT03MaritalStatus = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo2.marriedStatus = GetMaritalStatus(strT03MaritalStatus); // Add
                                                                        //ar[intAIndex] = cCo2.marriedStatus; //86
                                                                        //arn[intAIndex++] = "cCo2.marriedStatus";

            tempData = dtb.Rows[0]["T03Card_Type"];
            string strT03CardType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo2.identificationType = GetIdentificationType(strT03CardType); // Add
                                                                             //ar[intAIndex] = cCo2.identificationType; //87
                                                                             //arn[intAIndex++] = "cCo2.identificationType";

            tempData = dtb.Rows[0]["T03Card_ID1"];
            cCo2.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo2.identification; //88
            //arn[intAIndex++] = "cCo2.identification";

            Address aCo2 = new Address();
            aCo2.addressType = "C";

            tempData = dtb.Rows[0]["T03House_Num"];
            aCo2.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo2.addressNo; //89
            //arn[intAIndex++] = "aCo2.addressNo";

            tempData = dtb.Rows[0]["T03House_Soi"];
            aCo2.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo2.addressAlley; //90
            //arn[intAIndex++] = "aCo2.addressAlley";

            tempData = dtb.Rows[0]["T03House_Road"];
            aCo2.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo2.addressRoad; //91
            //arn[intAIndex++] = "aCo2.addressRoad";

            tempData = dtb.Rows[0]["T03House_Distinct_ID"];
            string strT03HouseDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo2.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT03HouseDistinctId); // ??? from function or not
                                                                                                          //ar[intAIndex] = aCo2.subDistrictId + " (int)"; //92
                                                                                                          //arn[intAIndex++] = "aCo2.subDistrictId";

            tempData = dtb.Rows[0]["T03House_Ampure_ID"];
            string strT03HouseAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo2.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT03HouseAmpureId); // ???
                                                                                             //ar[intAIndex] = aCo2.districtId + " (int)"; //93
                                                                                             //arn[intAIndex++] = "aCo2.districtId";

            tempData = dtb.Rows[0]["T03House_Province"];
            string strT03HouseProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo2.provinceId = GetProvinceId(strT03HouseProvince); // ???
                                                                  //ar[intAIndex] = aCo2.provinceId + " (int)"; //94
                                                                  //arn[intAIndex++] = "aCo2.provinceId";

            tempData = dtb.Rows[0]["T03House_Zip_Code"];
            aCo2.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo2.postalCode; //95
            //arn[intAIndex++] = "aCo2.postalCode";

            Address aDocCo2 = new Address();
            aDocCo2.addressType = "M";

            tempData = dtb.Rows[0]["T03Census_Num"];
            aDocCo2.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo2.addressNo; //247
            //arn[intAIndex++] = "aDocCo2.addressNo";

            tempData = dtb.Rows[0]["T03Census_Soi"];
            aDocCo2.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo2.addressAlley; //248
            //arn[intAIndex++] = "aDocCo2.addressAlley";

            tempData = dtb.Rows[0]["T03Census_Road"];

            aDocCo2.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo2.addressRoad; //249
            //arn[intAIndex++] = "aDocCo2.addressRoad";

            tempData = dtb.Rows[0]["T03Census_Distinct"]; // census already used ID
            string strT03CensusDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo2.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT03CensusDistinctId); // ??? from function or not
                                                                                                              //ar[intAIndex] = aDocCo2.subDistrictId + " (int)"; //250
                                                                                                              //arn[intAIndex++] = "aDocCo2.subDistrictId";

            tempData = dtb.Rows[0]["T03Census_Ampure"]; // census already used ID
            string strT03CensusAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo2.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT03CensusAmpureId); // ???
                                                                                                 //ar[intAIndex] = aDocCo2.districtId + " (int)"; //251
                                                                                                 //arn[intAIndex++] = "aDocCo2.districtId";

            tempData = dtb.Rows[0]["T03Census_Province"];
            string strT03CensusProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo2.provinceId = GetProvinceId(strT03CensusProvince); // ???
                                                                      //ar[intAIndex] = aDocCo2.provinceId + " (int)"; //252
                                                                      //arn[intAIndex++] = "aDocCo2.provinceId";

            tempData = dtb.Rows[0]["T03Census_Zip_Code"];
            aDocCo2.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo2.postalCode; //253
            //arn[intAIndex++] = "aDocCo2.postalCode";

            List<Address> lstTempCCo2Address = new List<Address>();
            lstTempCCo2Address.Add(aCo2);
            lstTempCCo2Address.Add(aDocCo2);
            cCo2.address = lstTempCCo2Address.ToArray();
            //cCo2.address = new List<Address>();
            //cCo2.address.Add(aCo2);
            //cCo2.address.Add(aDocCo2);

            tempData = dtb.Rows[0]["T03House_Phone"];
            cCo2.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo2.telephoneNo; //96
            //arn[intAIndex++] = "cCo2.telephoneNo";

            tempData = dtb.Rows[0]["T03House_Fax"];
            cCo2.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo2.faxNo; //97
            //arn[intAIndex++] = "cCo2.faxNo";

            tempData = dtb.Rows[0]["T03House_Mobile"];
            cCo2.mobilePhoneNo = ((tempData == null) || (tempData == "0")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo2.mobilePhoneNo; //98
            //arn[intAIndex++] = "cCo2.mobilePhoneNo";

            tempData = dtb.Rows[0]["T03House_Email"];
            cCo2.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = cCo2.email; //99
            //arn[intAIndex++] = "cCo2.email";

            tempData = dtb.Rows[0]["T03Birth_Date"];
            cCo2.birthDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo2.birthDate; //270
            //arn[intAIndex++] = "cCo2.birthDate";

            tempData = dtb.Rows[0]["T03Birth_Date"];
            cCo2.registerDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo2.registerDate; //270
            //arn[intAIndex++] = "cCo2.registerDate";

            Spouse sCo2 = new Spouse();
            tempData = dtb.Rows[0]["T03Title_Name_Thai2"];
            sCo2.titleId = GetTitleId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                             //ar[intAIndex] = sCo2.titleId + " (int)"; //100
                                                                                                             //arn[intAIndex++] = "sCo2.titleId";

            tempData = dtb.Rows[0]["T03Name_Thai2"];
            sCo2.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo2.cusNameTh; //101
            //arn[intAIndex++] = "sCo2.cusNameTh";

            tempData = dtb.Rows[0]["T03Surname_Thai2"];
            sCo2.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo2.cusSurnameTh; //102
            //arn[intAIndex++] = "sCo2.cusSurnameTh";

            tempData = dtb.Rows[0]["T03Card_ID2"];
            sCo2.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo2.identification; //103
            //arn[intAIndex++] = "sCo2.identification";

            Address asCo2 = new Address();
            tempData = dtb.Rows[0]["T03House_Num2"];
            asCo2.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo2.addressNo; //104
            //arn[intAIndex++] = "asCo2.addressNo";

            tempData = dtb.Rows[0]["T03House_Soi2"];
            asCo2.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo2.addressAlley; //105
            //arn[intAIndex++] = "asCo2.addressAlley";

            tempData = dtb.Rows[0]["T03House_Road2"];
            asCo2.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo2.addressRoad; //106
            //arn[intAIndex++] = "asCo2.addressRoad";

            tempData = dtb.Rows[0]["T03House_Distinct2"]; // don't have data whether it is id or not ... have to check *****
            asCo2.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ??? from function  or not
                                                                                                                                                          //ar[intAIndex] = asCo2.subDistrictId + " (int)"; //107
                                                                                                                                                          //arn[intAIndex++] = "asCo2.subDistrictId";

            tempData = dtb.Rows[0]["T03House_Ampure2"]; // don't have data whether it is id or not ... have to check *****
            asCo2.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                                               //ar[intAIndex] = asCo2.districtId + " (int)"; //108
                                                                                                                                               //arn[intAIndex++] = "asCo2.districtId";

            tempData = dtb.Rows[0]["T03House_Province2"];
            asCo2.provinceId = GetProvinceId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                    //ar[intAIndex] = asCo2.provinceId + " (int)"; //109
                                                                                                                    //arn[intAIndex++] = "asCo2.provinceId";

            tempData = dtb.Rows[0]["T03House_Zip_Code2"];
            asCo2.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo2.postalCode; //110
            //arn[intAIndex++] = "asCo2.postalCode";

            List<Address> lstTempSCo2Address = new List<Address>();
            lstTempSCo2Address.Add(asCo2);
            sCo2.address = lstTempSCo2Address.ToArray();
            //sCo2.address = new List<Address>();
            //sCo2.address.Add(asCo2);

            tempData = dtb.Rows[0]["T03House_Phone2"];
            sCo2.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo2.telephoneNo; //111
            //arn[intAIndex++] = "sCo2.telephoneNo";

            tempData = dtb.Rows[0]["T03House_Fax2"];
            sCo2.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo2.faxNo; //112
            //arn[intAIndex++] = "sCo2.faxNo";

            tempData = dtb.Rows[0]["T03House_Mobile2"];
            sCo2.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo2.mobilePhoneNo; //113
            //arn[intAIndex++] = "sCo2.mobilePhoneNo";

            tempData = dtb.Rows[0]["T03House_Email2"];
            sCo2.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = sCo2.email; //114
            //arn[intAIndex++] = "sCo2.email";
            cCo2.spouse = sCo2;

            //=====

            // Not so sure
            Customer cCo3 = new Customer();
            tempData = dtb.Rows[0]["T04Title_Name_Thai"];
            strTitleNameThaiCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            cCo3.titleId = GetTitleId(strTitleNameThaiCode); // Add
                                                             //ar[intAIndex] = cCo3.titleId + " (int)"; //115
                                                             //arn[intAIndex++] = "cCo3.titleId";

            tempData = dtb.Rows[0]["T04Name_Thai"];
            cCo3.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo3.cusNameTh; //116
            //arn[intAIndex++] = "cCo3.cusNameTh";

            tempData = dtb.Rows[0]["T04Surname_Thai"];
            cCo3.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo3.cusSurnameTh; //117
            //arn[intAIndex++] = "cCo3.cusSurnameTh";

            cCo3.customerType = this.GetCustomerType(strTitleNameThaiCode, cCo3.cusSurnameTh);
            //ar[intAIndex] = cCo3.customerType; //284
            //arn[intAIndex++] = "cCo3.customerType";

            cCo3.gender = this.GetGender(strTitleNameThaiCode);
            //ar[intAIndex] = cCo3.gender; //285
            //arn[intAIndex++] = "cCo3.gender";

            tempData = dtb.Rows[0]["T04Marital_Status"];
            string strT04MaritalStatus = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo3.marriedStatus = GetMaritalStatus(strT04MaritalStatus); // Add
                                                                        //ar[intAIndex] = cCo3.marriedStatus; //118
                                                                        //arn[intAIndex++] = "cCo3.marriedStatus";

            tempData = dtb.Rows[0]["T04Card_Type"];
            string strT04CardType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo3.identificationType = GetIdentificationType(strT04CardType); // Add
                                                                             //ar[intAIndex] = cCo3.identificationType; //119
                                                                             //arn[intAIndex++] = "cCo3.identificationType";

            tempData = dtb.Rows[0]["T04Card_ID1"];
            cCo3.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo3.identification; //120
            //arn[intAIndex++] = "cCo3.identification";

            Address aCo3 = new Address();
            aCo3.addressType = "C";

            tempData = dtb.Rows[0]["T04House_Num"];
            aCo3.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo3.addressNo; //121
            //arn[intAIndex++] = "aCo3.addressNo";

            tempData = dtb.Rows[0]["T04House_Soi"];
            aCo3.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo3.addressAlley; //122
            //arn[intAIndex++] = "aCo3.addressAlley";

            tempData = dtb.Rows[0]["T04House_Road"];
            aCo3.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo3.addressRoad; //123
            //arn[intAIndex++] = "aCo3.addressRoad";

            tempData = dtb.Rows[0]["T04House_Distinct_ID"];
            string strT04HouseDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo3.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT04HouseDistinctId); // ??? from function or not
                                                                                                          //ar[intAIndex] = aCo3.subDistrictId + " (int)"; //124
                                                                                                          //arn[intAIndex++] = "aCo3.subDistrictId";

            tempData = dtb.Rows[0]["T04House_Ampure_ID"];
            string strT04HouseAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo3.districtId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT04HouseAmpureId); // ???
                                                                                                     //ar[intAIndex] = aCo3.districtId + " (int)"; //125
                                                                                                     //arn[intAIndex++] = "aCo3.districtId";

            tempData = dtb.Rows[0]["T04House_Province"];
            string strT04HouseProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo3.provinceId = GetProvinceId(strT04HouseProvince); // ???
                                                                  //ar[intAIndex] = aCo3.provinceId + " (int)"; //126
                                                                  //arn[intAIndex++] = "aCo3.provinceId";

            tempData = dtb.Rows[0]["T04House_Zip_Code"];
            aCo3.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo3.postalCode; //127
            //arn[intAIndex++] = "aCo3.postalCode";

            Address aDocCo3 = new Address();
            aDocCo3.addressType = "M";

            tempData = dtb.Rows[0]["T04Census_Num"];
            aDocCo3.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo3.addressNo; //254
            //arn[intAIndex++] = "aDocCo3.addressNo";

            tempData = dtb.Rows[0]["T04Census_Soi"];
            aDocCo3.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo3.addressAlley; //255
            //arn[intAIndex++] = "aDocCo3.addressAlley";

            tempData = dtb.Rows[0]["T04Census_Road"];
            aDocCo3.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo3.addressRoad; //256
            //arn[intAIndex++] = "aDocCo3.addressRoad";

            tempData = dtb.Rows[0]["T04Census_Distinct"]; // census already used ID
            string strT04CensusDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo3.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT04CensusDistinctId); // ??? from function or not
                                                                                                              //ar[intAIndex] = aDocCo3.subDistrictId + " (int)"; //257
                                                                                                              //arn[intAIndex++] = "aDocCo3.subDistrictId";

            tempData = dtb.Rows[0]["T04Census_Ampure"]; // census already used ID
            string strT04CensusAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo3.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT04CensusAmpureId); // ???
                                                                                                 //ar[intAIndex] = aDocCo3.districtId + " (int)"; //258
                                                                                                 //arn[intAIndex++] = "aDocCo3.districtId";

            tempData = dtb.Rows[0]["T04Census_Province"];
            string strT04CensusProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo3.provinceId = GetProvinceId(strT04CensusProvince); // ???
                                                                      //ar[intAIndex] = aDocCo3.provinceId + " (int)"; //259
                                                                      //arn[intAIndex++] = "aDocCo3.provinceId";

            tempData = dtb.Rows[0]["T04Census_Zip_Code"];
            aDocCo3.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo3.postalCode; //260
            //arn[intAIndex++] = "aDocCo3.postalCode";

            List<Address> lstTempCCo3Address = new List<Address>();
            lstTempCCo3Address.Add(aCo3);
            lstTempCCo3Address.Add(aDocCo3);
            cCo3.address = lstTempCCo3Address.ToArray();
            //cCo3.address = new List<Address>();
            //cCo3.address.Add(aCo3);
            //cCo3.address.Add(aDocCo3);

            tempData = dtb.Rows[0]["T04House_Phone"];
            cCo3.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo3.telephoneNo; //128
            //arn[intAIndex++] = "cCo3.telephoneNo";

            tempData = dtb.Rows[0]["T04House_Fax"];
            cCo3.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo3.faxNo; //129
            //arn[intAIndex++] = "cCo3.faxNo";

            tempData = dtb.Rows[0]["T04House_Mobile"];
            cCo3.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo3.mobilePhoneNo; //130
            //arn[intAIndex++] = "cCo3.mobilePhoneNo";

            tempData = dtb.Rows[0]["T04House_Email"];
            cCo3.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = cCo3.email; //131
            //arn[intAIndex++] = "cCo3.email";

            tempData = dtb.Rows[0]["T04Birth_Date"];
            cCo3.birthDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo3.birthDate; //271
            //arn[intAIndex++] = "cCo3.birthDate";

            tempData = dtb.Rows[0]["T04Birth_Date"];
            cCo3.registerDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo3.registerDate; //271
            //arn[intAIndex++] = "cCo3.registerDate";

            Spouse sCo3 = new Spouse();
            tempData = dtb.Rows[0]["T04Title_Name_Thai2"];
            sCo3.titleId = GetTitleId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                             //ar[intAIndex] = sCo3.titleId + " (int)"; //132
                                                                                                             //arn[intAIndex++] = "sCo3.titleId";

            tempData = dtb.Rows[0]["T04Name_Thai2"];
            sCo3.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo3.cusNameTh; //133
            //arn[intAIndex++] = "sCo3.cusNameTh";

            tempData = dtb.Rows[0]["T04Surname_Thai2"];
            sCo3.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo3.cusSurnameTh; //134
            //arn[intAIndex++] = "sCo3.cusSurnameTh";

            tempData = dtb.Rows[0]["T04Card_ID2"];
            sCo3.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo3.identification; //135
            //arn[intAIndex++] = "sCo3.identification";

            Address asCo3 = new Address();
            tempData = dtb.Rows[0]["T04House_Num2"];
            asCo3.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo3.addressNo; //136
            //arn[intAIndex++] = "asCo3.addressNo";

            tempData = dtb.Rows[0]["T04House_Soi2"];
            asCo3.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo3.addressAlley; //137
            //arn[intAIndex++] = "asCo3.addressAlley";

            tempData = dtb.Rows[0]["T04House_Road2"];
            asCo3.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo3.addressRoad; //138
            //arn[intAIndex++] = "asCo3.addressRoad";

            tempData = dtb.Rows[0]["T04House_Distinct2"]; // don't have data whether it is id or not ... have to check *****
            asCo3.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ??? from function or not
                                                                                                                                                          //ar[intAIndex] = asCo3.subDistrictId + " (int)"; //139
                                                                                                                                                          //arn[intAIndex++] = "asCo3.subDistrictId";

            tempData = dtb.Rows[0]["T04House_Ampure2"]; // don't have data whether it is id or not ... have to check *****
            asCo3.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                                               //ar[intAIndex] = asCo3.districtId + " (int)"; //140
                                                                                                                                               //arn[intAIndex++] = "asCo3.districtId";

            tempData = dtb.Rows[0]["T04House_Province2"];
            asCo3.provinceId = GetProvinceId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                    //ar[intAIndex] = asCo3.provinceId + " (int)"; //141
                                                                                                                    //arn[intAIndex++] = "asCo3.provinceId";

            tempData = dtb.Rows[0]["T04House_Zip_Code2"];
            asCo3.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo3.postalCode; //142
            //arn[intAIndex++] = "asCo3.postalCode";

            List<Address> lstTempSCo3Address = new List<Address>();
            lstTempSCo3Address.Add(aCo1);
            lstTempSCo3Address.Add(aDocCo1);
            sCo3.address = lstTempSCo3Address.ToArray();
            //sCo3.address = new List<Address>();
            //sCo3.address.Add(asCo3);

            tempData = dtb.Rows[0]["T04House_Phone2"];
            sCo3.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo3.telephoneNo; //143
            //arn[intAIndex++] = "sCo3.telephoneNo";

            tempData = dtb.Rows[0]["T04House_Fax2"];
            sCo3.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo3.faxNo; //144
            //arn[intAIndex++] = "sCo3.faxNo";

            tempData = dtb.Rows[0]["T04House_Mobile2"];
            sCo3.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo3.mobilePhoneNo; //145
            //arn[intAIndex++] = "sCo3.mobilePhoneNo";

            tempData = dtb.Rows[0]["T04House_Email2"];
            sCo3.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = sCo3.email; //146
            //arn[intAIndex++] = "sCo3.email";
            cCo3.spouse = sCo3;

            //=====

            // Not so sure
            Customer cCo4 = new Customer();
            tempData = dtb.Rows[0]["T05Title_Name_Thai"];
            strTitleNameThaiCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            cCo4.titleId = GetTitleId(strTitleNameThaiCode); // Add
                                                             //ar[intAIndex] = cCo4.titleId + " (int)"; //147
                                                             //arn[intAIndex++] = "cCo4.titleId";

            tempData = dtb.Rows[0]["T05Name_Thai"];
            cCo4.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo4.cusNameTh; //148
            //arn[intAIndex++] = "cCo4.cusNameTh";

            tempData = dtb.Rows[0]["T05Surname_Thai"];
            cCo4.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo4.cusSurnameTh; //149
            //arn[intAIndex++] = "cCo4.cusSurnameTh";

            cCo4.customerType = this.GetCustomerType(strTitleNameThaiCode, cCo4.cusSurnameTh);
            //ar[intAIndex] = cCo4.customerType; //286
            //arn[intAIndex++] = "cCo4.customerType";

            cCo4.gender = this.GetGender(strTitleNameThaiCode);
            //ar[intAIndex] = cCo4.gender; //287
            //arn[intAIndex++] = "cCo4.gender";

            tempData = dtb.Rows[0]["T05Marital_Status"];
            string strT05MaritalStatus = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo4.marriedStatus = GetMaritalStatus(strT05MaritalStatus); // Add
                                                                        //ar[intAIndex] = cCo4.marriedStatus; //150
                                                                        //arn[intAIndex++] = "cCo4.marriedStatus";

            tempData = dtb.Rows[0]["T05Card_Type"];
            string strT05CardType = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            cCo4.identificationType = GetIdentificationType(strT05CardType); // Add
                                                                             //ar[intAIndex] = cCo4.identificationType; //151
                                                                             //arn[intAIndex++] = "cCo4.identificationType";

            tempData = dtb.Rows[0]["T05Card_ID1"];
            cCo4.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cCo4.identification; //152
            //arn[intAIndex++] = "cCo4.identification";

            Address aCo4 = new Address();
            aCo4.addressType = "C";

            tempData = dtb.Rows[0]["T05House_Num"];
            aCo4.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo4.addressNo; //153
            //arn[intAIndex++] = "aCo4.addressNo";

            tempData = dtb.Rows[0]["T05House_Soi"];
            aCo4.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo4.addressAlley; //154
            //arn[intAIndex++] = "aCo4.addressAlley";

            tempData = dtb.Rows[0]["T05House_Road"];
            aCo4.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo4.addressRoad; //155
            //arn[intAIndex++] = "aCo4.addressRoad";

            tempData = dtb.Rows[0]["T05House_Distinct_ID"];
            string strT05HouseDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo4.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT05HouseDistinctId); // ??? from function or not
                                                                                                          //ar[intAIndex] = aCo4.subDistrictId + " (int)"; //156
                                                                                                          //arn[intAIndex++] = "aCo4.subDistrictId";

            tempData = dtb.Rows[0]["T05House_Ampure_ID"];
            string strT05HouseAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo4.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT05HouseAmpureId); // ???
                                                                                             //ar[intAIndex] = aCo4.districtId + " (int)"; //157
                                                                                             //arn[intAIndex++] = "aCo4.districtId";

            tempData = dtb.Rows[0]["T05House_Province"];
            string strT05HouseProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aCo4.provinceId = GetProvinceId(strT05HouseProvince); // ???
                                                                  //ar[intAIndex] = aCo4.provinceId + " (int)"; //158
                                                                  //arn[intAIndex++] = "aCo4.provinceId";

            tempData = dtb.Rows[0]["T05House_Zip_Code"];
            aCo4.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aCo4.postalCode; //159
            //arn[intAIndex++] = "aCo4.postalCode";

            Address aDocCo4 = new Address();
            aDocCo4.addressType = "M";

            tempData = dtb.Rows[0]["T05Census_Num"];
            aDocCo4.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo4.addressNo; //261
            //arn[intAIndex++] = "aDocCo4.addressNo";

            tempData = dtb.Rows[0]["T05Census_Soi"];
            aDocCo4.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo4.addressAlley; //262
            //arn[intAIndex++] = "aDocCo4.addressAlley";

            tempData = dtb.Rows[0]["T05Census_Road"];
            aDocCo4.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo4.addressRoad; //263
            //arn[intAIndex++] = "aDocCo4.addressRoad";

            tempData = dtb.Rows[0]["T05Census_Distinct"]; // census already used ID
            string strT05CensusDistinctId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo4.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(strT05CensusDistinctId); // ??? from function or not
                                                                                                              //ar[intAIndex] = aDocCo4.subDistrictId + " (int)"; //264
                                                                                                              //arn[intAIndex++] = "aDocCo4.subDistrictId";

            tempData = dtb.Rows[0]["T05Census_Ampure"]; // census already used ID
            string strT05CensusAmpureId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo4.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(strT05CensusAmpureId); // ???
                                                                                                 //ar[intAIndex] = aDocCo4.districtId + " (int)"; //265
                                                                                                 //arn[intAIndex++] = "aDocCo4.districtId";

            tempData = dtb.Rows[0]["T05Census_Province"];
            string strT05CensusProvince = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            aDocCo4.provinceId = GetProvinceId(strT05CensusProvince); // ???
                                                                      //ar[intAIndex] = aDocCo4.provinceId + " (int)"; //266
                                                                      //arn[intAIndex++] = "aDocCo4.provinceId";

            tempData = dtb.Rows[0]["T05Census_Zip_Code"];
            aDocCo4.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = aDocCo4.postalCode; //267
            //arn[intAIndex++] = "aDocCo4.postalCode";

            List<Address> lstTempCCo4Address = new List<Address>();
            lstTempCCo4Address.Add(aCo4);
            lstTempCCo4Address.Add(aDocCo4);
            cCo4.address = lstTempCCo4Address.ToArray();
            //cCo4.address = new List<Address>();
            //cCo4.address.Add(aCo4);
            //cCo4.address.Add(aDocCo4);

            tempData = dtb.Rows[0]["T05House_Phone"];
            cCo4.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo4.telephoneNo; //160
            //arn[intAIndex++] = "cCo4.telephoneNo";

            tempData = dtb.Rows[0]["T05House_Fax"];
            cCo4.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo4.faxNo; //161
            //arn[intAIndex++] = "cCo4.faxNo";

            tempData = dtb.Rows[0]["T05House_Mobile"];
            cCo4.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = cCo4.mobilePhoneNo; //162
            //arn[intAIndex++] = "cCo4.mobilePhoneNo";

            tempData = dtb.Rows[0]["T05House_Email"];
            cCo4.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = cCo4.email; //163
            //arn[intAIndex++] = "cCo4.email";

            tempData = dtb.Rows[0]["T05Birth_Date"];
            cCo4.birthDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo4.birthDate; //272
            //arn[intAIndex++] = "cCo4.birthDate";

            tempData = dtb.Rows[0]["T05Birth_Date"];
            cCo4.registerDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDateTime(tempData.ToString());
            //ar[intAIndex] = cCo4.registerDate; //272
            //arn[intAIndex++] = "cCo4.registerDate";

            Spouse sCo4 = new Spouse();
            tempData = dtb.Rows[0]["T05Title_Name_Thai2"];
            sCo4.titleId = GetTitleId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                             //ar[intAIndex] = sCo4.titleId + " (int)"; //164
                                                                                                             //arn[intAIndex++] = "sCo4.titleId";

            tempData = dtb.Rows[0]["T05Name_Thai2"];
            sCo4.cusNameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo4.cusNameTh; //165
            //arn[intAIndex++] = "sCo4.cusNameTh";

            tempData = dtb.Rows[0]["T05Surname_Thai2"];
            sCo4.cusSurnameTh = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo4.cusSurnameTh; //166
            //arn[intAIndex++] = "sCo4.cusSurnameTh";

            tempData = dtb.Rows[0]["T05Card_ID2"];
            sCo4.identification = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = sCo4.identification; //167
            //arn[intAIndex++] = "sCo4.identification";

            Address asCo4 = new Address();
            tempData = dtb.Rows[0]["T05House_Num2"];
            asCo4.addressNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo4.addressNo; //168
            //arn[intAIndex++] = "asCo4.addressNo";

            tempData = dtb.Rows[0]["T05House_Soi2"];
            asCo4.addressAlley = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo4.addressAlley; //169
            //arn[intAIndex++] = "asCo4.addressAlley";

            tempData = dtb.Rows[0]["T05House_Road2"];
            asCo4.addressRoad = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo4.addressRoad; //170
            //arn[intAIndex++] = "asCo4.addressRoad";

            tempData = dtb.Rows[0]["T05House_Distinct2"]; // don't have data whether it is id or not ... have to check *****
            asCo4.subDistrictId = GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ??? from function or not
                                                                                                                                                          //ar[intAIndex] = asCo4.subDistrictId + " (int)"; //171
                                                                                                                                                          //arn[intAIndex++] = "asCo4.subDistrictId";

            tempData = dtb.Rows[0]["T05House_Ampure2"]; // don't have data whether it is id or not ... have to check *****
            asCo4.districtId = GetDistrictIdFromDistrictCodeTCGAmpureId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                                               //ar[intAIndex] = asCo4.districtId + " (int)"; //172
                                                                                                                                               //arn[intAIndex++] = "asCo4.districtId";

            tempData = dtb.Rows[0]["T05House_Province2"];
            asCo4.provinceId = GetProvinceId(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // ???
                                                                                                                    //ar[intAIndex] = asCo4.provinceId + " (int)"; //173
                                                                                                                    //arn[intAIndex++] = "asCo4.provinceId";

            tempData = dtb.Rows[0]["T05House_Zip_Code2"];
            asCo4.postalCode = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = asCo4.postalCode; //174
            //arn[intAIndex++] = "asCo4.postalCode";

            List<Address> lstTempSCo4Address = new List<Address>();
            lstTempSCo4Address.Add(aCo1);
            sCo4.address = lstTempSCo4Address.ToArray();
            //sCo4.address = new List<Address>();
            //sCo4.address.Add(asCo4);

            tempData = dtb.Rows[0]["T05House_Phone2"];
            sCo4.telephoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo4.telephoneNo; //175
            //arn[intAIndex++] = "sCo4.telephoneNo";

            tempData = dtb.Rows[0]["T05House_Fax2"];
            sCo4.faxNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo4.faxNo; //176
            //arn[intAIndex++] = "sCo4.faxNo";

            tempData = dtb.Rows[0]["T05House_Mobile2"];
            sCo4.mobilePhoneNo = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            //ar[intAIndex] = sCo4.mobilePhoneNo; //177
            //arn[intAIndex++] = "sCo4.mobilePhoneNo";

            tempData = dtb.Rows[0]["T05House_Email2"];
            sCo4.email = ((tempData == null) || (tempData == "")) ? "noone@tcg.or.th" : tempData.ToString();
            //ar[intAIndex] = sCo4.email; //178
            //arn[intAIndex++] = "sCo4.email";
            cCo4.spouse = sCo4;

            tempData = dtb.Rows[0]["T01Education"];
            cMain.educationLevel = GetEducationLevel(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                                           //ar[intAIndex] = cMain.educationLevel; //227
                                                                                                                           //arn[intAIndex++] = "cMain.educationLevel";

            tempData = dtb.Rows[0]["T02Education"];
            cCo1.educationLevel = GetEducationLevel(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                                          //ar[intAIndex] = cCo1.educationLevel; //228
                                                                                                                          //arn[intAIndex++] = "cCo1.educationLevel";

            tempData = dtb.Rows[0]["T03Education"];
            cCo2.educationLevel = GetEducationLevel(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                                          //ar[intAIndex] = cCo2.educationLevel; //229
                                                                                                                          //arn[intAIndex++] = "cCo2.educationLevel";

            tempData = dtb.Rows[0]["T04Education"];
            cCo3.educationLevel = GetEducationLevel(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                                          //ar[intAIndex] = cCo3.educationLevel; //230
                                                                                                                          //arn[intAIndex++] = "cCo3.educationLevel";

            tempData = dtb.Rows[0]["T05Education"];
            cCo4.educationLevel = GetEducationLevel(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                                          //ar[intAIndex] = cCo4.educationLevel; //231
                                                                                                                          //arn[intAIndex++] = "cCo4.educationLevel";

            // Fix null with [] ***** Need to be reviewed *****
            List<int> lstCMainCareer = new List<int>();
            lstCMainCareer.Add(777);
            cMain.career = lstCMainCareer.ToArray();
            List<int> lstCCo1Career = new List<int>();
            cCo1.career = lstCCo1Career.ToArray();
            List<int> lstCCo2Career = new List<int>();
            cCo2.career = lstCCo2Career.ToArray();
            List<int> lstCCo3Career = new List<int>();
            cCo3.career = lstCCo3Career.ToArray();
            List<int> lstCCo4Career = new List<int>();
            cCo4.career = lstCCo4Career.ToArray();
            // Fix null with [] ***** Need to be reviewed *****

            // Fix null with [] ***** Need to be reviewed *****
            //List<string> lstCMainRelation = new List<string>();
            //cMain.relation = lstCMainRelation.ToArray();
            //List<string> lstCCo1Relation = new List<string>();
            //cCo1.relation = lstCCo1Relation.ToArray();
            //List<string> lstCCo2Relation = new List<string>();
            //cCo2.relation = lstCCo2Relation.ToArray();
            //List<string> lstCCo3Relation = new List<string>();
            //cCo3.relation = lstCCo3Relation.ToArray();
            //List<string> lstCCo4Relation = new List<string>();
            //cCo4.relation = lstCCo4Relation.ToArray();
            // Fix null with [] ***** Need to be reviewed *****

            // Not so sure
            Contract ct1 = new Contract();

            //ct1.loanContractId = 42; // ***** need to correct

            tempData = dtb.Rows[0]["T01Loan_Subject_1"];
            _T01Loan_Subject_1 = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString();
            ct1.contractName = _T01Loan_Subject_1;
            //ar[intAIndex] = ct1.contractName; //183
            //arn[intAIndex++] = "ct1.contractName";

            tempData = dtb.Rows[0]["T01Loan_Amount_1"];
            ct1.loanLimit = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // ??? Should it be number?
                                                                                                 //ar[intAIndex] = ct1.loanLimit; //184
                                                                                                 //arn[intAIndex++] = "ct1.loanLimit";

            // if not specify, let guaLimit = loanLimit
            tempData = dtb.Rows[0]["T01Request_Amount_1"];
            ct1.guaLimit = ((tempData == null) || (tempData == "")) ? ct1.loanLimit : tempData.ToString(); // ???
                                                                                                           //ar[intAIndex] = ct1.guaLimit; //185
                                                                                                           //arn[intAIndex++] = "ct1.guaLimit";

            tempData = dtb.Rows[0]["T01Loan_Type_1"];
            _T01Loan_Type_1 = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            // ct1.loanTypeName = GetTCGLoanTypeName(_T01Loan_Type_1);
            ct1.loanTypeName = GetLoanTypeNameNew(_T01Loan_Type_1, _BankId);
            //ar[intAIndex] = ct1.loanTypeName; //186
            //arn[intAIndex++] = "ct1.loanTypeName";

            //tempData = dtb.Rows[0]["T01Investment_Objective_1"];
            //ct1.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : this.GetPurposeCode(tempData.ToString()); // Add
            ////ar[intAIndex] = ct1.purposeCode; //210
            ////arn[intAIndex++] = "ct1.purposeCode";

            tempData = dtb.Rows[0]["T01Investment_Objective_1"];
            ct1.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString(); // Add
                                                                                                    //ar[intAIndex] = ct1.purposeCode; //210
                                                                                                    //arn[intAIndex++] = "ct1.purposeCode";

            tempData = dtb.Rows[0]["T01Debt_Year_1"];
            ct1.debtPeriod = GetDebtYear(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                                //ar[intAIndex] = ct1.debtPeriod; //211
                                                                                                                //arn[intAIndex++] = "ct1.debtPeriod";

            tempData = dtb.Rows[0]["T01Debt_Year_1"];
            ct1.debtMonth = GetDebtMonth(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString()); // Add
                                                                                                                //ar[intAIndex] = ct1.debtMonth + " (int)"; //294
                                                                                                                //arn[intAIndex++] = "ct1.debtMonth";

            //tempData = dtb.Rows[0]["T01Debt_Define_1"]; // Temperary *****
            //ct1.debtorTypeId = GetDebtorTypeId(((tempData == null) || (tempData == "")) ? "" : tempData.ToString(), bk.bankId); // Add
            //ar[intAIndex212] = ct1.debtorTypeId + " (int)";
            //arn[intAIndex++212] = "ct1.debtorTypeId";

            // not work - correct below
            //tempData = dtb.Rows[0]["T01Debt_Define_1"]; // Temperary *****
            //ct1.debtorTypeId = GetDebtorTypeIdWithBankInfo(bk.bankId, ((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
            ////ar[intAIndex] = ct1.debtorTypeId + " (int)"; //212
            ////arn[intAIndex++] = "ct1.debtorTypeId";

            tempData = dtb.Rows[0]["T01Debt_Define_1"]; // Temperary *****
            ct1.debtDescription = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString(); // Add
                                                                                                        //ar[intAIndex] = ct1.debtDescription; //212
                                                                                                        //arn[intAIndex++] = "ct1.debtDescription";

            /*
            // Comment Out
            tempData = dtb.Rows[0]["T01ContractNo_1"];
            ct1.contractDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDate(tempData.ToString()); // Add - no more
            ar[intAIndex213] = ct1.contractDate;
            arn[intAIndex++213] = "ct1.contractDate";
            */

            tempData = dtb.Rows[0]["T01ContractNo_1"];
            ct1.contractNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // Add - no more
                                                                                                  //ar[intAIndex] = ct1.contractNo; //213
                                                                                                  //arn[intAIndex++] = "ct1.contractNo";

            // Not so sure
            Contract ct2 = new Contract();

            //ct2.loanContractId = ct1.loanContractId; // ***** need to correct

            tempData = dtb.Rows[0]["T01Loan_Subject_2"];
            ct2.contractName = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString();
            //ar[intAIndex] = ct2.contractName; //187
            //arn[intAIndex++] = "ct2.contractName";

            tempData = dtb.Rows[0]["T01Loan_Amount_2"];
            ct2.loanLimit = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // ??? Should it be number?
                                                                                                 //ar[intAIndex] = ct2.loanLimit; //188
                                                                                                 //arn[intAIndex++] = "ct2.loanLimit";

            // if not specify, let guaLimit = loanLimit
            tempData = dtb.Rows[0]["T01Request_Amount_2"];
            ct2.guaLimit = ((tempData == null) || (tempData == "")) ? ct1.guaLimit : tempData.ToString(); // ???
                                                                                                          //ar[intAIndex] = ct1.guaLimit; //189 แก้
                                                                                                          //arn[intAIndex++] = "ct2.guaLimit";

            tempData = dtb.Rows[0]["T01Loan_Type_2"];
            ct2.loanTypeName = GetTCGLoanTypeName(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString());
            //ar[intAIndex] = ct2.loanTypeName; //190
            //arn[intAIndex++] = "ct2.loanTypeName";

            tempData = dtb.Rows[0]["T01Investment_Objective_2"];
            ct2.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
                                                                                                      //ar[intAIndex] = ct2.purposeCode; //214
                                                                                                      //arn[intAIndex++] = "ct2.purposeCode";

            tempData = dtb.Rows[0]["T01Debt_Year_2"];
            ct2.debtPeriod = GetDebtYear(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct2.debtPeriod; //215
                                                                                                               //arn[intAIndex++] = "ct2.debtPeriod";

            tempData = dtb.Rows[0]["T01Debt_Year_2"];
            ct2.debtMonth = GetDebtMonth(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct2.debtMonth + " (int)"; //215
                                                                                                               //arn[intAIndex++] = "ct2.debtMonth";

            tempData = dtb.Rows[0]["T01Debt_Define_2"];
            ct2.debtDescription = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString(); // Add
                                                                                                        //ar[intAIndex] = ct2.debtDescription; //216
                                                                                                        //arn[intAIndex++] = "ct2.debtDescription";

            /*
            // Comment Out
            tempData = dtb.Rows[0]["T01ContractNo_2"];
            ct2.contractDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDate(tempData.ToString()); // Add - no more
            ar[intAIndex217] = ct2.contractDate;
            arn[intAIndex++217] = "ct2.contractDate";
            */

            tempData = dtb.Rows[0]["T01ContractNo_2"];
            ct2.contractNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // Add - no more
                                                                                                  //ar[intAIndex] = ct2.contractNo; //217
                                                                                                  //arn[intAIndex++] = "ct2.contractNo";

            // Not so sure
            Contract ct3 = new Contract();

            //ct3.loanContractId = ct1.loanContractId; // ***** need to correct

            tempData = dtb.Rows[0]["T01Loan_Subject_3"];
            ct3.contractName = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString();
            //ar[intAIndex] = ct3.contractName; //191
            //arn[intAIndex++] = "ct3.contractName";

            tempData = dtb.Rows[0]["T01Loan_Amount_3"];
            ct3.loanLimit = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // ??? Should it be number?
                                                                                                 //ar[intAIndex] = ct3.loanLimit; //192
                                                                                                 //arn[intAIndex++] = "ct3.loanLimit";

            // if not specify, let guaLimit = loanLimit
            tempData = dtb.Rows[0]["T01Request_Amount_3"];
            ct3.guaLimit = ((tempData == null) || (tempData == "")) ? ct1.guaLimit : tempData.ToString(); // ???
                                                                                                          //ar[intAIndex] = ct3.guaLimit; //193
                                                                                                          //arn[intAIndex++] = "ct3.guaLimit";

            tempData = dtb.Rows[0]["T01Loan_Type_3"];
            ct3.loanTypeName = GetTCGLoanTypeName(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString());
            //ar[intAIndex] = ct3.loanTypeName; //194
            //arn[intAIndex++] = "ct3.loanTypeName";

            tempData = dtb.Rows[0]["T01Investment_Objective_3"];
            ct3.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
                                                                                                      //ar[intAIndex] = ct3.purposeCode; //218
                                                                                                      //arn[intAIndex++] = "ct3.purposeCode";

            tempData = dtb.Rows[0]["T01Debt_Year_3"];
            ct3.debtPeriod = GetDebtYear(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct3.debtPeriod; //219
                                                                                                               //arn[intAIndex++] = "ct3.debtPeriod";

            tempData = dtb.Rows[0]["T01Debt_Year_3"];
            ct3.debtMonth = GetDebtMonth(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct3.debtMonth + " (int)"; //219
                                                                                                               //arn[intAIndex++] = "ct3.debtMonth";

            tempData = dtb.Rows[0]["T01Debt_Define_3"];
            ct3.debtDescription = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString(); // Add
                                                                                                        //ar[intAIndex] = ct3.debtDescription; //220
                                                                                                        //arn[intAIndex++] = "ct3.debtDescription";

            /*
            // Comment Out
            tempData = dtb.Rows[0]["T01ContractNo_3"];
            ct3.contractDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDate(tempData.ToString()); // Add - no more
            ar[intAIndex221] = ct3.contractDate;
            arn[intAIndex++221] = "ct3.contractDate";
            */

            tempData = dtb.Rows[0]["T01ContractNo_3"];
            ct3.contractNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // Add - no more
                                                                                                  //ar[intAIndex] = ct3.contractNo; //221
                                                                                                  //arn[intAIndex++] = "ct3.contractNo";

            // Not so sure
            Contract ct4 = new Contract();

            //ct4.loanContractId = ct1.loanContractId; // ***** need to correct

            tempData = dtb.Rows[0]["T01Loan_Subject_4"];
            ct4.contractName = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString();
            //ar[intAIndex] = ct4.contractName; //195
            //arn[intAIndex++] = "ct4.contractName";

            tempData = dtb.Rows[0]["T01Loan_Amount_4"];
            ct4.loanLimit = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // ??? Should it be number?
                                                                                                 //ar[intAIndex] = ct4.loanLimit; //196
                                                                                                 //arn[intAIndex++] = "ct4.loanLimit";

            tempData = dtb.Rows[0]["T01Request_Amount_4"];
            ct4.guaLimit = ((tempData == null) || (tempData == "")) ? ct1.guaLimit : tempData.ToString(); // ???
                                                                                                          //ar[intAIndex] = ct4.guaLimit; //197
                                                                                                          //arn[intAIndex++] = "ct4.guaLimit";

            tempData = dtb.Rows[0]["T01Loan_Type_4"];
            ct4.loanTypeName = GetTCGLoanTypeName(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString());
            //ar[intAIndex] = ct4.loanTypeName; //198
            //arn[intAIndex++] = "ct4.loanTypeName";

            tempData = dtb.Rows[0]["T01Investment_Objective_4"];
            ct4.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
                                                                                                      //ar[intAIndex] = ct4.purposeCode; //222
                                                                                                      //arn[intAIndex++] = "ct4.purposeCode";

            tempData = dtb.Rows[0]["T01Debt_Year_4"];
            ct4.debtPeriod = GetDebtYear(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct4.debtPeriod; //223
                                                                                                               //arn[intAIndex++] = "ct4.debtPeriod";

            tempData = dtb.Rows[0]["T01Debt_Year_4"];
            ct4.debtMonth = GetDebtMonth(((tempData == null) || (tempData == "")) ? "" : tempData.ToString()); // Add
                                                                                                               //ar[intAIndex] = ct4.debtMonth + " (int)"; //223
                                                                                                               //arn[intAIndex++] = "ct4.debtMonth";

            tempData = dtb.Rows[0]["T01Debt_Define_4"];
            ct4.debtDescription = ((tempData == null) || (tempData == "")) ? "-" : tempData.ToString(); // Add
                                                                                                        //ar[intAIndex] = ct4.debtDescription; //224
                                                                                                        //arn[intAIndex++] = "ct4.debtDescription";

            /*
            // Comment Out ... *****
            tempData = dtb.Rows[0]["T01ContractNo_4"];
            ct4.contractDate = ((tempData == null) || (tempData == "")) ? "" : this.ConvertDate(tempData.ToString()); // Add - no more
            ar[intAIndex225] = ct4.contractDate;
            arn[intAIndex++225] = "ct4.contractDate";
            */

            tempData = dtb.Rows[0]["T01ContractNo_4"];
            ct4.contractNo = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // Add - no more
                                                                                                  //ar[intAIndex] = ct4.contractNo; //225
                                                                                                  //arn[intAIndex++] = "ct4.contractNo";

            List<Contract> lstTempCtContract = new List<Contract>();
            /*
            if (ct1.contractName != "")
            {
                lstTempCtContract.Add(ct1);
            }
            if (ct2.contractName != "")
            {
                lstTempCtContract.Add(ct2);
            }
            if (ct3.contractName != "")
            {
                lstTempCtContract.Add(ct3);
            }
            if (ct4.contractName != "")
            {
                lstTempCtContract.Add(ct4);
            }
            */

            if (StringTools.ToDouble(ct1.guaLimit)>0 || StringTools.ToDouble(ct1.loanLimit) > 0)
            {
                lstTempCtContract.Add(ct1);
            }
            if (StringTools.ToDouble(ct2.guaLimit)> 0 || StringTools.ToDouble(ct2.loanLimit) > 0)
            {
                lstTempCtContract.Add(ct2);
            }
            if (StringTools.ToDouble(ct3.guaLimit)> 0 || StringTools.ToDouble(ct3.loanLimit) > 0)
            {
                lstTempCtContract.Add(ct3);
            }
            if (StringTools.ToDouble(ct4.guaLimit)> 0 || StringTools.ToDouble(ct4.loanLimit) > 0)
            {
                lstTempCtContract.Add(ct4);
            }


            ct.contract = lstTempCtContract.ToArray();
            //ct.contract = new List<Contract>();
            //ct.contract.Add(ct1);
            //ct.contract.Add(ct2);
            //ct.contract.Add(ct3);
            //ct.contract.Add(ct4);

            Credit cr1 = new Credit();
            Credit cr2 = new Credit();
            Credit cr3 = new Credit();
            Credit cr4 = new Credit();
            cr1.guaByTcg = "Y";
            cr2.guaByTcg = "Y";
            cr3.guaByTcg = "Y";
            cr4.guaByTcg = "Y";
            //ar[intAIndex] = cr1.guaByTcg; //292
            //arn[intAIndex++] = "c1.guaByTcg";

            cr1.debtPeriod = ct1.debtPeriod;
            cr2.debtPeriod = ct2.debtPeriod;
            cr3.debtPeriod = ct3.debtPeriod;
            cr4.debtPeriod = ct4.debtPeriod;
            //ar[intAIndex] = cr1.debtMonth.ToString();
            //arn[intAIndex++] = "c1.debtPeriod";

            cr1.debtMonth = ct1.debtMonth;
            cr2.debtMonth = ct2.debtMonth;
            cr3.debtMonth = ct3.debtMonth;
            cr4.debtMonth = ct4.debtMonth;
            //ar[intAIndex] = cr1.debtMonth.ToString();
            //arn[intAIndex++] = "c1.debtMonth";

            //tempData = dtb.Rows[0]["T01Loan_Amount"];
            //c1.loanLimit = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString(); // ??? does not have
            cr1.loanLimit = ct1.loanLimit; // get from contract
            cr2.loanLimit = ct2.loanLimit; // get from contract
            cr3.loanLimit = ct3.loanLimit; // get from contract
            cr4.loanLimit = ct4.loanLimit; // get from contract
                                           //ar[intAIndex] = cr1.loanLimit; //199
                                           //arn[intAIndex++] = "c1.loanLimit";

            // assume it to be like this, may need to change *****
            cr1.guaLimit = ct1.guaLimit; // get from contract
            cr2.guaLimit = ct2.guaLimit; // get from contract
            cr3.guaLimit = ct3.guaLimit; // get from contract
            cr4.guaLimit = ct4.guaLimit; // get from contract
                                         //ar[intAIndex] = cr1.guaLimit; //295
                                         //arn[intAIndex++] = "c1.guaLimit";

            cr1.loanTypeName = ct1.loanTypeName;
            cr2.loanTypeName = ct2.loanTypeName;
            cr3.loanTypeName = ct3.loanTypeName;
            cr4.loanTypeName = ct4.loanTypeName;
            cr1.loanName = ct1.loanTypeName;
            cr2.loanName = ct2.loanTypeName;
            cr3.loanName = ct3.loanTypeName;
            cr4.loanName = ct4.loanTypeName;
            // This is just for interim.  Highly chance to be wrong. *****
            tempData = dtb.Rows[0]["T01Loan_Subject_1"];
            cr1.contractName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            tempData = dtb.Rows[0]["T01Loan_Subject_2"];
            cr2.contractName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            tempData = dtb.Rows[0]["T01Loan_Subject_3"];
            cr3.contractName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            tempData = dtb.Rows[0]["T01Loan_Subject_4"];
            cr4.contractName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //ar[intAIndex] = cr1.contractName; //289
            //arn[intAIndex++] = "c1.contractName";

            // This is just for interim.  Highly chance to be wrong. *****
            tempData = dtb.Rows[0]["T01Investment_Objective_1"];
            cr1.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
            tempData = dtb.Rows[0]["T01Investment_Objective_2"];
            cr2.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
            tempData = dtb.Rows[0]["T01Investment_Objective_3"];
            cr3.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
            tempData = dtb.Rows[0]["T01Investment_Objective_4"];
            cr4.purposeCode = ((tempData == null) || (tempData == "")) ? "0" : (tempData.ToString()); // Add
                                                                                                      //ar[intAIndex] = cr1.purposeCode; //293
                                                                                                      //arn[intAIndex++] = "c1.purposeCode";

            List<Credit> lstTempCrCredit = new List<Credit>();
            //lstTempCrCredit.Add(cr1);
            //lstTempCrCredit.Add(cr2);
            //lstTempCrCredit.Add(cr3);
            //lstTempCrCredit.Add(cr4);

            if (StringTools.ToDouble(cr1.guaLimit) > 0 || StringTools.ToDouble(cr1.loanLimit) > 0)
            {
                lstTempCrCredit.Add(cr1);
            }
            if (StringTools.ToDouble(cr2.guaLimit) > 0 || StringTools.ToDouble(cr2.loanLimit) > 0)
            {
                lstTempCrCredit.Add(cr2);
            }
            if (StringTools.ToDouble(cr3.guaLimit) > 0 || StringTools.ToDouble(cr3.loanLimit) > 0)
            {
                lstTempCrCredit.Add(cr3);
            }
            if (StringTools.ToDouble(cr4.guaLimit) > 0 || StringTools.ToDouble(cr4.loanLimit) > 0)
            {
                lstTempCrCredit.Add(cr4);
            }

            cr.credit = lstTempCrCredit.ToArray();
            //cr.credit = new List<Credit>();
            //cr.credit.Add(c1);

            //tempData = dtb.Rows[0]["T01Investment_Objective_1"]; // temperary use *****
            tempData = dtb.Rows[0]["T01Project_Character"];
            cr.guaLoadPurpose = GetGuaLoadPurpose(((tempData == null) || (tempData == "")) ? "00000" : tempData.ToString());
            //ar[intAIndex] = cr.guaLoadPurpose; //275
            //arn[intAIndex++] = "cr.guaLoadPurpose";

            tempData = dtb.Rows[0]["T01CostEstimate"];
            _T01CostEstimate = Convert.ToSingle(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString());

            tempData = dtb.Rows[0]["T01Total_Asset"];
            _T01Total_Asset = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();

            tempData = dtb.Rows[0]["T01Total_Debt"];
            _T01Total_Debt = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();

            tempData = dtb.Rows[0]["T01Total_Loan_Amount"];
            _T01Total_Loan_Amount = Convert.ToSingle(((tempData == null) || (tempData == "")) ? "0" : tempData.ToString());

            // Fix null with [] ***** Need to be reviewed *****
            List<Credit> lstTempCrOldCredit = new List<Credit>();
            // cr.oldCredit = lstTempCrCredit.ToArray(); // Yoh said not to put in
            cr.oldCredit = new Credit[] { };
            List<string> lstTempCrCol = new List<string>();
            cr.col = lstTempCrCol.ToArray();
            List<Customer> lstTempCrGuarantorContract = new List<Customer>();
            cr.guarantorContract = lstTempCrGuarantorContract.ToArray();
            List<Customer> lstTempCrGuarantorTcg = new List<Customer>();
            cr.guarantorTcg = lstTempCrGuarantorTcg.ToArray();
            // Fix null with [] ***** Need to be reviewed *****

            string strYearFolder = (DateTime.Now.Year + 543).ToString().Substring(2, 2);
            string strBaseFolder = @"\\192.168.10.17\d$\WWW\tcgcyber\Online\documents\" + strYearFolder + @"\" + strTempOnlineID; // ***** Add: Specify Base Folder *****

            //int intTotalFile = 9; // assume that there are 9 attached files. *****
            //List<ListFile> lstTempFlListFile = new List<ListFile>();

            //fl.listFile = new List<ListFile>(); // contain ListFile(s)
            /*
            ListFile f1 = new ListFile();
            tempData = dtb.Rows[0]["T01File_1"];
            f1.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[201] = f1.fileName;
            arn[201] = "f1.fileName";
            if (f1.fileName != "") {
                //f1.fileBase64 = this.GetBase24FromFile(strBaseFolder, f1.fileName);
                lstTempFlListFile.Add(f1);
                //fl.listFile.Add(f1);
            }

            ListFile f2 = new ListFile();
            tempData = dtb.Rows[0]["T01File_2"];
            f2.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[202] = f2.fileName;
            arn[202] = "f2.fileName";
            if (f2.fileName != "") {
                //f2.fileBase64 = this.GetBase24FromFile(strBaseFolder, f2.fileName);
                lstTempFlListFile.Add(f2);
                //fl.listFile.Add(f2);
            }

            ListFile f3 = new ListFile();
            tempData = dtb.Rows[0]["T01File_3"];
            f3.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[203] = f3.fileName;
            arn[203] = "f3.fileName";
            if (f3.fileName != "") {
                //f3.fileBase64 = this.GetBase24FromFile(strBaseFolder, f3.fileName);
                lstTempFlListFile.Add(f3);
                //fl.listFile.Add(f3);
            }

            ListFile f4 = new ListFile();
            tempData = dtb.Rows[0]["T01File_4"];
            f4.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[204] = f4.fileName;
            arn[204] = "f4.fileName";
            if (f4.fileName != "") {
                //f4.fileBase64 = this.GetBase24FromFile(strBaseFolder, f4.fileName);
                lstTempFlListFile.Add(f4);
                //fl.listFile.Add(f4);
            }

            ListFile f5 = new ListFile();
            tempData = dtb.Rows[0]["T01File_5"];
            f5.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[205] = f5.fileName;
            arn[205] = "f5.fileName";
            if (f5.fileName != "") {
                //f5.fileBase64 = this.GetBase24FromFile(strBaseFolder, f5.fileName);
                lstTempFlListFile.Add(f5);
                //fl.listFile.Add(f5);
            }

            ListFile f6 = new ListFile();
            tempData = dtb.Rows[0]["T01File_6"];
            f6.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[206] = f6.fileName;
            arn[206] = "f6.fileName";
            if (f6.fileName != "") {
                //f6.fileBase64 = this.GetBase24FromFile(strBaseFolder, f6.fileName);
                lstTempFlListFile.Add(f6);
                //fl.listFile.Add(f6);
            }

            ListFile f7 = new ListFile();
            tempData = dtb.Rows[0]["T01File_7"];
            f7.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[207] = f7.fileName;
            arn[207] = "f7.fileName";
            if (f7.fileName != "") {
                //f7.fileBase64 = this.GetBase24FromFile(strBaseFolder, f7.fileName);
                lstTempFlListFile.Add(f7);
                //fl.listFile.Add(f7);
            }

            ListFile f8 = new ListFile();
            tempData = dtb.Rows[0]["T01File_8"];
            f8.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[208] = f8.fileName;
            arn[208] = "f8.fileName";
            if (f8.fileName != "") {
                //f8.fileBase64 = this.GetBase24FromFile(strBaseFolder, f8.fileName);
                lstTempFlListFile.Add(f8);
                //fl.listFile.Add(f8);
            }

            ListFile f9 = new ListFile();
            tempData = dtb.Rows[0]["T01File_9"];
            f9.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            ar[209] = f9.fileName;
            arn[209] = "f9.fileName";
            if (f9.fileName != "") {
                //f9.fileBase64 = this.GetBase24FromFile(strBaseFolder, f9.fileName);
                lstTempFlListFile.Add(f9);
                //fl.listFile.Add(f9);
            }
            */

            // int[] intFileTypeId = this.GetFileListFromProductId(pd.productId.ToString());
            int[] intFileTypeId = this.GetFileListFromProductId(_productId);
            if (intFileTypeId != null)
            {
                fl = new Files[intFileTypeId.Length]; // # of files is depended on productId
                                                      // assume that there are 9 attached files. (specified from max of TCGCyber) *****
                int intTotalFile = 9;
                List<ListFile> lstTempFlListFile = null;
                for (int i = 0; i < intFileTypeId.Length; i++)
                {
                    lstTempFlListFile = null;
                    lstTempFlListFile = new List<ListFile>();
                    fl[i] = new Files();
                    fl[i].fileTypeId = intFileTypeId[i];
                    //ar[intAIndex] = fl[i].fileTypeId + " (int)";
                    ar[intAIndex++] = "fl.fileTypeId";
                    ListFile fT = new ListFile();
                    if (i < intTotalFile)
                    {
                        string strFileField = "T01File_" + (i + 1).ToString();
                        tempData = dtb.Rows[0][strFileField];
                        //fT.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
                        string tempfilename = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
                        fT.fileName = tempfilename;
                        ar[intAIndex + i] = fT.fileName;
                        arn[intAIndex + i] = string.Format("f{0}.fileName", (i + 1).ToString());
                        ++intAIndex;
                        if (fT.fileName != "")
                        {
                            if (File.Exists(strBaseFolder + "\\" + fT.fileName))
                            {
                                fT.fileBase64 = this.GetBase24FromFile(strBaseFolder, fT.fileName);
                                lstTempFlListFile.Add(fT);
                            }
                            else
                            {
                                fT.fileName = ""; //clear file if file does not exist
                            }
                        }
                    }
                    fl[i].listFile = lstTempFlListFile.ToArray();
                }

            }
            // int intTotalFile = 9; // assume that there are 9 attached files. *****
            // List<ListFile> lstTempFlListFile = new List<ListFile>();
            //for (int i = 0; i < intTotalFile; i++) {
            //    ListFile fT = new ListFile();
            //    string strFileField = "T01File_" + (i + 1).ToString();
            //    tempData = dtb.Rows[0][strFileField];
            //    fT.fileName = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
            //    ar[intAIndex + i] = fT.fileName;
            //    arn[intAIndex++ + i] = string.Format("f{0}.fileName", (i + 1).ToString());
            //    if (fT.fileName != "") {
            //        //fT.fileBase64 = this.GetBase24FromFile(strBaseFolder, fT.fileName);
            //        lstTempFlListFile.Add(fT);
            //    }
            //}
            //fl.listFile = lstTempFlListFile.ToArray();

            reqNo6 = new RequestNo6();

            reqNo6.product = pd;
            reqNo6.bank = bk;

            List<Customer> lstTempReqNo6Customer = new List<Customer>();
            lstTempReqNo6Customer.Add(cMain);
            if (((cCo1.cusNameTh != "") && (cCo1.cusNameTh != null)))
                lstTempReqNo6Customer.Add(cCo1);
            if (((cCo2.cusNameTh != "") && (cCo2.cusNameTh != null)))
                lstTempReqNo6Customer.Add(cCo2);
            if (((cCo3.cusNameTh != "") && (cCo3.cusNameTh != null)))
                lstTempReqNo6Customer.Add(cCo3);
            if (((cCo4.cusNameTh != "") && (cCo4.cusNameTh != null)))
                lstTempReqNo6Customer.Add(cCo4);

            reqNo6.customer = lstTempReqNo6Customer.ToArray();
            //reqNo6.customer = new List<Customer>();
            //reqNo6.customer.Add(cMain);
            //reqNo6.customer.Add(cCo1);
            //reqNo6.customer.Add(cCo2);
            //reqNo6.customer.Add(cCo3);
            //reqNo6.customer.Add(cCo4);
            reqNo6.finance = fns;
            reqNo6.credit = cr;

            reqNo6.contract = ct;

            /* used by old ... no more
            List<Files> lstTempReqNo6File = new List<Files>();
            lstTempReqNo6File.Add(fl);
            reqNo6.file = lstTempReqNo6File.ToArray();
            */

            reqNo6.file = fl;
            //reqNo6.file = new List<Files>();
            //reqNo6.file.Add(fl);
            reqNo6.payInSlip = ps;

            // Fix null with [] ***** Need to be reviewed *****
            // Answers is inner class, Answer is outer class
            int intAwArray = 4; // array size of Answer (outer class)
            int intAwsArray = 24; // array size of Answers (inner class) in all Answer (outer class)
                                  //List<Answers> lstTempAwsAnswers = new List<Answers>();
            Answer[] awArray = new Answer[intAwArray];
            Answers[] awsArray = new Answers[intAwsArray];
            for (int i = 0; i < intAwsArray; i++)
            {
                awsArray[i] = new Answers();
                if ((i < 2) || (i == 4) || (i == 11) || (i == 18))
                {
                    continue;
                }
                else
                {
                    if (i == 2)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01Receive_Loan_First"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 3)
                    {
                        awsArray[i].answerId = 3;
                        awsArray[i].status = !(awsArray[i - 1].status);
                        ar[intAIndex + i] = awsArray[i].status.ToString(); //308
                        arn[intAIndex++ + i] = "awsArray[3].status";
                    }
                    else if (i == 5)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Seminar"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 6)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Booth"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 7)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Suggest"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 8)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Media"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 9)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Bank"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 10)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01KnowTCG_Gov"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 12)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_Easy"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 13)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_Many"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 14)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_Person"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 15)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_NoBank"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 16)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_Product"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 17)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01UseTCG_Policy"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 19)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01Dont_Info"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 20)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01Dont_Step"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 21)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01Dont_Rules"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 22)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01NotEnough_Money"], string.Format("awsArray[{0}].status", i));
                    }
                    else if (i == 23)
                    {
                        awsArray[i] = this.WorkWithQuestionaire(awsArray[i], i, dtb.Rows[0]["T01Dont_Have"], string.Format("awsArray[{0}].status", i));
                    }
                }
            }
            awArray[0] = new Answer();
            awArray[0].questionDetailId = 1;
            awArray[0].answers = new Answers[] {
                        awsArray[2],
                        awsArray[3]
                    };
            awArray[1] = new Answer();
            awArray[1] = new Answer();
            awArray[1].questionDetailId = 4;
            awArray[1].answers = new Answers[] {
                        awsArray[5],
                        awsArray[6],
                        awsArray[7],
                        awsArray[8],
                        awsArray[9],
                        awsArray[10]
                    };
            awArray[2] = new Answer();
            awArray[2].questionDetailId = 11;
            awArray[2].answers = new Answers[] {
                        awsArray[12],
                        awsArray[13],
                        awsArray[14],
                        awsArray[15],
                        awsArray[16],
                        awsArray[17]
                    };
            awArray[3] = new Answer();
            awArray[3].questionDetailId = 18;
            awArray[3].answers = new Answers[] {
                        awsArray[19],
                        awsArray[20],
                        awsArray[21],
                        awsArray[22],
                        awsArray[23]
                    };
            reqNo6.answer = awArray;

            reqNo6.remark = fns.operation; //179



            // ========= for prescreening ============

            /*
            TCG KoB ($T01Project_Type >= '00049' && $T01Project_Type <= '00057') || ($T01Project_Type >= '00953' && $T01Project_Type <= '00960') || $T01Project_Type == '00980' || $T01Project_Type == '00981'  )
            13:48 TCG KoB ตอนนี้มีแค่นี้ครับที่จะมีการเช็ค ให้ใส่ T01CostEstimate T01Total_Loan_Amount
             */

            string strNetworth = Math.Floor((Single)(Convert.ToSingle(_T01Total_Asset) - Convert.ToSingle(_T01Total_Debt))).ToString();

            string strAssetCollateral = "0";
            if (_T01Total_Loan_Amount > 0)
            {
                Single sglAssetCollateral = Convert.ToSingle((_T01CostEstimate / _T01Total_Loan_Amount) * 100);
                strAssetCollateral = Math.Floor(sglAssetCollateral).ToString();
            }

            PreScreenInitialNewData[] psind = new PreScreenInitialNewData[2];

            psind[0] = new PreScreenInitialNewData();
            psind[0].screeningNameField = "networth";
            psind[0].value = strNetworth;

            psind[1] = new PreScreenInitialNewData();
            psind[1].screeningNameField = "assetCollateral";
            psind[1].value = strAssetCollateral;

            //reqNo6.preScreeningInitialNew = new List<string>();
            ////reqNo6.preScreeningInitialNew.Add(null);
            //reqNo6.preScreeningInitialNew.Add(GetPreScreeningInitialNew()); // Add
            //List<string> lstTempReqNo6PreScreeningInitialNew = new List<string>();
            //lstTempReqNo6PreScreeningInitialNew.Add(GetPreScreeningInitialNew());
            reqNo6.preScreeningInitialNew = new PreScreenInitialNewData[] { psind[0], psind[1] };

            // ========= for prescreening ============

            /*
            strJsonOutput = JsonSerializer.Serialize(obj, typeof(T), new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true });
             */

            //strTempRequestNo6Json = JsonSerializer.Serialize(reqNo6, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true });
            strTempRequestNo6Json = JsonSerializer.Serialize(reqNo6, new JsonSerializerOptions() {
                /*   
                    Encoder = System.Text.Encoding.UTF8.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true 
                */

            }
            );
            strJSONResult = strTempRequestNo6Json;
            return strJSONResult;
            /*} catch (Exception ex) {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
                return "";
            }
            */
        }

        private string GetGuaLoadPurpose(string strGuaLoadPurpose)
        {
            string strReturn = "";
            switch (strGuaLoadPurpose)
            {
                case "00001":
                    strReturn = "โครงการใหม่";
                    break;
                case "00002":
                    strReturn = "โครงการขยายงาน";
                    break;
                default:
                    strReturn = "-";
                    break;
            }
            return strReturn;
        }

        public void ReadDebtorJSON()
        {
            try
            {
                //StreamReader sr = File.OpenText((new Default()).StrDebtorJSONLocation);
                StreamReader sr = File.OpenText("Debtor.json");
                strDebtorJson = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception ex)
            {
                WriteLogFile(string.Format("Exception happened at \"{0}(...)\":{1}{2}{1}Stack:{1}{3}", MethodBase.GetCurrentMethod().Name, Environment.NewLine, ex.Message, ex.StackTrace));
            }
        }

        public string[] CheckWorkingStatus()
        {
            string[] strArrayWorkingStatus = new string[7] { "-1", "-1", "-1", "-1", "-1", "-1", "-1" };
            try
            {
                string strSql0 = "select count(1) from TBL_CI_Import_Status where Imported='3';";
                string strSql1 = "select count(1) from TBL_CI_Import_Status where Imported='4';";
                string strSql2 = "select count(1) from TBL_CI_Import_Status where Imported='6';";
                string strSql3 = "select count(1) from TBL_CI_Import_Status where Imported='8';";
                string strSql4 = "select count(1) from TBL_CI_Import_Status where Imported='Y';";
                string strSql5 = "select count(1) from TBL_CI_Import_Status where Imported='N';";
                string strSql6 = "select count(1) from TBL_CI_Import_Status where Imported is null;";
                string strConnectionString = "LogConnectionString";
                string strDbTableName = "[dbo].[TBL_CI_Import_Status]";
                string strPrimaryKey = "T01Online_ID";
                DataSet dtsResult0 = GetData(strSql0, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[0] = dtsResult0.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult1 = GetData(strSql1, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[1] = dtsResult1.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult2 = GetData(strSql2, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[2] = dtsResult2.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult3 = GetData(strSql3, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[3] = dtsResult3.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult4 = GetData(strSql4, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[4] = dtsResult4.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult5 = GetData(strSql5, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[5] = dtsResult5.Tables[0].Rows[0][0].ToString();
                DataSet dtsResult6 = GetData(strSql6, strDbTableName, strPrimaryKey, strConnectionString);
                strArrayWorkingStatus[6] = dtsResult6.Tables[0].Rows[0][0].ToString();
                return strArrayWorkingStatus;
            }
            catch (Exception ex)
            {
                WriteLogFile(string.Format("Exception happened at \"{0}(...)\":{1}{2}{1}Stack:{1}{3}", MethodBase.GetCurrentMethod().Name, Environment.NewLine, ex.Message, ex.StackTrace));
            }
            return strArrayWorkingStatus;
        }

        private string GetTCGBusinessNameFromISICCode(string strTempISICCode)
        {
            string strTCGBusinessName = "";
            try
            {
                string strConnectionString = "ViewConnectionString";
                string strDbTableName = "dbo.TBL_ISIC2014_TCG_INDUSTRY_SUB_TYPE";
                string strPrimaryKey = "ISIC2014_Code";
                string strSql = "SELECT industry_name FROM " + strDbTableName + " WHERE ISIC2014_Code='" + strTempISICCode + "';";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                strTCGBusinessName = dtsResult.Tables[0].Rows[0][0].ToString();
                return strTCGBusinessName;
            }
            catch (Exception ex)
            {
                WriteLogFile(string.Format("Exception happened at \"{0}(...)\":{1}{2}{1}Stack:{1}{3}", MethodBase.GetCurrentMethod().Name, Environment.NewLine, ex.Message, ex.StackTrace));
                return "-";
            }
        }

        Answers WorkWithQuestionaire(Answers aws, int i, object o, string strPropertyName)
        {
            aws.answerId = i;
            var tempData = o;
            aws.status = ((tempData == null) || (tempData == "") || ((bool)tempData == false)) ? false : true;
            ar[intAIndex + i] = aws.status.ToString(); //308
            arn[intAIndex + i] = strPropertyName;
            return aws;
        }

        //private int GetProductId(string strTempProductCode) { // Product Code is TCGCyber's Project Type
        //    int intProductId = 0;
        //    if ((strTempProductCode != "") && (strTempProductCode != "0")) {
        //        string strConnectionString = "ViewConnectionString";
        //        string strDbTableName = "dbo.TBL_MD_PRODUCT";
        //        string strPrimaryKey = "PRODUCT_ID";
        //        // Go to get prefix (if any)
        //        strTempProductCode = this.GetPrefixOfProductCode(strTempProductCode);
        //        _productCode = strTempProductCode; // used in log
        //        string strSql = "SELECT PRODUCT_ID FROM " + strDbTableName + " WHERE PRODUCT_CODE='" + strTempProductCode + "';";
        //        DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
        //        intProductId = Convert.ToInt32(dtsResult.Tables[0].Rows[0][0].ToString());
        //    }
        //    _productId = intProductId.ToString(); // used in log
        //    return intProductId;
        //    //return 238;
        //}

        private int GetProductId(string strTempProductCode)
        { // Product Code is TCGCyber's Project Type
            int intProductId = 0;
            if ((strTempProductCode != "") && (strTempProductCode != "0"))
            {
                string strConnectionString = "ViewConnectionString";
                string strDbTableName = "dbo.TBL_MD_PRODUCT_WITH_TCG";
                string strPrimaryKey = "PRODUCT_ID";
                _productCode = strTempProductCode; // used in log
                string strSql = "SELECT PRODUCT_ID FROM " + strDbTableName + " WHERE R09TYPE_CODE='" + strTempProductCode + "';";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                intProductId = Convert.ToInt32(dtsResult.Tables[0].Rows[0][0].ToString());
            }
            _productId = intProductId.ToString(); // used in log
            return intProductId;
        }

        // ***** These are needed to be take care ... *****
        // Now, there are still inconsistencies between the picture D.Jet sent and real Db
        // 192.168.10.17
        // DB: [DB_CGSAPI_MASTER]
        // Table: [dbo].[TBL_MD_PRODUCT]
        private string GetPrefixOfProductCode(string strProductCode2BPrefixed)
        {
            string strProductCodeAfterPrefixed = "";
            switch (strProductCode2BPrefixed)
            {
                case "00995":
                case "00984":
                case "00985":
                case "00986":
                case "00987":
                case "00988":
                case "00989":
                    strProductCodeAfterPrefixed = "PGS9" + strProductCode2BPrefixed;
                    break;
                case "00903":
                case "00904":
                case "00905":
                    strProductCodeAfterPrefixed = "MICRO4" + strProductCode2BPrefixed;
                    break;
                default: // normal cases
                    strProductCodeAfterPrefixed = strProductCode2BPrefixed;
                    break;
            }
            return strProductCodeAfterPrefixed;
        }

        // ad hoc, must be improve asap *****
        private string GetLoanTypeNameNew(string strLoanType, int intBankId)
        {
            string strLoanTypeName = ""; // used in "ประเภทสินเชื่อ"
            if (intBankId == 2)
            { // BBL
                strLoanTypeName = "Term Loan";
            }
            if (intBankId == 3)
            { // BBL
                strLoanTypeName = "วงเงินกู้อื่น ๆ";
            }
            if (intBankId == 4)
            { // Thai Farmer Bank (Kasikorn Bank)
              // if (_T01Loan_Type_1 == "") {
                strLoanTypeName = "วงเงินกู้อื่น ๆ";
                //}
                //strLoanTypeName = _T01Loan_Type_1;
            }
            if (intBankId == 35)
            { // Exim
                strLoanTypeName = "Loan";
            }
            if (intBankId == 14)
            { // SCB
                strLoanTypeName = "Term loan";
            }
            if (intBankId == 6)
            { // KTB
                strLoanTypeName = "Term Loan";
            }
            if (intBankId == 71)
            { // Thai Credit
                strLoanTypeName = "Term Loan";
            }
            if (intBankId == 67)
            { // Tisco
                //if (strLoanType.Contains("เช่า")) {
                //    strLoanTypeName = "สัญญาเช่าซื้อ";
                //} else if ((strLoanType.Contains("เพิ่มวง"))) {
                //    strLoanTypeName = "สัญญาสินเชื่อเพิ่มวงเงิน";
                //} else if ((strLoanType.Contains("เพื่อธุร"))) {
                //    strLoanTypeName = "สัญญาสินเชื่อเพื่อธุรกิจ";
                //} else if ((strLoanType.Contains("ปรับปรุง"))) {
                //    strLoanTypeName = "สัญญาการปรับปรุงโครงสร้างหนี้";
                //} else {
                //    strLoanTypeName = "สัญญาเงินกู้";
                //}
                strLoanTypeName = "Term Loan";
            }
            return strLoanTypeName;
        }

        private string GetTCGLoanTypeName(string strLoanType)
        {
            if (strLoanType == "")
                strLoanType = "0";
            strLoanType = Convert.ToInt32(strLoanType).ToString("00000");
            string strTCGLoanTypeName = "-";
            // 192.168.10.17
            // FROM [DB_SICGC3].[dbo].[R09_Type] where [R09Type_Group] = '14'
            switch (strLoanType)
            {
                case "00001":
                    strTCGLoanTypeName = "OD";
                    break;
                case "00002":
                    strTCGLoanTypeName = "เงินกู้ระยะยาว";
                    break;
                case "00003":
                    strTCGLoanTypeName = "ตั๋วสัญญาใช้เงิน, ขายลดเช็ค";
                    break;
                case "00004":
                    strTCGLoanTypeName = "LC/TR,Packing Credit";
                    break;
                case "00005":
                    strTCGLoanTypeName = "วงเงินกู้อื่นๆ";
                    break;
                case "00006":
                    strTCGLoanTypeName = "PN";
                    break;
                case "00007":
                    strTCGLoanTypeName = "วงเงินกู้ RP แม่บท";
                    break;
                case "00008":
                    strTCGLoanTypeName = "LG";
                    break;
                case "00009":
                    strTCGLoanTypeName = "Factoring";
                    break;
                case "00010":
                    strTCGLoanTypeName = "Combine Line";
                    break;
                case "00011":
                    strTCGLoanTypeName = "เพื่อเช่าซื้อ/เช่าลีสซิ่ง";
                    break;
                default:
                    //strTCGLoanTypeName = "วงเงินกู้อื่นๆ";
                    //strTCGLoanTypeName = "เงินกู้ระยะยาว";
                    break;
            }
            return strTCGLoanTypeName;
        }

        string GetBase24FromFile(string strBaseFolder, string strFileName)
        {
            string strFilePath = strBaseFolder + "\\" + strFileName;
            // from 
            // https://stackoverflow.com/questions/25919387/converting-file-into-base64string-and-back-again
            byte[] bytes = System.IO.File.ReadAllBytes(strFilePath);
            string strBase24File = Convert.ToBase64String(bytes);
            return strBase24File;
        }

        private string GetPreScreeningInitialNew()
        {
            string strPreScreenData = null;
            if (strTempPreScreenData == "" || strTempPreScreenData == null)
            {
                strPreScreenData = null;
            }
            else
            {
                strPreScreenData = strTempPreScreenData;
            }
            return strPreScreenData;
        }

        // Temperary ... need to be fixed
        /*
        From DB: 192.168.10.17
        From Query
        SELECT TOP (1000) [DEBTOR_TYPE_ID], [BANK_ID], [DEBTOR_TYPE_NAME], [STATUS], [CREATE_BY], [CREATE_DT], [UPDATE_BY]
      ,[UPDATE_DT]
      ,[DEBTOR_TYPE_NAME_MIGRATE]
        FROM [DB_CGSAPI_MASTER].[dbo].[TBL_MD_DEBTOR_TYPE]
         */
        private int GetDebtorTypeId(string strT01Debt_Define_X, int intBankId)
        {
            int intDebtorTypeId = 0;
            // use "Long Term Debt" as default
            switch (intBankId)
            {
                case 2:
                    intDebtorTypeId = 31;
                    break;
                case 3:
                    intDebtorTypeId = 35;
                    break;
                case 4:
                    intDebtorTypeId = 59;
                    break;
                case 6:
                    intDebtorTypeId = 93;
                    break;
                case 11:
                    intDebtorTypeId = 107;
                    break;
                case 12:
                    intDebtorTypeId = 114;
                    break;
                case 13:
                    intDebtorTypeId = 115;
                    break;
                case 14:
                    intDebtorTypeId = 125;
                    break;
                case 15:
                    intDebtorTypeId = 133;
                    break;
                case 17:
                    intDebtorTypeId = 134; // OD
                    break;
                case 20:
                    intDebtorTypeId = 140;
                    break;
                case 21:
                    intDebtorTypeId = 143;
                    break;
                case 22:
                    intDebtorTypeId = 163;
                    break;
                case 24:
                    intDebtorTypeId = 173;
                    break;
                case 25:
                    intDebtorTypeId = 183;
                    break;
                case 30:
                    intDebtorTypeId = 198;
                    break;
                case 34:
                    intDebtorTypeId = 208;
                    break;
                case 35:
                    intDebtorTypeId = 229;
                    break;
                case 65:
                    intDebtorTypeId = 238;
                    break;
                case 66:
                    intDebtorTypeId = 261;
                    break;
                case 67:
                    intDebtorTypeId = 266;
                    break;
                case 69:
                    intDebtorTypeId = 275;
                    break;
                case 70:
                    intDebtorTypeId = 281;
                    break;
                case 71:
                    intDebtorTypeId = 294;
                    break;
                case 73:
                    intDebtorTypeId = 299;
                    break;
                case 79:
                    intDebtorTypeId = 306;
                    break;
                case 80:
                    intDebtorTypeId = 315;
                    break;
                case 613:
                    intDebtorTypeId = 317;
                    break;
                default:
                    intDebtorTypeId = 284;
                    break;
            }
            return intDebtorTypeId;
        }

        // Experiment ... Not complete yet (has assumption in getting the first match ... this is the best try) *****
        private int GetDebtorTypeIdWithBankInfo(int intBankId, string strT01Debt_Define_X)
        {
            int intDebtorTypeId = 0;
            string strTempResult = "";
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = true
            };
            DebtorTypeInfo dti = JsonSerializer.Deserialize<DebtorTypeInfo>(strDebtorJson, options);
            List<AResult> lstAllResult = dti.result;
            List<AResult> lstResultByBank = new List<AResult>();
            foreach (AResult itemResult in lstAllResult)
            {
                if (itemResult.bankId == intBankId)
                {
                    lstResultByBank.Add(itemResult);
                }
            }

            List<AResult> lstResultByBankAndTypeName = new List<AResult>();
            foreach (AResult itemResult in lstResultByBank)
            {
                if (!(itemResult.debtorTypeName == "" || itemResult.debtorTypeName == null))
                {
                    string strTypeName = itemResult.debtorTypeName.Replace(" ", "");
                    strT01Debt_Define_X = strT01Debt_Define_X.Replace(" ", "");
                    if (strTypeName.Contains(strT01Debt_Define_X))
                    {
                        lstResultByBankAndTypeName.Add(itemResult);
                        break;
                    }
                }
            }
            foreach (AResult itemResult in lstResultByBankAndTypeName)
            {
                strTempResult += itemResult.debtorTypeId + " ";
                intDebtorTypeId = itemResult.debtorTypeId;
            }
            return intDebtorTypeId;
        }

        private int GetDebtMonth(string strT01Debt_Year_X)
        { // convert double to int for month
            int intDebtMonth = 0;
            if ((strT01Debt_Year_X != "") && (strT01Debt_Year_X != null))
            {
                int intT01Debt_Year = Convert.ToInt32(Math.Floor(Convert.ToSingle(strT01Debt_Year_X)));
                Single sglT01Debt_Year = Convert.ToSingle(strT01Debt_Year_X);
                Single sglMonthRemain = sglT01Debt_Year - intT01Debt_Year;
                intDebtMonth = Convert.ToInt32(Math.Round(sglMonthRemain * 12));
            }
            return intDebtMonth;
        }

        private string GetDebtYear(string strT01Debt_Year_X)
        {
            string strDebtYear = "";
            if (strT01Debt_Year_X != "" && strT01Debt_Year_X != null)
            {
                int intT01Debt_Year = Convert.ToInt32(Math.Floor(Convert.ToSingle(strT01Debt_Year_X)));
                strDebtYear = intT01Debt_Year.ToString();
            }
            return strDebtYear;
        }

        private string GetPurposeCode(string strT01Investment_Objective_X)
        {
            //strT01Investment_Objective_X = strT01Investment_Objective_X.Replace("0", "");
            /* Old System */
            /*
            R63Object_Request_Code	R63Object_Request_Name
    2	เพื่อการทำ Refinance 4
    3	เพื่อการลงทุนในสินทรัพย์ถาวร 2
    4	เพื่อเป็นเงินทุนหมุนเวียน 1
    5	เพื่อวัตถุประสงค์อื่น 0
    6	เพื่อการทำ Refinance และ เพื่อการลงทุนในสินทรัพย์ถาวร 4
    7	เพื่อการทำ Refinance และ เพื่อเป็นเงินทุนหมุนเวียน 4
    8	เพื่อการทำ Refinance และ เพื่อวัตถุประสงค์อื่น 4
    9	เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อเป็นเงินทุนหมุนเวียน 2
    10	เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อวัตถุประสงค์อื่น 2
    11	เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 1
    12	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อเป็นเงินทุนหมุนเวียน 4
    13	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อวัตถุประสงค์อื่น 4
    14	เพื่อการทำ Refinance เพื่อเป็นเงินทุนหมุนเวียน และเพื่อวัตถุประสงค์อื่น 4
    15	เพื่อการลงทุนในสินทรัพย์ถาวร เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 2
    16	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 4
            // New System
R09Type_Code	    R09Type_Name	R09Type_Group
00000	                                                                98
00001	หมุนเวียนในกิจการ                                         98
00002	ลงทุนในกิจการ                                             98
00003	การค้ำประกันต่างๆ ในการดำเนินธุรกิจ               98
00004	Refinance	                                                98
11111	เพื่อเช่าซื้อ/เช่าลีสซิ่ง ยานพาหนะที่ใช้ในเชิงพาณิชย์	    98
22222	เพื่อเช่าซื้อ/เช่าลีสซิ่ง เครื่องจักร	                        98
44444	เพื่อหมุนเวียนในกิจการ สินเชื่อแฟ็กเตอริงแบบมีสิทธิไล่เบี้ย SMEs ได้ (factoring with recourse)	98
             */
            string strPurposeCode = "";
            // Assessing by myself *****
            switch (strT01Investment_Objective_X)
            {
                case "00000":
                    strPurposeCode = "-";
                    break;
                case "00001":
                    strPurposeCode = "หมุนเวียนในกิจการ";
                    break;
                case "00002":
                    strPurposeCode = "ลงทุนในกิจการ";
                    break;
                case "00003":
                    strPurposeCode = "การค้ำประกันต่างๆ ในการดำเนินธุรกิจ";
                    break;
                case "00004":
                    strPurposeCode = "Refinance";
                    break;
                case "11111":
                    strPurposeCode = "เพื่อเช่าซื้อ/เช่าลีสซิ่ง ยานพาหนะที่ใช้ในเชิงพาณิชย์	";
                    break;
                case "22222":
                    strPurposeCode = "เพื่อเช่าซื้อ/เช่าลีสซิ่ง เครื่องจักร";
                    break;
                case "44444":
                    strPurposeCode = "เพื่อหมุนเวียนในกิจการ สินเชื่อแฟ็กเตอริงแบบมีสิทธิไล่เบี้ย SMEs ได้";
                    break;
                default:
                    strPurposeCode = "-";
                    break;
            }
            return strPurposeCode;
        }

        //        private string GetPurposeCode(string strT01Investment_Objective_X) {
        //            strT01Investment_Objective_X = strT01Investment_Objective_X.Replace("0", "");
        //            /* Old System */
        //            /*
        //            R63Object_Request_Code	R63Object_Request_Name
        //    2	เพื่อการทำ Refinance 4
        //    3	เพื่อการลงทุนในสินทรัพย์ถาวร 2
        //    4	เพื่อเป็นเงินทุนหมุนเวียน 1
        //    5	เพื่อวัตถุประสงค์อื่น 0
        //    6	เพื่อการทำ Refinance และ เพื่อการลงทุนในสินทรัพย์ถาวร 4
        //    7	เพื่อการทำ Refinance และ เพื่อเป็นเงินทุนหมุนเวียน 4
        //    8	เพื่อการทำ Refinance และ เพื่อวัตถุประสงค์อื่น 4
        //    9	เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อเป็นเงินทุนหมุนเวียน 2
        //    10	เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อวัตถุประสงค์อื่น 2
        //    11	เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 1
        //    12	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อเป็นเงินทุนหมุนเวียน 4
        //    13	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร และ เพื่อวัตถุประสงค์อื่น 4
        //    14	เพื่อการทำ Refinance เพื่อเป็นเงินทุนหมุนเวียน และเพื่อวัตถุประสงค์อื่น 4
        //    15	เพื่อการลงทุนในสินทรัพย์ถาวร เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 2
        //    16	เพื่อการทำ Refinance เพื่อการลงทุนในสินทรัพย์ถาวร เพื่อเป็นเงินทุนหมุนเวียน และ เพื่อวัตถุประสงค์อื่น 4
        //            // New System
        //R09Type_Code	    R09Type_Name	R09Type_Group
        //00000	                                                                98
        //00001	หมุนเวียนในกิจการ                                         98
        //00002	ลงทุนในกิจการ                                             98
        //00003	การค้ำประกันต่างๆ ในการดำเนินธุรกิจ               98
        //00004	Refinance	                                                98
        //11111	เพื่อเช่าซื้อ/เช่าลีสซิ่ง ยานพาหนะที่ใช้ในเชิงพาณิชย์	    98
        //22222	เพื่อเช่าซื้อ/เช่าลีสซิ่ง เครื่องจักร	                        98
        //44444	เพื่อหมุนเวียนในกิจการ สินเชื่อแฟ็กเตอริงแบบมีสิทธิไล่เบี้ย SMEs ได้ (factoring with recourse)	98
        //             */
        //            string strPurposeCode = "";
        //            // Assessing by myself *****
        //            switch (strT01Investment_Objective_X) {
        //                case "2":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "3":
        //                    strPurposeCode = "00002";
        //                    break;
        //                case "4":
        //                    strPurposeCode = "00001";
        //                    break;
        //                case "5":
        //                    strPurposeCode = "00000";
        //                    break;
        //                case "6":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "7":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "8":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "9":
        //                    strPurposeCode = "00002";
        //                    break;
        //                case "10":
        //                    strPurposeCode = "00002";
        //                    break;
        //                case "11":
        //                    strPurposeCode = "00001";
        //                    break;
        //                case "12":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "13":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "14":
        //                    strPurposeCode = "00004";
        //                    break;
        //                case "15":
        //                    strPurposeCode = "00002";
        //                    break;
        //                case "16":
        //                    strPurposeCode = "00004";
        //                    break;
        //                default:
        //                    strPurposeCode = "22222";
        //                    break;
        //            }
        //            return strPurposeCode;
        //        }

        private string GetEducationLevel(string strT0XEducation)
        {
            string strEducationLevel = "";
            switch (strT0XEducation)
            {
                case "2":
                    strEducationLevel = "7"; // higher than bachelor
                    break;
                case "3":
                    strEducationLevel = "6"; // bachelor
                    break;
                case "4":
                    strEducationLevel = "4"; // vocational
                    break;
                case "5":
                    strEducationLevel = "3"; // high-school
                    break;
                case "6":
                    strEducationLevel = "1"; // primary school
                    break;
                case "7":
                    strEducationLevel = "99"; // not specify
                    break;
                case "0":
                    strEducationLevel = "99"; // null
                    break;
                default:
                    strEducationLevel = "99";
                    break;
            }
            return strEducationLevel;
        }

        private string GetIdentificationType(string strT0XCard_Type)
        {
            // IP: 192.168.0.6
            // DB: DB_SICGC3
            // Select  * from R09_Type Where R09Type_Group = '11' 
            strT0XCard_Type = strT0XCard_Type.Replace("0", "");
            string strIdentificationType = "";
            switch (strT0XCard_Type)
            {
                case "1": // id - citizen
                case "2": // id - civil servant
                    strIdentificationType = "C"; // C = Id Card
                    strCustomerType = "02"; // person in CGS
                    break;
                case "3": // id - company
                    strIdentificationType = "C"; // C = Id Card
                    strCustomerType = "01";  // Organization in CGS
                    break;
                case "4": // others
                    strIdentificationType = ""; // P = Passport
                    break;
            }
            return strIdentificationType;
        }

        private string GetCustomerType(string strT0XCustomer_Type)
        {
            // IP: 192.168.0.6
            // DB: DB_SICGC3
            // Select  * from R09_Type Where R09Type_Group = '1' 
            strT0XCustomer_Type = strT0XCustomer_Type.Replace("0", "");
            switch (strT0XCustomer_Type)
            {
                case "1": // person
                    strCustomerType = "02";
                    break;
                case "2": // organization
                    strCustomerType = "01";
                    break;
            }
            return strCustomerType;
        }

        string GetCGSNotReduce(string strTCGNotReduce)
        {
            string strCGSNotReduce = "";
            switch (strTCGNotReduce)
            {
                case "0":
                    strCGSNotReduce = "01"; // ลดภาระ
                    break;
                case "1":
                    strCGSNotReduce = "02"; // ไม่ลดภาระ
                    break;
                default:
                    strCGSNotReduce = "";
                    break;
            }
            return strCGSNotReduce;
        }

        private string GetCustomerTypeFromID(string strId)
        {
            string strCustomerType = "";
            if (strId.StartsWith("0"))
            {
                strCustomerType = "01"; // organization
            }
            else
            {
                strCustomerType = "02"; // individual
            }
            return strCustomerType;
        }

        private string GetCustomerType(string strTitleCode, string strLastname)
        {
            string strCustomerType = "02";
            switch (strTitleCode)
            {
                case "001":
                case "002":
                case "003":
                case "009":
                case "011":
                case "012":
                case "013":
                case "014":
                case "015":
                case "016":
                case "017":
                case "018":
                case "019":
                case "020":
                case "021":
                case "022":
                case "024":
                case "025":
                case "026":
                case "027":
                case "028":
                case "029":
                case "031":
                case "032":
                case "033":
                case "034":
                case "035":
                case "036":
                case "037":
                case "038":
                case "039":
                case "040":
                case "041":
                case "042":
                case "043":
                case "044":
                case "045":
                case "046":
                case "047":
                case "048":
                case "049":
                case "050":
                case "051":
                case "052":
                case "053":
                case "054":
                case "055":
                case "056":
                case "057":
                case "058":
                case "059":
                case "060":
                case "061":
                case "062":
                case "063":
                case "064":
                case "065":
                case "066":
                case "067":
                case "069":
                case "071":
                case "072":
                case "073":
                case "074":
                case "075":
                case "076":
                case "077":
                case "078":
                case "079":
                case "081":
                case "082":
                case "083":
                case "084":
                case "085":
                case "086":
                case "087":
                case "089":
                case "090":
                case "091":
                case "092":
                case "093":
                case "094":
                case "095":
                case "096":
                case "097":
                case "098":
                case "099":
                case "100":
                case "101":
                case "102":
                case "103":
                case "104":
                case "105":
                case "106":
                case "107":
                case "108":
                case "109":
                case "110":
                case "112":
                case "113":
                case "115":
                case "117":
                case "118":
                case "119":
                case "120":
                case "121":
                case "122":
                case "123":
                case "124":
                case "125":
                case "126":
                case "127":
                case "128":
                case "129":
                case "130":
                case "131":
                case "132":
                case "133":
                case "134":
                case "135":
                case "136":
                case "137":
                case "138":
                case "139":
                case "140":
                case "141":
                case "142":
                case "143":
                case "144":
                case "145":
                case "147":
                case "148":
                case "149":
                case "150":
                case "151":
                case "152":
                case "153":
                case "154":
                case "155":
                case "156":
                case "157":
                case "160":
                case "161":
                case "162":
                case "163":
                case "164":
                case "165":
                case "166":
                case "168":
                case "169":
                case "170":
                case "171":
                case "172":
                case "173":
                case "174":
                case "175":
                case "176":
                case "177":
                case "178":
                case "179":
                case "180":
                case "181":
                case "182":
                case "183":
                case "184":
                case "185":
                case "186":
                case "187":
                case "188":
                case "189":
                case "190":
                case "191":
                case "192":
                case "193":
                case "194":
                case "195":
                case "196":
                case "197":
                case "198":
                case "199":
                case "200":
                case "201":
                case "202":
                case "203":
                case "204":
                case "205":
                case "206":
                case "207":
                case "208":
                case "209":
                case "210":
                case "211":
                case "212":
                case "213":
                case "214":
                case "215":
                case "216":
                case "217":
                case "218":
                case "219":
                case "220":
                case "221":
                case "222":
                case "223":
                case "224":
                case "225":
                case "226":
                case "227":
                case "228":
                case "229":
                case "230":
                case "231":
                case "232":
                case "233":
                case "234":
                    strCustomerType = "02"; // person in CGS
                    break;
                case "004":
                case "005":
                case "006":
                case "007":
                case "010":
                case "023":
                case "030":
                case "068":
                case "070":
                case "080":
                case "088":
                case "116":
                case "146":
                case "158":
                case "159":
                case "167":
                    strCustomerType = "01";  // organization in CGS
                    break;
            }
            return strCustomerType;
        }

        private string GetGender(string strTitleCode)
        {
            string strGender = "";
            switch (strTitleCode)
            {
                case "001":
                case "009":
                case "011":
                case "012":
                case "013":
                case "014":
                case "015":
                case "017":
                case "018":
                case "019":
                case "020":
                case "021":
                case "022":
                case "024":
                case "025":
                case "026":
                case "027":
                case "028":
                case "029":
                case "031":
                case "032":
                case "034":
                case "035":
                case "036":
                case "037":
                case "038":
                case "040":
                case "041":
                case "042":
                case "044":
                case "045":
                case "046":
                case "047":
                case "048":
                case "049":
                case "050":
                case "051":
                case "053":
                case "054":
                case "055":
                case "057":
                case "058":
                case "059":
                case "060":
                case "061":
                case "062":
                case "066":
                case "067":
                case "069":
                case "071":
                case "072":
                case "075":
                case "076":
                case "078":
                case "079":
                case "081":
                case "082":
                case "083":
                case "085":
                case "086":
                case "090":
                case "091":
                case "092":
                case "094":
                case "095":
                case "096":
                case "101":
                case "104":
                case "105":
                case "106":
                case "107":
                case "112":
                case "113":
                case "115":
                case "117":
                case "118":
                case "120":
                case "121":
                case "122":
                case "123":
                case "124":
                case "127":
                case "129":
                case "130":
                case "131":
                case "132":
                case "134":
                case "135":
                case "136":
                case "138":
                case "141":
                case "143":
                case "145":
                case "147":
                case "148":
                case "149":
                case "151":
                case "153":
                case "154":
                case "155":
                case "156":
                case "161":
                case "163":
                case "165":
                case "168":
                case "169":
                case "174":
                case "175":
                case "177":
                case "178":
                case "179":
                case "182":
                case "183":
                case "184":
                case "191":
                case "194":
                case "198":
                case "205":
                case "206":
                case "213":
                case "221":
                case "225":
                case "226":
                case "227":
                case "228":
                case "229":
                case "230":
                case "231":
                case "232":
                    strGender = "M";
                    break;
                case "002":
                case "003":
                case "016":
                case "039":
                case "043":
                case "052":
                case "056":
                case "064":
                case "065":
                case "073":
                case "077":
                case "084":
                case "087":
                case "089":
                case "097":
                case "098":
                case "099":
                case "100":
                case "102":
                case "103":
                case "108":
                case "109":
                case "110":
                case "119":
                case "125":
                case "126":
                case "128":
                case "133":
                case "137":
                case "139":
                case "140":
                case "142":
                case "144":
                case "152":
                case "157":
                case "160":
                case "162":
                case "164":
                case "166":
                case "170":
                case "171":
                case "172":
                case "173":
                case "176":
                case "180":
                case "181":
                case "185":
                case "186":
                case "187":
                case "188":
                case "189":
                case "190":
                case "192":
                case "193":
                case "195":
                case "196":
                case "197":
                case "199":
                case "200":
                case "201":
                case "202":
                case "203":
                case "204":
                case "207":
                case "208":
                case "209":
                case "210":
                case "211":
                case "212":
                case "214":
                case "215":
                case "216":
                case "217":
                case "218":
                case "219":
                case "220":
                case "222":
                case "223":
                case "224":
                case "233":
                case "234":
                    strGender = "F";
                    break;
            }
            return strGender;
        }

        private string GetMaritalStatus(string strT0XMarital_Status)
        {
            // IP: 192.168.0.6
            // DB: DB_SICGC3
            // Table: [dbo].[R67Marital_Status]
            // Postman: https://sme-bank.tcg.or.th/master-service/api/system-configinfs/search?configId=5
            string strMaritalStatus = "";
            switch (strT0XMarital_Status)
            {
                case "2": // single
                case "4": // informal married
                    strMaritalStatus = "02";
                    break;
                case "3": // legal married
                    strMaritalStatus = "01";
                    break;
                case "5": // divorce
                    strMaritalStatus = "03";
                    break;
                case "6": // widow
                    strMaritalStatus = "04";
                    break;
                case "7":
                case "0":
                    strMaritalStatus = "";
                    break;
            }
            return strMaritalStatus;
        }

        private int GetTitleId(string strTitleName)
        {
            int intTitleNameId = 0;
            if (strTitleName != "" && strTitleName != "0")
            {
                string strConnectionString = "ViewConnectionString";
                string strDbTableName = "dbo.TBL_MD_TITLE_NAME";
                string strPrimaryKey = "title_name_id";
                // string strSql = "select title_name_id from " + strDbTableName + " where title_abbr_th='" + strTitleName + "' OR title_name_th='" + strTitleName + "';";
                string strSql = "select title_name_id from " + strDbTableName + " where title_name_code='" + strTitleName + "';";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                if (dtsResult.Tables[0].Rows.Count > 0)
                {
                    intTitleNameId = Convert.ToInt32(dtsResult.Tables[0].Rows[0][0].ToString());
                }
            }
            return intTitleNameId;
        }

        public string GetPREFromT01Online_ID(string strT01Online_ID)
        {
            string strPRE = "-1";
            if (strT01Online_ID != "" && strT01Online_ID != "0")
            {
                string strConnectionString = "LogConnectionString";
                string strDbTableName = "dbo.TBL_CI_LOG";
                string strPrimaryKey = "T01Online_ID";
                // string strSql = "select title_name_id from " + strDbTableName + " where title_abbr_th='" + strT01Online_ID + "' OR title_name_th='" + strT01Online_ID + "';";
                string strSql = "select preReqNumber from " + strDbTableName + " where T01Online_ID='" + strT01Online_ID + "';";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                if (dtsResult.Tables[0].Rows.Count > 0)
                {
                    strPRE = dtsResult.Tables[0].Rows[0][0].ToString();
                }
            }
            return strPRE;
        }

        public string GetT01OnlineIDFromPRE(string strPRE)
        {
            string strT01Online_ID = "-1";
            if (strPRE != "" && strPRE != "0")
            {
                string strConnectionString = "LogConnectionString";
                string strDbTableName = "dbo.TBL_CI_LOG";
                string strPrimaryKey = "T01Online_ID";
                // string strSql = "select title_name_id from " + strDbTableName + " where title_abbr_th='" + strT01Online_ID + "' OR title_name_th='" + strT01Online_ID + "';";
                string strSql = "select T01Online_ID from " + strDbTableName + " where preReqNumber='" + strPRE + "';";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                if (dtsResult.Tables[0].Rows.Count > 0)
                {
                    strT01Online_ID = dtsResult.Tables[0].Rows[0][0].ToString();
                }
            }
            return strT01Online_ID;
        }

        private string GetIsicId(string strT01ISIC_Code)
        {
            string strIsicId = "";
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_ISIC";
            string strPrimaryKey = "isic_id";
            string strSql = "select isic_id from " + strDbTableName + " where isic_code='" + strT01ISIC_Code + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            strIsicId = tempData.ToString();
            return strIsicId;
        }

        //private int GetTcgBusinessId(string strISICCode) { // used to be strT01Industry_Name
        //                                                   // use ISIC instead
        //    int intTcgBusinessId = 0;
        //    string strConnectionString = "ViewConnectionString";
        //    string strDbTableName = "dbo.TBL_MD_ISIC";
        //    string strPrimaryKey = "isic_id";
        //    string strSql = "select isic_id from " + strDbTableName + " where isic_code='" + strISICCode + "';";
        //    DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
        //    var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
        //    tempData = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
        //    intTcgBusinessId = Convert.ToInt32(tempData.ToString());
        //    return intTcgBusinessId;
        //}

        private int GetTcgBusinessId(string strISICCode)
        { // used to be strT01Industry_Name
          // use ISIC instead
            int intTcgBusinessId = 0;
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_ISIC";
            string strPrimaryKey = "isic_id";
            string strSql = "select Top 1 a.TCG_BUSINESS_ID from [dbo].[TBL_MD_TCG_BUSINESS] a, [dbo].[TBL_ISIC2014_TCG_INDUSTRY_SUB_TYPE] b where a.TCG_BUSINESS_CODE = (SELECT c.[industry_sub_type] from [dbo].[TBL_ISIC2014_TCG_INDUSTRY_SUB_TYPE] c where c.[isic2014_code]='" + strISICCode + "')";
            //string strSql = "select isic_id from " + strDbTableName + " where isic_code='" + strISICCode + "';";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            tempData = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            intTcgBusinessId = Convert.ToInt32(tempData.ToString());
            return intTcgBusinessId;
        }

        string GetProvinceCodeFromR04ProvinceCode(string strR04ProvinceCode)
        {
            string strProvinceCode = "0";
            if (strR04ProvinceCode != "" && strR04ProvinceCode != "0")
            {
                string strConnectionString = "CGSAConnectionString";
                string strDbTableName = "[DB_SICGC3].[dbo].[R04_Province]";
                string strPrimaryKey = "R04Province_Code";
                // string strSql = "select province_id from " + strDbTableName + " where province_name='" + strProvinceName + "'";
                string strSql = "select R04Province_CodeReal from " + strDbTableName + " where R04Province_Code='" + strR04ProvinceCode + "'";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                if (dtsResult != null && dtsResult.Tables[0].Rows.Count>0)
                    strProvinceCode = dtsResult.Tables[0].Rows[0][0].ToString();
            }
            return strProvinceCode;
        }



        //====================================== DUT EDIT ==========================================================
        private int findProvince(string strProvinceCode)
        {
            int intProvinceId = 0;
            try
            {
                if (strProvinceCode != "" && strProvinceCode != "0")
                {
                    //get R04ProvinceCodeReal from DB_SICG3
                    strProvinceCode = this.GetProvinceCodeFromR04ProvinceCode(strProvinceCode);
                    InterfaceDatabase conProd = ConnectCgs("CGSPROD");
                    Recordset rsProvince = conProd.GetRecordset("SELECT PROVINCE_ID FROM TBL_MD_PROVINCE WHERE STATUS ='A' AND PROVINCE_CODE = '" + strProvinceCode + "' ",1);

                    if (rsProvince.RecordCount > 0)
                    {
                        intProvinceId = rsProvince["PROVINCE_ID"].Int32Value;
                    }
                    else
                    {
                        //find '-' Province
                        Recordset rsProvinceAlt = conProd.GetRecordset("SELECT PROVINCE_ID FROM TBL_MD_PROVINCE WHERE STATUS = 'A' AND PROVINCE_NAME = '-'",1);
                        if (rsProvinceAlt.RecordCount > 0)
                        {
                            intProvinceId = rsProvinceAlt["PROVINCE_ID"].Int32Value;
                        }
                        else
                        {
                            intProvinceId = 0;
                        }
                        rsProvinceAlt.Close();
                    }
                    rsProvince.Close();
                }
                return intProvinceId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intProvinceId;
            }

        }

        private int findDistrict(string strProvinceCode, string strDistrictName)
        {
            int intProvinceId = 0;
            intProvinceId = findProvince(strProvinceCode);
            int intDistrictId = 0;
            try
            {
                if (strDistrictName != "" && strDistrictName != "0" && intProvinceId > 0)
                {
                    InterfaceDatabase conProd = ConnectCgs("CGSPROD");
                    Recordset rsDistrict = conProd.GetRecordset("SELECT PROVINCE_ID FROM TBL_MD_DISTRICT WHERE STATUS ='A' AND DISTRICT_NAME = '" + strDistrictName + "' AND PROVINCE_ID = " + intProvinceId + " ",1);
                    if (rsDistrict.RecordCount > 0)
                    {
                        intDistrictId = rsDistrict["DISTRICT_ID"].Int32Value;
                    }
                    else
                    {
                        //find '-' district
                        Recordset rsDistrictAlt = conProd.GetRecordset("SELECT DISTRICT_ID FROM TBL_MD_DISTRICT WHERE STATUS = 'A' AND DISTRICT_NAME = '-' AND PROVINCE_ID = " + intProvinceId + " ",1);
                        if (rsDistrictAlt.RecordCount > 0)
                        {
                            intDistrictId = rsDistrictAlt["DISTRICT_ID"].Int32Value;
                        }
                        else
                        {
                            intDistrictId = 0;
                        }
                        rsDistrictAlt.Close();
                    }
                    rsDistrict.Close();
                }
                return intDistrictId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intDistrictId;
            }
        }

        private int findSubDistrict(string strProvinceCode, string strDistrictName, string strSubdistrictName)
        {
            int intDistrictId = 0;
            int intSubdistrictId = 0;
            intDistrictId = findDistrict(strProvinceCode, strDistrictName);
            try
            {
                if (strDistrictName != "" && strDistrictName != "0" && intDistrictId > 0)
                {
                    InterfaceDatabase conProd = ConnectCgs("CGSPROD");
                    Recordset rsSubdistrict = conProd.GetRecordset("SELECT SUBDISTRICT_ID FROM TBL_MD_SUBDISTRICT WHERE STATUS ='A' AND DISTRICT_ID = '" + intDistrictId + "' AND SUBDISTRICT_NAME = " + strSubdistrictName + " ",1);
                    if (rsSubdistrict.RecordCount > 0)
                    {
                        intSubdistrictId = rsSubdistrict["SUBDISTRICT_ID"].Int32Value;
                    }
                    else
                    {
                        //find '-' district
                        Recordset rsSubDistrictAlt = conProd.GetRecordset("SELECT SUBDISTRICT_ID FROM TBL_MD_SUBDISTRICT WHERE STATUS = 'A' AND SUBDISTRICT_NAME = '-' AND DISTRICT_ID = " + intDistrictId + " ",1);
                        if (rsSubDistrictAlt.RecordCount > 0)
                        {
                            intSubdistrictId = rsSubDistrictAlt["DISTRICT_ID"].Int32Value;
                        }
                        else
                        {
                            intSubdistrictId = 0;
                        }
                        rsSubDistrictAlt.Close();
                    }
                    rsSubdistrict.Close();
                }
                return intSubdistrictId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intSubdistrictId;
            }

        }
        //======================================================== END DUT EDIT =====================================================================



        private int GetProvinceId(string strProvinceCode)
        {
            int intProvinceId = 0;
            try
            {
                if (strProvinceCode != "" && strProvinceCode != "0")
                {
                    strProvinceCode = this.GetProvinceCodeFromR04ProvinceCode(strProvinceCode); // ***
                    string strConnectionString = "ViewConnectionString";
                    string strDbTableName = "dbo.TBL_MD_PROVINCE";
                    string strPrimaryKey = "province_id";
                    // string strSql = "select province_id from " + strDbTableName + " where province_name='" + strProvinceName + "'";
                    string strSql = "select province_id from " + strDbTableName + " where province_code='" + strProvinceCode + "'";
                    DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                    if (dtsResult != null)
                    {
                        intProvinceId = Convert.ToInt32(dtsResult.Tables[0].Rows[0][0].ToString());
                    }
                }
                return intProvinceId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intProvinceId;
            }
        }

        private int GetDistrictIdFromDistrictCodeTCGAmpureId(string strTCGAmpureId)
        {
            int intDistrictId = 0;
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_DISTRICT";
            string strPrimaryKey = "district_id";
            string strSql = "select district_id from " + strDbTableName + " where district_code='" + strTCGAmpureId + "'";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            intDistrictId = Convert.ToInt32(tempData.ToString());
            return intDistrictId;
        }

        private int GetSubDistrictIdFromSubDistrictCodeTCGDistinctId(string strTCGDistinctId)
        {
            int intSubDistrictId = 0;
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_SUBDISTRICT";
            string strPrimaryKey = "subdistrict_id";
            string strSql = "select subdistrict_id from " + strDbTableName + " where subdistrict_code='" + strTCGDistinctId + "'";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            intSubDistrictId = Convert.ToInt32(tempData.ToString());
            return intSubDistrictId;
        }

        private int GetDistrictId(string strDistrictName)
        {
            int intDistrictId = 0;
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_DISTRICT";
            string strPrimaryKey = "district_id";
            string strSql = "select district_id from " + strDbTableName + " where district_name='" + strDistrictName + "'";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            intDistrictId = Convert.ToInt32(tempData.ToString());
            return intDistrictId;
        }

        private int GetSubDistrictId(string strSubDistrictName)
        {
            int intSubDistrictId = 0;
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_SUBDISTRICT";
            string strPrimaryKey = "subdistrict_id";
            string strSql = "select subdistrict_id from " + strDbTableName + " where subdistrict_name='" + strSubDistrictName + "'";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            var tempData = dtsResult.Tables[0].Rows.Count == 0 ? 0 : dtsResult.Tables[0].Rows[0][0];
            intSubDistrictId = Convert.ToInt32(tempData.ToString());
            return intSubDistrictId;
        }

        private string GetBankBrnUseLimit(string strT01Branch_Code, string strBankId)
        {
            int intTempBranch = 0;
            string strBranchCode = "";
            string strBranchId = "";
            if (strT01Branch_Code == "")
            {
                return "";
            }
            else
            {
                
                if (strT01Branch_Code.Length <= 4)
                {
                    if (int.TryParse(strT01Branch_Code, out intTempBranch))
                    {
                        if (strBankId != "71" && strBankId != "67")
                        { // other than Thai Credit Bank, Tisco ...
                            strBranchCode = intTempBranch.ToString("0000");
                        }
                        else
                        {
                            strBranchCode = strT01Branch_Code;
                        }
                    }
                    else
                    {
                        strBranchCode = strT01Branch_Code;
                    }
                    //strBranchCode = Convert.ToInt32(strT01Branch_Code).ToString("0000");
                }
                //Dut Edit 2020/08/30
                else
                {
                    strBranchCode = strT01Branch_Code;
                }

                //strBranchCode = strT01Branch_Code;
                string strConnectionString = "ViewConnectionString";
                string strDbTableName = "dbo.TBL_MD_BRANCH";
                string strPrimaryKey = "bank_id";
                string strSql = "select branch_id from " + strDbTableName + " where branch_code='" + strBranchCode + "' and bank_id='" + strBankId + "'";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                var tempData = dtsResult.Tables[0].Rows.Count == 0 ? null : dtsResult.Tables[0].Rows[0][0];
                strBranchId = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
                return strBranchId;
            }
        }

        private string GetBankBrnSendOper(string strT01Branch_Code, string strBankId)
        {
            int intTempBranch = 0;
            string strBranchCode = "";
            string strBranchId = "";

            if (strT01Branch_Code == "")
            {
                return "";
            }
            else
            {
                
                if (strT01Branch_Code.Length <= 4)
                {
                    if (int.TryParse(strT01Branch_Code, out intTempBranch))
                    {
                        if (strBankId != "71" && strBankId != "67")
                        { // other than Thai Credit Bank, Tisco, ...
                            strBranchCode = intTempBranch.ToString("0000");
                        }
                        else
                        {
                            strBranchCode = strT01Branch_Code;
                        }
                    }
                    else
                    {
                        strBranchCode = strT01Branch_Code;
                    }
                    //strBranchCode = Convert.ToInt32(strT01Branch_Code).ToString("0000");
                }
                //Dut Edit 2020/08/30
                else
                {
                    strBranchCode = strT01Branch_Code;
                }

                string strConnectionString = "ViewConnectionString";
                string strDbTableName = "dbo.TBL_MD_BRANCH";
                string strPrimaryKey = "bank_id";
                string strSql = "select branch_id from " + strDbTableName + " where branch_code='" + strBranchCode + "' and bank_id='" + strBankId + "'";
                DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
                var tempData = dtsResult.Tables[0].Rows.Count == 0 ? null : dtsResult.Tables[0].Rows[0][0];
                strBranchId = ((tempData == null) || (tempData == "")) ? "" : tempData.ToString();
                return strBranchId;
            }
        }

        private int GetBankId(string strT01Bank_Code)
        {
            string strBankCode = "";
            if (strT01Bank_Code.Length <= 3)
            {
                int intT01Bank_Code = Convert.ToInt32(strT01Bank_Code);
                strBankCode = intT01Bank_Code.ToString("000");
            }
            string strConnectionString = "ViewConnectionString";
            string strDbTableName = "dbo.TBL_MD_BANK";
            string strPrimaryKey = "bank_id";
            string strSql = "select bank_id from " + strDbTableName + " where bank_code='" + strBankCode + "'";
            DataSet dtsResult = GetData(strSql, strDbTableName, strPrimaryKey, strConnectionString);
            int intBankId = Convert.ToInt32(dtsResult.Tables[0].Rows[0][0].ToString());
            return intBankId;
        }
        private string CheckWhetherShouldMoveOnAfterPreScreening(string strPk)
        {
            string strResultOfPreScreening = "";
            string strConnectionString = "CGConnectionString";
            string strTableName = "dbo.t01_request_online";
            string strPrimaryKey = "dbo.t01_request_online.t01online_id";
            string strJsonResultFromRequestNo37 = "";
            DateTime today = DateTime.Now;
            string strBuddhistYear = (today.Year + 543).ToString();
            string strMonth = today.Month.ToString("00");
            string strDate = today.Day.ToString("00");
            string strFullDate = strBuddhistYear + strMonth + strDate;
            int intTempBankId = 0;
            int intTempProductId = 0;
            DataSet dtsTempPreScreening = null;
            try
            {
                string strSql = string.Format("select top(1) T01Online_ID, T01Project_Type, T01Bank_Code from {0} where (T01Online_ID='{1}');", strTableName, strPk);
                //dtsTemp = await Task.Run(() => GetData(strSql, strTableName, strPrimaryKey, strConnectionString));
                dtsTempPreScreening = GetData(strSql, strTableName, strPrimaryKey, strConnectionString);
                // Get bankId
                var tempData = dtsTempPreScreening.Tables[0].Rows[0]["T01Bank_Code"];
                string strTempBankCode = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
                strTempBankCode = strTempBankCode.Trim();
                intTempBankId = GetBankId(strTempBankCode); // Add
                // Get productId
                tempData = dtsTempPreScreening.Tables[0].Rows[0]["T01Project_Type"];
                string strTempProductId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
                intTempProductId = Convert.ToInt32(strTempProductId);
                strJsonResultFromRequestNo37 = this.JSONForPreScreening(dtsTempPreScreening);
                // Getting Result from No 37 to send as part of No 38 Request
                if (strJsonResultFromRequestNo37 != "")
                {
                    BeforePreScreeningNo37 bp37 = JsonSerializer.Deserialize<BeforePreScreeningNo37>(strJsonResultFromRequestNo37);
                    List<ResultItem37> lstResultItem37 = bp37.result;
                    //foreach (var item in lstResultItem37) { // add "2" as "Yo" said
                    //    item.value = "2"; // can put in constructor
                    //}
                    int intTotalMemberOfListResultItem37 = lstResultItem37.Count;
                    string strPreScreeningData = JsonSerializer.Serialize<List<ResultItem37>>(lstResultItem37, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true });
                    // put detail in No 38 object
                    rp38 = new RequestPostPreScreeningNo38();
                    rp38.result = null; // not use (P.139 of official document)
                    rp38.bankId = intTempBankId;
                    rp38.productId = intTempProductId;
                    rp38.detail = new Detail[intTotalMemberOfListResultItem37];
                    for (int i = 0; i < intTotalMemberOfListResultItem37; i++)
                    {
                        rp38.detail[i] = new Detail();
                        rp38.detail[i].screeningTpInfId = lstResultItem37[i].screeningTpInfId;
                        rp38.detail[i].screeningCode = lstResultItem37[i].screeningCode;
                        rp38.detail[i].screeningId = lstResultItem37[i].screeningId;
                        rp38.detail[i].screeningNameField = lstResultItem37[i].screeningNameField;
                        rp38.detail[i].value = lstResultItem37[i].value;
                    }
                    // serialize to json
                    string strJSONForPOSTRequestNo38 = JsonSerializer.Serialize<RequestPostPreScreeningNo38>(rp38);
                    string strUrl = strWorkServerURL + "/request-service/api/request/pre-screening/check";
                    string strContentType = "application/json";
                    string strParameter0 = strJSONForPOSTRequestNo38;
                    string strJSONFromPOSTRequestNo38 = this.GetStringFromPOSTRequestWithTokenToRequestBody(strUrl, strContentType, strParameter0);
                    // deserialize
                    ResponsePostPreScreeningNo38 rpo38 = JsonSerializer.Deserialize<ResponsePostPreScreeningNo38>(strJSONFromPOSTRequestNo38);
                    strResultOfPreScreening = rpo38.result;
                }
                /* ไม่ได้ใช้
                DateTime dtmNow = DateTime.Now;
                string strPrefix = string.Format("{0}{1:00}{2:00}_{3:00}{4:00}{5:00}_", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);
                this.lblJSONInput.Text = strJsonResultFromRequestNo37;
                this.WriteToFile(strJsonResultFromRequestNo37, strPrefix + "Json4Request.json");
                this.btnHiddenButton_Click(null, null);
                strFinalJsonResponse = this.WriteRequest(strJsonResultFromRequestNo37);
                this.lblJSONOutput.Text = strFinalJsonResponse;
                this.WriteToFile(strFinalJsonResponse, strPrefix + "JsonResponse.json");
                stkToken.Clear();
                strPk = "";
                */
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            return strResultOfPreScreening;
        }
        private string JSONForPreScreening(DataSet dtsTemp)
        {
            string strJsonFromNo37 = "";
            string strScreeningTpId = "";
            BeforePreScreeningNo24 bp24 = new BeforePreScreeningNo24();
            DataTable dtb = dtsTemp.Tables[0];
            var tempData = dtb.Rows[0]["T01Project_Type"];
            string strTempProductId = ((tempData == null) || (tempData == "")) ? "0" : tempData.ToString();
            int intTempProductId = Convert.ToInt32(strTempProductId);
            if (intTempProductId != 0)
            {
                strScreeningTpId = this.GetScreeningTpId(intTempProductId);
            }
            // if ScreeningTpId != null, call No37
            string strUrl = strWorkServerURL + "/master-service/api/screening-tp-inf";
            string strContentType = "application/json";
            if ((strScreeningTpId != null) && (strScreeningTpId != ""))
            {
                strUrl += "?screeningTpId=" + strScreeningTpId;
            }
            strJsonFromNo37 = this.GetStringFromGETRequestWithTokenToRequestBody(strUrl, strContentType);
            return strJsonFromNo37;
        }

        private string GetScreeningTpId(int intTempProductId)
        {
            string strScreeningTpId = null;
            string strUrl = strWorkServerURL + "/product-service/api/products/product-dropdowns/1/A?guaType=I";
            string strContentType = "application/json";
            string strJsonResponse = this.GetStringFromGETRequestWithTokenToRequestBody(strUrl, strContentType);
            List<BeforePreScreeningNo24> lstBp24 = JsonSerializer.Deserialize<List<BeforePreScreeningNo24>>(strJsonResponse, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true });
            foreach (var item in lstBp24)
            {
                if (item.productId == intTempProductId)
                {
                    strScreeningTpId = item.screeningTpId.ToString();
                    break;
                }
            }
            return strScreeningTpId;
        }

        private int[] GetFileListFromProductId(string strProductId)
        {
            int[] intFileTypeIdArray = null;
            try
            {
                string strUrl = strWorkServerURL + "/request-service/api/request/file?productId=" + strProductId;
                string strContentType = "application/json";
                string strJsonResponse = this.GetStringFromGETRequestWithTokenToRequestBody(strUrl, strContentType);
                FileGroup fg = JsonSerializer.Deserialize<FileGroup>(strJsonResponse, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) });
                if (fg.content != null)
                {
                    int intNoOfFiles = fg.content.Length;
                    intFileTypeIdArray = new int[intNoOfFiles];
                    for (int i = 0; i < intFileTypeIdArray.Length; i++)
                    {
                        intFileTypeIdArray[i] = fg.content[i].fileTypeId;
                    }
                }
                return intFileTypeIdArray;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
                return null;
            }
        }

        // public async void GetDataFromCriteria(string strPk) {
        public string GetDataFromT01Online_ID(string strPk)
        {
            Console.WriteLine("Reading data for " + strPk + "...");
            try
            {
                if (this.CheckingUsedToBeFired(strPk) == true)
                { // repete already
                    UpdateImportStatusDb(strPk, "8");
                    return "-888";
                }
                if (this.IsProjectHavingSendDate(strPk) == false)
                { // not complete yet
                    return "-444";
                }
                if (this.CheckingIsValidProduct(strPk) == false)
                { // less than 900
                    UpdateImportStatusDb(strPk, "6");
                    return "-666";
                }
                if (this.IsProjectType995(strPk))
                {
                    if (this.IsInExcelList995(strPk) == true)
                    {
                        if (this.IsPaidFor995(strPk) == false)
                        {
                            if (this.Is995NotPaidIn3Days(strPk) == false)
                            { // not paid for less than 3 days
                                UpdateImportStatusDb(strPk, "3"); // not paid
                                return "-333"; // less than 3 day, forget about it first
                            }
                            else
                            { // not paid for more than 3 days
                                UpdateImportStatusDb(strPk, "NP3"); // let it go //NP3 ไม่มีการชำระเงินเกิน 3 วัน
                                _RejectFlag = "2";
                            }
                        }
                        else
                        {
                            UpdateImportStatusDb(strPk, "N0"); //N0 ชำระเงินแล้ว รอส่ง
                            _RejectFlag = "";
                        }
                    }
                    else
                    {
                        UpdateImportStatusDb(strPk, "NEX"); //NEX ไม่มีใน Excel แบงค์ชาติ
                        _RejectFlag = "1"; // is not in excel
                    }
                }
                ReadDebtorJSON();
                string strConnectionString = "CGConnectionString";
                string strTableName = "dbo.t01_request_online";
                //string strPrimaryKey = "dbo.t01_request_online.t01online_id";
                string strPrimaryKey = "dbo.t01_request_online.t01request_no";
                string strJsonResult4Request = "";
                DateTime today = DateTime.Now;
                string strBuddhistYear = (today.Year + 543).ToString();
                string strMonth = today.Month.ToString("00");
                string strDate = today.Day.ToString("00");
                string strFullDate = strBuddhistYear + strMonth + strDate;
                //string strSql = string.Format("select top(1) T01Last_Status, T01Online_ID, T01Send_Date, T01Send_Time, T01Ref_1, T01Ref_2, T01Project_Type, T01Bank_Code, T01Branch_Code, T01Brn_Cnt_Person, T01Bank_Cnt_Telephone, T01Bank_Cnt_Mobile, T01Bank_Cnt_Email, T89Enterprise_No, T89Enterprise_Soi, T89Enterprise_Road, T89Enterprise_Distinct_ID, T89Enterprise_Ampure_ID, T89Enterprise_Province, T89Enterprise_Zip_Code, T01Title_Name_Thai, T01Name_Thai, T01Surname_Thai, T01Marital_Status, T01Customer_Type, T01Card_Type, T01Card_ID1, T01House_Num, T01House_Soi, T01House_Road, T01House_Distinct_ID, T01House_Ampure_ID, T01House_Province, T01House_Zip_Code, T01House_Phone, T01House_Fax, T01House_Mobile, T01House_Email, T01Title_Name_Thai2, T01Name_Thai2, T01Surname_Thai2, T01Card_ID2, T01House_Num2, T01House_Soi2, T01House_Road2, T01House_Distinct2, T01House_Ampure2, T01House_Province2, T01House_Zip_Code2, T01House_Phone2, T01House_Fax2, T01House_Mobile2, T01House_Email2, T02Title_Name_Thai, T02Name_Thai, T02Surname_Thai, T02Marital_Status, T02Card_Type, T02Card_ID1, T02House_Num, T02House_Soi, T02House_Road, T02House_Distinct_ID, T02House_Ampure_ID, T02House_Province, T02House_Zip_Code, T02House_Phone, T02House_Fax, T02House_Mobile, T02House_Email, T02Title_Name_Thai2, T02Name_Thai2, T02Surname_Thai2, T02Card_ID2, T02House_Num2, T02House_Soi2, T02House_Road2, T02House_Distinct2, T02House_Ampure2, T02House_Province2, T02House_Zip_Code2, T02House_Phone2, T02House_Fax2, T02House_Mobile2, T02House_Email2, T03Title_Name_Thai, T03Name_Thai, T03Surname_Thai, T03Marital_Status, T03Card_Type, T03Card_ID1, T03House_Num, T03House_Soi, T03House_Road, T03House_Distinct_ID, T03House_Ampure_ID, T03House_Province, T03House_Zip_Code, T03House_Phone, T03House_Fax, T03House_Mobile, T03House_Email, T03Title_Name_Thai2, T03Name_Thai2, T03Surname_Thai2, T03Card_ID2, T03House_Num2, T03House_Soi2, T03House_Road2, T03House_Distinct2, T03House_Ampure2, T03House_Province2, T03House_Zip_Code2, T03House_Phone2, T03House_Fax2, T03House_Mobile2, T03House_Email2, T04Title_Name_Thai, T04Name_Thai, T04Surname_Thai, T04Marital_Status, T04Card_Type, T04Card_ID1, T04House_Num, T04House_Soi, T04House_Road, T04House_Distinct_ID, T04House_Ampure_ID, T04House_Province, T04House_Zip_Code, T04House_Phone, T04House_Fax, T04House_Mobile, T04House_Email, T04Title_Name_Thai2, T04Name_Thai2, T04Surname_Thai2, T04Card_ID2, T04House_Num2, T04House_Soi2, T04House_Road2, T04House_Distinct2, T04House_Ampure2, T04House_Province2, T04House_Zip_Code2, T04House_Phone2, T04House_Fax2, T04House_Mobile2, T04House_Email2, T05Title_Name_Thai, T05Name_Thai, T05Surname_Thai, T05Marital_Status, T05Card_Type, T05Card_ID1, T05House_Num, T05House_Soi, T05House_Road, T05House_Distinct_ID, T05House_Ampure_ID, T05House_Province, T05House_Zip_Code, T05House_Phone, T05House_Fax, T05House_Mobile, T05House_Email, T05Title_Name_Thai2, T05Name_Thai2, T05Surname_Thai2, T05Card_ID2, T05House_Num2, T05House_Soi2, T05House_Road2, T05House_Distinct2, T05House_Ampure2, T05House_Province2, T05House_Zip_Code2, T05House_Phone2, T05House_Fax2, T05House_Mobile2, T05House_Email2, T01Industry_Name, T01Staff_Amount, T01Staff_Amount_Inc, T01Asset_Money, T01Loan_Subject_1, T01Loan_Amount_1, T01Request_Amount_1, T01Loan_Type_1, T01Loan_Subject_2, T01Loan_Amount_2, T01Request_Amount_2, T01Loan_Type_2, T01Loan_Subject_3, T01Loan_Amount_3, T01Request_Amount_3, T01Loan_Type_3, T01Loan_Subject_4, T01Loan_Amount_4, T01Request_Amount_4, T01Loan_Type_4, T01Loan_Amount, T01Request_Amount, T01File_1, T01File_2, T01File_3, T01File_4, T01File_5, T01File_6, T01File_7, T01File_8, T01File_9, T01Investment_Objective_1, T01Debt_Year_1, T01Debt_Define_1, T01ContractNo_1, T01Investment_Objective_2, T01Debt_Year_2, T01Debt_Define_2, T01ContractNo_2, T01Investment_Objective_3, T01Debt_Year_3, T01Debt_Define_3, T01ContractNo_3, T01Investment_Objective_4, T01Debt_Year_4, T01Debt_Define_4, T01ContractNo_4, T01ISIC_Code, T01Education, T02Education, T03Education, T04Education, T05Education, T01Not_Reduce, T01Census_Num, T01Census_Soi, T01Census_Road, T01Census_Distinct, T01Census_Ampure, T01Census_Province, T01Census_Zip_Code, T02Census_Num, T02Census_Soi, T02Census_Road, T02Census_Distinct, T02Census_Ampure, T02Census_Province, T02Census_Zip_Code, T03Census_Num, T03Census_Soi, T03Census_Road, T03Census_Distinct, T03Census_Ampure, T03Census_Province, T03Census_Zip_Code, T04Census_Num, T04Census_Soi, T04Census_Road, T04Census_Distinct, T04Census_Ampure, T04Census_Province, T04Census_Zip_Code, T05Census_Num, T05Census_Soi, T05Census_Road, T05Census_Distinct, T05Census_Ampure, T05Census_Province, T05Census_Zip_Code, T01Birth_Date, T02Birth_Date, T03Birth_Date, T04Birth_Date, T05Birth_Date, T01Asset_Money_Building, T01Asset_Money_Machine, T01Project_Character, T01DSCR, T01Experience_Direct, T01Start_Date_Business, T01Request_No, T01_Year_Later, T01_Year_Now, T01_1Year_Next, T01BOT_Account_Classify, T01BOTAccountClassify_Date, T01Receive_Loan_First, T01Fee_Before, T01KnowTCG_Seminar, T01KnowTCG_Booth, T01KnowTCG_Suggest, T01KnowTCG_Media,T01KnowTCG_Bank, T01KnowTCG_Gov, T01UseTCG_Easy, T01UseTCG_Many, T01UseTCG_Person, T01UseTCG_NoBank, T01UseTCG_Product, T01UseTCG_Policy, T01Dont_Info, T01Dont_Step, T01Dont_Rules, T01NotEnough_Money, T01Dont_Have, T01Total_Asset, T01Total_Debt, T01CostEstimate, T01Total_Loan_Amount from {0} where ((T01Online_ID='{1}') and (T01Last_Status='010'));", strTableName, strPk); //  and (T01Last_Status='010')


                //string strSql = string.Format("select top(1) T01Last_Status, T01Online_ID, T01Send_Date, T01Send_Time, T01Ref_1, T01Ref_2, T01Project_Type, T01Bank_Code, T01Branch_Code, T01Brn_Cnt_Person, T01Bank_Cnt_Telephone, T01Bank_Cnt_Mobile, T01Bank_Cnt_Email, T89Enterprise_No, T89Enterprise_Soi, T89Enterprise_Road, T89Enterprise_Distinct_ID, T89Enterprise_Ampure_ID, T89Enterprise_Province, T89Enterprise_Zip_Code, T01Title_Name_Thai, T01Name_Thai, T01Surname_Thai, T01Marital_Status, T01Customer_Type, T01Card_Type, T01Card_ID1, T01House_Num, T01House_Soi, T01House_Road, T01House_Distinct_ID, T01House_Ampure_ID, T01House_Province, T01House_Zip_Code, T01House_Phone, T01House_Fax, T01House_Mobile, T01House_Email, T01Title_Name_Thai2, T01Name_Thai2, T01Surname_Thai2, T01Card_ID2, T01House_Num2, T01House_Soi2, T01House_Road2, T01House_Distinct2, T01House_Ampure2, T01House_Province2, T01House_Zip_Code2, T01House_Phone2, T01House_Fax2, T01House_Mobile2, T01House_Email2, T02Title_Name_Thai, T02Name_Thai, T02Surname_Thai, T02Marital_Status, T02Card_Type, T02Card_ID1, T02House_Num, T02House_Soi, T02House_Road, T02House_Distinct_ID, T02House_Ampure_ID, T02House_Province, T02House_Zip_Code, T02House_Phone, T02House_Fax, T02House_Mobile, T02House_Email, T02Title_Name_Thai2, T02Name_Thai2, T02Surname_Thai2, T02Card_ID2, T02House_Num2, T02House_Soi2, T02House_Road2, T02House_Distinct2, T02House_Ampure2, T02House_Province2, T02House_Zip_Code2, T02House_Phone2, T02House_Fax2, T02House_Mobile2, T02House_Email2, T03Title_Name_Thai, T03Name_Thai, T03Surname_Thai, T03Marital_Status, T03Card_Type, T03Card_ID1, T03House_Num, T03House_Soi, T03House_Road, T03House_Distinct_ID, T03House_Ampure_ID, T03House_Province, T03House_Zip_Code, T03House_Phone, T03House_Fax, T03House_Mobile, T03House_Email, T03Title_Name_Thai2, T03Name_Thai2, T03Surname_Thai2, T03Card_ID2, T03House_Num2, T03House_Soi2, T03House_Road2, T03House_Distinct2, T03House_Ampure2, T03House_Province2, T03House_Zip_Code2, T03House_Phone2, T03House_Fax2, T03House_Mobile2, T03House_Email2, T04Title_Name_Thai, T04Name_Thai, T04Surname_Thai, T04Marital_Status, T04Card_Type, T04Card_ID1, T04House_Num, T04House_Soi, T04House_Road, T04House_Distinct_ID, T04House_Ampure_ID, T04House_Province, T04House_Zip_Code, T04House_Phone, T04House_Fax, T04House_Mobile, T04House_Email, T04Title_Name_Thai2, T04Name_Thai2, T04Surname_Thai2, T04Card_ID2, T04House_Num2, T04House_Soi2, T04House_Road2, T04House_Distinct2, T04House_Ampure2, T04House_Province2, T04House_Zip_Code2, T04House_Phone2, T04House_Fax2, T04House_Mobile2, T04House_Email2, T05Title_Name_Thai, T05Name_Thai, T05Surname_Thai, T05Marital_Status, T05Card_Type, T05Card_ID1, T05House_Num, T05House_Soi, T05House_Road, T05House_Distinct_ID, T05House_Ampure_ID, T05House_Province, T05House_Zip_Code, T05House_Phone, T05House_Fax, T05House_Mobile, T05House_Email, T05Title_Name_Thai2, T05Name_Thai2, T05Surname_Thai2, T05Card_ID2, T05House_Num2, T05House_Soi2, T05House_Road2, T05House_Distinct2, T05House_Ampure2, T05House_Province2, T05House_Zip_Code2, T05House_Phone2, T05House_Fax2, T05House_Mobile2, T05House_Email2, T01Industry_Name, T01Staff_Amount, T01Staff_Amount_Inc, T01Asset_Money, T01Loan_Subject_1, T01Loan_Amount_1, T01Request_Amount_1, T01Loan_Type_1, T01Loan_Subject_2, T01Loan_Amount_2, T01Request_Amount_2, T01Loan_Type_2, T01Loan_Subject_3, T01Loan_Amount_3, T01Request_Amount_3, T01Loan_Type_3, T01Loan_Subject_4, T01Loan_Amount_4, T01Request_Amount_4, T01Loan_Type_4, T01Loan_Amount, T01Request_Amount, T01File_1, T01File_2, T01File_3, T01File_4, T01File_5, T01File_6, T01File_7, T01File_8, T01File_9, T01Investment_Objective_1, T01Debt_Year_1, T01Debt_Define_1, T01ContractNo_1, T01Investment_Objective_2, T01Debt_Year_2, T01Debt_Define_2, T01ContractNo_2, T01Investment_Objective_3, T01Debt_Year_3, T01Debt_Define_3, T01ContractNo_3, T01Investment_Objective_4, T01Debt_Year_4, T01Debt_Define_4, T01ContractNo_4, T01ISIC_Code, T01Education, T02Education, T03Education, T04Education, T05Education, T01Not_Reduce, T01Census_Num, T01Census_Soi, T01Census_Road, T01Census_Distinct, T01Census_Ampure, T01Census_Province, T01Census_Zip_Code, T02Census_Num, T02Census_Soi, T02Census_Road, T02Census_Distinct, T02Census_Ampure, T02Census_Province, T02Census_Zip_Code, T03Census_Num, T03Census_Soi, T03Census_Road, T03Census_Distinct, T03Census_Ampure, T03Census_Province, T03Census_Zip_Code, T04Census_Num, T04Census_Soi, T04Census_Road, T04Census_Distinct, T04Census_Ampure, T04Census_Province, T04Census_Zip_Code, T05Census_Num, T05Census_Soi, T05Census_Road, T05Census_Distinct, T05Census_Ampure, T05Census_Province, T05Census_Zip_Code, T01Birth_Date, T02Birth_Date, T03Birth_Date, T04Birth_Date, T05Birth_Date, T01Asset_Money_Building, T01Asset_Money_Machine, T01Project_Character, T01DSCR, T01Experience_Direct, T01Start_Date_Business, T01Request_No, T01_Year_Later, T01_Year_Now, T01_1Year_Next, T01BOT_Account_Classify, T01BOTAccountClassify_Date, T01Receive_Loan_First, T01Fee_Before, T01KnowTCG_Seminar, T01KnowTCG_Booth, T01KnowTCG_Suggest, T01KnowTCG_Media,T01KnowTCG_Bank, T01KnowTCG_Gov, T01UseTCG_Easy, T01UseTCG_Many, T01UseTCG_Person, T01UseTCG_NoBank, T01UseTCG_Product, T01UseTCG_Policy, T01Dont_Info, T01Dont_Step, T01Dont_Rules, T01NotEnough_Money, T01Dont_Have, T01Total_Asset, T01Total_Debt, T01CostEstimate, T01Total_Loan_Amount from {0} where ((T01Online_ID='{1}') );", strTableName, strPk); //  and (T01Last_Status='010')
                //Dut Add and change T01Contract_xx
                string strSql = string.Format("select top(1) T01Last_Status, T01Online_ID, T01Send_Date, T01Send_Time, T01Ref_1, T01Ref_2, T01Project_Type, T01Bank_Code, T01Branch_Code, T01Brn_Cnt_Person, T01Bank_Cnt_Telephone, T01Bank_Cnt_Mobile, T01Bank_Cnt_Email, T89Enterprise_No, T89Enterprise_Soi, T89Enterprise_Road, T89Enterprise_Distinct_ID, T89Enterprise_Ampure_ID, T89Enterprise_Province, T89Enterprise_Zip_Code, T01Title_Name_Thai, T01Name_Thai, T01Surname_Thai, T01Marital_Status, T01Customer_Type, T01Card_Type, T01Card_ID1, T01House_Num, T01House_Soi, T01House_Road, T01House_Distinct_ID, T01House_Ampure_ID, T01House_Province, T01House_Zip_Code, T01House_Phone, T01House_Fax, T01House_Mobile, T01House_Email, T01Title_Name_Thai2, T01Name_Thai2, T01Surname_Thai2, T01Card_ID2, T01House_Num2, T01House_Soi2, T01House_Road2, T01House_Distinct2, T01House_Ampure2, T01House_Province2, T01House_Zip_Code2, T01House_Phone2, T01House_Fax2, T01House_Mobile2, T01House_Email2, T02Title_Name_Thai, T02Name_Thai, T02Surname_Thai, T02Marital_Status, T02Card_Type, T02Card_ID1, T02House_Num, T02House_Soi, T02House_Road, T02House_Distinct_ID, T02House_Ampure_ID, T02House_Province, T02House_Zip_Code, T02House_Phone, T02House_Fax, T02House_Mobile, T02House_Email, T02Title_Name_Thai2, T02Name_Thai2, T02Surname_Thai2, T02Card_ID2, T02House_Num2, T02House_Soi2, T02House_Road2, T02House_Distinct2, T02House_Ampure2, T02House_Province2, T02House_Zip_Code2, T02House_Phone2, T02House_Fax2, T02House_Mobile2, T02House_Email2, T03Title_Name_Thai, T03Name_Thai, T03Surname_Thai, T03Marital_Status, T03Card_Type, T03Card_ID1, T03House_Num, T03House_Soi, T03House_Road, T03House_Distinct_ID, T03House_Ampure_ID, T03House_Province, T03House_Zip_Code, T03House_Phone, T03House_Fax, T03House_Mobile, T03House_Email, T03Title_Name_Thai2, T03Name_Thai2, T03Surname_Thai2, T03Card_ID2, T03House_Num2, T03House_Soi2, T03House_Road2, T03House_Distinct2, T03House_Ampure2, T03House_Province2, T03House_Zip_Code2, T03House_Phone2, T03House_Fax2, T03House_Mobile2, T03House_Email2, T04Title_Name_Thai, T04Name_Thai, T04Surname_Thai, T04Marital_Status, " +
                    "T04Card_Type, T04Card_ID1, T04House_Num, T04House_Soi, T04House_Road, T04House_Distinct_ID, T04House_Ampure_ID, T04House_Province, T04House_Zip_Code, T04House_Phone, T04House_Fax, T04House_Mobile, T04House_Email, T04Title_Name_Thai2, T04Name_Thai2, T04Surname_Thai2, T04Card_ID2, T04House_Num2, T04House_Soi2, T04House_Road2, T04House_Distinct2, T04House_Ampure2, T04House_Province2, T04House_Zip_Code2, T04House_Phone2, T04House_Fax2, T04House_Mobile2, T04House_Email2, T05Title_Name_Thai, T05Name_Thai, T05Surname_Thai, T05Marital_Status, T05Card_Type, T05Card_ID1, T05House_Num, T05House_Soi, T05House_Road, T05House_Distinct_ID, T05House_Ampure_ID, T05House_Province, T05House_Zip_Code, T05House_Phone, T05House_Fax, T05House_Mobile, T05House_Email, T05Title_Name_Thai2, T05Name_Thai2, T05Surname_Thai2, T05Card_ID2, T05House_Num2, T05House_Soi2, T05House_Road2, T05House_Distinct2, T05House_Ampure2, T05House_Province2, T05House_Zip_Code2, T05House_Phone2, T05House_Fax2, T05House_Mobile2, T05House_Email2, T01Industry_Name, T01Staff_Amount, T01Staff_Amount_Inc, T01Asset_Money, T01Loan_Subject_1, T01Loan_Amount_1, T01Request_Amount_1, T01Loan_Type_1, T01Loan_Subject_2, T01Loan_Amount_2, T01Request_Amount_2, T01Loan_Type_2, T01Loan_Subject_3, T01Loan_Amount_3, T01Request_Amount_3, T01Loan_Type_3, T01Loan_Subject_4, T01Loan_Amount_4, T01Request_Amount_4, T01Loan_Type_4, T01Loan_Amount, T01Request_Amount, T01File_1, T01File_2, T01File_3, T01File_4, T01File_5, T01File_6, T01File_7, T01File_8, T01File_9, T01Investment_Objective_1, T01Debt_Year_1, T01Debt_Define_1, T01ContractNo_1, T01Investment_Objective_2, T01Debt_Year_2, T01Debt_Define_2, T01ContractNo_2, T01Investment_Objective_3, T01Debt_Year_3, T01Debt_Define_3, T01ContractNo_3, T01Investment_Objective_4, T01Debt_Year_4, T01Debt_Define_4, T01ContractNo_4, T01ISIC_Code, T01Education, T02Education, T03Education, T04Education, T05Education, T01Not_Reduce, T01Census_Num, T01Census_Soi, T01Census_Road, T01Census_Distinct, T01Census_Ampure, T01Census_Province, T01Census_Zip_Code, T02Census_Num, T02Census_Soi, T02Census_Road, T02Census_Distinct, " +
                    "T02Census_Ampure, T02Census_Province, T02Census_Zip_Code, T03Census_Num, T03Census_Soi, T03Census_Road, T03Census_Distinct, T03Census_Ampure, T03Census_Province, T03Census_Zip_Code, T04Census_Num, T04Census_Soi, T04Census_Road, T04Census_Distinct, T04Census_Ampure, T04Census_Province, T04Census_Zip_Code, T05Census_Num, T05Census_Soi, T05Census_Road, T05Census_Distinct, T05Census_Ampure, T05Census_Province, T05Census_Zip_Code, T01Birth_Date, T02Birth_Date, T03Birth_Date, T04Birth_Date, T05Birth_Date, T01Asset_Money_Building, T01Asset_Money_Machine, T01Project_Character, T01DSCR, T01Experience_Direct, T01Start_Date_Business, T01Request_No, T01_Year_Later, T01_Year_Now, T01_1Year_Next, T01BOT_Account_Classify, T01BOTAccountClassify_Date, T01Receive_Loan_First, T01Fee_Before, T01KnowTCG_Seminar, T01KnowTCG_Booth, T01KnowTCG_Suggest, T01KnowTCG_Media,T01KnowTCG_Bank, T01KnowTCG_Gov, T01UseTCG_Easy, T01UseTCG_Many, T01UseTCG_Person, T01UseTCG_NoBank, T01UseTCG_Product, T01UseTCG_Policy, T01Dont_Info, T01Dont_Step, T01Dont_Rules, T01NotEnough_Money, T01Dont_Have, T01Total_Asset, " +
                    "T01Total_Debt, T01CostEstimate, T01Total_Loan_Amount ," +
                    "T01Contract_Soi,T01Contract_Road," +
                    "CASE WHEN T01Contract_Province IS NULL AND T01Contract_Ampure = 'พรเจริญ' THEN 411 ELSE T01Contract_Province END AS T01Contract_Province," +
                    "T01Contract_Ampure,T01Contract_Ampure_ID,T01Contract_Distinct_ID,T01Contract_Distinct,T01Contract_Zip_Code," +
                    " CASE WHEN CHARINDEX('หมู่', T01Contract_No) > 0 AND T01Contract_No NOT LIKE '%หมู่บ้าน%' AND T01Contract_No NOT LIKE '%หมู่ที่%' THEN SUBSTRING(T01Contract_No, 0, CHARINDEX('หมู่', T01Contract_No) - 1) ELSE T01Contract_No END AS addressNo, " +
                    " CASE WHEN CHARINDEX('หมู่', T01Contract_No) > 0 AND T01Contract_No NOT LIKE '%หมู่บ้าน%' AND T01Contract_No NOT LIKE '%หมู่ที่%' THEN SUBSTRING(T01Contract_No, CHARINDEX('หมู่', T01Contract_No), LEN(T01Contract_No)) " +
                    " WHEN CHARINDEX('หมู่', T01Contract_No) > 0 AND T01Contract_No NOT LIKE '%หมู่บ้าน%' AND T01Contract_No LIKE '%หมู่ที่%' THEN SUBSTRING(T01Contract_No, CHARINDEX('หมู่ที่', T01Contract_No), LEN(T01Contract_No))  " +
                    " WHEN CHARINDEX('หมู่', T01Contract_No) > 0 AND T01Contract_No NOT LIKE '%หมู่บ้าน%' AND T01Contract_No NOT LIKE '%หมู่ที่%' THEN SUBSTRING(T01Contract_No, CHARINDEX('หมู่', T01Contract_No), LEN(T01Contract_No)) ELSE NULL END AS addressMoo "+
                    " from {0} where ((T01Online_ID='{1}') )", strTableName, strPk); //  and (T01Last_Status='010')


                //dtsTemp = await Task.Run(() => GetData(strSql, strTableName, strPrimaryKey, strConnectionString));
                dtsTemp = GetData(strSql, strTableName, strPrimaryKey, strConnectionString);
                string laststatus = "";
                if (dtsTemp.Tables[0].Rows.Count > 0)
                {
                    laststatus = dtsTemp.Tables[0].Rows[0]["T01Last_Status"].ToString();
                    if (laststatus == "010" || laststatus=="140") //140 ปฏิเสธ ส่งเข้าใหม่
                    {
                        strJsonResult4Request = this.JSONFromSendOldToNew(dtsTemp);
                        DateTime dtmNow = DateTime.Now;
                        string strPrefix = string.Format("{0}{1:00}{2:00}_{3:00}{4:00}{5:00}_{6}_{7}_{8}_{9}", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second, _T01Online_ID, _preReqNumber, _productId, _T01Bank_Code);
                        //this.lblJSONInput.Text = strJsonResult4Request;
                        strResult += "JSON requested ..." + Environment.NewLine; // added for web services
                        strJSONRequestFilePath = this.WriteToFile(strJsonResult4Request, strPrefix + "Json4Request.json");
                        //this.btnHiddenButton_Click(null, null);


                        strFinalJsonResponse = this.WriteRequest(strJsonResult4Request);
                        string strFromWriteLogToDb = "";
                        //this.lblJSONOutput.Text = strFinalJsonResponse;
                        strResult += "JSON responsed ..." + Environment.NewLine; // added for web services
                        strJSONResponseFilePath = this.WriteToFile(strFinalJsonResponse, strPrefix + "JsonResponse.json");

                        if (strFinalJsonResponse.Length > 500)
                        {
                            strFromWriteLogToDb = WriteLogToDb(strFinalJsonResponse);
                            //this.lblJSONOutput.Text += "<hr/>" + "log to db: " + strFromWriteLogToDb + "<br/><hr/>";
                            strResult += "<hr/>" + "log to db: " + strFromWriteLogToDb + "<br/><hr/>" + Environment.NewLine;
                            string strTemp0 = this.UpdateImportStatusDb(strPk, "Y");
                            strResult += "update status to Y: " + strTemp0 + "<br/><hr/>" + Environment.NewLine;
                        }
                        else
                        {
                            Console.WriteLine("Recheck Checking " + strPk + " in CGS...");
                            string strTemp0 = "";
                            int dupl = CheckDuplInCGS(strPk);
                            if (dupl > 0)
                            {
                                if (_RejectFlag.Length > 0)
                                {
                                    strTemp0 = UpdateImportStatusDb(strPk, "YR" + _RejectFlag); //YRn เข้า CGS แบบปฏิเสธ
                                }
                                else
                                {
                                    strTemp0 = UpdateImportStatusDb(strPk, "Y"); //Y เข้า CGS
                                }
                            }
                            else
                            {
                                if (_RejectFlag.Length > 0)
                                {
                                    strTemp0 = UpdateImportStatusDb(strPk, "NR"+ _RejectFlag); //NRn แบบมี Reject ยิงแล้วไม่เข้า
                                }
                                else
                                {
                                    strTemp0 = UpdateImportStatusDb(strPk, "N"); //N ยิงแล้วไม่เข้า มี Error
                                }
                                strResult += "update status to N: " + strTemp0 + "<br/><hr/>" + Environment.NewLine;
                            }
                        }
                    }
                    else
                    {
                        string strTemp0 = this.UpdateImportStatusDb(strPk, "Y300");
                    }
                }
                stkToken.Clear();
                strPk = "";
                WriteLogFile(strResult);
                return strResult;

            
            }
            
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
                return strResult;
            }
            
            

        }

        public string GetT01Online_IDFromT01Request_No(string strT01Request_No)
        {
            string strT01Online_ID = "";
            DataSet dts = null;
            try
            {
                string strTableName = "[dbo].[T01_Request_Online]";
                string strCon = ConfigurationManager.ConnectionStrings["CGConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT T01Online_ID FROM {0} WHERE T01Request_No='{1}'", strTableName, strT01Request_No);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                dts = new DataSet("TempDS");
                dta.Fill(dts, strTableName);
                if (dts.Tables[strTableName].Rows[0][0] != null)
                {
                    strT01Online_ID = dts.Tables[strTableName].Rows[0][0].ToString();
                }
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
            }
            return strT01Online_ID;
        }


        // public async void GetDataFromCriteria(string strPk) {
        public string GetDataFromT01Request_No(string strPk)
        {
            try
            {
                string strT01Online_ID = GetT01Online_IDFromT01Request_No(strPk);
                if (this.CheckingUsedToBeFired(strT01Online_ID) == true)
                { // repete already
                    UpdateImportStatusDb(strT01Online_ID, "7");
                    return "-777";
                }
                strResult = GetDataFromT01Online_ID(strT01Online_ID);
                return strResult;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
                return strResult;
            }
        }

        public static DataSet GetData(string strSql, string strTableName, string strPrimaryKeyColumn, string strConnectionString)
        {
            DataSet dts = null;
            try
            {
                //string strTable = "[dbo].[_TempR09_Type]";
                string strCon = ConfigurationManager.ConnectionStrings[strConnectionString].ConnectionString;
                //string strSql = string.Format("SELECT TOP(100) * FROM {0}", strTable);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                dts = new DataSet("TempDS");
                dta.Fill(dts, strTableName);
                dts.Tables[strTableName].PrimaryKey = new DataColumn[] { dts.Tables[strTableName].Columns[strPrimaryKeyColumn] };
                strResult = dts.Tables[strTableName].Rows.Count.ToString() + " were sent.<br/>";
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                dts = null;
            }
            return dts;
        }
        private int GetAdvFeeYearIdFromProductIdAndBankId(string strProductId, string strBankId, string strAdvFeeyear)
        {
            int intAdvFeeYearId = 0;
            try
            {
                string strUrl = strWorkServerURL + string.Format("/master-service/api/mst-adv/year/{0}/{1}", strProductId, strBankId);
                string strContentType = "application/json";
                string strJsonResponse = this.GetStringFromGETRequestWithTokenToRequestBody(strUrl, strContentType);
                AdvanceFeeYear afy = JsonSerializer.Deserialize<AdvanceFeeYear>(strJsonResponse, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) });
                AvResult[] arArray = afy.result;
                if (arArray != null)
                {
                    foreach (var item in arArray)
                    {
                        if (item.advFeeYear == strAdvFeeyear)
                        {
                            return item.advFeeYearId;
                        }
                    }
                }
                return intAdvFeeYearId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intAdvFeeYearId;
            }
        }
        private int GetStatusOfT01Online_ID(string strProductId, string strBankId, string strAdvFeeyear)
        {
            int intAdvFeeYearId = 0;
            try
            {
                string strUrl = strWorkServerURL + string.Format("/master-service/api/mst-adv/year/{0}/{1}", strProductId, strBankId);
                string strContentType = "application/json";
                string strJsonResponse = this.GetStringFromGETRequestWithTokenToRequestBody(strUrl, strContentType);
                AdvanceFeeYear afy = JsonSerializer.Deserialize<AdvanceFeeYear>(strJsonResponse, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All) });
                AvResult[] arArray = afy.result;
                if (arArray != null)
                {
                    foreach (var item in arArray)
                    {
                        if (item.advFeeYear == strAdvFeeyear)
                        {
                            return item.advFeeYearId;
                        }
                    }
                }
                return intAdvFeeYearId;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return intAdvFeeYearId;
            }
        }

        string WriteLogToDb(string strFinalJsonResponse)
        {
            string strResult = "";
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };
            try
            {
                ResponseFinalJustProduct rfp = JsonSerializer.Deserialize<ResponseFinalJustProduct>(strFinalJsonResponse, options);
                _status = rfp.status;
                _requestId = rfp.content.requestId;
                _requestIdSeq = rfp.content.requestIdSeq.ToString();
                _preReqNumber = rfp.content.product.preReqNumber;
                _preReqStatus = rfp.content.product.preReqStatus;
                _preReqStatusStr = rfp.content.product.preReqStatusStr;
                _jsonRequestFileLocation = strJSONRequestFilePath;
                _jsonResponseFileLocation = strJSONResponseFilePath;
                DateTime dtmNow = DateTime.Now;
                _writtenDateTime = string.Format("{0}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);
                _writtenBy = "CyberInterface";
                ///*
                //                //string strTable = "[dbo].[_TempR09_Type]";
                //    string strCon = ConfigurationManager.ConnectionStrings[strConnectionString].ConnectionString;
                //    //string strSql = string.Format("SELECT TOP(100) * FROM {0}", strTable);
                //    SqlConnection con = new SqlConnection(strCon);
                //    SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                //    SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                //    dts = new DataSet("TempDS");
                //    dta.Fill(dts, strTableName);
                //    dts.Tables[strTableName].PrimaryKey = new DataColumn[] { dts.Tables[strTableName].Columns[strPrimaryKeyColumn] };
                //    strResult = dts.Tables[strTableName].Rows.Count.ToString() + " were sent.<br/>";
                //    con.Close();
                // */
                string strCon = ConfigurationManager.ConnectionStrings["logConnectionString"].ConnectionString;
                string strSql = string.Format("INSERT INTO [dbo].[TBL_CI_LOG] (T01Online_ID, T01Request_No, T01Send_Date, T01Project_Type, T01Name_Thai, T01Surname_Thai, productCode, productId, status, requestId, requestIdSeq, preReqNumber, preReqStatus, preReqStatusStr, jsonRequestFileLocation, jsonResponseFileLocation, writtenDateTime, writtenBy) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}', '{17}')", _T01Online_ID, _T01Request_No, _T01Send_Date, _T01Project_Type, _T01Name_Thai, _T01Surname_Thai, _productCode, _productId, _status, _requestId, _requestIdSeq, _preReqNumber, _preReqStatus, _preReqStatusStr, _jsonRequestFileLocation, _jsonResponseFileLocation, _writtenDateTime, _writtenBy);
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = new SqlCommand(strSql, con);
                con.Open();
                int intInsertedRow = cmd.ExecuteNonQuery();
                con.Close();
                strResult += intInsertedRow + " row was logged." + "<br/>";
                string strPreRequestNoOutput = string.Format("T01Request_No: {0} - Request Id: {1} - PreRequest No: {2} ({3} {4}) - Project Type: {5} - T01Online_ID: {6} - WrittenDateTime: {7}", _T01Request_No, _requestId, _preReqNumber, _T01Name_Thai, _T01Surname_Thai, _productId, _T01Online_ID, _writtenDateTime);
                WriteOutputFile(strPreRequestNoOutput);
                strResult += strPreRequestNoOutput + Environment.NewLine;
                return strResult;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return strResult;
            }
        }

        void CheckWhetherExistingInImportStatusDb(string strT01Online_ID, string[] strData)
        {
            int intRow = 0;
            string strTempResult = "";
            try
            {
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT COUNT(1) FROM [dbo].[TBL_CI_Import_Status] WHERE T01Online_ID='{0}'", strT01Online_ID);
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = new SqlCommand(strSql, con);
                con.Open();
                intRow = Convert.ToInt32(cmd.ExecuteScalar());
                con.Close();
                cmd = null;
                if (intRow <= 0)
                {
                    strSql = string.Format("INSERT INTO[dbo].[TBL_CI_Import_Status](T01Online_ID, T01Send_Date, T01Send_Time, T01Project_Type, T01Last_Status, T01Bank_Code, T01House_Province) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')", strT01Online_ID, strData[0], strData[1], strData[2], strData[3], strData[4], strData[5]);
                    cmd = new SqlCommand(strSql, con);
                    con.Open();
                    intRow = cmd.ExecuteNonQuery();
                    con.Close();
                    cmd = null;
                    strTempResult = string.Format("{0} new row from {1} was added to 'TBL_CI_Import_Status'", intRow.ToString(), strT01Online_ID);
                    WriteLogFile(strTempResult);
                }
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strTempResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                strResult += strTempResult;
                WriteLogFile(strTempResult);
            }
            return;
        }

        // _Temp_CI_Import_Temp
        void CheckWhetherExistingIn_Temp_CI_Import_TempDb(string strT01Online_ID, string[] strData)
        {
            int intRow = 0;
            string strTempResult = "";
            try
            {
                string strCon = ConfigurationManager.ConnectionStrings["CGSAConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT COUNT(T01Online_ID) FROM [dbo].[_Temp_CI_Import_Temp] WHERE T01Online_ID='{0}'", strT01Online_ID);
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = new SqlCommand(strSql, con);
                con.Open();
                intRow = Convert.ToInt32(cmd.ExecuteScalar());
                con.Close();
                cmd = null;
                if (intRow <= 0)
                {
                    strSql = string.Format("INSERT INTO[dbo].[_Temp_CI_Import_Temp](T01Online_ID, T01Project_Type, T01Last_Status, T01Bank_Code) VALUES('{0}', '{1}', '{2}', '{3}')", strT01Online_ID, strData[0], strData[1], strData[2]);
                    cmd = new SqlCommand(strSql, con);
                    con.Open();
                    intRow = cmd.ExecuteNonQuery();
                    con.Close();
                    cmd = null;
                    strTempResult = string.Format("{0} new row from {1} was added to '_Temp_CI_Import_Temp'", intRow.ToString(), strT01Online_ID);
                    WriteLogFile(strTempResult);
                }
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strTempResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                strResult += strTempResult;
                WriteLogFile(strTempResult);
            }
            return;
        }


        string UpdateImportStatusDb(string strT01Online_ID, string strFlagStatus)
        {
            string strResult = "";
            string[] strData = new string[] { _T01Send_Date_Cyber, _T01Send_Time_Cyber, _T01Project_Type, _T01Last_Status, _T01Bank_Code, _T01House_Province };
            string[] strDataTemp = new string[] { _T01Project_Type, _T01Last_Status, _T01Bank_Code };
            CheckWhetherExistingInImportStatusDb(strT01Online_ID, strData); // if not exist, fill first
            CheckWhetherExistingIn_Temp_CI_Import_TempDb(strT01Online_ID, strDataTemp);
            try
            {
                int inCGSFlag = 0;
                if (strFlagStatus.IndexOf("Y") >= 0){
                    inCGSFlag=1;
                }
                string strCon = ConfigurationManager.ConnectionStrings["logConnectionString"].ConnectionString;
                string strSql = string.Format("UPDATE [dbo].[TBL_CI_Import_Status] SET Imported='{0}', InCGSFlag={1},LastRunDateTime=GetDate() WHERE T01Online_ID='{2}'", strFlagStatus, inCGSFlag,strT01Online_ID);
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = new SqlCommand(strSql, con);
                con.Open();
                int intInsertedRow = cmd.ExecuteNonQuery();
                con.Close();
                strResult += intInsertedRow + " row was Updated." + "<br/>";
                string strImportStatusOutput = string.Format("T01Online_ID: {0} was updated Import status to 'Y'", strT01Online_ID);
                //this.lblPre.Text = strPreRequestNoOutput;
                strResult += strImportStatusOutput + Environment.NewLine;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
            }
            return strResult;
        }

        string WriteToFile(string strContent, string strFileName)
        {
            string strFilePath = "-";
            string strFolderPath = @"D:\cyber\json\";
            try
            {
                StreamWriter sw = File.CreateText(strFolderPath + strFileName);
                sw.Write(strContent);
                sw.Close();
                strFilePath = strFolderPath + strFileName;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            return strFilePath;
        }
        private string GetStringFromPOSTRequestWithTokenToRequestBody(string strUrl, string strContentType, params string[] strParametersArray)
        {
            string strRawData = "";
            try
            {
                var client = new RestClient(strUrl);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                if (stkToken.Count > 0)
                {
                    strToken = stkToken.Peek();
                }
                else
                {
                    strToken = GetToken();
                }
                //this.txtToken.Text = strToken;
                WriteLogFile("GetStringFromPOSTRequestWithTokenToRequestBody" + Environment.NewLine);
                WriteLogFile(strToken + Environment.NewLine);
                request.AddHeader("Authorization", "Bearer " + strToken);
                request.AddHeader("Content-Type", "application/json");
                for (int i = 0; i < strParametersArray.Length; i++)
                {
                    request.AddParameter(strContentType, strParametersArray[i], ParameterType.RequestBody);
                }
                IRestResponse response = client.Execute(request);
                strRawData = response.Content;
                return strRawData;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return strRawData;
            }
        }
        private string GetStringFromGETRequestWithTokenToRequestBody(string strUrl, string strContentType, params string[] strParametersArray)
        {
            string strRawData = "";
            try
            {
                var client = new RestClient(strUrl);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                if (stkToken.Count > 0)
                {
                    strToken = stkToken.Peek();
                }
                else
                {
                    strToken = GetToken();
                }
                //this.txtToken.Text = strToken;
                request.AddHeader("Authorization", "Bearer " + strToken);
                request.AddHeader("Content-Type", strContentType);
                for (int i = 0; i < strParametersArray.Length; i++)
                {
                    request.AddParameter(strContentType, strParametersArray[i], ParameterType.RequestBody);
                }
                IRestResponse response = client.Execute(request);
                strRawData = response.Content;
                return strRawData;
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                WriteLogFile(strResult);
                return strRawData;
            }
        }
        string WriteRequest(string strJsonRequest)
        {
            string strReturnData = "";
            string strUrl = strWorkServerURL + "/request-service/api/external/request";
            string strContentType = "application/json";
            string strParameter0 = strJsonRequest;
            strReturnData = this.GetStringFromPOSTRequestWithTokenToRequestBody(strUrl, strContentType, strParameter0);
            return strReturnData;
        }

        public string[] GetRequestStatusFromT01Online_ID(string strT01Online_ID)
        {
            string[] strStatusDescriptionLgIdLgNo = new string[5];
            string strPRE = GetPREFromT01Online_ID(strT01Online_ID);
            strStatusDescriptionLgIdLgNo = GetRequestStatusFromPRE(strPRE);
            return strStatusDescriptionLgIdLgNo;
        }

        public string GetTCGRoughStatusForCyber(string strCGSStatus)
        {
            string strRoughTCGStatus = "010";
            switch (strCGSStatus)
            {
                case "00":
                case "01":
                case "02":
                    strRoughTCGStatus = "000";
                    break;
                case "10":
                case "11":
                case "12":
                    strRoughTCGStatus = "100";
                    break;
                case "20":
                case "21":
                case "22":
                    strRoughTCGStatus = "200";
                    break;
                case "30":
                case "31":
                case "32":
                    strRoughTCGStatus = "300";
                    break;
                case "40":
                case "41":
                case "42":
                    strRoughTCGStatus = "400";
                    break;
                case "50":
                case "51":
                case "52":
                    strRoughTCGStatus = "500";
                    break;
                default:
                    strRoughTCGStatus = "-9999";
                    break;
            }
            return strRoughTCGStatus;
        }

        public string GetLGLocationAndPDF(string strGuaranteeDocType, string strLgId)
        {
            string strLgFileInfo = "";
            if ((strLgId == "") || (strLgId == null) || (strLgId == "-"))
            {
                return null;
            }
            string strPrefix = "";
            // From Yoh DGI
            switch (strGuaranteeDocType)
            {
                case "01":
                    strPrefix = "RPTLG200I002"; // default
                    break;
                case "02":
                    strPrefix = "RPTLG200I001"; // renew
                    break;
                case "03":
                    strPrefix = "RPTLG200I004"; // แบบฟอร์ม SMEs ที่ได้รับสินเชื่อหนังสือค้ำประกัน
                    break;
                case "04":
                    strPrefix = "RPTLG200I003"; //  แบบฟอร์มปกติ (PGS8,Micro)
                    break;
                case "05":
                    strPrefix = "RPTLG200I005"; // DG
                    break;
                case "06":
                    strPrefix = "RPTLG200I006"; // พรก
                    break;
                default:
                    strPrefix = "RPTLG200I002";
                    break;
            }
            byte[] obr = null;
            string strUrl = string.Format("{0}/report-service/api/pdf/{1}?P_LG_ID={2}&&signPdfFlg=Y", strWorkServerURL, strPrefix, strLgId);
            var client = new RestClient(strUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            string strToken = "";
            if (stkToken.Count > 0)
            {
                strToken = stkToken.Peek();
            }
            else
            {
                strToken = GetToken();
            }
            request.AddHeader("Authorization", "Bearer " + strToken);
            IRestResponse response = client.Execute(request);
            obr = response.RawBytes;
            string strFn = response.Headers[6].Value.ToString();
            DateTime dtmNow = DateTime.Now;
            string strBaseFolder = @"d:\LG\";
            //string strFileName = string.Format("LG_{0}{1:00}{2:00}{3:00}{4:00}{5:00}_{6}.pdf", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second, strLgId);
            string strFileName = strFn;
            string strFilePath = strBaseFolder + strFileName;
            FileStream sw = File.Create(strFilePath);
            sw.Write(obr, 0, obr.Length);
            sw.Flush();
            sw.Close();
            strLgFileInfo = strFilePath;
            return strLgFileInfo;
        }


        public string GetCgsLGPDF(string strGuaranteeDocType, string strLgId,string lg_no="")
        {
            string strLgFileInfo = "";
            if ((strLgId == "") || (strLgId == null) || (strLgId == "-"))
            {
                return "";
            }
            Console.WriteLine("Start get lg file from cgs..");
            string strPrefix = "";
            // From Yoh DGI
            switch (strGuaranteeDocType)
            {
                case "01":
                    strPrefix = "RPTLG200I002"; // default
                    break;
                case "02":
                    strPrefix = "RPTLG200I001"; // renew
                    break;
                case "03":
                    strPrefix = "RPTLG200I004"; // แบบฟอร์ม SMEs ที่ได้รับสินเชื่อหนังสือค้ำประกัน
                    break;
                case "04":
                    strPrefix = "RPTLG200I003"; //  แบบฟอร์มปกติ (PGS8,Micro)
                    break;
                case "05":
                    strPrefix = "RPTLG200I005"; // DG
                    break;
                case "06":
                    strPrefix = "RPTLG200I006"; // พรก
                    break;
                default:
                    strPrefix = "RPTLG200I002";
                    break;
            }
            byte[] obr = null;
            string strUrl = string.Format("{0}/report-service/api/pdf/{1}?P_LG_ID={2}&&signPdfFlg=Y", strWorkServerURL, strPrefix, strLgId);
            var client = new RestClient(strUrl);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            string strToken = "";
            if (stkToken.Count > 0)
            {
                strToken = stkToken.Peek();
            }
            else
            {
                strToken = GetToken();
            }
            request.AddHeader("Authorization", "Bearer " + strToken);
            IRestResponse response = client.Execute(request);
            obr = response.RawBytes;
            if (response.Headers.Count >= 6)
            {
                string strFn = response.Headers[6].Value.ToString();
                DateTime dtmNow = DateTime.Now;
                string strBaseFolder = @"d:\Cyber\lg\";
                //string strFileName = string.Format("LG_{0}{1:00}{2:00}{3:00}{4:00}{5:00}_{6}.pdf", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second, strLgId);
                string strFileName = strFn;
                if (lg_no.Length > 0)
                {
                    strFileName = lg_no + ".pdf";
                }

                string strFilePath = strBaseFolder + strFileName;
                Console.WriteLine("Write file " + strFilePath);
                FileStream sw = File.Create(strFilePath);
                sw.Write(obr, 0, obr.Length);
                sw.Flush();
                sw.Close();
                if(obr.Length>100)
                strLgFileInfo = strFilePath;

            }
            return strLgFileInfo;
        }

        public string[] GetRequestStatusFromPRE(string strPreRequestNumber)
        {
            if (strPreRequestNumber == "-1")
            {
                return new string[] { "No Data, Unexisting T01Online_ID", "-", "-", "-", "-", "-" };
            }
            string[] strStatusDescriptionLgIdLgNo = new string[6] { "No Data, Unexisting PRE", "-", "-", "-", "-", "-" };
            try
            {
                string strJsonResponse = "";
                string strParameter0 = "1975-01-01 00:00:00";
                DateTime dtmNow = DateTime.Now;
                string strYear = dtmNow.Year > 2500 ? (dtmNow.Year - 543).ToString() : dtmNow.Year.ToString();
                string strParameter1 = string.Format("{0}-{1:00}-{2:00} 23:59:59", strYear, dtmNow.Month, dtmNow.Day);
                string strParameter2 = strPreRequestNumber;
                string strUrl = string.Format("{0}/request-service/api/external/request?reqStartDate={1}&reqEndDate={2}&requestNum={3}", strWorkServerURL, strParameter0, strParameter1, strParameter2);
                var client = new RestClient(strUrl);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                string strToken = "";
                if (stkToken.Count > 0)
                {
                    strToken = stkToken.Peek();
                }
                else
                {
                    strToken = GetToken();
                }
                request.AddHeader("Authorization", "Bearer " + strToken);
                IRestResponse response = client.Execute(request);
                strJsonResponse = response.Content;
                StatusOfRequest statusAndLgInfoOfRequest = JsonSerializer.Deserialize<StatusOfRequest>(strJsonResponse, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All), WriteIndented = true });
                //StatusOfRequest statusAndLgInfoOfRequest = JsonSerializer.Deserialize<StatusOfRequest>(strJsonResponse,options:null);
                string strStatus = statusAndLgInfoOfRequest.result.status;
                string strDescription = statusAndLgInfoOfRequest.result.statusStr;
                string strTCGRoughStatus = GetTCGRoughStatusForCyber(strStatus); // need to update *****
                string strLgId = Convert.ToString(statusAndLgInfoOfRequest.result.result[0].lgId);
                string strLgNo = Convert.ToString(statusAndLgInfoOfRequest.result.result[0].lgNo);
                string strGuaranteeDocType = Convert.ToString(statusAndLgInfoOfRequest.result.result[0].guaranteeDocType);
                strStatusDescriptionLgIdLgNo[0] = strStatus;
                strStatusDescriptionLgIdLgNo[1] = strDescription;
                strStatusDescriptionLgIdLgNo[2] = strTCGRoughStatus;
                strStatusDescriptionLgIdLgNo[3] = strLgId;
                strStatusDescriptionLgIdLgNo[4] = strLgNo;
                strStatusDescriptionLgIdLgNo[5] = strGuaranteeDocType;
            }
            catch
            {
                return new string[] { "Exception when getting status from PRE", "-", "-", "-", "-", "-" };
            }
            return strStatusDescriptionLgIdLgNo;
        }
        string ConvertDateTime(string strInputDate, string strInputTime = "000000")
        {
            string strOutputDate = "";
            if ((strInputDate != "") && (strInputDate != null))
            {
                try
                {
                    string strBYear, strCYear, strMonth, strDay;
                    string strHour, strMinute, strSecond;
                    int intCYear = 0;
                    strBYear = strInputDate.Substring(0, 4);
                    intCYear = (Convert.ToInt32(strBYear)) - 543;
                    strCYear = intCYear.ToString();
                    strMonth = strInputDate.Substring(4, 2);
                    strDay = strInputDate.Substring(6, 2);
                    strHour = strInputTime.Substring(0, 2);
                    strMinute = strInputTime.Substring(2, 2);
                    strSecond = strInputTime.Substring(4, 2);
                    strOutputDate = string.Format("{0}-{1:00}-{2:00} {3:00}:{4:00}:{5:00}", strCYear, strMonth, strDay, strHour, strMinute, strSecond);
                }
                catch
                {
                    strOutputDate = strInputDate;
                    WriteLogFile("Exception happened in ConvertDate(...)");
                }
            }
            return strOutputDate;
        }
        

        /// <summary>
        /// Date diff with Horiday
        /// </summary>
        /// <param name="firstdate"></param>
        /// <param name="enddate"></param>
        /// <returns></returns>
        public int DateDiffWithHoliday(DateTime firstdate,DateTime enddate)
        {
            int diff = 0;
            int days = 1;
            DateTime checkdate = firstdate.AddDays(days);
            InterfaceDatabase conmastecgs = ConnectMasterCgs();
            while (checkdate <= enddate)
            {
                if (!(checkdate.DayOfWeek == DayOfWeek.Saturday || checkdate.DayOfWeek == DayOfWeek.Sunday))
                {
                    string todays = DateTimeTool.gsFormatDateTime(checkdate, "bbbbMMdd");
                    if (conmastecgs != null)
                    {
                        Recordset rsHoliday = conmastecgs.GetRecordset("select top 1 Holiday_Date from TBL_Holiday where Holiday_Date='" + todays + "'");
                        if (rsHoliday.RecordCount <= 0)
                        {
                            diff++;
                        }
                        rsHoliday.Close();
                    }
                    else
                    {
                        diff++;
                    }
                }
                days++;
                checkdate = firstdate.AddDays(days);
            }
            return diff;
        }

        /*
        private static string GetLastLG(string bankid, string maxlg)
        {
            string ret = "";
            string realmax = "";
            string sql = "select max(lg_no) as nmax from tbl_rd_lg where lg_no >= '" + maxlg + "'  and bank_id=" + bankid;
            Recordset recordset = conCGS.GetRecordset(sql);
            if (recordset.RecordCount > 0)
            {
                realmax = recordset["nmax"].StringValue;
            }
            while (ret == "")
            {
                int intNextLG = StringTools.ToInt32(maxlg) + 100;
                string sqlx = "select max(lg_no) as nmax from tbl_rd_lg where lg_no between '" + maxlg + "' and '" + intNextLG + "'  and bank_id=" + bankid;
                Recordset recordset2 = conCGS.GetRecordset(sqlx);
                if (recordset2.RecordCount > 0)
                {
                    if (recordset2["nmax"].Int32Value > StringTools.ToInt32(maxlg))
                    {
                        ret = recordset2["nmax"].StringValue;
                    }
                }
                recordset2.Close();
                if (intNextLG >= StringTools.ToInt32(realmax))
                {
                    break;
                }
            }
            if (ret == "") { ret = maxlg; }
            return ret;
        }
        */
    }//end class
}//end date
