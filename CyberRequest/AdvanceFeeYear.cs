namespace CyberRequest {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class AvResult {
        public int advFeeId { get; set; }
        public int advFeeYearId { get; set; }
        public int productId { get; set; }
        public int bankId { get; set; }
        public string advFeeYear { get; set; }
        public string advFeeYearDesc { get; set; }
    }
    public class AdvanceFeeYear {
        public int responseCode { get; set; }
        public string responseStatus { get; set; }
        public string responseMessage { get; set; }
        // public List<Result> result { get; set; }
        public AvResult[] result { get; set; }
    }
}