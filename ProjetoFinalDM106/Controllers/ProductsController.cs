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
    public class ProductsController : ApiController
    {
        private ProjetoFinalDM106Context db = new ProjetoFinalDM106Context();

        // GET: api/Products
        [Authorize]
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: api/Products/5
        [Authorize]
        [ResponseType(typeof(Product))]
        public IHttpActionResult GetProduct(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // PUT: api/Products/5
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutProduct(int id, Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != product.Id)
            {
                return BadRequest();
            }

            if (ModeloOuCodigoJaPresenteNaTabela(product, true))
            {
                return BadRequest("Modelo e/ou Codigo já presente na tabela!");
            }

            db.Entry(product).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Products
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult PostProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (ModeloOuCodigoJaPresenteNaTabela(product, false))
            {
                return BadRequest("Modelo e/ou Codigo já presente na tabela!");
            }

            db.Products.Add(product);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = product.Id }, product);
        }

        // DELETE: api/Products/5
        [Authorize(Roles = "ADMIN")]
        [ResponseType(typeof(Product))]
        public IHttpActionResult DeleteProduct(int id)
        {
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            db.Products.Remove(product);
            db.SaveChanges();

            return Ok(product);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(int id)
        {
            return db.Products.Count(e => e.Id == id) > 0;
        }

        private bool ModeloOuCodigoJaPresenteNaTabela(Product product, Boolean isPutOperation)
        {
            if (isPutOperation)
            {
                return (db.Products.Count(p => p.modelo.Equals(product.modelo)) == 1) || (db.Products.Count(p => p.codigo.Equals(product.codigo)) == 1);
            }

            return (db.Products.Count(p => p.modelo.Equals(product.modelo)) > 0) || (db.Products.Count(p => p.codigo.Equals(product.codigo)) > 0);
        }
    }
}