using System;
namespace HelloWorld.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }  // foreign key to ApplicationUser.Id
        public string ReceiverId { get; set; } // foreign key to ApplicationUser.Id
        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
