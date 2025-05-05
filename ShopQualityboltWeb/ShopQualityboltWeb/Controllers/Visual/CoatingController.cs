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
    public class CoatingController : Controller
    {
        private readonly IModelService<Coating, Coating> _coatingService;

        public CoatingController(IModelService<Coating, Coating> coatingService)
        {
            _coatingService = coatingService;
        }

        // GET: Coating
        public async Task<IActionResult> Index()
        {
            return View(_coatingService.GetAll());
        }

        // GET: Coating/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coating = _coatingService?.GetById((int)id);
           
            if (coating == null)
            {
                return NotFound();
            }

            return View(coating);
        }

        // GET: Coating/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Coating/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DisplayName,Description")] Coating coating)
        {
            if (ModelState.IsValid)
            {
                _coatingService.Create(coating);                
                return RedirectToAction(nameof(Index));
            }
            return View(coating);
        }

        // GET: Coating/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coating = _coatingService?.GetById((int)id);
           
            if (coating == null)
            {
                return NotFound();
            }
            return View(coating);
        }

        // POST: Coating/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Description")] Coating coating)
        {
            if (id != coating.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _coatingService.Update(coating);                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CoatingExists(coating.Id))
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
            return View(coating);
        }

        // GET: Coating/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var coating = _coatingService?.GetById((int)id);
           
            if (coating == null)
            {
                return NotFound();
            }

            return View(coating);
        }

        // POST: Coating/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coating = _coatingService.GetById(id);
            
            if (coating != null)
            {
                _coatingService.Delete(coating);
               
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool CoatingExists(int id)
        {
            return _coatingService.Exists(e => e.Id == id);           
        }
    }
}
