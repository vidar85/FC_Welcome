using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FCWelcome
{
    public class MessageSettings
    {
        public string Message { get; set; } = "";

        public bool IsEnabled { get; set; } = true;
    }

    public class Message
    {
        public string message = "";
        public DateTime time;
    }


    public class MessageTypeSettings
    {
        public bool IsEnabled { get; set; } = false;
        public int MinSeconds { get; set; } = 10;
        public int MaxSeconds { get; set; } = 20;

        public bool UseOnlyFirstName { get; set; } = true;

        private Random random = new Random();

        public List<MessageSettings> Messages { get; set; } = new List<MessageSettings>();

        public Message CreateMessage(string name)
        {
            if (this.UseOnlyFirstName)
            {
                name = name.Split(' ')[0];
            }

            List<MessageSettings> enabledMessages = this.Messages.Where(x => x.IsEnabled).ToList();
            string message = enabledMessages[this.random.Next(0, enabledMessages.Count - 1)].Message.Replace("<t>", name);
            return new Message { message = message, time = DateTime.Now.AddSeconds(this.random.Next(this.MinSeconds, this.MaxSeconds)) };
        }
    }
}
