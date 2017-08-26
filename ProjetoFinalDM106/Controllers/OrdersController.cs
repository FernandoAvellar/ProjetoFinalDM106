using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ProjetoFinalDM106.Models;

namespace ProjetoFinalDM106.Controllers
{
    [Authorize]
    [RoutePrefix("api/Orders")]
    public class OrdersController : ApiController
    {
        private ProjetoFinalDM106Context db = new ProjetoFinalDM106Context();

        // GET: api/Orders
        [Authorize(Roles = "ADMIN")]
        public IQueryable<Order> GetOrders()
        {
            return db.Orders;
        }

        // GET: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult GetOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            return Ok(order);
        }

        // POST: api/Orders
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Orders.Add(order);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
        }

        [ResponseType(typeof(Order))]
        [HttpGet]
        [Route("byemail")]
        public IHttpActionResult GetOrderByEmail(string email)
        {
            var order = db.Orders.Where(o => o.userName == email);
            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(email))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            return Ok(order);
        }


        // DELETE: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }
    }
}