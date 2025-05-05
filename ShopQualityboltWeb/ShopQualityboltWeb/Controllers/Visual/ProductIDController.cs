using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Data.Repositories;
using Thread = QBExternalWebLibrary.Models.Products.Thread;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class ProductIDController : Controller {

        private readonly IModelService<ProductID, ProductIDEditViewModel> _productIDService;
        private readonly IModelService<Group, GroupEditViewModel> _groupService;
        private readonly IModelService<Shape, Shape> _shapeService;
        private readonly IModelService<Material, Material> _materialService;
        private readonly IModelService<Coating, Coating> _coatingService;
        private readonly IModelService<Thread, Thread> _threadService;
        private readonly IModelService<Spec, Spec> _specService;

        public ProductIDController(IModelService<ProductID, ProductIDEditViewModel> productIDService,
            IModelService<Group, GroupEditViewModel> groupService, IModelService<Shape, Shape> shapeService, IModelService<Material, Material> materialService,
            IModelService<Coating, Coating> coatingService, IModelService<Thread, Thread> threadService, IModelService<Spec, Spec> specService) {
            _productIDService = productIDService;
        }

        // GET: ProductID
        public async Task<IActionResult> Index() {
            var productIDs = _productIDService.GetAll().ToList();            
            return View(productIDs);
        }

        // GET: ProductID/Details/5
        public async Task<IActionResult> Details(int? id) {
            if (id == null) {
                return NotFound();
            }

            var productID = _productIDService.GetById(id);

            if (productID == null) {
                return NotFound();
            }

            return View(productID);
        }

        // GET: ProductID/Create
        public IActionResult Create() {
            ViewData["CoatingId"] = new SelectList(_coatingService.GetAll(), "Id", "DisplayName");
            ViewData["GroupId"] = new SelectList(_groupService.GetAll(), "Id", "Name");
            ViewData["MaterialId"] = new SelectList(_materialService.GetAll(), "Id", "DisplayName");
            ViewData["ShapeId"] = new SelectList(_shapeService.GetAll(), "Id", "DisplayName");
            ViewData["SpecId"] = new SelectList(_specService.GetAll(), "Id", "DisplayName");
            ViewData["ThreadId"] = new SelectList(_threadService.GetAll(), "Id", "DisplayName");
            return View();
        }

        // POST: ProductID/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LegacyName,GroupId,ShapeId,MaterialId,CoatingId,ThreadId,SpecId,LegacyId,Description")] ProductIDEditViewModel productIDEditViewModel) {
            
            if (ModelState.IsValid) {
                _productIDService.Create(null, productIDEditViewModel);
                return RedirectToAction(nameof(Index));
            }
            ViewData["CoatingId"] = new SelectList(_coatingService.GetAll(), "Id", "Id", productIDEditViewModel.CoatingId);
            ViewData["GroupId"] = new SelectList(_groupService.GetAll(), "Id", "Id", productIDEditViewModel.GroupId);
            ViewData["MaterialId"] = new SelectList(_materialService.GetAll(), "Id", "Id", productIDEditViewModel.MaterialId);
            ViewData["ShapeId"] = new SelectList(_shapeService.GetAll(), "Id", "Id", productIDEditViewModel.ShapeId);
            ViewData["SpecId"] = new SelectList(_specService.GetAll(), "Id", "Id", productIDEditViewModel.SpecId);
            ViewData["ThreadId"] = new SelectList(_threadService.GetAll(), "Id", "Id", productIDEditViewModel.ThreadId);
            return View(productIDEditViewModel);
        }

        // GET: ProductID/Edit/5
        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }

            var productID = _productIDService.GetById(id);
            var productIDEditViewModel = _productIDService.GetView(productID);

            if (productID == null) {
                return NotFound();
            }
            ViewData["CoatingId"] = new SelectList(_coatingService.GetAll(), "Id", "DisplayName", productID.CoatingId);
            ViewData["GroupId"] = new SelectList(_groupService.GetAll(), "Id", "Name", productID.GroupId);
            ViewData["MaterialId"] = new SelectList(_materialService.GetAll(), "Id", "DisplayName", productID.MaterialId);
            ViewData["ShapeId"] = new SelectList(_shapeService.GetAll(), "Id", "DisplayName", productID.ShapeId);
            ViewData["SpecId"] = new SelectList(_specService.GetAll(), "Id", "DisplayName", productID.SpecId);
            ViewData["ThreadId"] = new SelectList(_threadService.GetAll(), "Id", "DisplayName", productID.ThreadId);
            return View(productIDEditViewModel);
        }

        // POST: ProductID/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LegacyName,GroupId,ShapeId,MaterialId,CoatingId,ThreadId,SpecId,LegacyId,Description")] ProductIDEditViewModel productIDEditViewModel) {
            var productID = _productIDService.GetById(id);
            if (productID == null) {
                return NotFound();
            }

            if (ModelState.IsValid) {
                try {
                    _productIDService.Update(null, productIDEditViewModel);
                } catch (DbUpdateConcurrencyException) {
                    if (!ProductIDExists(productID.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CoatingId"] = new SelectList(_coatingService.GetAll(), "Id", "DisplayName", productID.CoatingId);
            ViewData["GroupId"] = new SelectList(_groupService.GetAll(), "Id", "Name", productID.GroupId);
            ViewData["MaterialId"] = new SelectList(_materialService.GetAll(), "Id", "DisplayName", productID.MaterialId);
            ViewData["ShapeId"] = new SelectList(_shapeService.GetAll(), "Id", "DisplayName", productID.ShapeId);
            ViewData["SpecId"] = new SelectList(_specService.GetAll(), "Id", "DisplayName", productID.SpecId);
            ViewData["ThreadId"] = new SelectList(_threadService.GetAll(), "Id", "DisplayName", productID.ThreadId);
            return View(productID);
        }

        // GET: ProductID/Delete/5
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }

            var productID = _productIDService.GetById(id);

            if (productID == null) {
                return NotFound();
            }

            return View(productID);
        }

        // POST: ProductID/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var productID = _productIDService.GetById(id);
            if (productID != null) {
                _productIDService.Delete(productID);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductIDExists(int id) {
            return _productIDService.Exists(e => e.Id == id);
        }
    }
}
