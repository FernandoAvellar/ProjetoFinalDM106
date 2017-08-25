using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ProjetoFinalDM106.Models
{
    public class Order
    {
        public Order()
        {
            this.OrderItems = new HashSet<OrderItem>();
        }

        public int Id { get; set; }

        public string userName { get; set; }

        public DateTime orderDate { get; set; }

        public DateTime deliveryDate { get; set; }

        public enum status { novo, fechado, cancelado, entregue };

        public decimal precoTotal { get; set; }

        public decimal pesoTotal { get; set; }

        public decimal precoFrete { get; set; }

        public virtual ICollection<OrderItem> OrderItems
        {
            get; set;
        }
    }
}