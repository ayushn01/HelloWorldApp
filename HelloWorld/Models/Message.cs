using System;
namespace HelloWorld.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string MessageGuid { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
