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
    public class SpecController : Controller
    {
        private readonly IModelService<Spec, Spec> _specService;

        public SpecController(IModelService<Spec, Spec> specService)
        {
            _specService = specService;
        }

        // GET: Spec
        public async Task<IActionResult> Index()
        {
            return View(_specService.GetAll());            
        }

        // GET: Spec/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var spec = _specService.GetById((int)id);
            if (spec == null)
            {
                return NotFound();
            }

            return View(spec);
        }

        // GET: Spec/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Spec/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DisplayName,Description")] Spec spec)
        {
            if (ModelState.IsValid)
            {
                _specService.Create(spec);
                return RedirectToAction(nameof(Index));
            }
            return View(spec);
        }

        // GET: Spec/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var spec = _specService.GetById((int)id);
            if (spec == null)
            {
                return NotFound();
            }
            return View(spec);
        }

        // POST: Spec/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Description")] Spec spec)
        {
            if (id != spec.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _specService.Update(spec);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecExists(spec.Id))
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
            return View(spec);
        }

        // GET: Spec/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var spec = _specService.GetById((int)id);
            if (spec == null)
            {
                return NotFound();
            }

            return View(spec);
        }

        // POST: Spec/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var spec = _specService.GetById(id);
            if (spec != null)
            {
                _specService.Delete(spec);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SpecExists(int id)
        {
            return _specService.Exists(e => e.Id == id);
        }
    }
}
