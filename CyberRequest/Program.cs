using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Configuration;
using System.Globalization;
using System.IO;
using jcs.clientdb;
using jcs.clientdb.sql;

namespace CyberRequest
{
    class Program
    {
        public static CyberRequest.XSQLProgramLink xSQLProgramlink;// = null;
        public static int StationID;
        public static int getLGPDF;
        public static int genmatchingReport;

        private static System.Timers.Timer AutoRequestTimer;
        static List<string> lstDayT01Online_ID = new List<string>();

        static void Main(string[] args)
        {
            string arg = ";";
            for (int i = 0; i < args.Length; i++)
            {
                arg += args[i];
            }
            arg += ";";
            string st = StringTools.gsInStrValue(arg, "stationid=", ";");
            string gl = StringTools.gsInStrValue(arg, "getlgpdf=", ";");
            string gmr = StringTools.gsInStrValue(arg, "genmatchingreport=", ";");
            string bankcode = StringTools.gsInStrValue(arg, "bankcode=", ";");
            string assignqueue = StringTools.gsInStrValue(arg, "assignQueue=", ";");
            StationID = StringTools.ToInt32(st);
            getLGPDF = StringTools.ToInt32(gl);
            genmatchingReport = StringTools.ToInt32(gmr);

            /// anan test 
            ///  08/09/2021
            AssignQueueToCGS();
            ///------------

            if (assignqueue == "1")
            {
                Console.WriteLine("Start Assign Queue to send request from Cyber to CGS");
                AssignQueueToCGS();
                return;
            }

            if (StationID <= 0)
            {
                return;
            }

            if (getLGPDF == 1)
            {
                GetLGPDFFromCGS(bankcode);
                return;
            }
            else
            {
                if (genmatchingReport == 1)
                {
                    GenMatchingReport(StringTools.ToDate("14/07/2021"), StringTools.ToDate("25/07/2021"));
                }
                else
                {
                    SetTimer();
                }
            }


            Console.WriteLine("Press 'Q' key to exit... ");
            do
            {

            } while (Console.ReadKey().Key != ConsoleKey.Q);
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            AutoRequestTimer = new System.Timers.Timer(700);
            // Hook up the Elapsed event for the timer. 
            AutoRequestTimer.Elapsed += OnTimedEvent;
            AutoRequestTimer.AutoReset = true;
            AutoRequestTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.Clear();
            Console.WriteLine("Auto Send Request to Cyber Station: " + StationID + " Start checking at {0:HH:mm:ss.fff}",
                              e.SignalTime);

            //Start
            StartJobAll();
        }

        //Get lg pdf from CGS
        private static void GetLGPDFFromCGS(string bankcode = "")
        {
            Request oRequest = new Request();
            InterfaceDatabase conMasterCsg = oRequest.ConnectMasterCgs();

            xSQLProgramlink = new CyberRequest.XSQLProgramLink(conMasterCsg);

            string disable_loadpdffromcgs = Program.xSQLProgramlink.GetSQL("disable_loadpdffromcgs");



            if (disable_loadpdffromcgs.IndexOf("yes") >= 0)
            {
                Console.WriteLine("disable_disable_loadpdffromcgs..");
                return;
            }
            string lastlgno_togetpdf = Program.xSQLProgramlink.GetSQL("lastlgno_togetpdf");
            if (lastlgno_togetpdf.Length == 0)
            {
                Console.WriteLine(lastlgno_togetpdf + " not set value.");
                return;
            }


            InterfaceDatabase conProd = oRequest.ConnectCgs("CGSPROD");
            InterfaceDatabase conCyber = oRequest.ConnectCyber();
            string sqlget = @"SELECT TOP (10000) 
                    [T01Online_ID]
                      ,[T01Request_No]
                      ,[T01Send_Date]
                      ,[T01Send_Time]
                      ,[T01LG_No]
                      ,[T01LG_Date]
                      ,[T01Project_Type]
                      ,[T01Bank_Code]
                      , T01Last_Status
                  FROM [DB_ONLINE_CG].[dbo].[T01_Request_Online] where t01LG_NO like '647%' and T01Last_Status='300'"+ " and t01LG_NO>'"+ lastlgno_togetpdf +"'";
            if (bankcode.Length > 0) {
                sqlget += " and T01Bank_Code='" + bankcode + "' ";
            }
            sqlget += " order by T01LG_NO";
            Recordset rsCyber = conCyber.GetRecordset(sqlget);
            if (rsCyber != null)
            {
                string lastpdf = lastlgno_togetpdf;
                string lastmoveerror = "";
                int i = 0;
                while (!rsCyber.EOF)
                {
                    string strLGFile = @"\\192.168.0.16\FileRoom_LG\" + rsCyber["T01LG_No"].StringValue + ".pdf";
                    //string strLGFile = @"d:\cyber\FileRoom_LG\" + rsCyber["T01LG_No"].StringValue + ".pdf";
                    Console.WriteLine("Checking LG file in " + strLGFile);
                    if (!File.Exists(strLGFile))
                    {
                        string sql = @"select r.ref_no3,r.pre_req_status,r.lg_id,r.product_id,p.guarantee_doc_type,r.pre_req_dt from TBL_RD_GUA_PRE_REQUEST r inner join
                                        TBL_MD_PRODUCT p on r.product_id = p.product_id
                                         where ref_no3 = '" + rsCyber["T01Online_ID"].StringValue + "' and  r.pre_req_status='18' and r.Status='A' order by pre_req_dt";
                        Recordset rs = conProd.GetRecordset(sql);
                        if (rs.RecordCount > 0)
                        {
                            string data = oRequest.GetCgsLGPDF(rs["guarantee_doc_type"].StringValue, rs["LG_ID"].StringValue, rsCyber["T01LG_No"].StringValue.Trim());
                            if (data.Length > 0)
                            {
                                try
                                {
                                    File.Move(data, strLGFile); //Move File
                                    if (lastmoveerror.Length == 0)
                                    {
                                        lastpdf = rsCyber["T01LG_No"].StringValue.Trim();
                                        if (i % 50 == 0)
                                        {
                                            conMasterCsg.Execute("update XSQLProgramLink set [SQL]='" + lastpdf + "' where ProjectName='CyberRequest' and SQLName='lastlgno_togetpdf'");
                                        }
                                    }

                                }
                                catch
                                {
                                    lastmoveerror = rsCyber["T01LG_No"].StringValue.Trim();
                                }
                            }
                        }
                        rs.Close();

                    }
                    else
                    {
                        Console.WriteLine(strLGFile + " is exists.");

                    }
                    rsCyber.MoveNext();
                    i++;
                }//While
                if(lastlgno_togetpdf!= lastpdf)
                {
                    conMasterCsg.Execute("update XSQLProgramLink set [SQL]='" + lastpdf + "' where ProjectName='CyberRequest' and SQLName='lastlgno_togetpdf'");
                }
                conMasterCsg.Close();
            }
            oRequest = null;

        }

        /// <summary>
        /// Create Matching Cyber to CGS report
        /// </summary>
        private static void GenMatchingReport(DateTime stdate, DateTime todate)
        {
            string sendDateStart = DateTimeTool.gsFormatDateTime(stdate, "bbbbMMdd");
            string sendDateEnd = DateTimeTool.gsFormatDateTime(todate, "bbbbMMdd");
            Request oRequest = new Request();
            InterfaceDatabase conCyber = oRequest.ConnectCyber();
            InterfaceDatabase conProd = oRequest.ConnectCgs("CGSPROD");
            string sql =
            @"SELECT 
                    [T01Online_ID]
                      ,[T01Request_No]
                      ,[T01Send_Date]
                      ,[T01Send_Time]
                      ,[T01LG_No]
                      ,[T01LG_Date]
                      ,[T01Project_Type]
                      ,[T01Bank_Code]
                      , T01Last_Status
                       ,p.R09Type_Name as R09Type_Name
                  FROM [DB_ONLINE_CG].[dbo].[T01_Request_Online] r inner join 
                      (select R09Type_Code,R09Type_Name from [DB_SICGC3].[dbo].[R09_Type]  WHERE R09Type_Group='4') p on p.R09Type_Code=r.T01Project_Type
                  where T01Send_Date between '" + sendDateStart + "' and '" + sendDateEnd + "' and (T01Project_Type like '0098%' or T01Project_Type in ('00903','00995')) order by T01Project_Type,T01Send_Date, T01Online_ID";

            Recordset rsCyber = conCyber.GetRecordset(sql);
            if (rsCyber != null)
            {
                StreamWriter sw = new StreamWriter(@"d:\Cyber\RequestSummary" + sendDateStart + "-" + sendDateEnd + ".csv", false, Encoding.Default);
                while (!rsCyber.EOF)
                {
                    //เช็คเข้า Cgs
                    string statuscgs = "0";
                    string cgsdate = "";
                    int sla = 0;
                    Recordset rsPreReq = conProd.GetRecordset("select ref_no3,pre_req_number,pre_req_status,pre_req_dt from TBL_RD_GUA_PRE_REQUEST where status='A' and ref_no3='" + rsCyber["T01Online_ID"].StringValue + "'");
                    if (rsPreReq.RecordCount > 0)
                    {
                        statuscgs = rsPreReq["pre_req_status"].StringValue;
                        cgsdate = DateTimeTool.gsFormatDateTime(rsPreReq["pre_req_dt"].DateTimeValue, "bbbbMMdd");
                        sla = StringTools.ToInt32(cgsdate) - (StringTools.ToInt32(rsCyber["T01Send_Date"].StringValue) + 1);
                    }
                    else
                    {
                        sla = StringTools.ToInt32(DateTimeTool.gsFormatDateTime(DateTime.Today, "bbbbMMdd")) - (StringTools.ToInt32(rsCyber["T01Send_Date"].StringValue) + 1);

                    }
                    Console.Clear();
                    Console.Write(rsCyber.CurrentRow);
                    string line = rsCyber["T01Project_Type"].StringValue + "," + rsCyber["R09Type_Name"].StringValue + "," + rsCyber["T01Online_ID"].StringValue + "," + rsCyber["T01Request_No"].StringValue + "," + rsCyber["T01Send_Date"].StringValue + "," + rsCyber["T01LG_No"].StringValue + "," + rsCyber["T01LG_DATE"].StringValue + "," + rsCyber["T01Last_Status"].StringValue + "," + cgsdate + "," + statuscgs + "," + sla;
                    sw.WriteLine(line);
                    rsPreReq.Close();

                    rsCyber.MoveNext();

                }
                sw.Close();
            }
        }

        private static void LoadCyberToJob(string prmProjectType)
        {
            DataSet dts = null;
            DateTime dtmStart = StringTools.ToDate("22/07/2021");
            if (dtmStart.Year < 2000)
            {
                dtmStart = DateTime.Now;
            }
            DateTime dtmEnd = StringTools.ToDate("22/07/2021");
            if (dtmEnd.Year < 2000)
            {
                dtmEnd = DateTime.Now;
            }
            string strBankId = "";
            string strBankCode = GetBankCodeFromBankId(strBankId);

            string strStartDate = string.Format("{0}{1:00}{2:00}", GetThaiYear(dtmStart.Year), dtmStart.Month, dtmStart.Day);
            string strEndDate = string.Format("{0}{1:00}{2:00}", GetThaiYear(dtmEnd.Year), dtmEnd.Month, dtmEnd.Day);
            try
            {
                //string strTable = "[dbo].[_TempR09_Type]";
                string strT01Project_Type = prmProjectType;
                string strCon = ConfigurationManager.ConnectionStrings["CGSAConnectionString"].ConnectionString;
                string strSql = "";
                if (strBankId == "" || strBankId == "0")
                {
                    strSql = string.Format("select a.[T01Online_ID] from [DB_ONLINE_CG].[dbo].[T01_Request_Online] a where (((a.[T01Send_Date] >= '{0}') and (a.[T01Send_Date] <= '{1}')) and (a.[T01Last_Status] = '010') and (a.[T01Project_Type]='{2}')) except select b.[T01Online_ID] from [DB_ONLINE_CG].[dbo].[_Temp_CI_Import_Temp] b; ", strStartDate, strEndDate, strT01Project_Type);
                }
                else
                {
                    strSql = string.Format("select a.[T01Online_ID] from [DB_ONLINE_CG].[dbo].[T01_Request_Online] a where (((a.[T01Send_Date] >= '{0}') and (a.[T01Send_Date] <= '{1}')) and (a.[T01Bank_Code] = '{2}') and (a.[T01Last_Status] = '010') and (a.[T01Project_Type]='{3}')) except select b.[T01Online_ID] from [DB_ONLINE_CG].[dbo].[_Temp_CI_Import_Temp] b; ", strStartDate, strEndDate, strBankCode, strT01Project_Type);
                }
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                dts = new DataSet("TempDS");
                dta.Fill(dts);
                int intRows = dts.Tables[0].Rows.Count;
                for (int i = 0; i < intRows; i++)
                {
                    lstDayT01Online_ID.Add(dts.Tables[0].Rows[i][0].ToString());
                }
                con.Close();
                WriteLogFile("Read " + intRows + " rows and kept in List 'lstDayT01Online_ID'.");
                int intDbRows = WriteTo_Temp_CI_Import_TempTable(lstDayT01Online_ID);
                WriteLogFile("Write " + intDbRows + " rows to '[_Temp_CI_Import_Temp].'");

                //Prepairing data to queue
                LoadUsingDbData();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                string strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
        }

        /// <summary>
        /// Assign Que to CGS
        /// </summary>
        private static void AssignQueueToCGS()
        {
            Request oRequest = new Request();
            InterfaceDatabase conMasterCsg = oRequest.ConnectMasterCgs();

            xSQLProgramlink = new CyberRequest.XSQLProgramLink(conMasterCsg);

            string disable_assignqueuetocgs = Program.xSQLProgramlink.GetSQL("disable_assignqueuetocgs");

            

            if (disable_assignqueuetocgs.IndexOf("yes") >= 0)
            {
                Console.WriteLine("disable_assignqueuetocgs..");
                return;
            }
            string t01projecttype_toassigntocgs = Program.xSQLProgramlink.GetSQL("t01projecttype_toassigntocgs");
            string t01send_date_start_toassigntocgs = Program.xSQLProgramlink.GetSQL("t01send_date_start_toassigntocgs");
            string numberof_toassigntocgs = Program.xSQLProgramlink.GetSQL("numberof_toassigntocgs");
            conMasterCsg.Close();

            oRequest.AssignQueueCybertoCGS(t01projecttype_toassigntocgs, t01send_date_start_toassigntocgs, StringTools.ToInt32(numberof_toassigntocgs));


        }

        /// <summary>
        /// Run send data to CGS
        /// </summary>
        private static void StartJobAll()
        {
            AutoRequestTimer.Enabled = false;
            Request oRequest = new Request();
            InterfaceDatabase conMasterCsg = oRequest.ConnectMasterCgs();

            xSQLProgramlink = new CyberRequest.XSQLProgramLink(conMasterCsg);

            string disable_sendrequesttocgs = Program.xSQLProgramlink.GetSQL("disable_sendrequesttocgs");
            if (disable_sendrequesttocgs.IndexOf("yes") >= 0)
            {
                Console.WriteLine("disable_sendrequesttocgs..");
                AutoRequestTimer.Enabled = true;
                return;
            }

            //ทำงานเฉพาะเวลาที่ให้ auto fill
            string autofillworkinhour = Program.xSQLProgramlink.GetSQL("autosendrequesttocgsworkinhour");
            if (autofillworkinhour.IndexOf(DateTime.Now.ToString("HH")) >= 0)
            {

                Console.WriteLine("Starting Job...");
                Console.WriteLine("New Request Object...");

                oRequest.StartAllInJob();
                Console.WriteLine("Done...");
            }
            else
            {
                Console.WriteLine("Not a Working Hour.");
            }
            conMasterCsg = null;
            oRequest = null;
            AutoRequestTimer.Enabled = true;


        }

        private static int WriteTo_Temp_CI_Import_TempTable(List<string> lstPK)
        {
            int intRow = 0;
            try
            {
                string strCon = ConfigurationManager.ConnectionStrings["CGSAConnectionString"].ConnectionString;
                string strSql = "";
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = null;
                for (int i = 0; i < lstPK.Count; i++)
                {
                    con.Open();
                    strSql = string.Format("INSERT INTO [dbo].[_Temp_CI_Import_Temp] SELECT a.[T01Online_ID], a.[T01Project_Type], a.[T01Last_Status], a.[T01Bank_Code]," + StationID + " as StationID FROM [dbo].[T01_Request_Online] a WHERE a.[T01Online_ID]='{0}'", lstPK[i]);
                    cmd = new SqlCommand(strSql, con);
                    intRow += cmd.ExecuteNonQuery();
                    cmd = null;
                    con.Close();
                }
                con = null;
                WriteLogFile("Write " + intRow + " rows to '_Temp_CI_Status_Temp' table.");
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                string strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            return intRow;
        }


        //Load data from temp to import table
        private static void LoadUsingDbData()
        {
            try
            {
                List<string> lstPk = new List<string>();
                string strCon = ConfigurationManager.ConnectionStrings["CGSAConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT * FROM [dbo].[_Temp_CI_Import_Temp] where StationID=" + StationID);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                DataSet dts = new DataSet("_Temp_CI_Import_Temp_DS");
                dta.Fill(dts, "_Temp_CI_Import_Temp");
                for (int i = 0; i < dts.Tables[0].Rows.Count; i++)
                {
                    DataRow dtr = dts.Tables[0].Rows[i];
                    lstPk.Add(dtr["T01Online_ID"].ToString());
                }
                con.Close();
                int intRow = WriteToAndUpdateToN_TBL_CI_Import_Status(lstPk);
                WriteLogFile(string.Format("Total {0} rows were written to 'TBL_CI_Import_Status'", intRow.ToString()));
            }
            catch (Exception ex)
            {
                string strException = string.Format("Exception happened in '{0}(...)':{1}{2}{1}Stack Trace:{1}{3}{1}", MethodBase.GetCurrentMethod().Name, Environment.NewLine, ex.Message, ex.StackTrace);
                WriteLogFile(strException);
            }
        }

        private static int WriteToAndUpdateToN_TBL_CI_Import_Status(List<string> lstPK)
        {
            int intRow = 0;
            int intRowUpdateToN = 0;
            try
            {
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                string strSql = "";
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = null;
                for (int i = 0; i < lstPK.Count; i++)
                {
                    if (Existing(lstPK[i].ToString()) == false)
                    {
                        strSql = string.Format("INSERT INTO [dbo].[TBL_CI_Import_Status](T01Online_ID, T01Send_Date, T01Send_Time, T01Project_Type, T01Last_Status, T01Bank_Code, T01House_Province,StationID) SELECT T01Online_ID, T01Send_Date, T01Send_Time, T01Project_Type, T01Last_Status, T01Bank_Code, T01House_Province,"+StationID +" as StationID FROM [DB_ONLINE_CG].dbo.T01_Request_Online a WHERE (T01Online_ID='{0}');", lstPK[i]);
                        con.Open();
                        cmd = new SqlCommand(strSql, con);
                        intRow += cmd.ExecuteNonQuery();
                        cmd = null;
                    }
                    con.Close();
                }
                con.Open();
                for (int i = 0; i < lstPK.Count; i++)
                {
                    strSql = string.Format("UPDATE dbo.TBL_CI_Import_Status SET Imported='N' WHERE (T01Online_ID='{0}');", lstPK[i]);
                    cmd = new SqlCommand(strSql, con);
                    intRowUpdateToN += cmd.ExecuteNonQuery();
                    cmd = null;
                    con.Close();
                }
                con = null;
                WriteLogFile("Write and Update TBL_CI_Import_Status " + intRow + " rows to 'TBL_CI_Import_Status' table.");
                WriteLogFile("Update to N status for " + intRowUpdateToN + " rows.");
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                string strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            return intRow;
        }


        private static bool Existing(string strPk)
        {
            bool blnExisting = false;
            try
            {
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT COUNT(1) FROM [dbo].[TBL_CI_Import_Status] WHERE T01Online_ID='{0}'", strPk);
                SqlConnection con = new SqlConnection(strCon);
                SqlCommand cmd = new SqlCommand(strSql, con);
                con.Open();
                int intOutput = Convert.ToInt32(cmd.ExecuteScalar().ToString());
                if (intOutput > 0)
                {
                    blnExisting = true;
                }
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                string strResult = "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>" + Environment.NewLine;
                blnExisting = false;
                WriteLogFile(strResult);
            }
            return blnExisting;
        }


        private static string GetBankCodeFromBankId(string strBankId)
        {
            string strBankCode = "0";
            string strResult = "";
            DataSet dts = null;
            try
            {
                string strTableName = "[dbo].[TBL_MD_BANK]";
                string strCon = ConfigurationManager.ConnectionStrings["LogConnectionString"].ConnectionString;
                string strSql = string.Format("SELECT BANK_CODE FROM {0} WHERE BANK_ID='{1}'", strTableName, strBankId);
                SqlConnection con = new SqlConnection(strCon);
                SqlDataAdapter dta = new SqlDataAdapter(strSql, con);
                SqlCommandBuilder cmb = new SqlCommandBuilder(dta);
                dts = new DataSet("TempDS");
                dta.Fill(dts, strTableName);
                strBankCode = dts.Tables[0].Rows[0][0].ToString();
                // convert to 2 digits
                strBankCode = (Convert.ToInt32(strBankCode)).ToString("00");
                con.Close();
            }
            catch (Exception ex)
            {
                string strCurrentMethod = System.Reflection.MethodBase.GetCurrentMethod().Name + "(...)";
                strResult += "Exception happened at \"" + strCurrentMethod + "\"<br/>Message: " + ex.Message + "<br/>Stack Trace:<br/>" + ex.StackTrace + "<br/>";
                WriteLogFile(strResult);
            }
            return strBankCode;
        }

        private static void WriteLogFile(string strData)
        {
            try
            {
                Console.WriteLine(strData);
                DateTime dtmNow = DateTime.Now;
                string strFilename = string.Format("Log{0}{1:00}{2:00}.txt", dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);
                StreamWriter sw = File.AppendText(@"d:\Cyber\Log\" + strFilename);
                sw.WriteLine(dtmNow.ToString());
                sw.WriteLine(strData);
                sw.WriteLine("----------");
                sw.Close();
            }
            catch (Exception ex)
            {

            }
        }

        public static int GetThaiYear(int intYear)
        {
            int intThaiYear = 0;
            if (intYear < 2500 && intYear > 0)
            {
                intThaiYear = intYear + 543;
            }
            return intThaiYear;
        }



    }//end class
}//end namespace
