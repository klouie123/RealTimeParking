using System;
using System.Collections.Generic;
using System.Text;

namespace RealTimeParkingApp.Models
{
    public class SimpleActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
