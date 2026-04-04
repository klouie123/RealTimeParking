using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.DTOs
{
    public class PaymentRequestDto
    {
        public bool Success { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MerchantDisplayName { get; set; }
        public string? MerchantGcashNumber { get; set; }
        public string? ExternalPaymentUrl { get; set; }
    }
}
