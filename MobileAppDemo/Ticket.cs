using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileAppDemo
{
    public class Ticket
    {
        public DateTime printTimestamp { get; set; }
        public string recipeStart {  get; set; }
        public string recipeStop {  get; set; }
        public string mixingTime { get; set; }
        public string recipeName { get; set; }
        public string volume { get; set; }
        public TicketReceiptComponent[] component { get; set; }
        public string totalWeight { get; set; }
        public string totalTarget { get; set; }
        public string totalError { get; set; }
        public string ticketId { get; set; }
        public string machineId { get; set; }
        public string destinationId { get; set; }
        public string operatorId { get; set; }

    }
}
