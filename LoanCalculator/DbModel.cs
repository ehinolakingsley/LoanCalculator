using System;

namespace LoanCalculator
{
    public  class DbModel
    {
        public int InvoiceId { get; set; }

        public string CustomerName { get; set; }

        public double CustomerContact { get; set; }

        public double InterestAmount { get; set; }

        public double TotalAmount { get; set; }

        public DateTime DateTime { get; set; }

        public string InterestAmount_ => InterestAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
        public string TotalAmount_ => TotalAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-NG"));
        public string CustomerContact_ => string.Format("{0:(###) ###-####}", CustomerContact);
    }
}
