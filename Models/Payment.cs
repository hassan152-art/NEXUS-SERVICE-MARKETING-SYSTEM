namespace Nexus_Service_Marketing.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        // 🔗 Bill Reference
        public int BillId { get; set; }
        public Bill Bill { get; set; }

        // 💰 Payment Info
        public decimal PaidAmount { get; set; }
        public DateTime PaymentDate { get; set; }

        // 💳 Payment Mode
        public string PaymentMethod { get; set; }
        // Cash / Bank Transfer / JazzCash / EasyPaisa / Card

        // 🧾 Reference (optional)
        public string TransactionNo { get; set; }

        // 👤 Who received payment
        public string ReceivedBy { get; set; } // Employee / Accounts username

        // 🏷 Status
        public string Status { get; set; }
        // Received / Verified / Rejected

        // 📝 Remarks
        public string Remarks { get; set; }
    }

}
