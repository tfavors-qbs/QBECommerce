using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QBExternalWebLibrary.Models.Products;
using QBExternalWebLibrary.Services.Model;

namespace ShopQualityboltWeb.Controllers.Visual
{
    public class DiameterController : Controller
    {
        private readonly IModelService<Diameter, Diameter> _diameterService;

        public DiameterController(IModelService<Diameter, Diameter> diameterService)
        {
            _diameterService = diameterService;
        }

        // GET: Diameter
        public async Task<IActionResult> Index()
        {
            return View(_diameterService.GetAll());
        }

        // GET: Diameter/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var diameter = _diameterService.GetById((int)id);
            if (diameter == null)
            {
                return NotFound();
            }

            return View(diameter);
        }

        // GET: Diameter/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Diameter/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,DisplayName,Value")] Diameter diameter)
        {
            if (ModelState.IsValid)
            {
                _diameterService.Create(diameter);
                return RedirectToAction(nameof(Index));
            }
            return View(diameter);
        }

        // GET: Diameter/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var diameter = _diameterService.GetById((int)id);
            if (diameter == null)
            {
                return NotFound();
            }
            return View(diameter);
        }

        // POST: Diameter/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Value")] Diameter diameter)
        {
            if (id != diameter.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _diameterService.Update(diameter);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiameterExists(diameter.Id))
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
            return View(diameter);
        }

        // GET: Diameter/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var diameter = _diameterService.GetById((int)id);
            if (diameter == null)
            {
                return NotFound();
            }

            return View(diameter);
        }

        // POST: Diameter/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var diameter = _diameterService.GetById(id);
            if (diameter != null)
            {
                _diameterService.Delete(diameter);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DiameterExists(int id)
        {
            return _diameterService.Exists(e => e.Id == id);
        }
    }
}
