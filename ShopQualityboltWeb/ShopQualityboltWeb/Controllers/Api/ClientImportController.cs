using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Models.Products;
using System.ComponentModel.DataAnnotations;
using ThreadModel = QBExternalWebLibrary.Models.Products.Thread;
using ShopQualityboltWeb.Services;
using System.Security.Claims;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/clientimport")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ClientImportController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ILogger<ClientImportController> _logger;
        private readonly IErrorLogService _errorLogService;

        public ClientImportController(DataContext context, ILogger<ClientImportController> logger, IErrorLogService errorLogService)
        {
            _context = context;
            _logger = logger;
            _errorLogService = errorLogService;
        }

        [HttpPost]
        public async Task<ActionResult<ClientImportResponse>> ImportClientWithContractItems([FromBody] ClientImportRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = new ClientImportResponse
                {
                    ClientName = request.Client.Name,
                    StartTime = DateTime.UtcNow
                };

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Step 1: Create or update Client
                    var client = await GetOrCreateClient(request.Client);
                    response.ClientId = client.Id;
                    response.IsNewClient = client.Id == 0 || !await _context.Clients.AnyAsync(c => c.Id == client.Id);

                    if (response.IsNewClient)
                    {
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Created new client: {ClientName} with ID: {ClientId}", client.Name, client.Id);
                    }

                    // Step 2: Process each contract item
                    foreach (var contractItemDto in request.ContractItems)
                    {
                        try
                        {
                            // Check if contract item already exists
                            var existingItem = await _context.ContractItems
                                .FirstOrDefaultAsync(ci => ci.CustomerStkNo == contractItemDto.CustomerStkNo && ci.ClientId == client.Id);

                            if (existingItem != null)
                            {
                                response.SkippedItems.Add(new ImportError
                                {
                                    CustomerStkNo = contractItemDto.CustomerStkNo,
                                    Reason = "Contract item already exists"
                                });
                                continue;
                            }

                            // Process dependencies and create contract item
                            var contractItem = await CreateContractItem(contractItemDto, client.Id);

                            if (contractItem != null)
                            {
                                _context.ContractItems.Add(contractItem);
                                response.ImportedItemsCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process contract item: {CustomerStkNo}", contractItemDto.CustomerStkNo);
                            response.FailedItems.Add(new ImportError
                            {
                                CustomerStkNo = contractItemDto.CustomerStkNo,
                                Reason = ex.Message
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    response.Success = true;
                    response.EndTime = DateTime.UtcNow;
                    response.Message = $"Successfully imported {response.ImportedItemsCount} contract items. Skipped: {response.SkippedItems.Count}, Failed: {response.FailedItems.Count}";

                    _logger.LogInformation("Client import completed. Imported: {Imported}, Skipped: {Skipped}, Failed: {Failed}", 
                        response.ImportedItemsCount, response.SkippedItems.Count, response.FailedItems.Count);
                    
                    return Ok(response);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Transaction rolled back during client import");
                    
                    await _errorLogService.LogErrorAsync(
                        "Client Import Error",
                        "Failed to Import Client and Contract Items",
                        ex.Message,
                        ex,
                        additionalData: new { 
                            clientName = request.Client?.Name,
                            clientLegacyId = request.Client?.LegacyId,
                            contractItemCount = request.ContractItems?.Count 
                        },
                        userId: userId,
                        userEmail: userEmail,
                        requestUrl: HttpContext.Request.Path,
                        httpMethod: HttpContext.Request.Method);
                    
                    response.Success = false;
                    response.EndTime = DateTime.UtcNow;
                    response.Message = $"Import failed: {ex.Message}";
                    
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in client import");
                
                await _errorLogService.LogErrorAsync(
                    "Client Import Error",
                    "Unexpected Error in Client Import",
                    ex.Message,
                    ex,
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    userEmail: User.FindFirst(ClaimTypes.Email)?.Value,
                    requestUrl: HttpContext.Request.Path,
                    httpMethod: HttpContext.Request.Method);
                
                return StatusCode(500, new ClientImportResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}",
                    EndTime = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Test endpoint to verify authentication and authorization
        /// </summary>
        [HttpGet("auth-test")]
        public ActionResult<object> TestAuthentication()
        {
            try
            {
                var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                var userName = User.Identity?.Name;
                var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

                var response = new
                {
                    isAuthenticated,
                    userId,
                    userEmail,
                    userName,
                    roles = userRoles,
                    hasAdminRole = User.IsInRole("Admin"),
                    allClaims,
                    authorizationHeader = Request.Headers["Authorization"].ToString(),
                    cookieHeader = Request.Headers["Cookie"].ToString()
                };

                _logger.LogInformation("Auth test: Authenticated={IsAuth}, Email={Email}, Roles={Roles}", 
                    isAuthenticated, userEmail, string.Join(", ", userRoles));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in auth test endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<Client> GetOrCreateClient(ClientDto clientDto)
        {
            // Try to find existing client by LegacyId
            var existingClient = await _context.Clients
                .FirstOrDefaultAsync(c => c.LegacyId == clientDto.LegacyId);

            if (existingClient != null)
            {
                return existingClient;
            }

            // Create new client
            return new Client
            {
                LegacyId = clientDto.LegacyId,
                Name = clientDto.Name
            };
        }

        private async Task<ContractItem?> CreateContractItem(ContractItemDto dto, int clientId)
        {
            // Resolve or create all foreign key dependencies
            int? skuId = null;
            if (dto.SKU != null)
            {
                var sku = await GetOrCreateSKU(dto.SKU);
                if (sku != null)
                {
                    skuId = sku.Id;
                }
            }

            int? diameterId = null;
            if (dto.Diameter != null)
            {
                var diameter = await GetOrCreateDimension<Diameter>("Diameters", dto.Diameter);
                diameterId = diameter?.Id;
            }

            int? lengthId = null;
            if (dto.Length != null)
            {
                var length = await GetOrCreateDimension<Length>("Lengths", dto.Length);
                lengthId = length?.Id;
            }

            return new ContractItem
            {
                CustomerStkNo = dto.CustomerStkNo,
                Description = dto.Description,
                Price = dto.Price,
                ClientId = clientId,
                SKUId = skuId,
                DiameterId = diameterId,
                LengthId = lengthId,
                NonStock = dto.NonStock
            };
        }

        private async Task<SKU?> GetOrCreateSKU(SKUDto skuDto)
        {
            // Try to find existing SKU by name
            var existingSKU = await _context.SKUs
                .Include(s => s.ProductId)
                .FirstOrDefaultAsync(s => s.Name == skuDto.Name);

            if (existingSKU != null)
            {
                return existingSKU;
            }

            // Get ProductID (optional for non-stock items like #XN)
            int? productIdId = null;
            if (skuDto.ProductID != null)
            {
                var productId = await GetOrCreateProductID(skuDto.ProductID);
                if (productId != null)
                {
                    productIdId = productId.Id;
                }
            }

            // Get Diameter (optional for non-stock items like #XN)
            int? diameterId = null;
            if (skuDto.Diameter != null)
            {
                var diameter = await GetOrCreateDimension<Diameter>("Diameters", skuDto.Diameter);
                if (diameter != null)
                {
                    diameterId = diameter.Id;
                }
            }

            // Get Length (optional)
            int? lengthId = null;
            if (skuDto.Length != null)
            {
                var length = await GetOrCreateDimension<Length>("Lengths", skuDto.Length);
                lengthId = length?.Id;
            }

            var sku = new SKU
            {
                Name = skuDto.Name,
                ProductIDId = productIdId,
                DiameterId = diameterId,
                LengthId = lengthId
            };

            _context.SKUs.Add(sku);
            await _context.SaveChangesAsync();

            return sku;
        }

        private async Task<ProductID?> GetOrCreateProductID(ProductIDDto productIdDto)
        {
            if (productIdDto == null) return null;

            // Try to find existing ProductID by LegacyId
            var existing = await _context.ProductIDs
                .FirstOrDefaultAsync(p => p.LegacyId == productIdDto.LegacyId);

            if (existing != null)
            {
                return existing;
            }

            // Create all required dependencies
            var group = await GetOrCreateGroup(productIdDto.Group);
            var shape = await GetOrCreateDimension<Shape>("Shapes", productIdDto.Shape);
            var material = await GetOrCreateDimension<Material>("Materials", productIdDto.Material);
            var coating = await GetOrCreateDimension<Coating>("Coatings", productIdDto.Coating);
            var thread = await GetOrCreateDimension<ThreadModel>("Threads", productIdDto.Thread);
            var spec = await GetOrCreateDimension<Spec>("Specs", productIdDto.Spec);

            if (group == null || shape == null || material == null || coating == null || thread == null || spec == null)
            {
                throw new InvalidOperationException("Failed to create required dependencies for ProductID");
            }

            var productId = new ProductID
            {
                GroupId = group.Id,
                ShapeId = shape.Id,
                MaterialId = material.Id,
                CoatingId = coating.Id,
                ThreadId = thread.Id,
                SpecId = spec.Id,
                LegacyId = productIdDto.LegacyId,
                LegacyName = productIdDto.LegacyName,
                Description = productIdDto.Description
            };

            _context.ProductIDs.Add(productId);
            await _context.SaveChangesAsync();

            return productId;
        }

        private async Task<Group?> GetOrCreateGroup(GroupDto groupDto)
        {
            if (groupDto == null) return null;

            // Try to find existing Group by LegacyId
            var existing = await _context.Groups
                .FirstOrDefaultAsync(g => g.LegacyId == groupDto.LegacyId);

            if (existing != null)
            {
                return existing;
            }

            // Need to create Class first
            var classEntity = await GetOrCreateClass(groupDto.Class);
            if (classEntity == null)
            {
                throw new InvalidOperationException($"Could not create or find Class: {groupDto.Class?.Name}");
            }

            var group = new Group
            {
                LegacyId = groupDto.LegacyId,
                Name = groupDto.Name,
                DisplayName = groupDto.DisplayName,
                Description = groupDto.Description,
                ClassId = classEntity.Id
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return group;
        }

        private async Task<Class?> GetOrCreateClass(ClassDto classDto)
        {
            if (classDto == null) return null;

            // Try to find existing Class by LegacyId
            var existing = await _context.Classes
                .FirstOrDefaultAsync(c => c.LegacyId == classDto.LegacyId);

            if (existing != null)
            {
                return existing;
            }

            var classEntity = new Class
            {
                LegacyId = classDto.LegacyId,
                Name = classDto.Name,
                DisplayName = classDto.DisplayName,
                Description = classDto.Description
            };

            _context.Classes.Add(classEntity);
            await _context.SaveChangesAsync();

            return classEntity;
        }

        private async Task<T?> GetOrCreateDimension<T>(string tableName, DimensionDto? dimensionDto) where T : class, new()
        {
            if (dimensionDto == null) return null;

            var dbSet = _context.Set<T>();
            
            // Try to find by Name property
            var nameProperty = typeof(T).GetProperty("Name");
            if (nameProperty != null)
            {
                var existing = await dbSet.FirstOrDefaultAsync(e => 
                    EF.Property<string>(e, "Name") == dimensionDto.Name);

                if (existing != null)
                {
                    return existing;
                }
            }

            // Create new dimension
            var dimension = new T();
            
            // Set properties
            typeof(T).GetProperty("Name")?.SetValue(dimension, dimensionDto.Name);
            typeof(T).GetProperty("DisplayName")?.SetValue(dimension, dimensionDto.DisplayName);
            
            // Set Value property if it exists (for Length/Diameter)
            typeof(T).GetProperty("Value")?.SetValue(dimension, dimensionDto.Value);
            
            // Set Description property if it exists
            typeof(T).GetProperty("Description")?.SetValue(dimension, dimensionDto.Description);

            dbSet.Add(dimension);
            await _context.SaveChangesAsync();

            return dimension;
        }
    }
}
