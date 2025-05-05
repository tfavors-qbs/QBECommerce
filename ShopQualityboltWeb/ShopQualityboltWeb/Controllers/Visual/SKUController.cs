using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class SKUController : Controller {
        private readonly IModelService<SKU, SKUEditViewModel> _skuService;
        private readonly IModelService<ProductID, ProductIDEditViewModel> _productIDService;
        private readonly IModelService<Length, Length> _lengthService;
        private readonly IModelService<Diameter, Diameter> _diameterService;

        public SKUController(IModelService<SKU, SKUEditViewModel> skuService, IModelService<ProductID, ProductIDEditViewModel> productIDService,
            IModelService<Length, Length> lengthService, IModelService<Diameter, Diameter> diameterService) {
            _skuService = skuService;
            _productIDService = productIDService;
            _lengthService = lengthService;
            _diameterService = diameterService;
        }

        // GET: SKU
        public async Task<IActionResult> Index() {
            var data = _skuService.GetAll();

            return View(data.ToList());
        }

        // GET: SKU/Details/5
        public async Task<IActionResult> Details(int? id) {
            if (id == null) {
                return NotFound();
            }

            var sku = _skuService.GetById((int)id);

            if (sku == null) {
                return NotFound();
            }

            return View(sku);
        }

        // GET: SKU/Create
        public IActionResult Create() {
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["ProductIDId"] = new SelectList(_productIDService.GetAll().OrderBy(x => x.LegacyName), "Id", "LegacyName");
            return View();
        }

        // POST: SKU/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LengthId,Name,DiameterId,ProductIDId")] SKUEditViewModel sKUEditViewModel) {

            if (ModelState.IsValid) {
                _skuService.Create(null, sKUEditViewModel);
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["ProductIDId"] = new SelectList(_productIDService.GetAll().OrderBy(x => x.LegacyName), "Id", "LegacyName");
            return View(sKUEditViewModel);
        }

        // GET: SKU/Edit/5
        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }

            var sku = _skuService.GetById((int)id);
            var skuEditViewModel = _skuService.GetView(sku);

            if (sku == null) {
                return NotFound();
            }
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["ProductIDId"] = new SelectList(_productIDService.GetAll().OrderBy(x => x.LegacyName), "Id", "LegacyName");
            return View(skuEditViewModel);
        }

        // POST: SKU/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LengthId,Name,DiameterId,ProductIDId")] SKUEditViewModel sKUEditViewModel) {
            var sku = _skuService.GetById(id);
            if (sku == null) {
                return NotFound();
            }

            if (ModelState.IsValid) {
                try {
                    _skuService.Update(null, sKUEditViewModel);
                } catch (DbUpdateConcurrencyException) {
                    if (!SKUExists(sKUEditViewModel.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["ProductIDId"] = new SelectList(_productIDService.GetAll().OrderBy(x => x.LegacyName), "Id", "LegacyName");
            return View(sKUEditViewModel);
        }

        // GET: SKU/Delete/5
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }

            var sku = _skuService.GetById((int)id);

            if (sku == null) {
                return NotFound();
            }

            return View(sku);
        }

        // POST: SKU/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {

            var sku = _skuService.GetById((int)id);
            if (sku != null) {
                _skuService.Delete(sku);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SKUExists(int id) {
            return _skuService.Exists(e => e.Id == id);
        }
    }
}
