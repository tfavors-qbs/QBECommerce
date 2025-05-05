using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models;
using QBExternalWebLibrary.Data;
using QBExternalWebLibrary.Models.Mapping;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class ClientsController : Controller
    {
        private readonly IModelService<Client, ClientEditViewModel> _service;

        public ClientsController(IModelService<Client, ClientEditViewModel> service)
        {
            _service = service;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            return View(_service.GetAll());
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = _service.GetById((int)id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,LegacyId,Name")] ClientEditViewModel clientEditViewModel)
        {
            if (ModelState.IsValid)
            {
                _service.Create(null,clientEditViewModel);
                return RedirectToAction(nameof(Index));
            }
            return View(clientEditViewModel);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = _service.GetById((int)id);

            if (client == null)
            {
                return NotFound();
            }

            var clientEditViewModel = _service.GetView(client);

            return View(clientEditViewModel);
        }

        // POST: Clients/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,LegacyId,Name")] ClientEditViewModel clientEditViewModel)
        {
            bool exists = _service.Exists(e => e.Id == clientEditViewModel.Id);
            if (!exists)
            {
                return NotFound();
            }

            Client client = null;

            if (ModelState.IsValid)
            {
                try
                {
                    client = _service.Update(null,clientEditViewModel);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(clientEditViewModel);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var client = _service.GetById((int)id);
            
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = _service.GetById(id);
            if (client != null)
            {
                _service.Delete(client);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _service.Exists(e => e.Id == id);
        }
    }
}
