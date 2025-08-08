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
                                domain = "DUNS",
                                Identity = new Identity()
                                {
                                    //Any = our DUNS number
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
                                    //Any = their DUNS number
                                }
                            }
                        ],
                        Sender = new()
                        {
                            Credential =
                            [
                                new Credential()
                                {
                                    domain = "qualitybolt.com",
                                    Identity = new Identity()
                                    {

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
                                //Any = session.BuyerCookie
                            },
                            PunchOutOrderMessageHeader = new PunchOutOrderMessageHeader()
                            {
                                operationAllowed = PunchOutOrderMessageHeaderOperationAllowed.edit,
                                Total = new Total()
                                {
                                    Money = new Money()
                                    {
                                        currency = "USD",
                                        Value = shoppingCartItems.Sum(a => a.Quantity * a.ContractItem.Price).ToString()
                                    }
                                }
                            },
                            ItemIn =
							shoppingCartItems.Select(
                                a =>
								new ItemIn()
								{
									quantity = a.Quantity.ToString(),
                                    ItemID = new ItemID()
                                    {
                                        SupplierPartID = a.ContractItem.SKU.Name,
                                        SupplierPartAuxiliaryID = new SupplierPartAuxiliaryID()
                                        {
                                            //Any = a.ContractItem.CustomerStkNo
                                        }
									},
                                    ItemDetail = new ItemDetail()
                                    {
                                        UnitPrice = new UnitPrice()
                                        {
                                            Money = new Money()
                                            {
                                                currency = "USD",
                                                Value = (a.Quantity * a.ContractItem.Price).ToString()
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
                                        UnitOfMeasure = "TODO: unit of measure",
                                        Classification =
                                        [
                                            new Classification()
                                            {
                                                domain = "UNSPSC",
                                                Value = "TODO: classification value"
                                            }
                                        ],
                                        LeadTime = "TODO: lead time? probably dont actually wanna do this"
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
					return xmlString; 
				}
			}
		}
    }

    public class ShoppingCartEVM {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = "";
    }
}
