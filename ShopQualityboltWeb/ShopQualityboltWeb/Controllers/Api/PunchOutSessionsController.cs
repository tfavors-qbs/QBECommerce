using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Http.ContentTypes.Identity;
using QBExternalWebLibrary.Services.Model;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ariba;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Ariba;
using NuGet.Protocol.Plugins;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Frozen;
using QBExternalWebLibrary.Models.Catalog;
using System.Collections.Generic;

namespace ShopQualityboltWeb.Controllers.Api
{
	[Route("api/punchoutsessions")]
	[ApiController]
	public class PunchOutSessionsController : ControllerBase
	{
		private readonly IConfiguration _configuration; // Added for configuration
		private readonly IModelService<PunchOutSession, PunchOutSession> _service;
		private readonly IModelService<QBExternalWebLibrary.Models.ContractItem, ContractItemEditViewModel> _contractItemService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IModelService<ShoppingCart, ShoppingCartEVM> _shoppingCartService;

		public PunchOutSessionsController(IConfiguration configuration, IModelService<PunchOutSession, PunchOutSession> service, UserManager<ApplicationUser> userManager, IModelService<QBExternalWebLibrary.Models.ContractItem, ContractItemEditViewModel> contractItemService, IModelService<ShoppingCart, ShoppingCartEVM> shoppingCartService)
		{
			_configuration = configuration;
			_service = service;
			_userManager = userManager;
			_contractItemService = contractItemService;
			_shoppingCartService = shoppingCartService;
		}

		[HttpPost("request-punch-out")]
		[AllowAnonymous]
		public async Task<ActionResult> Register()
		{
			try
			{
				// Read raw cXML from request body
				string cxmlString;
				using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
				{
					cxmlString = await reader.ReadToEndAsync();
				}

				// Deserialize cXML
				var serializer = new XmlSerializer(typeof(cXML));
				cXML cxmlRequest;
				using (var stringReader = new StringReader(cxmlString))
				{
					cxmlRequest = (cXML)serializer.Deserialize(stringReader);
				}

				// Find Header and Request in Items
				Header header = null;
				Request request = null;
				foreach (var item in cxmlRequest.Items)
				{
					if (item is Header h)
					{
						header = h;
					}
					else if (item is Request r)
					{
						request = r;
					}
				}

				// Validate request
				if (request == null || request.Item is not PunchOutSetupRequest punchOutSetupRequest)
				{
					return BadRequest(CreateErrorResponse("400", "Invalid or missing PunchOutSetupRequest"));
				}

				var from = header?.From?.FirstOrDefault(c => c.domain == "DUNS")?.Identity?.Any?.FirstOrDefault()?.Value ?? "";
				if (string.IsNullOrEmpty(from))
				{
					return BadRequest(CreateErrorResponse("400", "Invalid header or From identity"));
				}

				var credential = header?.Sender?.Credential?.FirstOrDefault(c => c.domain == "NetworkId");
				// Validate credentials (e.g., SharedSecret)
				if (header == null || credential?.Item is not SharedSecret sharedSecret)
				{
					return BadRequest(CreateErrorResponse("400", "Invalid header or shared secret credential"));
				}

				string aribaId = credential?.Identity.Any?.FirstOrDefault()?.Value;
				if (header == null || string.IsNullOrEmpty(aribaId))
				{
					return BadRequest(CreateErrorResponse("400", "Invalid ariba credential"));
				}

				// Extract SharedSecret value from Any property
				string sharedSecretValue = sharedSecret.Any?.FirstOrDefault()?.Value;
				if (string.IsNullOrEmpty(sharedSecretValue) || sharedSecretValue != "abracadabra") //TODO: store secret somewhere good
				{
					return Unauthorized(CreateErrorResponse("401", "Invalid credentials"));
				}

				// Store BuyerCookie and BrowserFormPost URL
				var buyerCookie = punchOutSetupRequest.BuyerCookie.Any?.FirstOrDefault()?.Value ?? "";
				var postUrl = punchOutSetupRequest.BrowserFormPost?.URL?.Value ?? "";
				var email = punchOutSetupRequest.Contact?.FirstOrDefault()?.Email?.FirstOrDefault()?.Value;
				email = punchOutSetupRequest.Extrinsic.Where(x => x.name == "ClientUserID")?.FirstOrDefault().Any[0].Value;
				HttpContext.Session.SetString("BuyerCookie", buyerCookie);
				HttpContext.Session.SetString("PostUrl", postUrl);

				if(email == null)
				{
					return BadRequest(CreateErrorResponse("400", "Invalid email"));
				}

				string newSessionId = $"{Random.Shared.Next(10000000, 99999999)}";

				// Create PunchOutSetupResponse
				var response = new cXML
				{
					version = "1.2.014",
					payloadID = $"{DateTime.UtcNow.Ticks}@yourdomain.com",
					timestamp = DateTime.UtcNow.ToString("o"),
					Items = new object[]
					{
						new Response
						{
							Status = new Status { code = "200", text = "success" },
							Item = new PunchOutSetupResponse
							{
								StartPage = new StartPage
								{
									URL = new(){ Value = $"https://localhost:7169/login?sessionId={newSessionId}" }
								}
							}
						}
					}
				};

				string responseString;
				var responseSerializer = new XmlSerializer(typeof(cXML));
				using (var writer = new StringWriter())
				{
					responseSerializer.Serialize(writer, response);
					responseString = writer.ToString();
				}

				if(responseString == null)
				{
					return StatusCode(500, CreateErrorResponse("500", $"Internal server error: failed to serialize cXML response"));
				}

				var user = await _userManager.FindByEmailAsync(email);
				if(user == null)
				{
					return BadRequest(CreateErrorResponse("400", "No user could be found for the given email"));
				}

				if(user.AribaId != aribaId)
				{
					return Unauthorized(CreateErrorResponse("401", "The ariba id given could not be associated with the email account given"));
				}

				
				if(punchOutSetupRequest.operation == PunchOutSetupRequestOperation.edit || punchOutSetupRequest.operation == PunchOutSetupRequestOperation.inspect)
				{
					var usersShoppingCart = _shoppingCartService.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
					if(usersShoppingCart == null)
					{
						return Unauthorized(CreateErrorResponse("401", "Could not find user's shopping cart to edit"));
					}

					List<ShoppingCartItemEVM> existingItems = new();
					for (int i = 0; i < punchOutSetupRequest.ItemOut.Length; i++)
					{
						ItemOut item = punchOutSetupRequest.ItemOut[i];
						if(item.ItemID != null)
						{
							SupplierPartAuxiliaryID supplierPartAuxiliaryID = item.ItemID.SupplierPartAuxiliaryID;
							string contractItemId = supplierPartAuxiliaryID?.Any?.FirstOrDefault()?.Value;
							if (!string.IsNullOrEmpty(contractItemId))
							{
								if(!int.TryParse(contractItemId, out int contractItemIdInt))
								{
									if (!int.TryParse(item.quantity, out int qty))
									{
										//REFACTOR: instead of getting 1 by 1 inside foreach, pool them up and get all at once after foreach loop
										QBExternalWebLibrary.Models.ContractItem contractItem = _contractItemService.GetById(contractItemIdInt);
										if(contractItem != null)
										{
											existingItems.Add(new() { ContractItemId = contractItemIdInt, Quantity = qty, ShoppingCartId = usersShoppingCart.Id });
										}
									}
								}
							}
						}
					}

					if (existingItems.Count > 0)
					{
						//TODO: clear the cart, then add existingItems to cart
					}
				}

				var punchOutResult = await PostSession(
					new() 
					{
						SessionId = newSessionId,
						PostUrl = postUrl,
						FromId = from,
						BuyerCookie = buyerCookie,
						CreatedDateTime = DateTime.Now,
						ExpirationDateTime = DateTime.Now.AddMinutes(PunchOutSession.StartingMinutesToExpire),
						Operation = punchOutSetupRequest.operation.ToString(),
						UserId = user.Id
					}
				);

				if(punchOutResult == null)
				{
					return StatusCode(500, CreateErrorResponse("500", $"Internal server error: failed to create a punch out session for request"));
				}

				return Content(responseString, "text/xml");
			}
			catch (Exception ex)
			{
				// Log exception (use your logging framework)
				return StatusCode(500, CreateErrorResponse("500", $"Internal server error: {ex.Message}"));
			}

			string CreateErrorResponse(string code, string message)
			{
				var errorResponse = new cXML
				{
					version = "1.2.014",
					payloadID = $"{DateTime.UtcNow.Ticks}@yourdomain.com",
					timestamp = DateTime.UtcNow.ToString("o"),
					Items = new object[]
					{
						new Response
						{
							Status = new Status { code = code, text = message }
						}
					}
				};

				var serializer = new XmlSerializer(typeof(cXML));
				using (var writer = new StringWriter())
				{
					serializer.Serialize(writer, errorResponse);
					return writer.ToString();
				}
			}
		}

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<IEnumerable<PunchOutSession>>> GetSessions()
		{
			return _service.GetAll().ToList();
		}

		[HttpGet("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<PunchOutSession>> GetSession(int id)
		{
			var session = _service.GetById(id);

			if (session == null)
			{
				return NotFound();
			}

			return session;
		}

		[HttpGet("sessionid/{sessionId}")]
		[AllowAnonymous]
		public async Task<ActionResult<PunchOutSession>> GetSessionBySessionId(string sessionId)
		{
			var session = _service.Find(a => a.SessionId == sessionId).FirstOrDefault();

			if (session == null)
			{
				return NotFound();
			}

			return session;
		}

		[HttpPut("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<PunchOutSession>> PutSession(int id, PunchOutSession session)
		{
			if (id != session.Id)
			{
				return BadRequest();
			}

			PunchOutSession updatedSession;
			try
			{
				updatedSession = _service.Update(session);
			}
			catch (DbUpdateConcurrencyException)
			{
				if (_service.GetById(id) == null)
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return Ok(updatedSession);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<PunchOutSession>> PostSession(PunchOutSession session)
		{
			if (_service.GetAll().Any(c => c.SessionId == session.SessionId))
			{
				return Conflict("Session with that session id already exists.");
			}

			var newSession = _service.Create(session);

			return CreatedAtAction(nameof(GetSession), new { id = newSession.Id }, newSession);
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteSession(int id)
		{
			var session = _service.GetById(id);
			if (session == null)
			{
				return NotFound();
			}

			_service.Delete(session);

			return NoContent();
		}
	}
}
