using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using jcs.clientdb;

namespace CyberRequest
{
    public class XSQLProgramLink
    {
        private Recordset rsXSQL = null;
        public XSQLProgramLink(InterfaceDatabase conMasterCgs)
        {
            rsXSQL = conMasterCgs.GetRecordset("select PID,ProjectName,SQLName ,SQL,Comment from XSQLProgramLink where ProjectName='CyberRequest'");
            rsXSQL.SetOrderAsc("SQLName");
        }

        public string GetSQL(string sqlname)
        {
            string ret = "";
            if (sqlname.Length > 0)
            {
                int pos=rsXSQL.Seek(0,sqlname);
                if (pos >= 0)
                {
                    ret = rsXSQL["SQL"].StringValue;
                }


            }
            return ret;
        }
    }
}
