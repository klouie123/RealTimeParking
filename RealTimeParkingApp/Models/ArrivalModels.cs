using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class ActiveReservationArrivalModel
    {
        public int Id { get; set; }
        public int ParkingSlotId { get; set; }
        public string ParkingLocationName { get; set; } = string.Empty;
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsPaid { get; set; }
        public decimal? Amount { get; set; }
        public string? QrCodeValue { get; set; }
    }

    public class ArrivalResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NewStatus { get; set; }
    }

    public class ManualArrivalReservationModel
    {
        public int ReservationId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string SlotCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReservedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsPaid { get; set; }
    }

    public class SimulatedPaymentRequestModel
    {
        public int ReservationId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Simulated";
        public string ReferenceNumber { get; set; } = string.Empty;
    }
}
