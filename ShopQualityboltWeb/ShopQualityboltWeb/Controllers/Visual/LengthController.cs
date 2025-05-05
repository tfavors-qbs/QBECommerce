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
    public class LengthController : Controller
    {
        private readonly IModelService<Length, Length> _lengthService;

        public LengthController(IModelService<Length, Length> lengthService)
        {
            _lengthService = lengthService;
        }

        // GET: Length
        public async Task<IActionResult> Index()
        {
            return View(_lengthService.GetAll());
        }

        // GET: Length/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var length = _lengthService.GetById((int)id);
            if (length == null)
            {
                return NotFound();
            }

            return View(length);
        }

        // GET: Length/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Length/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,DisplayName,Value")] Length length)
        {
            if (ModelState.IsValid)
            {
                _lengthService.Create(length);
                return RedirectToAction(nameof(Index));
            }
            return View(length);
        }

        // GET: Length/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var length = _lengthService?.GetById((int)id);
            if (length == null)
            {
                return NotFound();
            }
            return View(length);
        }

        // POST: Length/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,DisplayName,Value")] Length length)
        {
            if (id != length.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _lengthService.Update(length);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LengthExists(length.Id))
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
            return View(length);
        }

        // GET: Length/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var length = _lengthService.GetById((int)id);
            if (length == null)
            {
                return NotFound();
            }

            return View(length);
        }

        // POST: Length/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var length = _lengthService?.GetById((int)id);
            if (length != null)
            {
                _lengthService.Delete(length);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LengthExists(int id)
        {
            return _lengthService.Exists(e => e.Id == id);
        }
    }
}
