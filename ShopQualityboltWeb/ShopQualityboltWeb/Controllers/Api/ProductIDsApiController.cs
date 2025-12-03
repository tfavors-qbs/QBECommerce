using Microsoft.AspNetCore.Mvc;
using QBExternalWebLibrary.Models;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;
using Microsoft.AspNetCore.Authorization;
using QBExternalWebLibrary.Data;

namespace ShopQualityboltWeb.Controllers.Api
{
    [Route("api/productids")]
    [ApiController]
    public class ProductIDsApiController : ControllerBase {
        private readonly IModelService<ProductID, ProductIDEditViewModel> _service;
        private readonly DataContext _context;

        public ProductIDsApiController(IModelService<ProductID, ProductIDEditViewModel> service, DataContext context) {
            _service = service;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductID>>> GetProductIDs() {
            return _service.GetAll().ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductID>> GetProductID(int id) {
            var productID = _service.GetById(id);

            if (productID == null) {
                return NotFound();
            }

            return productID;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProductID(int id, ProductIDEditViewModel productIDEVM) {
            if (id != productIDEVM.Id) {
                return BadRequest();
            }
            try {
                _service.Update(null, productIDEVM);
            } catch (DbUpdateConcurrencyException) {
                if (!ProductIDExists(id)) {
                    return NotFound();
                } else {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductID>> PostProductID(ProductIDEditViewModel productIDEVM) {
            if (_service.GetAll().Any(p => p.LegacyName == productIDEVM.LegacyName)) {
                return Conflict("ProductID with that name already exists.");
            }
            var productID = _service.Create(null, productIDEVM);

            return CreatedAtAction("GetProductID", new { id = productID.Id }, productID);
        }

        [HttpPost("range")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> PostProductIDs([FromBody] List<ProductIDEditViewModel> productIDEVMs) {
            productIDEVMs = productIDEVMs.Where(p => !_service.GetAll().Any(p2 => p2.LegacyName == p.LegacyName)).ToList();
            _service.CreateRange(null, productIDEVMs);
            return Ok();
        }

        /// <summary>
        /// Bulk import ProductIDs with all required properties specified by name
        /// </summary>
        /// <param name="request">The bulk import request containing ProductIDs to create</param>
        /// <returns>Detailed response with success/failure information for each ProductID</returns>
        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProductIDImportResponse>> ImportProductIDs([FromBody] ProductIDImportRequest request)
        {
            var response = new ProductIDImportResponse
            {
                StartTime = DateTime.UtcNow,
                TotalRequested = request.ProductIDs.Count
            };

            if (request.ProductIDs == null || request.ProductIDs.Count == 0)
            {
                response.Success = false;
                response.Message = "No ProductIDs provided in request";
                response.EndTime = DateTime.UtcNow;
                return BadRequest(response);
            }

            // Pre-load all lookup data to avoid multiple database queries
            var classes = await _context.Classes.ToListAsync();
            var groups = await _context.Groups.Include(g => g.Class).ToListAsync();
            var shapes = await _context.Shapes.ToListAsync();
            var materials = await _context.Materials.ToListAsync();
            var coatings = await _context.Coatings.ToListAsync();
            var threads = await _context.Threads.ToListAsync();
            var specs = await _context.Specs.ToListAsync();
            var existingProductIDs = _service.GetAll().ToList();

            foreach (var productIDDto in request.ProductIDs)
            {
                var result = new ProductIDImportResult
                {
                    LegacyName = productIDDto.LegacyName,
                    LegacyId = productIDDto.LegacyId
                };

                try
                {
                    // Check if already exists
                    if (existingProductIDs.Any(p => p.LegacyName == productIDDto.LegacyName))
                    {
                        result.Success = false;
                        result.Status = "Skipped";
                        result.Reason = $"ProductID with LegacyName '{productIDDto.LegacyName}' already exists";
                        response.Skipped++;
                        response.Results.Add(result);
                        continue;
                    }

                    // Get or Create Class
                    var classEntity = classes.FirstOrDefault(c => c.Name.Equals(productIDDto.Group.Class.Name, StringComparison.OrdinalIgnoreCase));
                    if (classEntity == null)
                    {
                        classEntity = new Class
                        {
                            LegacyId = productIDDto.Group.Class.LegacyId,
                            Name = productIDDto.Group.Class.Name,
                            DisplayName = productIDDto.Group.Class.DisplayName,
                            Description = productIDDto.Group.Class.Description
                        };
                        _context.Classes.Add(classEntity);
                        await _context.SaveChangesAsync();
                        classes.Add(classEntity);
                    }

                    // Get or Create Group
                    var group = groups.FirstOrDefault(g => g.Name.Equals(productIDDto.Group.Name, StringComparison.OrdinalIgnoreCase));
                    if (group == null)
                    {
                        group = new Group
                        {
                            LegacyId = productIDDto.Group.LegacyId,
                            Name = productIDDto.Group.Name,
                            DisplayName = productIDDto.Group.DisplayName,
                            Description = productIDDto.Group.Description,
                            ClassId = classEntity.Id
                        };
                        _context.Groups.Add(group);
                        await _context.SaveChangesAsync();
                        groups.Add(group);
                    }

                    // Get or Create Shape
                    var shape = shapes.FirstOrDefault(s => s.Name.Equals(productIDDto.Shape.Name, StringComparison.OrdinalIgnoreCase));
                    if (shape == null)
                    {
                        shape = new Shape
                        {
                            Name = productIDDto.Shape.Name,
                            DisplayName = productIDDto.Shape.DisplayName,
                            Description = productIDDto.Shape.Description
                        };
                        _context.Shapes.Add(shape);
                        await _context.SaveChangesAsync();
                        shapes.Add(shape);
                    }

                    // Get or Create Material
                    var material = materials.FirstOrDefault(m => m.Name.Equals(productIDDto.Material.Name, StringComparison.OrdinalIgnoreCase));
                    if (material == null)
                    {
                        material = new Material
                        {
                            Name = productIDDto.Material.Name,
                            DisplayName = productIDDto.Material.DisplayName,
                            Description = productIDDto.Material.Description
                        };
                        _context.Materials.Add(material);
                        await _context.SaveChangesAsync();
                        materials.Add(material);
                    }

                    // Get or Create Coating
                    var coating = coatings.FirstOrDefault(c => c.Name.Equals(productIDDto.Coating.Name, StringComparison.OrdinalIgnoreCase));
                    if (coating == null)
                    {
                        coating = new Coating
                        {
                            Name = productIDDto.Coating.Name,
                            DisplayName = productIDDto.Coating.DisplayName,
                            Description = productIDDto.Coating.Description
                        };
                        _context.Coatings.Add(coating);
                        await _context.SaveChangesAsync();
                        coatings.Add(coating);
                    }

                    // Get or Create Thread
                    var thread = threads.FirstOrDefault(t => t.Name.Equals(productIDDto.Thread.Name, StringComparison.OrdinalIgnoreCase));
                    if (thread == null)
                    {
                        thread = new QBExternalWebLibrary.Models.Products.Thread
                        {
                            Name = productIDDto.Thread.Name,
                            DisplayName = productIDDto.Thread.DisplayName,
                            Description = productIDDto.Thread.Description
                        };
                        _context.Threads.Add(thread);
                        await _context.SaveChangesAsync();
                        threads.Add(thread);
                    }

                    // Get or Create Spec
                    var spec = specs.FirstOrDefault(s => s.Name.Equals(productIDDto.Spec.Name, StringComparison.OrdinalIgnoreCase));
                    if (spec == null)
                    {
                        spec = new Spec
                        {
                            Name = productIDDto.Spec.Name,
                            DisplayName = productIDDto.Spec.DisplayName,
                            Description = productIDDto.Spec.Description
                        };
                        _context.Specs.Add(spec);
                        await _context.SaveChangesAsync();
                        specs.Add(spec);
                    }

                    // Create the ProductIDEditViewModel
                    var viewModel = new ProductIDEditViewModel
                    {
                        LegacyId = productIDDto.LegacyId,
                        LegacyName = productIDDto.LegacyName,
                        Description = productIDDto.Description,
                        GroupId = group.Id,
                        ShapeId = shape.Id,
                        MaterialId = material.Id,
                        CoatingId = coating.Id,
                        ThreadId = thread.Id,
                        SpecId = spec.Id
                    };

                    // Create the ProductID
                    var createdProductID = _service.Create(null, viewModel);
                    existingProductIDs.Add(createdProductID); // Add to local cache to prevent duplicates in same batch

                    result.Success = true;
                    result.Status = "Created";
                    result.CreatedProductIDId = createdProductID.Id;
                    response.SuccessfullyCreated++;
                    response.Results.Add(result);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Status = "Failed";
                    result.Reason = $"Exception: {ex.Message}";
                    response.Failed++;
                    response.Results.Add(result);
                }
            }

            response.EndTime = DateTime.UtcNow;
            response.Success = response.Failed == 0;
            response.Message = $"Import completed: {response.SuccessfullyCreated} created, {response.Skipped} skipped, {response.Failed} failed";

            return Ok(response);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductID(int id) {
            var productID = _service.GetById(id);
            if (productID == null) {
                return NotFound();
            }

            _service.Delete(productID);

            return NoContent();
        }

        private bool ProductIDExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
