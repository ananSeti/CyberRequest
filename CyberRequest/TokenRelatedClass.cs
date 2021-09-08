/*
converted automatically from: 
https://www.freecodeformat.com/json2csharp.php
*/

using System.Collections.Generic;

namespace CyberRequest {

    public class BranchPermitItem {
        public string branchType { get; set; }
        public string branchCode { get; set; }
        public string permissionCode { get; set; }
    }

    public class TokenRelatedClass {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
        public int userId { get; set; }
        public List<BranchPermitItem> branchPermit { get; set; }
        public string iss { get; set; }
        public string iat { get; set; }
        public string salt { get; set; }
        public string jti { get; set; }
    }
}