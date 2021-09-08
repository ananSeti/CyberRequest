using System;
using System.Collections.Generic;

namespace CyberRequest {

    public class ContentFile {
        public int fileTypeId { get; set; }
        public string fileTypeName { get; set; }
        public Object[] fileList { get; set; }
    }

    public class FileGroup {
        public string status { get; set; }
        public string message { get; set; }
        public ContentFile[] content { get; set; }
        public object page { get; set; }
        public object perPage { get; set; }
        public object totalPage { get; set; }
        public object totalElement { get; set; }
    }
}