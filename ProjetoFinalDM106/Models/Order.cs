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
            this.orderDate = DateTime.Now;
            this.deliveryDate = DateTime.Now;
            this.status = "novo";
            this.pesoTotal = 0;
            this.precoFrete = 0;
            this.precoTotal = 0;
        }

        public int Id { get; set; }

        public string userName { get; set; }

        public DateTime orderDate { get; set; }

        public DateTime deliveryDate { get; set; }

        public string status { get; set; }

        public decimal precoTotal { get; set; }

        public decimal pesoTotal { get; set; }

        public decimal precoFrete { get; set; }

        public virtual ICollection<OrderItem> OrderItems
        {
            get; set;
        }
    }
}