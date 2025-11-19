using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Ariba;
using QBExternalWebLibrary.Models.Ariba;

namespace QBExternalWebLibrary.Models.Catalog
{
	public class ShoppingCart
	{
		public int Id { get; set; }
		[ForeignKey("ApplicationUser")]
		public string ApplicationUserId { get; set; }
		public ApplicationUser ApplicationUser { get; set; }
		public List<ShoppingCartItem>? ShoppingCartItems { get; set; }

		public static string GeneratePunchOutOrderMessage(IEnumerable<ShoppingCartItem> shoppingCartItems, PunchOutSession session)
		{
			XmlDocument doc = new XmlDocument();

			XmlNode[] MakeIdentityNodes(string value) =>
				new XmlNode[] { doc.CreateTextNode(value) };

			cXML cXML = new cXML()
			{
				Items =
				[
					new Header()
			{
				From =
				[
					new Credential()
					{
						domain = "NetworkId",
						Identity = new Identity()
						{
							Any = MakeIdentityNodes("AN01000625964")
						}
					}
				],
				To =
				[
					new Credential()
					{
						domain = "DUNS",
						Identity = new Identity()
						{
							Any = MakeIdentityNodes(session.FromId)
						}
					}
				],
				Sender = new()
				{
					Credential =
					[
						new Credential()
						{
							domain = "shop.qualitybolt.com",
							Identity = new Identity()
							{
								Any = MakeIdentityNodes("shop.qualitybolt.com")
							}
						}
					],
					UserAgent = "Quality Bolt & Screw Punch Out Application"
				}
			},
			new Message()
			{
				Item = new PunchOutOrderMessage()
				{
					BuyerCookie = new BuyerCookie()
					{
						Any = MakeIdentityNodes(session.BuyerCookie)
					},
					PunchOutOrderMessageHeader = new PunchOutOrderMessageHeader()
					{
						operationAllowed = PunchOutOrderMessageHeaderOperationAllowed.edit,
						Total = new Total()
						{
							Money = new Money()
							{
								currency = "USD",
								Value = shoppingCartItems.Sum(a => a.Quantity * a.ContractItem.Price).ToString("F2")
							}
						}
					},
					ItemIn =
					shoppingCartItems.Select(
						(a, index) =>
						new ItemIn()
						{
							quantity = a.Quantity.ToString(),
							lineNumber = (index + 1).ToString(),
							ItemID = new ItemID()
							{
								SupplierPartID = a.ContractItem.SKU.Name,
								SupplierPartAuxiliaryID = new SupplierPartAuxiliaryID()
								{
									Any = MakeIdentityNodes(a.ContractItem.Id.ToString())
								}
							},
							ItemDetail = new ItemDetail()
							{
								UnitPrice = new UnitPrice()
								{
									Money = new Money()
									{
										currency = "USD",
										Value = a.ContractItem.Price.ToString("F2")
									}
								},
								Description =
								[
									new Description()
									{
										lang = "en",
										Items = [a.ContractItem.Description]
									}
								],
								UnitOfMeasure = "EA",
								Classification =
								[
									new Classification()
									{
										domain = "UNSPSC",
										Value = "31160000"
									}
								]
							}
						}
					).ToArray()
				}
			}
				]
			};

			var serializer = new XmlSerializer(typeof(cXML));
			using (var buffer = new MemoryStream())
			{
				serializer.Serialize(buffer, cXML);
				buffer.Position = 0;
				using (var reader = new StreamReader(buffer))
				{
					string xmlString = reader.ReadToEnd();
					
					// Fix Description structure: Remove <ShortName> wrapper and keep just the text
					// Ariba expects: <Description xml:lang="en">Text</Description>
					// Not: <Description xml:lang="en"><ShortName>Text</ShortName></Description>
					xmlString = System.Text.RegularExpressions.Regex.Replace(
						xmlString,
						@"<Description xml:lang=""([^""]+)"">\s*<ShortName>([^<]+)</ShortName>\s*</Description>",
						@"<Description xml:lang=""$1"">$2</Description>"
					);
					
					// Decode HTML entities in the XML (&amp; should stay as & in content, not in URLs)
					// This is important because XmlSerializer encodes & as &amp; everywhere
					// but we need actual & in the text content
					
					return xmlString;
				}
			}
		}

	}

	public class ShoppingCartEVM
	{
		public int Id { get; set; }
		public string ApplicationUserId { get; set; } = "";
	}
}
