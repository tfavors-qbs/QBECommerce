using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class ContractItemController : Controller {
        private readonly IModelService<ContractItem, ContractItemEditViewModel> _service;
        private readonly IModelService<Client, ClientEditViewModel> _clientService;
        private readonly IModelService<SKU, SKUEditViewModel> _skuService;
        private readonly IModelService<Length, Length> _lengthService;
        private readonly IModelService<Diameter, Diameter> _diameterService;

        public ContractItemController(IModelService<ContractItem, ContractItemEditViewModel> service, IModelService<Client, ClientEditViewModel> clientService,
            IModelService<SKU, SKUEditViewModel> skuService, IModelService<Length, Length> lengthService, IModelService<Diameter, Diameter> diameterService) {
            _service = service;
            _clientService = clientService;
            _skuService = skuService;
            _lengthService = lengthService;
            _diameterService = diameterService;
        }

        // GET: ContractItems
        public async Task<IActionResult> Index() {
            var contractItems = _service.GetAll();
            return View(contractItems.ToList());
        }

        // GET: ContractItems/Details/5
        public async Task<IActionResult> Details(int? id) {
            if (id == null) {
                return NotFound();
            }

            var contractItem = _service.GetById((int)id);

            if (contractItem == null) {
                return NotFound();
            }

            return View(contractItem);
        }

        // GET: ContractItems/Create
        public IActionResult Create() {
            ViewData["ClientId"] = new SelectList(_clientService.GetAll(), "Id", "Name"); ;
            ViewData["SKUId"] = new SelectList(_skuService.GetAll(), "Id", "Name");
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            return View();
        }

        // POST: ContractItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerStkNo,Description,Price,ClientId,SKUId,DiameterId,LengthId,NonStock")] ContractItemEditViewModel contractItemEVM) {
            if (ModelState.IsValid) {
                _service.Create(null, contractItemEVM);
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_clientService.GetAll(), "Id", "Name"); ;
            ViewData["SKUId"] = new SelectList(_skuService.GetAll(), "Id", "Name");
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            return View(contractItemEVM);
        }

        // GET: ContractItems/Edit/5
        public async Task<IActionResult> Edit(int? id) {
            if (id == null) {
                return NotFound();
            }

            var contractItem = _service.GetById((int)id);
            var contractItemEVM = _service.GetView(contractItem);

            if (contractItem == null) {
                return NotFound();
            }

            ViewData["ClientId"] = new SelectList(_clientService.GetAll(), "Id", "Name"); ;
            ViewData["SKUId"] = new SelectList(_skuService.GetAll(), "Id", "Name");
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");

            return View(contractItemEVM);
        }

        // POST: ContractItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerStkNo,Description,Price,ClientId,SKUId,DiameterId,LengthId,NonStock")] ContractItemEditViewModel contractItemEVM) {
            var contractItem = _service.GetById(id);
            if (contractItem == null) {
                return NotFound();
            }

            if (ModelState.IsValid) {
                try {
                    _service.Update(null, contractItemEVM);
                } catch (DbUpdateConcurrencyException) {
                    if (!ContractItemExists(contractItem.Id)) {
                        return NotFound();
                    } else {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_clientService.GetAll(), "Id", "Name"); ;
            ViewData["SKUId"] = new SelectList(_skuService.GetAll(), "Id", "Name");
            ViewData["DiameterId"] = new SelectList(_diameterService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            ViewData["LengthId"] = new SelectList(_lengthService.GetAll().OrderBy(x => x.Value), "Id", "DisplayName");
            return View(contractItem);
        }

        // GET: ContractItems/Delete/5
        public async Task<IActionResult> Delete(int? id) {
            if (id == null) {
                return NotFound();
            }

            var contractItem = _service.GetById((id));

            if (contractItem == null) {
                return NotFound();
            }

            return View(contractItem);
        }

        // POST: ContractItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            var contractItem = _service.GetById(id);
            if (contractItem != null) {
                _service.Delete(contractItem);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ContractItemExists(int id) {
            return _service.Exists(e => e.Id == id);
        }
    }
}
