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
using ProjetoFinalDM106.br.com.correios.ws;
using ProjetoFinalDM106.CRMClient;
using System.Globalization;

namespace ProjetoFinalDM106.Controllers
{
    [Authorize]
    [RoutePrefix("api/Orders")]
    public class OrdersController : ApiController
    {
        private ProjetoFinalDM106Context db = new ProjetoFinalDM106Context();

        // GET: api/Orders
        [Authorize(Roles = "ADMIN")]
        public List<Order> GetOrders()
        {
            return db.Orders.Include(order => order.OrderItems).ToList();
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

        // GET: api/Orders/byemail?email={email}
        [ResponseType(typeof(Order))]
        [HttpGet]
        [Route("byemail")]
        public IHttpActionResult GetOrderByEmail(string email)
        {
            var order = db.Orders.Where(o => o.userName == email).ToList();
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

        // POST: api/Orders/{orderId}/close
        [ResponseType(typeof(Order))]
        [HttpPost]
        [Route("{id}/close")]
        public IHttpActionResult CloseProductById(int id)
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

            if(order.precoFrete == 0)
            {
                return StatusCode(HttpStatusCode.MethodNotAllowed);
            }                

            order.status = "fechado";
            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();

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

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            db.Orders.Remove(order);
            db.SaveChanges();

            return Ok(order);
        }

        // POST: api/Orders/{orderId}/calcularfrete
        [ResponseType(typeof(Order))]
        [HttpPost]
        [Route("{id}/calcularfrete")]
        public IHttpActionResult CalculaFreteEPrazo(int id)
        {
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!order.status.Equals("novo"))
            {
                return BadRequest("Status do pedido inválido, o pedido precisa estar no estado 'novo'.");
            }

            if (order.OrderItems.Count == 0 )
            {
                return BadRequest("Pedido sem nenhum item.");
            }

            if (!User.IsInRole("ADMIN") && !User.Identity.Name.Equals(order.userName))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            cResultado resultadoCorreios = calculaFretePrazoEntrega(order);

            string frete = "0";
            string prazoEntrega = "0";

            if (resultadoCorreios.Servicos[0].Erro.Equals("0"))
            {
                frete = resultadoCorreios.Servicos[0].Valor;
                prazoEntrega = resultadoCorreios.Servicos[0].PrazoEntrega;
            }
            else
            {
                return BadRequest("Falha na consulta aos Correios - Código do erro: " + resultadoCorreios.Servicos[0].Erro + "- Mensagem: " + resultadoCorreios.Servicos[0].MsgErro);
            }

            order.precoFrete = Convert.ToDecimal(frete, new CultureInfo("pt-BR"));
            order.precoTotal = calculaPrecoOrdem(order) + order.precoFrete;
            order.deliveryDate = order.orderDate.AddDays(Convert.ToDouble(prazoEntrega));

            db.Entry(order).State = EntityState.Modified;
            db.SaveChanges();

            return Ok(order);
        }

        private cResultado calculaFretePrazoEntrega(Order order)
        {
            string codigoEmpresa = "";          //Empresa não conveniada com os correios
            string senhaEmpresa = "";           //Empresa não conveniada com os correios
            string codigoServico = "40010";     //Sedex varejo
            string cepOrigem = "37540000";      //Empresa situada em Santa Rita do Sapucaí
            string cepDestino = recuperaCepDoUsuarioNoCRM(order.userName);
            string pesoTotalEmKg = calculaPesoTotal(order).ToString();
            int formatoEmbalagem = 1;           //Formato de envio fixo usando caixa
            decimal comprimentoTotalEmCm = calculaComprimentoTotal(order);
            decimal itemDeMaiorAlturaEmCm = calculaMaiorAltura(order);
            decimal itemDeMaiorLarguraEmCm = calculaMaiorLargura(order);
            decimal itemDeMaiorDiametroEmCm = 0;      //Fixado em zero dado que só enviaremos produtos em caixa e não em embalagem do tipo Rolo ou Prisma
            string avisoMaoPropia = "N";
            decimal valorDeclarado = 0;
            string avisoRecebimento = "N";

            CalcPrecoPrazoWS correios = new CalcPrecoPrazoWS();

            return correios.CalcPrecoPrazo(codigoEmpresa, senhaEmpresa, codigoServico, cepOrigem, cepDestino, pesoTotalEmKg, formatoEmbalagem,
                comprimentoTotalEmCm, itemDeMaiorAlturaEmCm, itemDeMaiorLarguraEmCm, itemDeMaiorDiametroEmCm, avisoMaoPropia, valorDeclarado, avisoRecebimento);
        }

        private string recuperaCepDoUsuarioNoCRM(string userEmail)
        {
            CRMRestClient crmClient = new CRMRestClient();
            return crmClient.GetCustomerByEmail(userEmail).zip;
        }

        private decimal calculaPesoTotal(Order order)
        {
            decimal pesoTotalOrdem = 0;

            foreach(OrderItem item in order.OrderItems)
            {
                Product product = db.Products.Find(item.ProductId);
                decimal pesoUnitarioProduto = product.peso;
                decimal pesoTotalProduto = pesoUnitarioProduto * item.quantity;
                pesoTotalOrdem += pesoTotalProduto;
            }

            //Atualiza Ordem
            order.pesoTotal = pesoTotalOrdem;

            return pesoTotalOrdem;
        }

        private decimal calculaComprimentoTotal(Order order)
        {
            decimal comprimentoTotalOrdem = 0;

            foreach (OrderItem item in order.OrderItems)
            {
                Product product = db.Products.Find(item.ProductId);
                decimal comprimentoUnitarioProduto = product.comprimento;
                decimal comprimentoTotalProduto = comprimentoUnitarioProduto * item.quantity;
                comprimentoTotalOrdem += comprimentoTotalProduto;
            }

            if(comprimentoTotalOrdem < 16)
            {
                comprimentoTotalOrdem = 16;
            }
            else if (comprimentoTotalOrdem > 105)
            {
                comprimentoTotalOrdem = 105;
            }

            return comprimentoTotalOrdem;
        }

        private decimal calculaMaiorAltura(Order order)
        {
            decimal maiorAltura = 0;

            foreach (OrderItem item in order.OrderItems)
            {
                Product product = db.Products.Find(item.ProductId);
                decimal alturaProdutoAtual = product.altura;
                if(alturaProdutoAtual > maiorAltura)
                {
                    maiorAltura = alturaProdutoAtual;
                }
            }

            if (maiorAltura < 2)
            {
                maiorAltura = 2;
            }
            else if (maiorAltura > 105)
            {
                maiorAltura = 105;
            }

            return maiorAltura;
        }

        private decimal calculaMaiorLargura(Order order)
        {
            decimal maiorLargura = 0;

            foreach (OrderItem item in order.OrderItems)
            {
                Product product = db.Products.Find(item.ProductId);
                decimal laguraProdutoAtual = product.largura;
                if (laguraProdutoAtual > maiorLargura)
                {
                    maiorLargura = laguraProdutoAtual;
                }
            }

            if (maiorLargura < 11)
            {
                maiorLargura = 11;
            }
            else if (maiorLargura > 105)
            {
                maiorLargura = 105;
            }

            return maiorLargura;
        }

        private decimal calculaPrecoOrdem(Order order)
        {
            decimal precoTotalOrdem = 0;

            foreach (OrderItem item in order.OrderItems)
            {
                Product product = db.Products.Find(item.ProductId);
                decimal precoUnitarioProduto = product.preco;
                decimal precoTotalProduto = precoUnitarioProduto * item.quantity;
                precoTotalOrdem += precoTotalProduto;
            }
            return precoTotalOrdem;
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