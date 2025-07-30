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

namespace ShopQualityboltWeb.Controllers.Api {
    [Route("api/sessions")]
    [ApiController]
    public class SessionsController : ControllerBase {

		private readonly IConfiguration _configuration; // Added for configuration

		public SessionsController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		[HttpPost("punch-out")]
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

				// Validate credentials (e.g., SharedSecret)
				if (header == null || header?.Sender?.Credential?.FirstOrDefault(c => c.domain == "NetworkId")?.Item is not SharedSecret sharedSecret)
				{
					return BadRequest(CreateErrorResponse("400", "Invalid header or shared secret credential"));
				}

				// Extract SharedSecret value from Any property
				string sharedSecretValue = sharedSecret.Any?.FirstOrDefault()?.Value;
				if (string.IsNullOrEmpty(sharedSecretValue) || sharedSecretValue != "abracadabra") //TODO: store secret somewhere good
				{
					return Unauthorized(CreateErrorResponse("401", "Invalid credentials"));
				}

				// Store BuyerCookie and BrowserFormPost URL
				var buyerCookie = punchOutSetupRequest.BuyerCookie;
				var postUrl = punchOutSetupRequest.BrowserFormPost?.URL;
				HttpContext.Session.SetString("BuyerCookie", buyerCookie.Any?.FirstOrDefault()?.Value ?? "");
				HttpContext.Session.SetString("PostUrl", postUrl?.Value ?? "");

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
									URL = new(){ Value = $"https://yourdomain.com/catalog?sessionId={HttpContext.Session.Id}" }
								}
							}
						}
					}
				};

				// Handle operation
				switch (punchOutSetupRequest.operation)
				{
					case PunchOutSetupRequestOperation.create:
						// Already set response for create
						break;
					case PunchOutSetupRequestOperation.edit:
					case PunchOutSetupRequestOperation.inspect:
						response.Items[0] = new Response
						{
							Status = new Status { code = "200", text = "success" },
							Item = new PunchOutSetupResponse
							{
								StartPage = new StartPage
								{
									URL = new() { Value = $"https://yourdomain.com/catalog?sessionId={HttpContext.Session.Id}&operation={punchOutSetupRequest.operation}" }
								}
							}
						};
						break;
					default:
						return BadRequest(CreateErrorResponse("400", "Unsupported operation"));
				}

				// Serialize response to cXML
				var responseSerializer = new XmlSerializer(typeof(cXML));
				using (var writer = new StringWriter())
				{
					responseSerializer.Serialize(writer, response);
					return Content(writer.ToString(), "text/xml");
				}
			}
			catch (Exception ex)
			{
				// Log exception (use your logging framework)
				return StatusCode(500, CreateErrorResponse("500", $"Internal server error: {ex.Message}"));
			}
		}

		private string CreateErrorResponse(string code, string message)
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
}
