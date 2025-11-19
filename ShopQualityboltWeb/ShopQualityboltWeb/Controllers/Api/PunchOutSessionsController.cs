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
using ShopQualityboltWeb.Services;

namespace ShopQualityboltWeb.Controllers.Api
{
	[Route("api/punchoutsessions")]
	[ApiController]
	public class PunchOutSessionsController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly IModelService<PunchOutSession, PunchOutSession> _service;
		private readonly IModelService<QBExternalWebLibrary.Models.ContractItem, ContractItemEditViewModel> _contractItemService;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IModelService<ShoppingCart, ShoppingCartEVM> _shoppingCartService;
		private readonly IErrorLogService _errorLogService;
		private readonly ILogger<PunchOutSessionsController> _logger;

		public PunchOutSessionsController(
			IConfiguration configuration, 
			IModelService<PunchOutSession, PunchOutSession> service, 
			UserManager<ApplicationUser> userManager, 
			IModelService<QBExternalWebLibrary.Models.ContractItem, ContractItemEditViewModel> contractItemService, 
			IModelService<ShoppingCart, ShoppingCartEVM> shoppingCartService,
			IErrorLogService errorLogService,
			ILogger<PunchOutSessionsController> logger)
		{
			_configuration = configuration;
			_service = service;
			_userManager = userManager;
			_contractItemService = contractItemService;
			_shoppingCartService = shoppingCartService;
			_errorLogService = errorLogService;
			_logger = logger;
		}

		[HttpPost("request-punch-out")]
		[AllowAnonymous]
		public async Task<ActionResult> Register()
		{
			string cxmlString = null;
			try
			{
				// Read raw cXML from request body
				using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
				{
					cxmlString = await reader.ReadToEndAsync();
				}

                //remove & symbols that break deserialization
				cxmlString = cxmlString.Replace("&", "&amp;");
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
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Invalid PunchOutSetupRequest",
						"Missing or invalid PunchOutSetupRequest in cXML",
						additionalData: new { cxmlContent = cxmlString?.Substring(0, Math.Min(500, cxmlString?.Length ?? 0)) },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "Invalid or missing PunchOutSetupRequest"));
				}

				var from = header?.From?.FirstOrDefault(c => c.domain == "DUNS" || c.domain == "NetworkID")?.Identity?.Any?.FirstOrDefault()?.Value ?? "";
				if (string.IsNullOrEmpty(from))
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Invalid Header Identity",
						"Missing or invalid From identity in header",
						additionalData: new { header = header },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "Invalid header or From identity"));
				}

				var credential = header?.Sender?.Credential?.FirstOrDefault(c => c.domain == "NetworkId" || c.domain == "AribaNetworkUserId");
				// Validate credentials (e.g., SharedSecret)
				if (header == null || credential?.Item is not SharedSecret sharedSecret)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Invalid Credentials",
						"Missing or invalid shared secret credential",
						additionalData: new { hasHeader = header != null, hasCredential = credential != null },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "Invalid header or shared secret credential"));
				}

				string aribaId = header?.From?[0].Identity?.Any?.FirstOrDefault()?.Value ?? "";

				if (header == null || string.IsNullOrEmpty(aribaId))
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Invalid Ariba Credential",
						"Missing or invalid Ariba ID",
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "Invalid ariba credential"));
				}

				// Extract SharedSecret value from Any property
				string sharedSecretValue = sharedSecret.Any?.FirstOrDefault()?.Value;
				string expectedSharedSecret = _configuration["Ariba:SharedSecret"];
				
				if (string.IsNullOrEmpty(sharedSecretValue) || string.IsNullOrEmpty(expectedSharedSecret))
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Missing Shared Secret",
						"Shared secret not provided or not configured",
						additionalData: new { hasProvidedSecret = !string.IsNullOrEmpty(sharedSecretValue), hasConfiguredSecret = !string.IsNullOrEmpty(expectedSharedSecret) },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 401);
					return Unauthorized(CreateErrorResponse("401", "Invalid credentials"));
				}
				
				if (sharedSecretValue != expectedSharedSecret)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Invalid Shared Secret",
						"Provided shared secret does not match expected value",
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 401);
					return Unauthorized(CreateErrorResponse("401", "Invalid credentials"));
				}

				// Store BuyerCookie and BrowserFormPost URL
				var buyerCookie = punchOutSetupRequest.BuyerCookie.Any?.FirstOrDefault()?.Value ?? "";
				var postUrl = punchOutSetupRequest.BrowserFormPost?.URL?.Value ?? "";
				var email = punchOutSetupRequest.Contact?.FirstOrDefault()?.Email?.FirstOrDefault()?.Value;
				email = punchOutSetupRequest.Extrinsic.Where(x => x.name == "UserEmail")?.FirstOrDefault().Any[0].Value;
				HttpContext.Session.SetString("BuyerCookie", buyerCookie);
				HttpContext.Session.SetString("PostUrl", postUrl);

				if(email == null)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Missing Email",
						"User email not found in PunchOut request",
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "Invalid email"));
				}

				string newSessionId = $"{Random.Shared.Next(10000000, 99999999)}";

				// Get the base URL from configuration (BlazorAppUrl for production, or default for development)
				string baseUrl = _configuration["AppSettings:BlazorAppUrl"] ?? "https://localhost:7169";

				// Create PunchOutSetupResponse
				var response = new cXML
				{
					version = "1.2.014",
					payloadID = $"{DateTime.UtcNow.Ticks}@yourdomain.com",
					timestamp = DateTime.UtcNow.ToString("o"),
					lang = "en-US",
					Items = new object[]
					{
						new Response
						{
							Status = new Status { code = "200", text = "success" },
							Item = new PunchOutSetupResponse
							{
								StartPage = new StartPage
								{
									URL = new(){ Value = $"{baseUrl}/login?sessionId={newSessionId}" }
								}
							}
						}
					}
				};

				string responseString;
				var responseSerializer = new XmlSerializer(typeof(cXML));
				
				// Create XmlWriterSettings to control the output
				var settings = new System.Xml.XmlWriterSettings
				{
					Encoding = new UTF8Encoding(false), // UTF-8 without BOM
					Indent = false,
					OmitXmlDeclaration = false
				};
				
				// Create empty namespaces to remove xsi and xsd namespace declarations
				var namespaces = new XmlSerializerNamespaces();
				namespaces.Add("", ""); // Add empty namespace
				
				using (var memoryStream = new MemoryStream())
				{
					using (var xmlWriter = System.Xml.XmlWriter.Create(memoryStream, settings))
					{
						responseSerializer.Serialize(xmlWriter, response, namespaces);
					}
					memoryStream.Position = 0;
					using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
					{
						responseString = reader.ReadToEnd();
					}
				}

				// Ensure version attribute is present (XmlSerializer may omit it due to DefaultValueAttribute)
				if (!responseString.Contains("version=\""))
				{
					responseString = responseString.Replace("<cXML ", "<cXML version=\"1.2.014\" ");
				}

				// Add DOCTYPE declaration after XML declaration
				const string xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
				const string docType = "<!DOCTYPE cXML SYSTEM \"http://xml.cxml.org/schemas/cXML/1.2.014/cXML.dtd\">";
				if (responseString.StartsWith(xmlDeclaration))
				{
					responseString = xmlDeclaration + docType + responseString.Substring(xmlDeclaration.Length);
				}

				if(responseString == null)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Failed to Serialize Response",
						"Failed to serialize cXML response",
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 500);
					return StatusCode(500, CreateErrorResponse("500", $"Internal server error: failed to serialize cXML response"));
				}

				var user = await _userManager.FindByEmailAsync(email);
				if(user == null)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"User Not Found",
						$"No user found for email: {email}",
						additionalData: new { email },
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						userEmail: email,
						statusCode: 400);
					return BadRequest(CreateErrorResponse("400", "No user could be found for the given email"));
				}

				if(user.AribaId != aribaId)
				{
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Ariba ID Mismatch",
						$"Ariba ID {aribaId} does not match user's Ariba ID",
						additionalData: new { providedAribaId = aribaId, userAribaId = user.AribaId, email },
						userId: user.Id,
						userEmail: user.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						statusCode: 401);
					return Unauthorized(CreateErrorResponse("401", "The ariba id given could not be associated with the email account given"));
				}

				
				if(punchOutSetupRequest.operation == PunchOutSetupRequestOperation.edit || punchOutSetupRequest.operation == PunchOutSetupRequestOperation.inspect)
				{
					var usersShoppingCart = _shoppingCartService.Find(a => a.ApplicationUserId == user.Id).FirstOrDefault();
					if(usersShoppingCart == null)
					{
						await _errorLogService.LogErrorAsync(
							"PunchOut Setup Error",
							"Shopping Cart Not Found",
							"Could not find user's shopping cart for edit operation",
							additionalData: new { operation = punchOutSetupRequest.operation.ToString(), userId = user.Id },
							userId: user.Id,
							userEmail: user.Email,
							requestUrl: HttpContext.Request.Path,
							httpMethod: HttpContext.Request.Method,
							statusCode: 401);
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
					await _errorLogService.LogErrorAsync(
						"PunchOut Setup Error",
						"Failed to Create Session",
						"Failed to create punch out session in database",
						additionalData: new { sessionId = newSessionId, userId = user.Id },
						userId: user.Id,
						userEmail: user.Email,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method,
						sessionId: newSessionId,
						statusCode: 500);
					return StatusCode(500, CreateErrorResponse("500", $"Internal server error: failed to create a punch out session for request"));
				}

				_logger.LogInformation("Successfully created PunchOut session {SessionId} for user {Email}", newSessionId, user.Email);

				return Content(responseString, "text/xml; charset=utf-8");
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"PunchOut Setup Error",
					"Unexpected Error in PunchOut Setup",
					ex.Message,
					ex,
					additionalData: new { cxmlPreview = cxmlString?.Substring(0, Math.Min(500, cxmlString?.Length ?? 0)) },
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					statusCode: 500);

				_logger.LogError(ex, "Unexpected error in PunchOut setup");
				return StatusCode(500, CreateErrorResponse("500", $"Internal server error: {ex.Message}"));
			}

			string CreateErrorResponse(string code, string message)
			{
				var errorResponse = new cXML
				{
					version = "1.2.014",
					payloadID = $"{DateTime.UtcNow.Ticks}@yourdomain.com",
					timestamp = DateTime.UtcNow.ToString("o"),
					lang = "en-US",
					Items = new object[]
					{
						new Response
						{
							Status = new Status { code = code, text = message }
						}
					}
				};

				var serializer = new XmlSerializer(typeof(cXML));
				
				// Create XmlWriterSettings to control the output
				var settings = new System.Xml.XmlWriterSettings
				{
					Encoding = new UTF8Encoding(false), // UTF-8 without BOM
					Indent = false,
					OmitXmlDeclaration = false
				};
				
				// Create empty namespaces to remove xsi and xsd namespace declarations
				var namespaces = new XmlSerializerNamespaces();
				namespaces.Add("", ""); // Add empty namespace
				
				string errorString;
				using (var memoryStream = new MemoryStream())
				{
					using (var xmlWriter = System.Xml.XmlWriter.Create(memoryStream, settings))
					{
						serializer.Serialize(xmlWriter, errorResponse, namespaces);
					}
					memoryStream.Position = 0;
					using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
					{
						errorString = reader.ReadToEnd();
					}
				}
				
				// Ensure version attribute is present (XmlSerializer may omit it due to DefaultValueAttribute)
				if (!errorString.Contains("version=\""))
				{
					errorString = errorString.Replace("<cXML ", "<cXML version=\"1.2.014\" ");
				}
				
				// Add DOCTYPE declaration after XML declaration
				const string xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
				const string docType = "<!DOCTYPE cXML SYSTEM \"http://xml.cxml.org/schemas/cXML/1.2.014/cXML.dtd\">";
				if (errorString.StartsWith(xmlDeclaration))
				{
					errorString = xmlDeclaration + docType + errorString.Substring(xmlDeclaration.Length);
				}
				
				return errorString;
			}
		}

		[HttpGet]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<IEnumerable<PunchOutSession>>> GetSessions()
		{
			try
			{
				return _service.GetAll().ToList();
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Get PunchOut Sessions",
					ex.Message,
					ex,
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, "Failed to retrieve sessions");
			}
		}

		[HttpGet("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<PunchOutSession>> GetSession(int id)
		{
			try
			{
				var session = _service.GetById(id);

				if (session == null)
				{
					return NotFound();
				}

				return session;
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Get PunchOut Session",
					ex.Message,
					ex,
					additionalData: new { sessionId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, "Failed to retrieve session");
			}
		}

		[HttpGet("sessionid/{sessionId}")]
		[AllowAnonymous]
		public async Task<ActionResult<PunchOutSession>> GetSessionBySessionId(string sessionId)
		{
			try
			{
				var session = _service.Find(a => a.SessionId == sessionId).FirstOrDefault();

				if (session == null)
				{
					return NotFound();
				}

				return session;
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Get PunchOut Session by SessionId",
					ex.Message,
					ex,
					additionalData: new { sessionId },
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					sessionId: sessionId);
				return StatusCode(500, "Failed to retrieve session");
			}
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
			catch (DbUpdateConcurrencyException ex)
			{
				if (_service.GetById(id) == null)
				{
					return NotFound();
				}
				else
				{
					await _errorLogService.LogErrorAsync(
						"API Error",
						"Concurrency Error Updating PunchOut Session",
						ex.Message,
						ex,
						additionalData: new { sessionId = id },
						userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
						userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
						requestUrl: HttpContext.Request.Path,
						httpMethod: HttpContext.Request.Method);
					throw;
				}
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Update PunchOut Session",
					ex.Message,
					ex,
					additionalData: new { sessionId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, "Failed to update session");
			}

			return Ok(updatedSession);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<ActionResult<PunchOutSession>> PostSession(PunchOutSession session)
		{
			try
			{
				if (_service.GetAll().Any(c => c.SessionId == session.SessionId))
				{
					return Conflict("Session with that session id already exists.");
				}

				var newSession = _service.Create(session);

				return CreatedAtAction(nameof(GetSession), new { id = newSession.Id }, newSession);
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Create PunchOut Session",
					ex.Message,
					ex,
					additionalData: new { sessionId = session.SessionId },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method,
					sessionId: session.SessionId);
				return StatusCode(500, "Failed to create session");
			}
		}

		[HttpDelete("{id}")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteSession(int id)
		{
			try
			{
				var session = _service.GetById(id);
				if (session == null)
				{
					return NotFound();
				}

				_service.Delete(session);

				return NoContent();
			}
			catch (Exception ex)
			{
				await _errorLogService.LogErrorAsync(
					"API Error",
					"Failed to Delete PunchOut Session",
					ex.Message,
					ex,
					additionalData: new { sessionId = id },
					userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
					userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
					requestUrl: HttpContext.Request.Path,
					httpMethod: HttpContext.Request.Method);
				return StatusCode(500, "Failed to delete session");
			}
		}
	}
}
